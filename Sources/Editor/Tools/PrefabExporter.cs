using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Armine.Editor.Tools
{
	internal sealed class PrefabExporter
	{
		#region Members
		private Dictionary<UnityEngine.Object, UnityEngine.Object> mapping;
		#endregion

		#region Constructors
		internal PrefabExporter()
		{
			mapping = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
		}
		#endregion

		#region Save as Prefab
		internal void Save(GameObject root, string file)
		{
			if(root != null)
			{
				// Get the path of the project
				string[] directories = Application.dataPath.Split('/');

				//the project name is two forward slashes back from the end of the array
				string project_name = directories[directories.Length - 2];

				//get the position of the project name in the path chosen in savefilepanel
				int offset = file.LastIndexOf(project_name, StringComparison.CurrentCulture);

				if(offset != -1)
				{
					// if offset does not return -1 (i.e. no position)
					string assets = "/";

					//add the length of the project name to the offset
					offset = offset + project_name.Length + assets.Length;

					//slice the path into a shortened relative path with the filename
					string shortenedpath = file.Substring(offset, file.Length - offset);
					string path = "";

					if(shortenedpath.LastIndexOf('/') != -1)
					{
						path = shortenedpath.Substring(0, shortenedpath.LastIndexOf('/') + 1);
						shortenedpath = shortenedpath.Substring(shortenedpath.LastIndexOf('/') + 1, shortenedpath.Length - (shortenedpath.LastIndexOf('/') + 1));
					}

					CreatePrefab(path, shortenedpath, root);
				}
				else
				{
					Debug.LogError("Impossible to export to prefab \"" + file + "\": prefabs must be exported inside the current project tree.");
				}
			}
			else
			{
				Debug.LogError("Export is not possible with this license.");
			}
		}

		/// <summary>
		/// Create prefab object by Creating an empty one, replacing it by the object we want to store, and repair
		/// broken link to mesh/Texture/Material (They are by consequence also added in the prefab Structure)
		/// </summary>
		/// <param name="path"> Path to the repository in wich you want to create the prefab </param>
		/// <param name="prefab_name"> Name of the prefab to create </param>
		/// <param name="root"> Root node of the object to save as prefab </param>
		private void CreatePrefab(string path, string prefab_name, GameObject root)
		{
			if(! Directory.Exists(path) && path != "")
			{
				Directory.CreateDirectory(path);
			}

			Model.Info[] import_info = root.GetComponentsInChildren<Model.Info>();

			foreach(Model.Info info in import_info)
			{
				UnityEngine.Object.DestroyImmediate(info);
			}

            // Load the prefab if it exist to avoid breaking links with objects referencing this prefab
            //TODO: not working!
            //UnityEngine.Object prefab_object = (GameObject) AssetDatabase.LoadAssetAtPath(path + prefab_name, typeof(GameObject));

#if UNITY_2018_3_OR_NEWER
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(root, path + prefab_name, InteractionMode.AutomatedAction);
#else
            UnityEngine.Object prefab_object = PrefabUtility.CreateEmptyPrefab(path + prefab_name);
            GameObject prefab = PrefabUtility.ReplacePrefab(root, prefab_object, ReplacePrefabOptions.ConnectToPrefab);
#endif
            mapping.Clear();

			CorrectBrokenLinks(root, prefab, path + prefab_name);

			AssetDatabase.SaveAssets();
		}

		private T AddToPrefab<T>(string prefab_path, T node_object) where T : UnityEngine.Object
		{
			UnityEngine.Object prefab_object = null;

			if(mapping != null && node_object != null)
			{
				if(! mapping.TryGetValue(node_object, out prefab_object))
				{
					prefab_object = UnityEngine.Object.Instantiate(node_object);

					prefab_object.name = node_object.name;

					AssetDatabase.AddObjectToAsset(prefab_object, prefab_path);
					
					mapping.Add(node_object, prefab_object);
				}
			}

			return (T) prefab_object;
		}

		private void CorrectBrokenLinks(GameObject node_object, GameObject prefab_object, string prefab_path)
		{
			if(node_object != null && prefab_object != null)
			{
				// Correct the links in the children nodes recursively
				int nb_children = node_object.transform.childCount;
				
				for(int i = 0; i < nb_children; ++i)
				{
					Transform node_child = node_object.transform.GetChild(i);
					Transform prefab_child = prefab_object.transform.GetChild(i);
					
					CorrectBrokenLinks(node_child.gameObject, prefab_child.gameObject, prefab_path);
				}

				Component[] node_components = node_object.GetComponents<Component>();
				Component[] prefab_components = prefab_object.GetComponents<Component>();

				if(node_components.Length == prefab_components.Length)
				{
					for(int i = 0; i < node_components.Length; i++)
					{
						System.Type node_type = node_components[i].GetType();
						System.Type prefab_type = prefab_components[i].GetType();

						if(node_type == prefab_type)
						{
							if(node_type == typeof(MeshRenderer))
							{
								Material[] shared_node_materials = ((MeshRenderer) node_components[i]).sharedMaterials;
								Material[] shared_prefab_materials = new Material[shared_node_materials.Length];

								for(int j = 0; j < shared_node_materials.Length; j++)
								{
									shared_prefab_materials[j] = AddToPrefab(prefab_path, shared_node_materials[j]);

									Shader shader = AddToPrefab(prefab_path, shared_node_materials[j].shader);

									shared_prefab_materials[j].shader = shader;

									// Parse the properties of the shader in search of Textures
									int nb_property = ShaderUtil.GetPropertyCount(shader);
									
									for(int k = 0; k < nb_property; k++)
									{
										String property = ShaderUtil.GetPropertyName(shader, k);
										
										switch(ShaderUtil.GetPropertyType(shader, k))
										{
											case ShaderUtil.ShaderPropertyType.TexEnv:
												shared_prefab_materials[j].SetTexture(property, AddToPrefab(prefab_path, shared_node_materials[j].GetTexture(property)));
												break;
										}
									}
								}

								((MeshRenderer) prefab_components[i]).sharedMaterials = shared_prefab_materials;
							}
							else if(node_type == typeof(MeshFilter))
							{
								((MeshFilter) prefab_components[i]).sharedMesh = AddToPrefab(prefab_path, ((MeshFilter) node_components[i]).sharedMesh);
							}
							else if(node_type == typeof(MeshCollider))
							{
								((MeshCollider) prefab_components[i]).sharedMesh = AddToPrefab(prefab_path, ((MeshCollider) node_components[i]).sharedMesh);
							}
							else if(node_type == typeof(TerrainCollider))
							{
								((TerrainCollider) prefab_components[i]).terrainData = AddToPrefab(prefab_path, ((TerrainCollider) node_components[i]).terrainData);
							}
						}
						else
						{
							Debug.LogErrorFormat("The components '{0}' and '{1}' does not have the same type.", node_type, prefab_type);
						}
					}
				}
				else
				{
					Debug.LogErrorFormat("GameObject '{0}' and prefab '{1}' does not have the same number of components", node_object.name, prefab_object.name); 
				}
			}
		}
		#endregion
	}
}
