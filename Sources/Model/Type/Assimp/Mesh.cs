#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.Collections.Generic;
using Assimp;
using UnityEngine;

namespace Armine.Model.Type
{
	public partial class Mesh
	{
		#region Members
		public const uint ASSIMP_PROGRESS_FACTOR = 3;

		public int assimpMaterial = -1;
		#endregion

		#region Import
		public static void FromAssimp(Module.Import.Assimp.Context context, aiScene scene)
		{
			if(scene.HasMeshes())
			{
				using(aiMeshArray meshes = scene.Meshes)
				{
					uint meshes_size = meshes.Size();

					// Reserve the right amount of memory
					context.scene.meshes = new Mesh[(int) meshes_size];

					// Load all the meshes
					for(uint i = 0; i < meshes_size; i++)
					{
						aiMesh mesh = meshes.Get(i);

						// LoadGeometry must dispose of the given mesh afterward
						// We must use a proxy method for saving the result into the array because the index i is captured by the lambda otherwise and it's value is indefinite across multiple threads.
						context.threads.ExecAndSaveToArray(context.scene.meshes, (int) i, () => FromAssimp(context, mesh));
					}
				}
			}
		}

		private static Mesh FromAssimp(Module.Import.Assimp.Context context, aiMesh mesh_data)
		{
			// Create new mesh
			Mesh mesh = new Mesh();

			// Assimp does not support submeshes
			mesh.submeshes = new SubMesh[1];
			mesh.submeshes[0] = new SubMesh();

			// Get material associated to this mesh
			mesh.assimpMaterial = (int) mesh_data.mMaterialIndex;

			// Get mesh name
			using(aiString mesh_name = mesh_data.mName)
			{
				mesh.name = Assimp.Convert.Name(mesh_name, "mesh");
			}

			// Get vertices
			if(mesh_data.HasPositions())
			{
				using(aiVector3DArray vertices = mesh_data.Vertices)
				{
					mesh.vertices = Assimp.Convert.AssimpToUnity.Array<aiVector3D, Vector3>(Assimp.Convert.AssimpToUnity.Vector3, vertices);
				}
			}

			// Get normals
			if(mesh_data.HasNormals())
			{
				using(aiVector3DArray normals = mesh_data.Normals)
				{
					mesh.normals = Assimp.Convert.AssimpToUnity.Array<aiVector3D, Vector3>(Assimp.Convert.AssimpToUnity.Vector3, normals);
				}
			}

			// Get tangents
			if(mesh_data.HasTangentsAndBitangents())
			{
				using(aiVector3DArray tangents = mesh_data.Tangents)
				{
					mesh.tangents = Assimp.Convert.AssimpToUnity.Array<aiVector3D, Vector4>(Assimp.Convert.AssimpToUnity.Tangent, tangents);
				}
			}

			// Get faces
			if(mesh_data.HasFaces())
			{
				using(aiFaceArray faces = mesh_data.Faces)
				{
					mesh.submeshes[0].triangles = Assimp.Convert.AssimpToUnity.Face(faces, out mesh.submeshes[0].topology);
				}
			}

			// Get UV coords
			if(mesh_data.GetNumUVChannels() > 0 && mesh_data.HasTextureCoords(0))
			{
				using(aiVector3DMultiArray texture_coords = mesh_data.TextureCoords)
				{
					using(aiVector3DArray texture_coords0 = texture_coords.Get(0))
					{
						mesh.uv1 = Assimp.Convert.AssimpToUnity.Array<aiVector3D, Vector2>(Assimp.Convert.AssimpToUnity.UV, texture_coords0);
					}

					if(mesh_data.GetNumUVChannels() > 1 && mesh_data.HasTextureCoords(1))
					{
						using(aiVector3DArray texture_coords1 = texture_coords.Get(1))
						{
							mesh.uv2 = Assimp.Convert.AssimpToUnity.Array<aiVector3D, Vector2>(Assimp.Convert.AssimpToUnity.UV, texture_coords1);
						}
					}
				}
			}
			else
			{
				// No texture UVs. We need to generate some to avoid problems with most default unity shaders
				int size = mesh.vertices.Length;

				mesh.uv1 = new Vector2[size];
			}

			// Get vertex colors
			if(mesh_data.GetNumColorChannels() > 0 && mesh_data.HasVertexColors(0))
			{
				using(aiColor4DMultiArray colors = mesh_data.Colors)
				{
					using(aiColor4DArray colors0 = colors.Get(0))
					{
						mesh.colors = Assimp.Convert.AssimpToUnity.Array<aiColor4D, Color>(Assimp.Convert.AssimpToUnity.Color, colors0);
					}
				}
			}

			// TODO: anims + bones

			// We must dispose of the given parameter to free unused memory
			mesh_data.Dispose();

			context.progress.Update(ASSIMP_PROGRESS_FACTOR);

			return mesh;
		}
		#endregion

