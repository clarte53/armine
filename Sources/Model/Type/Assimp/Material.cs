#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Collections.Generic;
using Assimp;
using UnityEngine;

namespace Armine.Model.Type
{
	public partial class Material
	{
		#region Members
		private const string defaultAssimpLineShader = "CLARTE/Line/WorldSpace";
		private const string defaultAssimpLineShaderWidth = "_LineWidth";

		public const uint ASSIMP_PROGRESS_FACTOR = 1;
		#endregion

		#region Utility methods
		public static float Smoothness(float shininess, float reflectivity)
		{
			return 0.5f * (shininess > 0.0f && reflectivity > 0.0f ? shininess + reflectivity : Mathf.Max(shininess, reflectivity));
		}
		#endregion

		#region Import
		public static void FromAssimp(Module.Import.Assimp.Context context, aiScene scene)
		{
			if(scene.HasMaterials())
			{
				using(aiMaterialArray assimp_materials = scene.Materials)
				{
					uint material_size = assimp_materials.Size();

					// Reserve the right amount of memory
					context.scene.materials = new Material[(int) material_size + 1];

					// Create a material for lines
					Material line_material = new Material();
					line_material.name = "Line";
					line_material.shader = defaultAssimpLineShader;
					line_material.floats = new Dictionary<string, float>();
					line_material.colors = new Dictionary<string, Color>();
					line_material.floats.Add(defaultAssimpLineShaderWidth, unityLineWidth);
					line_material.colors.Add(Assimp.Convert.unityDiffuseColorName, Color.black);
					context.scene.materials[(int) material_size] = line_material;

					// Load all the materials
					for(uint i = 0; i < material_size; i++)
					{
						aiMaterial material = assimp_materials.Get(i);

						// LoadMaterial must dispose of the given material afterward
						// We must use a proxy method for saving the result into the array because the index i is captured by the lambda otherwise and it's value is indefinite across multiple threads.
						context.threads.ExecAndSaveToArray(context.scene.materials, (int) i, () => FromAssimp(context, scene, material));
					}
				}
			}
		}

		private static Material FromAssimp(Module.Import.Assimp.Context context, aiScene scene, aiMaterial material_data)
		{
			Material material = new Material();

			// Initialize dictionaries before hand because we do not know in advance wich ones we will need
			material.floats = new Dictionary<string, float>();
			material.colors = new Dictionary<string, Color>();
			material.textures = new Dictionary<string, TextureParams>();

			// Name
			using(aiString material_name = new aiString())
			{
				if(material_data.GetName(material_name))
				{
					material.name = material_name.ToString();
				}
			}

			// shader
			material.shader = Constants.defaultAssimpShader;

			// Shininess
			float shininess;

			if(material_data.GetShininess(out shininess))
			{
				aiShadingMode shading;

				if(material_data.GetShadingModel(out shading))
				{
					if(shading != aiShadingMode.aiShadingMode_Blinn && shading != aiShadingMode.aiShadingMode_Phong)
					{
						// Unsupported shading model
						Debug.LogWarningFormat("The shading model for material {0} is not supported. The value for the shininess is likely to be incorrect.", material.name);
					}
				}

				const int factor = 128; // unity shader factor
				shininess /= factor;
			}

			// Gloss
			float gloss;

			if(material_data.GetShininessStrength(out gloss))
			{
				shininess *= gloss;
			}

			// Reflectivity
			float reflectivity;

			if(material_data.GetReflectivity(out reflectivity))
			{
				material.floats.Add(Assimp.Convert.unityMetallicValueName, reflectivity);
			}

			material.floats.Add(Assimp.Convert.unityGlossinessValueName, Smoothness(shininess, reflectivity));

			// Colors
			foreach(KeyValuePair<string, Assimp.Convert.GetColor> pair in Assimp.Convert.GetColors(material_data))
			{
				using(aiColor4D color = new aiColor4D())
				{
					if(pair.Value(color))
					{
						Color unity_color = Assimp.Convert.AssimpToUnity.Color(color);

						bool set_color = true;

						switch(pair.Key)
						{
							case Assimp.Convert.unityDiffuseColorName:
								// Global opacity
								float opacity;

								if(material_data.GetOpacity(out opacity) && opacity < 1.0f)
								{
									unity_color.a = opacity;

									material.floats.Add(Assimp.Convert.unityRenderModeName, (float) CLARTE.Shaders.Standard.Utility.BlendMode.TRANSPARENT);
								}

								break;

							case Assimp.Convert.unitySpecularColorName:
								// Specular color must be very close to black
								unity_color = 0.1f * unity_color;

								break;

							case Assimp.Convert.unityEmissiveColorName:
								if(!CLARTE.Shaders.Standard.Utility.ShouldEmissionBeEnabled(unity_color))
								{
									set_color = false;
								}

								break;
						}

						if(set_color)
						{
							material.colors.Add(pair.Key, unity_color);
						}
					}
				}
			}

			// Textures
			foreach(KeyValuePair<string, aiTextureType> pair in Assimp.Convert.textureTypes)
			{
				// Make a copy to avoid problem of loop variable captured by reference by lambda expression
				string texture_key = pair.Key;
				aiTextureType texture_type = pair.Value;

				context.threads.AddTask(() => Texture.FromAssimp(context, material, scene, material_data, texture_key, texture_type, reflectivity));
			}

			// We must dispose of the given parameter to free unused memory. However other tasks may still be using this material (i.e. textures), so we will let the garbage collector do it's job.
			//material_data.Dispose();

			context.progress.Update(ASSIMP_PROGRESS_FACTOR);

			return material;
		}
		#endregion

