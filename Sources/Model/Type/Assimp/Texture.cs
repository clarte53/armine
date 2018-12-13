#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.IO;
using Assimp;
using UnityEngine;

namespace Armine.Model.Type
{
	public partial class Texture
	{
		#region Members
		public const uint ASSIMP_PROGRESS_FACTOR = 5;
		#endregion

		#region Constructors
		private Texture(Module.Import.Assimp.Context context, string filename, aiScene scene)
		{
			LoadFromAssimp(context, filename, scene);
		}
		#endregion

		#region Load
		private void LoadFromAssimp(Module.Import.Assimp.Context context, string filename, aiScene scene)
		{
			if(!string.IsNullOrEmpty(filename))
			{
				byte[] tex;

				if(filename[0] == '*')
				{
					// Embeded texture
					string index_str = filename.Remove(0, 1);

					uint index;
					if(uint.TryParse(index_str, out index))
					{
						using(aiTextureArray array = scene.Textures)
						{
							if(index < array.Size())
							{
								using(aiTexture texture = array.Get(index))
								{
									DecodeTexture(filename, texture.data);
								}
							}
							else
							{
								Debug.LogError("Invalid embeded texture index \"" + index + "\" (out of bound).");
							}
						}
					}
					else
					{
						Debug.LogError("Invalid embeded texture name \"" + filename + "\" (not an index).");
					}
				}
				else if(context.importer != null && (tex = context.importer.GetTexture(filename)) != null)
				{
					DecodeTexture(filename, tex);
				}
				else
				{
					LoadFromFile(context.path, filename);
				}
			}
		}

		private void LoadFromFile(string base_path, string filename)
		{
			// Because Path.DirectorySeparatorChar return '\' on windows, while Mono always expect it to be '/'
			const char path_separator = '/';

			byte[] texture_data = null;

			string filename_path = filename;

			if(!string.IsNullOrEmpty(base_path))
			{
				base_path += path_separator;
			}
			else
			{
				base_path = null;
			}

			if(!string.IsNullOrEmpty(filename_path))
			{
				if(!(Path.IsPathRooted(filename_path) && File.Exists(filename_path)))
				{
					if(Path.IsPathRooted(filename_path))
					{
						filename_path.Remove(0, Path.GetPathRoot(filename_path).Length);
					}

					string[] directories = filename_path.Split(path_separator);
					int start = 0;

					while(filename_path != null && !File.Exists(string.Format("{0}{1}", base_path, filename_path)))
					{
						start++;

						if(start < directories.Length)
						{
							filename_path = string.Join(path_separator.ToString(), directories, start, directories.Length - start);
						}
						else
						{
							filename_path = null;
						}
					}

					if(filename_path != null)
					{
						filename_path = string.Format("{0}{1}", base_path, filename_path);
					}
				}
			}
			else
			{
				filename_path = null;
			}

			if(filename_path != null && File.Exists(filename_path))
			{
				texture_data = File.ReadAllBytes(filename_path);
			}
			else
			{
				Debug.LogError(string.Format("Texture '{0}' can not be found, including in sub & upper directories.", filename));
			}

			if(texture_data != null)
			{
				DecodeTexture(filename, texture_data);
			}
		}

		private void DecodeTexture(string filename, byte[] texture_data)
		{
			unityTexture = null;

			byte[] decoded;
			int width;
			int height;

			if(Utils.DevIL.Load(filename, texture_data, out decoded, out width, out height))
			{
				Reset(filename, decoded, width, height);
			}
			else
			{
				Debug.LogErrorFormat("Unsupported texture format '{0}' for texture '{1}'.", Path.GetExtension(filename), filename);

				Reset(filename, null, 0, 0);
			}
		}
		#endregion

		#region Save
		private Texture Save(string path)
		{
			if(unityTexture != null)
			{
				try
				{
					string file_name = Path.GetFileNameWithoutExtension(filename);

					FileStream stream = File.OpenWrite(path + "/" + file_name + ".png");

					byte[] texture_data = unityTexture.EncodeToPNG();

					stream.Write(texture_data, 0, texture_data.Length);

					stream.Close();
				}
				catch(IOException error)
				{
					Debug.LogError(error.Message);
				}
			}

			return this;
		}
		#endregion

