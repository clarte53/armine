using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Armine.Model.Type
{
	public partial class Scene
	{
		#region Members
		private GameObject unityRoot = null;
		private Dictionary<uint, GameObject> unityMapping = null;
		private Dictionary<UnityEngine.Mesh, int> unityMeshes = null;
		private Dictionary<UnityEngine.Material, int> unityMaterials = null;
		private Dictionary<Texture2D, int> unityTextures = null;
		#endregion

		#region Getter / Setter
		public GameObject UnityRoot
		{
			get
			{
				return unityRoot;
			}
		}

		public Dictionary<uint, GameObject> UnityMapping
		{
			get
			{
				return unityMapping;
			}
		}

		public int GetUnityMeshIndex(UnityEngine.Mesh mesh)
		{
			int index = -1;

			if(mesh != null)
			{
				if(!unityMeshes.TryGetValue(mesh, out index))
				{
					index = unityMeshes.Count;

					unityMeshes.Add(mesh, index);
				}
			}

			return index;
		}

		public int GetUnityMaterialIndex(UnityEngine.Material material)
		{
			int index = -1;

			if(material != null)
			{
				if(!unityMaterials.TryGetValue(material, out index))
				{
					index = unityMaterials.Count;

					unityMaterials.Add(material, index);
				}
			}

			return index;
		}

		public int GetUnityTextureIndex(Texture2D texture)
		{
			int index = -1;

			if(texture != null)
			{
				if(!unityTextures.TryGetValue(texture, out index))
				{
					index = unityTextures.Count;

					unityTextures.Add(texture, index);
				}
			}

			return index;
		}
		#endregion

		#region Import
		public static IEnumerator FromUnity(GameObject root, Action<Scene> callback, Module.ProgressCallback progress_callback = null)
		{
			if(root != null && callback != null)
			{
				Scene scene = new Scene();

				scene.unityMeshes = new Dictionary<UnityEngine.Mesh, int>();
				scene.unityMaterials = new Dictionary<UnityEngine.Material, int>();
				scene.unityTextures = new Dictionary<Texture2D, int>();

				uint nb_nodes = CountNodes(root.transform);

				Utils.Progress progress = new Utils.Progress();
				progress.Init(nb_nodes, progress_callback);

				// Parse the node hierarchy
				IEnumerator it = Node.FromUnity(scene, root.transform, n => scene.root_node = n, progress);

				while(it.MoveNext())
				{
					progress.Display();

					yield return it.Current;
				}

				int meshes_count = scene.unityMeshes.Count;
				int materials_count = scene.unityMaterials.Count;
				int textures_count = scene.unityTextures.Count;

				progress.Init((uint) (nb_nodes + meshes_count + materials_count + textures_count), progress_callback);
				progress.Update(nb_nodes);

				// Convert the quick lookup meshes dictionary to the final destination array
				if(meshes_count > 0)
				{
					Mesh[] meshes = new Mesh[meshes_count];

					foreach(KeyValuePair<UnityEngine.Mesh, int> mesh_pair in scene.unityMeshes)
					{
						meshes[mesh_pair.Value] = Mesh.FromUnity(mesh_pair.Key);

						progress.Update(1);
						progress.Display();

						yield return null;
					}

					scene.meshes = meshes;
				}

				// Convert the quick lookup materials dictionary to the final destination array
				if(materials_count > 0)
				{
					Material[] materials = new Material[materials_count];

					foreach(KeyValuePair<UnityEngine.Material, int> mat_pair in scene.unityMaterials)
					{
						materials[mat_pair.Value] = Material.FromUnity(scene, mat_pair.Key);

						progress.Update(1);
						progress.Display();

						yield return null;
					}

					scene.materials = materials;
				}

				// Convert the quick lookup materials dictionary to the final destination array
				if(textures_count > 0)
				{
					Texture[] textures = new Texture[textures_count];

					foreach(KeyValuePair<Texture2D, int> tex_pair in scene.unityTextures)
					{
						textures[tex_pair.Value] = Texture.FromUnity(tex_pair.Key);

						progress.Update(1);
						progress.Display();

						yield return null;
					}

					scene.textures = textures;
				}

				// Clean up
				scene.unityMeshes = null;
				scene.unityMaterials = null;
				scene.unityTextures = null;

				// Return the result
				callback(scene);
			}
		}
		#endregion

		#region Export
		public IEnumerator ToUnity(Module.ProgressCallback progress_callback = null)
		{
			unityRoot = null;

			if(root_node != null)
			{
				GameObject node_template = new GameObject("Node");
				GameObject mesh_template = new GameObject("Mesh");
				mesh_template.AddComponent<MeshFilter>();
				mesh_template.AddComponent<MeshRenderer>();

				unityMapping = new Dictionary<uint, GameObject>();

				uint nb_steps = CountNodes(root_node);
				nb_steps += (uint) (meshes != null ? meshes.Length : 0);
				nb_steps += (uint) (materials != null ? materials.Length : 0);
				nb_steps += (uint) (textures != null ? textures.Length : 0);

				Utils.Progress progress = new Utils.Progress();
				progress.Init(nb_steps, progress_callback);

				IEnumerator it = root_node.ToUnity(this, null, unityMapping, node_template, mesh_template, progress);

				while(it.MoveNext())
				{
					progress.Display();

					yield return it.Current;
				}

				// Destroy templates
				if(Application.isPlaying)
				{
					UnityEngine.Object.Destroy(node_template);
					UnityEngine.Object.Destroy(mesh_template);
				}
				else
				{
					UnityEngine.Object.DestroyImmediate(node_template);
					UnityEngine.Object.DestroyImmediate(mesh_template);
				}

				if(root_node.UnityNodes != null && root_node.UnityNodes.Length > 0)
				{
					GameObject[] nodes = root_node.UnityNodes;

					// This can happen if we have a single original mesh splitted into multiple unity meshes
					if(nodes.Length > 1)
					{
						unityRoot = new GameObject(nodes[0].name);

						// Regiter an ID for this object
						unityMapping.Add((uint) unityMapping.Count, unityRoot);

						unityRoot.SetActive(false);

						unityRoot.transform.position = nodes[0].transform.position;
						unityRoot.transform.rotation = nodes[0].transform.rotation;
						unityRoot.transform.localScale = nodes[0].transform.localScale;

						foreach(GameObject go in nodes)
						{
							go.transform.parent = unityRoot.transform;

							go.SetActive(root_node.Active);
						}
					}
					else
					{
						unityRoot = nodes[0];
					}
				}

				if(unityRoot != null)
				{
					unityRoot.SetActive(root_node.Active);
				}
			}
		}
		#endregion

		#region Progress
		private static uint CountNodes(Transform root)
		{
			uint result = 1;

			foreach(Transform child in root)
			{
				result += CountNodes(child);
			}

			return result;
		}
		#endregion
	}
}
