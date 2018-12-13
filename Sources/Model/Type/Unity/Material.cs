using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Armine.Model.Type
{
	public partial class Material
	{
		#region Members
		private const string defaultUnityShader = "Standard";

		private static readonly Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();

		private static Dictionary<int, string[]> shadersProperties = null;
		public static float unityLineWidth = 3.0f;

		private UnityEngine.Material unityMaterial = null;
		#endregion

		#region Import
		public static Material FromUnity(Scene scene, UnityEngine.Material unity_material)
		{
			if(shadersProperties == null)
			{
				TextAsset properties = Resources.Load<TextAsset>(Constants.shaderDatabase);

				if(properties != null && properties.bytes.Length > 0)
				{
					IEnumerator it = Module.Import.Binary.serializer.Deserialize<Dictionary<int, string[]>>(properties.bytes, p => shadersProperties = p);

					while(it.MoveNext())
					{ }
				}
			}

			if(shadersProperties != null)
			{
				// Create new material
				Material material = new Material();

				// Save material options
				material.name = unity_material.name;
				material.shader = unity_material.shader.name;
				material.renderQueue = unity_material.renderQueue;
				material.hideFlags = unity_material.hideFlags;
				material.globalIlluminationFlags = unity_material.globalIlluminationFlags;
				material.keywords = unity_material.shaderKeywords;
				material.passes = new bool[unity_material.passCount];

				// Save material enabled passes
				for(int i = 0; i < material.passes.Length; i++)
				{
					material.passes[i] = unity_material.GetShaderPassEnabled(unity_material.GetPassName(i));
				}

				// Save material properties
				// Range properties in Unity are fucked-up. It can be either int or float values, and their is no way to know wich one it is.
				// However, if we restore them using the wrong type, we get no errors but they are (obviously) ignored...
				// Therefore, we will save range as both int and float to be conservative.
				FromUnityMaterialProperties(unity_material, Shaders.PropertyType.RANGE, ref material.ints, (m, p) => m.GetInt(p));
				FromUnityMaterialProperties(unity_material, Shaders.PropertyType.RANGE, ref material.floats, (m, p) => m.GetFloat(p));
				FromUnityMaterialProperties(unity_material, Shaders.PropertyType.FLOAT, ref material.floats, (m, p) => m.GetFloat(p));
				FromUnityMaterialProperties(unity_material, Shaders.PropertyType.VECTOR, ref material.vectors, (m, p) => m.GetVector(p));
				FromUnityMaterialProperties(unity_material, Shaders.PropertyType.COLOR, ref material.colors, (m, p) => m.GetColor(p));
				FromUnityMaterialProperties(unity_material, Shaders.PropertyType.TEXTURE, ref material.textures, (m, p) =>
				{
					UnityEngine.Texture unity_tex = m.GetTexture(p);

					int index = (unity_tex != null && unity_tex is Texture2D ? scene.GetUnityTextureIndex((Texture2D) unity_tex) : -1);

					return (index >= 0 ? new TextureParams((uint) index, m.GetTextureOffset(p), m.GetTextureScale(p)) : null);
				});

				return material;
			}

			return null;
		}

		private static void FromUnityMaterialProperties<T>(UnityEngine.Material unity_material, Shaders.PropertyType type, ref Dictionary<string, T> values, Func<UnityEngine.Material, string, T> getter)
		{
			string[] properties;

			if(shadersProperties.TryGetValue((int) type, out properties))
			{
				if(properties != null && properties.Length > 0)
				{
					foreach(string property in properties)
					{
						if(unity_material.HasProperty(property))
						{
							T value = getter(unity_material, property);

							if(value != null)
							{
								if(values == null)
								{
									values = new Dictionary<string, T>();
								}

								values[property] = value;
							}
						}
					}
				}
			}
		}
		#endregion

		#region Export
		public UnityEngine.Material ToUnity(Scene scene, Utils.Progress progress = null)
		{
			if(unityMaterial == null)
			{
				// Get the material shader
				Shader unity_shader = GetUnityShader(shader);

				if(unity_shader != null)
				{
					// Create a new material with the selected shader
					unityMaterial = new UnityEngine.Material(unity_shader);

					// Set material options
					unityMaterial.name = name;
					unityMaterial.renderQueue = renderQueue;
					unityMaterial.hideFlags = hideFlags;
					unityMaterial.globalIlluminationFlags = globalIlluminationFlags;
					unityMaterial.shaderKeywords = keywords;
					
					// Activate required shader passes
					if(passes != null)
					{
						for(int i = 0; i < passes.Length; i++)
						{
							unityMaterial.SetShaderPassEnabled(unityMaterial.GetPassName(i), passes[i]);
						}
					}

					// Set material properties
					ToUnityMaterialProperties<int, int>(unityMaterial, ints, (m, p, v) => m.SetInt(p, v));
					ToUnityMaterialProperties<float, float>(unityMaterial, floats, (m, p, v) => m.SetFloat(p, v));
					ToUnityMaterialProperties<Vector3, Vector3>(unityMaterial, vectors, (m, p, v) => m.SetVector(p, v));
					ToUnityMaterialProperties<Color, Color>(unityMaterial, colors, (m, p, v) => m.SetColor(p, v));
					ToUnityMaterialProperties<TextureParams, UnityEngine.Texture>(unityMaterial, textures, (m, p, v) =>
					{
						m.SetTexture(p, scene.textures[v.index].ToUnity(progress));
						m.SetTextureOffset(p, v.offset);
						m.SetTextureScale(p, v.scale);
					});

					// Set materials missing properties / keywords if material is imported from assimp
					if((keywords == null || keywords.Length <= 0) && shader == Constants.defaultAssimpShader)
					{
						CLARTE.Shaders.Standard.Utility.MaterialChanged(unityMaterial);
					}
				}

				if(progress != null)
				{
					progress.Update(1);
				}
			}

			return unityMaterial;
		}

		private static void ToUnityMaterialProperties<T, U>(UnityEngine.Material unity_material, Dictionary<string, T> values, Action<UnityEngine.Material, string, T> setter)
		{
			if(values != null && setter != null && unity_material != null)
			{
				lock(values)
				{
					foreach(KeyValuePair<string, T> pair in values)
					{
						if(unity_material.HasProperty(pair.Key))
						{
							setter(unity_material, pair.Key, pair.Value);
						}
					}
				}
			}
		}
		#endregion

		#region Shaders handling
		private static Shader GetUnityShader(string shader_name, bool fallback = true)
		{
			Shader unity_shader;

			// Get the material shader
			lock(shaders)
			{
				if(!shaders.TryGetValue(shader_name, out unity_shader))
				{
					unity_shader = Shader.Find(shader_name);

					if(unity_shader != null)
					{
						shaders.Add(shader_name, unity_shader);
					}
				}
			}

			if(unity_shader == null)
			{
				Debug.LogErrorFormat("Unknown shader '{0}'.", shader_name);

				// Try to fall back to default standard shader
				if(fallback)
				{
					unity_shader = GetUnityShader(defaultUnityShader, false);
				}
			}

			return unity_shader;
		}
		#endregion
	}
}