		#region Import
		public static void FromAssimp(Module.Import.Assimp.Context context, Material material, aiScene scene, aiMaterial material_data, string texture_key, aiTextureType texture_type, float reflectivity)
		{
			if(texture_type != aiTextureType.aiTextureType_NONE && material_data.GetTextureCount(texture_type) > 0)
			{
				using(aiString texture_name = new aiString())
				{
					if(material_data.GetTexturePath(texture_type, 0, texture_name))
					{
						string filename = texture_name.C_Str();

						uint index = context.scene.GetAssimpTexture(filename, () => new Texture(context, filename, scene)).Item2;

						material.AddTextureParams(texture_key, new Material.TextureParams(index));

						context.progress.Update(ASSIMP_PROGRESS_FACTOR);
					}
				}
			}

			Color default_color;

			// Add textures as alpha channel of existing textures, or compute normal map from heightmap if not defined.
			switch(texture_key)
			{
				case Assimp.Convert.unityMainTexName:
					default_color = Color.white;

					Color? diffuse = material.GetColor(Assimp.Convert.unityDiffuseColorName);

					if(diffuse.HasValue)
					{
						default_color = diffuse.Value;
					}

					// Opacity as main texture alpha channel
					FromAssimpAlphaTexture(context, material, material_data, scene, Assimp.Convert.unityMainTexName, aiTextureType.aiTextureType_OPACITY, default_color, c => c.grayscale);
					break;
				case Assimp.Convert.unityMetallicGlossName:
					default_color = Color.black;

					float? metallic = material.GetFloat(Assimp.Convert.unityMetallicValueName);

					if(metallic.HasValue)
					{
						default_color = new Color(metallic.Value, metallic.Value, metallic.Value, 1f);
					}

					// Shininess as alpha channel of metallic gloss map
					FromAssimpAlphaTexture(context, material, material_data, scene, Assimp.Convert.unityMetallicGlossName, aiTextureType.aiTextureType_SHININESS, default_color, c => Material.Smoothness(c.grayscale, reflectivity));
					break;
				case Assimp.Convert.unitySpecGlossName:
					default_color = Color.black;

					Color? specular = material.GetColor(Assimp.Convert.unitySpecularColorName);

					if(specular.HasValue)
					{
						default_color = specular.Value;
					}

					// Shininess as alpha channel of specular gloss map
					FromAssimpAlphaTexture(context, material, material_data, scene, Assimp.Convert.unitySpecGlossName, aiTextureType.aiTextureType_SHININESS, default_color, c => Material.Smoothness(c.grayscale, reflectivity));
					break;
				case Assimp.Convert.unityBumpName:
					// Bump mapping from heightmap if not defined from normals
					FromAssimpNormalsFromHeightmap(context, material, material_data, scene);
					break;
			}
		}

		private static void FromAssimpAlphaTexture(Module.Import.Assimp.Context context, Material material, aiMaterial material_data, aiScene scene, string unity_property, aiTextureType texture_type, Color default_color, Func<Color, float> op)
		{
			if(material_data.GetTextureCount(texture_type) > 0)
			{
				using(aiString texture_name = new aiString())
				{
					if(material_data.GetTexturePath(texture_type, 0, texture_name))
					{
						Texture alpha = new Texture(context, texture_name.ToString(), scene);
						Texture base_tex = null;

						Material.TextureParams param = material.GetTextureParams(unity_property);

						if(param != null)
						{
							CLARTE.Backport.Tuple<Texture, uint> res = context.scene.GetAssimpTexture(param.index);

							if(res != null)
							{
								base_tex = res.Item1;
							}
							else
							{
								Debug.LogErrorFormat("Invalid texture index. '{0}' was registered for material '{1}' as texture with index '{2}'. However no texture was found with this index.", unity_property, material.Name, param.index);
							}
						}
						else
						{
							CLARTE.Backport.Tuple<Texture, uint> assimp_tex = context.scene.GetAssimpTexture(Guid.NewGuid().ToString(), () => new Texture("**E", alpha.width, alpha.height, default_color));

							material.AddTextureParams(unity_property, new Material.TextureParams(assimp_tex.Item2));

							base_tex = assimp_tex.Item1;
						}

						if(base_tex != null)
						{
							base_tex.AddToAlpha(alpha, op);

							base_tex.filename = string.Format("**A{0}|{1}", base_tex.filename, texture_name.C_Str());
						}
					}
				}
			}
		}