		#region Export
		public static void ToAssimp(Module.Export.Assimp.Context context, Scene scene)
		{
			if(scene.materials != null && scene.materials.Length > 0)
			{
				using(aiMaterialArray materials = context.scene.Materials)
				{
					uint count = (uint) scene.materials.Length;

					materials.Reserve(count, true);

					for(uint i = 0; i < count; i++)
					{
						uint index = i; // To avoid problems of lambda expression getting 'for' variable by reference (even if uint are not ref normaly!)

						aiMaterial assimp_material = new aiMaterial(); // Allocation in another thread fails so we must do it before starting the task

						materials.Set(index, assimp_material.Unmanaged());

						context.threads.AddTask(() => scene.materials[index].ToAssimp(context, scene, assimp_material));
					}
				}
			}
		}

		private void ToAssimp(Module.Export.Assimp.Context context, Scene scene, aiMaterial assimp_material)
		{
			// Name
			if(!string.IsNullOrEmpty(name))
			{
				using(aiString assimp_material_name = new aiString(name))
				{
					assimp_material.SetName(assimp_material_name.Unmanaged());
				}
			}

			// Set flag for transparent texture if the shader use transparency
			if(renderQueue == (int) UnityEngine.Rendering.RenderQueue.Transparent)
			{
				assimp_material.SetTextureFlags(aiTextureType.aiTextureType_DIFFUSE, 0, aiTextureFlags.aiTextureFlags_UseAlpha);
			}

			// Reflectivity
			float reflectivity;
			if(floats == null || !floats.TryGetValue("_Metallic", out reflectivity))
			{
				reflectivity = 0f;
			}

			assimp_material.SetReflectivity(reflectivity);

			// Shininess
			float smoothness;
			if(floats == null || !floats.TryGetValue("_Glossiness", out smoothness))
			{
				smoothness = 0f;
			}

			float shininess = 2.0f * smoothness - (reflectivity > 0.0f ? reflectivity : 0.0f);

			if(shininess > 0.0f)
			{
				const int factor = 128; // unity shader factor

				assimp_material.SetShadingModel(aiShadingMode.aiShadingMode_Phong);
				assimp_material.SetShininess(shininess * factor);
				assimp_material.SetShininessStrength(1.0f);
			}
			else
			{
				assimp_material.SetShadingModel(aiShadingMode.aiShadingMode_Gouraud);
			}

			// Colors
			if(colors != null)
			{
				foreach(KeyValuePair<string, Assimp.Convert.SetColor> pair in Assimp.Convert.SetColors(assimp_material))
				{
					if(colors.ContainsKey(pair.Key))
					{
						Color unity_color = colors[pair.Key];

						switch(pair.Key)
						{
							case Assimp.Convert.unityDiffuseColorName:
								if(unity_color.a < 1.0f)
								{
									assimp_material.SetOpacity(unity_color.a);
								}

								break;

							case Assimp.Convert.unitySpecularColorName:
								// Revert specular color to original value
								unity_color = 10.0f * unity_color;

								break;

							default:
								break;
						}

						using(aiColor4D color = Assimp.Convert.UnityToAssimp.Color(unity_color))
						{
							pair.Value(color);
						}
					}
				}
			}

			// Textures
			if(textures != null)
			{
				Dictionary<Texture, aiTextureType> textures_types = new Dictionary<Texture, aiTextureType>();

				// Get supported textures
				foreach(KeyValuePair<string, aiTextureType> pair in Assimp.Convert.textureTypes)
				{
					if(textures.ContainsKey(pair.Key))
					{
						Texture texture = scene.textures[textures[pair.Key].index];

						if(texture != null)
						{
							textures_types.Add(texture, pair.Value);
						}
					}
				}

				// Export each supported textures
				foreach(KeyValuePair<Texture, aiTextureType> texture_pair in textures_types)
				{
					// Make a copy to avoid problem of loop variable captured by reference by lambda expression
					Texture texture = texture_pair.Key;
					aiTextureType texture_type = texture_pair.Value;

					context.threads.AddTask(() => texture.ToAssimp(context, texture_type, assimp_material));
				}
			}

			context.progress.Update(ASSIMP_PROGRESS_FACTOR);
		}
		#endregion
	}
}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
