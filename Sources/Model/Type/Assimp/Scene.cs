#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections;
using System.Collections.Generic;
using Assimp;
using CLARTE.Threads;
using UnityEngine;

namespace Armine.Model.Type
{
	public partial class Scene
	{
		#region Members
		private const float assimpNativeLoadingPrecentage = 0.25f;

		private Dictionary<string, CLARTE.Backport.Tuple<Texture, uint>> assimpTextures;
		#endregion

		#region Import
		public static IEnumerator FromAssimp(Module.Import.Assimp.Context context, global::Assimp.Importer importer, Func<aiScene> loader, Module.ImporterReturnCallback return_callback, Module.ProgressCallback progress_callback)
		{
			if(progress_callback != null)
			{
				progress_callback(0.01f);
			}

			aiScene scene = null;

			// Async loading does not work in editor when not in play mode
			context.threads.AddTask(() => scene = loader());

			IEnumerator it = context.threads.WaitForTasksCompletion();
			while(it.MoveNext())
			{
				context.progress.Display();

				yield return it.Current;
			}

			if(scene != null)
			{
				InitProgress(context, progress_callback, scene);

				context.progress.Update((uint) (assimpNativeLoadingPrecentage * context.progress.TotalSteps));

				context.scene = new Scene();

				context.scene.assimpTextures = new Dictionary<string, CLARTE.Backport.Tuple<Texture, uint>>();

				context.threads.AddTask(() => Mesh.FromAssimp(context, scene));
				context.threads.AddTask(() => Material.FromAssimp(context, scene));
				context.threads.AddTask(() => context.scene.root_node = Node.FromAssimp(context, scene, scene.mRootNode));

				it = context.threads.WaitForTasksCompletion();
				while(it.MoveNext())
				{
					context.progress.Display();

					yield return it.Current;
				}

				// Assign materials to meshes now that all data is available
				context.threads.AddTask(() => Node.SetAssimpMeshesMaterials(context.scene, context.scene.root_node));
				// Assign textures to final array
				context.threads.AddTask(() =>
				{
					context.scene.textures = new Texture[context.scene.assimpTextures.Count];

					foreach(KeyValuePair<string, CLARTE.Backport.Tuple<Texture, uint>> pair in context.scene.assimpTextures)
					{
						context.scene.textures[pair.Value.Item2] = pair.Value.Item1;
					}
				});

				// We can safelly dispose of the scene because all tasks must have been completed to reach this point, therefore we do not risque deallocating data used in another thread.
				// this method can take a few hundred milliseconds, therefore, we do it async and wait for it to complete before allowing new imports.
				context.threads.AddTask(scene.Dispose);
				context.threads.AddTask(importer.FreeScene);

				it = context.threads.WaitForTasksCompletion();
				while(it.MoveNext())
				{
					context.progress.Display();

					yield return it.Current;
				}

				// Clean up
				context.scene.assimpTextures = null;

				if(return_callback != null)
				{
					return_callback(context.scene);
				}

				context.Clean();
			}
			else
			{
				Debug.LogErrorFormat("Failed to open file: {0}{1}{2}.\nThe importer reported the following error: {3}", context.path, System.IO.Path.PathSeparator, context.filename, importer.GetErrorString());
			}
		}

		public CLARTE.Backport.Tuple<Texture, uint> GetAssimpTexture(string filename, Func<Texture> constructor)
		{
			CLARTE.Backport.Tuple<Texture, uint> texture_info;

			if(!assimpTextures.TryGetValue(filename, out texture_info))
			{
				Texture texture = constructor();

				lock(assimpTextures)
				{
					texture_info = CLARTE.Backport.Tuple.Create(texture, (uint) assimpTextures.Count);

					assimpTextures.Add(filename, texture_info);
				}
			}

			return texture_info;
		}

		public CLARTE.Backport.Tuple<Texture, uint> GetAssimpTexture(uint index)
		{
			CLARTE.Backport.Tuple<Texture, uint> result = null;

			foreach(KeyValuePair<string, CLARTE.Backport.Tuple<Texture, uint>> pair in assimpTextures)
			{
				if(pair.Value.Item2 == index)
				{
					result = pair.Value;

					break;
				}
			}

			return result;
		}
		#endregion

