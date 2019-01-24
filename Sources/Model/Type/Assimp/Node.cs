#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Collections.Generic;
using Assimp;
using UnityEngine;

namespace Armine.Model.Type
{
	public partial class Node
	{
		#region Members
		public const uint ASSIMP_PROGRESS_FACTOR = 1;
		#endregion

		#region Import
		public static Node FromAssimp(Module.Import.Assimp.Context context, aiScene scene, aiNode assimp_node)
		{
			// Create new node object
			Node node = new Node();

            // Get node ID
            node.id = context.id++;

            // Get node name
            node.name = Assimp.Convert.Name(assimp_node.mName, "node");

			// Get node metadata
			using(aiMetadata meta = assimp_node.mMetaData)
			{
				node.metadata = Metadata.FromAssimp(context, meta);
			}

			// Parse children recursively
			using(aiNodeArray children = assimp_node.Children)
			{
				uint children_size = children.Size();

				if(children_size > 0)
				{
					node.children = new Node[children_size];

					for(uint i = 0; i < children_size; i++)
					{
						aiNode child = children.Get(i);

						// ParseNode must dispose of the given node afterward
						// We must use a proxy method for saving the result into the array because the index i is captured by the lambda otherwise and it's value is indefinite across multiple threads.
						context.threads.ExecAndSaveToArray(node.children, (int) i, () => FromAssimp(context, scene, child));
					}
				}
			}

			// Parse meshes associated to this node
			using(aiUIntArray meshes = assimp_node.Meshes)
			{
				uint meshes_size = meshes.Size();

				if(meshes_size > 0)
				{
					int global_meshes_size = (int) scene.Meshes.Size();

					node.meshes = new GraphicMesh[meshes_size];

					for(uint j = 0; j < meshes_size; j++)
					{
						node.meshes[j] = new GraphicMesh();

						if(j < global_meshes_size)
						{
							uint mesh_index = meshes.Get(j);

							node.meshes[j].meshIndex = (int) mesh_index;
						}
					}
				}
			}

			// Get the transform of this node
			using(aiVector3D position = new aiVector3D())
			{
				using(aiVector3D scaling = new aiVector3D())
				{
					using(aiQuaternion rotation = new aiQuaternion())
					{
						assimp_node.mTransformation.Decompose(scaling, rotation, position);

						node.position = Assimp.Convert.AssimpToUnity.Vector3(position);
						node.rotation = Assimp.Convert.AssimpToUnity.Quaternion(rotation);
						node.scale = Assimp.Convert.AssimpToUnity.Vector3(scaling);
					}
				}
			}

			// We must dispose of the given parameter to avoid memory leaks
			assimp_node.Dispose();

			context.progress.Update(ASSIMP_PROGRESS_FACTOR);

			return node;
		}

		public static void SetAssimpMeshesMaterials(Scene scene, Node node)
		{
			if(node != null)
			{
				if(node.meshes != null)
				{
					// Assimp loading automatically add a new material for line rendering at the end of the material list
					int line_material = scene.materials.Length - 1;

					foreach(GraphicMesh graphic_mesh in node.meshes)
					{
						Mesh mesh = scene.meshes[graphic_mesh.meshIndex];

						int[] materials = null;

						int size = mesh.SubMeshesCount;

						if(size > 0)
						{
							materials = new int[size];

							int material = mesh.assimpMaterial;
	
							IEnumerator<MeshTopology> it = mesh.Topologies;

							int i = 0;

							while(i < size && it.MoveNext())
							{
								switch(it.Current)
								{
									case MeshTopology.Lines:
									case MeshTopology.LineStrip:
										materials[i] = line_material;
										break;
									default:
										materials[i] = material;
										break;
								}

								i++;
							}
						}

						graphic_mesh.materialsIndexes = materials;
					}
				}

				if(node.children != null)
				{
					foreach(Node child in node.children)
					{
						SetAssimpMeshesMaterials(scene, child);
					}
				}
			}
		}
		#endregion

		#region Export
		public aiNode ToAssimp(Module.Export.Assimp.Context context, Scene scene, aiNode parent)
		{
			uint index = 0;

			aiNode node_object = new aiNode(name);

			// Set parent
			node_object.mParent = parent;

			// Set transform
			using(aiVector3D assimp_scale = Assimp.Convert.UnityToAssimp.Vector3(scale))
			{
				using(aiQuaternion assimp_rotation = Assimp.Convert.UnityToAssimp.Quaternion(rotation))
				{
					using(aiVector3D assimp_position = Assimp.Convert.UnityToAssimp.Vector3(position))
					{
						using(aiMatrix4x4 matrix = new aiMatrix4x4(assimp_scale, assimp_rotation, assimp_position))
						{
							node_object.mTransformation = matrix.Unmanaged();
						}
					}
				}
			}

			// Parse the children nodes
			if(children != null && children.Length > 0)
			{
				using(aiNodeArray assimp_children = node_object.Children)
				{
					assimp_children.Reserve((uint) children.Length, true);

					index = 0;

					foreach(Node child in children)
					{
						using(aiNode assimp_child = child.ToAssimp(context, scene, node_object))
						{
							if(assimp_child != null)
							{
								assimp_children.Set(index++, assimp_child.Unmanaged());
							}
						}
					}
				}
			}

			// Parse the mesh objects
			if(meshes != null && meshes.Length > 0)
			{
				using(aiUIntArray assimp_meshes = node_object.Meshes)
				{
					assimp_meshes.Reserve((uint) meshes.Length, true);

					index = 0;

					foreach(GraphicMesh graphic_mesh in meshes)
					{
						Mesh mesh = scene.meshes[graphic_mesh.meshIndex];

						int nb_materials = (graphic_mesh.materialsIndexes != null ? graphic_mesh.materialsIndexes.Length : 0);

						// Handle unity submeshes by creating new meshes for each submesh
						for(int i = 0; i < mesh.SubMeshesCount; i++)
						{
							// Assimp meshes can only have one material. Therefore, mutliple instances of one mesh
							// using different materials must be detected and replaced by different output meshes.

							uint assimp_mesh_index;

							int mat_index = (i < nb_materials ? graphic_mesh.materialsIndexes[i] : 0 /*Assimp default*/);

							Module.Export.Assimp.Mesh key = new Module.Export.Assimp.Mesh(graphic_mesh.meshIndex, i, mat_index);

							if(!context.meshes.TryGetValue(key, out assimp_mesh_index))
							{
								assimp_mesh_index = (uint) context.meshes.Count;

								context.meshes.Add(key, assimp_mesh_index);
							}

							assimp_meshes.Set(index++, assimp_mesh_index);
						}
					}
				}
			}
			
			// Parse the node metadata
			if(metadata != null)
			{
				aiMetadata assimp_meta = metadata.ToAssimp();

				if(assimp_meta != null)
				{
					node_object.mMetaData = assimp_meta.Unmanaged();
				}
			}

			context.progress.Update(ASSIMP_PROGRESS_FACTOR);

			return node_object;
		}
		#endregion
	}
}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