		private static void FromAssimpNormalsFromHeightmap(Module.Import.Assimp.Context context, Material material, aiMaterial material_data, aiScene scene)
		{
			if(material_data.GetTextureCount(Assimp.Convert.textureTypes[Assimp.Convert.unityBumpName]) <= 0)
			{
				using(aiString texture_name = new aiString())
				{
					if(material_data.GetTexturePath(aiTextureType.aiTextureType_HEIGHT, 0, texture_name))
					{
						string filename = string.Format("**N{0}", texture_name.C_Str());

						material.AddTextureParams(Assimp.Convert.unityBumpName, new Material.TextureParams(context.scene.GetAssimpTexture(filename, () =>
						{
							Texture texture = new Texture(context, texture_name.C_Str(), scene).HeightmapToNormals(0.5).Blur(0.5);

							texture.filename = filename;

							return texture;
						}).Item2));
					}
				}
			}
		}
		#endregion

		#region Export
		public void ToAssimp(Module.Export.Assimp.Context context, aiTextureType texture_type, aiMaterial material)
		{
			ToAssimp(context, filename, texture_type, material);
		}

		private void ToAssimp(Module.Export.Assimp.Context context, string texture_name, aiTextureType texture_type, aiMaterial material)
		{
			if(!string.IsNullOrEmpty(texture_name))
			{
				string final_texture_name = null;

				if(texture_name.Length >= 1 && texture_name[0] == '*')
				{
					// Special textures
					if(texture_name.Length >= 3 && texture_name[1] == '*')
					{
						switch(texture_name[2])
						{
							case 'N':
								// New normal texture generated from height map
								texture_type = aiTextureType.aiTextureType_HEIGHT;

								final_texture_name = texture_name.Substring(3);

								break;

							case 'A':
								// Secondary texture encoded in alpha channel
								string[] textures = texture_name.Substring(3).Split('|');

								if(textures.Length == 2)
								{
									ToAssimp(context, textures[0], texture_type, material);

									switch(texture_type)
									{
										case aiTextureType.aiTextureType_DIFFUSE:
											texture_type = aiTextureType.aiTextureType_OPACITY;
											break;
										case aiTextureType.aiTextureType_SPECULAR:
											texture_type = aiTextureType.aiTextureType_SHININESS;
											break;
										default:
											break;
									}

									ToAssimp(context, textures[1], texture_type, material);
								}
								else
								{
									throw new FormatException("The texture + alpha should contain identifiers to only two original textures");
								}

								break;

							case 'E':
								// Empty texture

								break;

							default:
								break;
						}
					}
					else // Embeded texture
					{
						if(unityTexture != null)
						{
							using(aiTextureArray textures = context.scene.Textures)
							{
								uint index = textures.Size();

								final_texture_name = "*" + index;

								using(aiTexture texture = new aiTexture())
								{
									texture.data = unityTexture.EncodeToPNG();
									texture.achFormatHint = "png";

									textures.Set(index, texture.Unmanaged());
								}
							}
						}
					}
				}
				else
				{
					final_texture_name = texture_name;
				}

				if(final_texture_name != null)
				{
					using(aiString assimp_texture_name = new aiString(final_texture_name))
					{
						lock(material)
						{
							material.SetTexturePath(texture_type, 0, assimp_texture_name.Unmanaged());
						}
					}

					context.progress.Update(ASSIMP_PROGRESS_FACTOR);
				}
			}
		}
		#endregion
	}
}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