		#region Export
		public static void ToAssimp(Module.Export.Assimp.Context context, Scene scene)
		{
			if(context.meshes != null && context.meshes.Count > 0)
			{
				using(aiMeshArray meshes = context.scene.Meshes)
				{
					uint count = (uint) context.meshes.Count;

					meshes.Reserve(count, true);

					foreach(KeyValuePair<Module.Export.Assimp.Mesh, uint> indexes in context.meshes)
					{
						if(indexes.Value >= 0 && indexes.Value < count)
						{
							// Save the values to local variables to avoid the problem of variables passed by reference to lambda functions.
							Module.Export.Assimp.Mesh mesh_indexes = indexes.Key;

							aiMesh assimp_mesh = new aiMesh(); // Allocation in another thread fails so we must do it before starting the task

							meshes.Set(indexes.Value, assimp_mesh.Unmanaged());

							context.threads.AddTask(() => ToAssimp(context, scene, mesh_indexes, assimp_mesh));
						}
					}
				}
			}
		}

		private static void ToAssimp(Module.Export.Assimp.Context context, Scene scene, Module.Export.Assimp.Mesh mesh_indexes, aiMesh assimp_mesh)
		{
			Mesh mesh = scene.meshes[mesh_indexes.mesh];

			assimp_mesh.mMaterialIndex = (uint) mesh_indexes.material;

			using(aiString assimp_mesh_name = new aiString(mesh.name))
			{
				assimp_mesh.mName = assimp_mesh_name.Unmanaged();
			}

			if(mesh.vertices.Length > 0)
			{
				using(aiVector3DArray vertices = assimp_mesh.Vertices)
				{
					Assimp.Convert.UnityToAssimp.Array(Assimp.Convert.UnityToAssimp.Vector3, mesh.vertices, vertices);
				}
			}
			if(mesh.normals.Length > 0)
			{
				using(aiVector3DArray normals = assimp_mesh.Normals)
				{
					Assimp.Convert.UnityToAssimp.Array(Assimp.Convert.UnityToAssimp.Vector3, mesh.normals, normals);
				}
			}
			if(mesh.tangents.Length > 0)
			{
				using(aiVector3DArray tangents = assimp_mesh.Tangents)
				{
					Assimp.Convert.UnityToAssimp.Array(Assimp.Convert.UnityToAssimp.Tangent, mesh.tangents, tangents);
				}
			}
			if(mesh_indexes.submesh < mesh.submeshes.Length)
			{
				// Support for submeshes: this mesh represent only one submesh of the original mesh
				SubMesh sub_mesh = mesh.submeshes[mesh_indexes.submesh];

				if(sub_mesh != null && sub_mesh.triangles != null && sub_mesh.triangles.Length > 0)
				{
					using(aiFaceArray faces = assimp_mesh.Faces)
					{
						Assimp.Convert.UnityToAssimp.Face(sub_mesh.triangles, sub_mesh.topology, faces);
					}
				}
			}
			if(mesh.uv1.Length > 0 || mesh.uv2.Length > 0)
			{
				using(aiVector3DMultiArray texture_coords = assimp_mesh.TextureCoords)
				{
					if(mesh.uv1.Length > 0)
					{
						using(aiVector3DArray texture_coords0 = texture_coords.Get(0))
						{
							Assimp.Convert.UnityToAssimp.Array(Assimp.Convert.UnityToAssimp.UV, mesh.uv1, texture_coords0);
						}
					}

					if(mesh.uv2.Length > 0)
					{
						using(aiVector3DArray texture_coords1 = texture_coords.Get(1))
						{
							Assimp.Convert.UnityToAssimp.Array(Assimp.Convert.UnityToAssimp.UV, mesh.uv2, texture_coords1);
						}
					}
				}
			}
			if(mesh.colors.Length > 0)
			{
				using(aiColor4DMultiArray colors = assimp_mesh.Colors)
				{
					using(aiColor4DArray colors0 = colors.Get(0))
					{
						Assimp.Convert.UnityToAssimp.Array(Assimp.Convert.UnityToAssimp.Color, mesh.colors, colors0);
					}
				}
			}

			context.progress.Update(ASSIMP_PROGRESS_FACTOR);
		}
		#endregion
	}
}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