		#region Export
		public IEnumerator ToAssimp(Module.Export.Assimp.Context context, string filename, aiPostProcessSteps steps, Module.ExporterSuccessCallback return_callback, Module.ProgressCallback progress_callback)
		{
			bool success = false;

			string extension = System.IO.Path.GetExtension(filename).Remove(0, 1).ToLower();

			uint export_format_count = context.exporter.GetExportFormatCount();

			bool found_exporter = false;

			for(uint i = 0; i < export_format_count; i++)
			{
				using(aiExportFormatDesc desc = context.exporter.GetExportFormatDescription(i))
				{
					if(extension == desc.fileExtension.ToLower())
					{
						using(aiScene scene = new aiScene())
						{
							InitProgress(context, progress_callback, this);

							context.scene = scene;

							// Export nodes
							Result nodes_result = context.threads.AddTask(() =>
							{
								using(aiNode root = root_node.ToAssimp(context, this, null))
								{
									scene.mRootNode = root.Unmanaged();
								}
							});

							// Export materials.
							context.threads.AddTask(() => Material.ToAssimp(context, this));

							// We must wait for all the nodes to be processed before exporting meshes because indexes are computed during parsing.
							while(!nodes_result.Done)
							{
								context.progress.Display();

								yield return null;
							}

							// Export meshes
							context.threads.AddTask(() => Mesh.ToAssimp(context, this));

							// Wait for all tasks to be completed
							IEnumerator it = context.threads.WaitForTasksCompletion();
							while(it.MoveNext())
							{
								context.progress.Display();

								yield return it.Current;
							}

							// Do the final export using Assimp now that we created the complete structure in the C++ DLL.
							Result<aiReturn> status = context.threads.AddTask(() => context.exporter.Export(scene, desc.id, filename, steps));

							// Wait for export to complete
							while(!status.Done)
							{
								context.progress.Display();

								yield return null;
							}

							if(progress_callback != null)
							{
								progress_callback(1f);
							}

							context.Clean();

							// Check export status
							if(status.Success && status.Value == aiReturn.aiReturn_SUCCESS)
							{
								success = true;
							}
							else
							{
								Debug.LogErrorFormat("Failed to export to: {0}. \nThe exporter reported the following error: {1}", filename, context.exporter.GetErrorString());
							}
						}

						found_exporter = true;

						break;
					}
				}
			}

			if(!found_exporter)
			{
				Debug.LogErrorFormat("No exporter for format '{0}' was found in Assimp.", extension);
			}

			if(return_callback != null)
			{
				return_callback(success);
			}
		}
		#endregion
		
		#region Progress
		private static void InitProgress(Module.Import.Assimp.Context context, Module.ProgressCallback callback, aiScene scene)
		{
			uint nb_textures = 0;

			using(aiMaterialArray assimp_materials = scene.Materials)
			{
				uint nb_materials = assimp_materials.Size();

				for(uint i = 0; i < nb_materials; i++)
				{
					using(aiMaterial material = assimp_materials.Get(i))
					{
						foreach(KeyValuePair<string, aiTextureType> pair in Assimp.Convert.textureTypes)
						{
							using(aiString texture_name = new aiString())
							{
								if(material.GetTexturePath(pair.Value, 0, texture_name))
								{
									nb_textures++;
								}
							}
						}
					}
				}
			}

			uint nb_steps = Node.ASSIMP_PROGRESS_FACTOR * CountNodes(scene.mRootNode);
			nb_steps += Mesh.ASSIMP_PROGRESS_FACTOR * scene.Meshes.Size();
			nb_steps += Material.ASSIMP_PROGRESS_FACTOR * scene.Materials.Size();
			nb_steps += Texture.ASSIMP_PROGRESS_FACTOR * nb_textures;
			nb_steps = (uint) (nb_steps / (1f - assimpNativeLoadingPrecentage));

			context.progress.Init(nb_steps, callback);
		}

		private static void InitProgress(Module.Export.Assimp.Context context, Module.ProgressCallback callback, Scene scene)
		{
			uint nb_steps = Node.ASSIMP_PROGRESS_FACTOR * CountNodes(scene.root_node);
			nb_steps += Mesh.ASSIMP_PROGRESS_FACTOR * (uint) (scene.meshes != null ? scene.meshes.Length : 0);
			nb_steps += Material.ASSIMP_PROGRESS_FACTOR * (uint) (scene.materials != null ? scene.materials.Length : 0);
			nb_steps += Texture.ASSIMP_PROGRESS_FACTOR * (uint) (scene.textures != null ? scene.textures.Length : 0);

			context.progress.Init(nb_steps, callback);
		}

		private static uint CountNodes(aiNode node)
		{
			uint result = 1;

			using(aiNodeArray children = node.Children)
			{
				uint children_size = children.Size();
				for(uint i = 0; i < children_size; i++)
				{
					using(aiNode child = children.Get(i))
					{
						result += CountNodes(child);
					}
				}
			}

			return result;
		}
		#endregion
	}
}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
