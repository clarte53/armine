﻿using System;
using System.Collections;
using System.Collections.Generic;
using Armine.Utils;
using CLARTE.Serialization;
using UnityEngine;

namespace Armine.Model.Type
{
	public partial class Node
	{
		#region Members
		private GameObject[] unityNodes = null;
		#endregion

		#region Getter / Setter
		public GameObject[] UnityNodes
		{
			get
			{
				return unityNodes;
			}
		}
		#endregion

		#region Import
		public static IEnumerator FromUnity(Scene scene, Transform node, Action<Node> callback, Progress progress = null, IEnumerable<Transform> meshes_nodes = null)
		{
			if(scene != null && node != null && callback != null)
			{
				// Create a new node
				Node current = new Node();

				// Handle easy stuff
				current.name = node.name;
				current.tag = node.tag;
				current.layer = node.gameObject.layer;
				current.active = node.gameObject.activeSelf;
				current.hideFlags = node.gameObject.hideFlags;
				current.position = node.localPosition;
				current.rotation = node.localRotation;
				current.scale = node.localScale;
				current.metadata = Metadata.FromUnity(node.gameObject);

				// Handle meshes and materials
				List<GraphicMesh> graphic_meshes = new List<GraphicMesh>();

				FromUnityMeshAndMaterials(scene, node, ref graphic_meshes);

				if(meshes_nodes != null)
				{
					foreach(Transform mesh in meshes_nodes)
					{
						FromUnityMeshAndMaterials(scene, mesh, ref graphic_meshes);
					}
				}

				if(graphic_meshes.Count > 0)
				{
					current.meshes = graphic_meshes.ToArray();
				}

				// Handle children
				if(node.childCount > 0)
				{
					Dictionary<string, HashSet<Transform>> children_sorted = new Dictionary<string, HashSet<Transform>>();

					// Group all children by hash, i.e. same name, transform and metadata
					foreach(Transform child in node)
					{
						HashSet<Transform> set;

						string hash = current.ComputeHash(child);

						if(!children_sorted.TryGetValue(hash, out set))
						{
							set = new HashSet<Transform>();

							children_sorted.Add(hash, set);
						}

						set.Add(child);
					}

					List<Node> children_nodes = new List<Node>(children_sorted.Count);

					foreach(KeyValuePair<string, HashSet<Transform>> pair in children_sorted)
					{
						Transform main_child = null;
						uint child_count = 0;

						// For each hash, check how many children have a descendancy
						foreach(Transform child in pair.Value)
						{
							if(child.childCount > 0)
							{
								main_child = child;

								child_count++;
							}
						}

						// Only one with descendancy ? The nodes must be grouped as they are only different meshes of the original unique node
						if(child_count <= 1)
						{
							// No families at all ? Select randomly one of the meshes to serve as the main one
							if(child_count <= 0)
							{
								IEnumerator<Transform> it = pair.Value.GetEnumerator();

								if(it.MoveNext())
								{
									main_child = it.Current;
								}
							}

							if(main_child != null)
							{
								pair.Value.Remove(main_child);

								IEnumerator it = FromUnity(scene, main_child, children_nodes.Add, progress, pair.Value);

								while(it.MoveNext())
								{
									yield return it.Current;
								}
							}
						}
						else // Many families ? We don't have sufficient information to group the nodes together
						{
							foreach(Transform child in pair.Value)
							{
								IEnumerator it = FromUnity(scene, child, children_nodes.Add, progress);

								while(it.MoveNext())
								{
									yield return it.Current;
								}
							}
						}

						// Yield between each children nodes
						yield return null;
					}

					if(children_nodes.Count > 0)
					{
						current.children = children_nodes.ToArray();
					}
				}

				callback(current);

				if(progress != null)
				{
					progress.Update(1);
				}
			}
		}

		private static void FromUnityMeshAndMaterials(Scene scene, Transform node, ref List<GraphicMesh> graphic_meshes)
		{
			if(scene != null && node != null && graphic_meshes != null)
			{
				MeshFilter mesh_filter = node.GetComponent<MeshFilter>();
				MeshRenderer mesh_renderer = node.GetComponent<MeshRenderer>();

				GraphicMesh graphic_mesh = null;

				// Get the index of the shared mesh
				if(mesh_filter != null)
				{
					UnityEngine.Mesh unity_mesh = mesh_filter.sharedMesh;

					if(unity_mesh != null)
					{
						graphic_mesh = new GraphicMesh();

						graphic_mesh.meshIndex = scene.GetUnityMeshIndex(unity_mesh);
					}
				}

				// Get the indexes of the shared materials
				if(mesh_renderer != null)
				{
					UnityEngine.Material[] unity_materials = mesh_renderer.sharedMaterials;

					if(unity_materials != null)
					{
						int size = unity_materials.Length;

						if(size > 0)
						{
							if(graphic_mesh == null)
							{
								graphic_mesh = new GraphicMesh();
							}

							graphic_mesh.materialsIndexes = new int[size];

							for(int i = 0; i < size; i++)
							{
								graphic_mesh.materialsIndexes[i] = scene.GetUnityMaterialIndex(unity_materials[i]);
							}
						}
					}
				}

				if(graphic_mesh != null)
				{
					graphic_meshes.Add(graphic_mesh);
				}
			}
		}
		#endregion

