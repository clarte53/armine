namespace Armine.Model.Type
{
	public partial class Mesh
	{
		#region Members
		private UnityEngine.Mesh unityMesh = null;
		#endregion

		#region Import
		public static Mesh FromUnity(UnityEngine.Mesh unity_mesh)
		{
			Mesh mesh = null;

			if(unity_mesh != null)
			{
				mesh = new Mesh();

				if(unity_mesh.name != null)
					mesh.name = unity_mesh.name;
				if(unity_mesh.vertices != null)
					mesh.vertices = unity_mesh.vertices;
				if(unity_mesh.normals != null)
					mesh.normals = unity_mesh.normals;
				if(unity_mesh.tangents != null)
					mesh.tangents = unity_mesh.tangents;
				if(unity_mesh.uv != null)
					mesh.uv1 = unity_mesh.uv;
				if(unity_mesh.uv2 != null)
					mesh.uv2 = unity_mesh.uv2;
				if(unity_mesh.colors != null)
					mesh.colors = unity_mesh.colors;

				int nb_submeshes = unity_mesh.subMeshCount;

				if(nb_submeshes > 0)
				{
					mesh.submeshes = new SubMesh[nb_submeshes];

					for(int i = 0; i < nb_submeshes; i++)
					{
						SubMesh submesh = new SubMesh();

						submesh.topology = unity_mesh.GetTopology(i);
						submesh.triangles = unity_mesh.GetIndices(i);

						mesh.submeshes[i] = submesh;
					}
				}
			}

			return mesh;
		}
		#endregion

		#region Export
		public UnityEngine.Mesh ToUnity(Utils.Progress progress = null)
		{
			if(unityMesh == null)
			{
				unityMesh = new UnityEngine.Mesh();

				if(name != null)
					unityMesh.name = name;
				if(vertices != null)
					unityMesh.vertices = vertices;
				if(normals != null)
					unityMesh.normals = normals;
				if(tangents != null)
					unityMesh.tangents = tangents;
				if(uv1 != null)
					unityMesh.uv = uv1;
				if(uv2 != null)
					unityMesh.uv2 = uv2;
				if(colors != null)
					unityMesh.colors = colors;
				if(submeshes != null)
				{
					int nb_submeshes = submeshes.Length;

					unityMesh.subMeshCount = nb_submeshes;

					for(int i = 0; i < nb_submeshes; i++)
					{
						SubMesh submesh = submeshes[i];

						unityMesh.SetIndices(submesh.triangles, submesh.topology, i);
					}
				}

				unityMesh.RecalculateBounds();

#if !UNITY_5_5_OR_NEWER
				mesh.Optimize();
#endif

				if(progress != null)
				{
					progress.Update(1);
				}
			}

			return unityMesh;
		}
		#endregion
	}
}