		#region Export
		public IEnumerator ToUnity(Scene scene, Node parent_node, Dictionary<uint, GameObject> mapping, GameObject node_template, GameObject mesh_template, Progress progress = null)
		{
			if(unityNodes == null)
			{
				if(meshes == null || meshes.Length <= 0)
				{
					unityNodes = new[] {
						CreateUnityNode(node_template, parent_node, mapping)
					};

					if(progress != null)
					{
						progress.Update(1);
					}
				}
				else
				{
					unityNodes = new GameObject[meshes.Length];

					int material_list_size = (scene.materials != null ? scene.materials.Length : 0);
					int index_node = 0;

					foreach(GraphicMesh graphic_mesh in meshes)
					{
						if(graphic_mesh.meshIndex >= 0 && graphic_mesh.meshIndex < scene.meshes.Length)
						{
							// Get the corresponding mesh
							Mesh mesh = scene.meshes[graphic_mesh.meshIndex];

							GameObject go = CreateUnityNode(mesh_template, parent_node, mapping);

							go.GetComponent<MeshFilter>().sharedMesh = mesh.ToUnity(progress);

							// Add the materials for each submesh
							int graphic_material_list_size = (graphic_mesh.materialsIndexes != null ? graphic_mesh.materialsIndexes.Length : 0);

							if(graphic_material_list_size > 0)
							{
								UnityEngine.Material[] unity_materials = new UnityEngine.Material[graphic_material_list_size];

								for(int i = 0; i < graphic_material_list_size; i++)
								{
									int current_material = graphic_mesh.materialsIndexes[i];

									if(current_material >= 0 && current_material < material_list_size)
									{
										unity_materials[i] = scene.materials[current_material].ToUnity(scene, progress);
									}
									else
									{
										Debug.LogErrorFormat("Can not associate material with index '{0}' to mesh (only {1} existing materials).", current_material, material_list_size);
									}
								}

								go.GetComponent<MeshRenderer>().sharedMaterials = unity_materials;
							}

							unityNodes[index_node++] = go;
						}
						else
						{
							Debug.LogErrorFormat("Can not create mesh with index '{0}' (only {1} existing meshes).", graphic_mesh.meshIndex, scene.meshes.Length);
						}
					}

					if(progress != null)
					{
						progress.Update(1);
					}
				}

				if(children != null)
				{
					foreach(Node child in children)
					{
						IEnumerator it = child.ToUnity(scene, this, mapping, node_template, mesh_template, progress);

						while(it.MoveNext())
						{
							yield return it.Current;
						}

						yield return null;
					}
				}
			}
		}

		private GameObject CreateUnityNode(GameObject template, Node parent_node, Dictionary<uint, GameObject> mapping)
		{
			// Create unity gameobject
			GameObject go = UnityEngine.Object.Instantiate(template);

			// Regiter an ID for this object
			mapping.Add((uint) mapping.Count, go);

			// Set object name
			go.name = name;

			// Set tag
			if(! string.IsNullOrEmpty(tag))
			{
				go.tag = tag;
			}

			// set layer
			go.layer = layer;

			// Disable root during loading
			if(parent_node == null)
			{
				go.SetActive(false);
			}
			else
			{
				go.SetActive(active);
			}

			// Set hide flags
			go.hideFlags = hideFlags;

			// Set the node transformation & parent
			go.transform.parent = (parent_node != null && parent_node.unityNodes != null && parent_node.unityNodes.Length > 0 ? parent_node.unityNodes[0].transform : null);
			go.transform.localPosition = position;
			go.transform.localRotation = rotation;
			go.transform.localScale = scale;

			// Add metadata info if present
			if(metadata != null)
			{
				metadata.ToUnity(go);
			}

			return go;
		}
		#endregion

		#region Hash
		private string ComputeHash(Transform trans)
		{
			Binary.Buffer buffer = Module.Import.Binary.serializer.GetBuffer(5 * 1024); // Start with 5 Ko buffer

			uint written = 0;

			Metadata meta = Metadata.FromUnity(trans.gameObject);

			if(meta != null)
			{
				written += Module.Import.Binary.serializer.ToBytes(ref buffer, written, meta);
			}
			// No need to add something to know wether meta is defined or not: the data will never
			// be deserialized. It's only function is to serve to compute the comparison hash.

			uint size = (uint) System.Text.Encoding.UTF8.GetByteCount(name);
			size += 2 * Binary.Size(Binary.SupportedTypes.VECTOR3);
			size += Binary.Size(Binary.SupportedTypes.QUATERNION);

			Module.Import.Binary.serializer.ResizeBuffer(ref buffer, written + size);

			written += Module.Import.Binary.serializer.ToBytes(ref buffer, written, trans.name);
			written += Module.Import.Binary.serializer.ToBytes(ref buffer, written, trans.localPosition);
			written += Module.Import.Binary.serializer.ToBytes(ref buffer, written, trans.localRotation);
			written += Module.Import.Binary.serializer.ToBytes(ref buffer, written, trans.localScale);

			return Hash.ComputeHash(buffer.Data, 0, (int) written);
		}
		#endregion
	}
}