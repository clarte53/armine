using System.Collections.Generic;
using UnityEngine;

namespace Armine.Model.Type
{
	public sealed partial class Mesh
	{
		public partial class SubMesh
		{
			#region Members
			public MeshTopology topology = MeshTopology.Triangles;
			public int[] triangles = null;
			#endregion
		}

		#region Members
		private string name = null;
		private SubMesh[] submeshes = null;
		private Vector3[] vertices = null;
		private Vector3[] normals = null;
		private Vector4[] tangents = null;
		private Vector2[] uv1 = null;
		private Vector2[] uv2 = null;
		private Color[] colors = null;
		#endregion

		#region Getter / Setter
		public IEnumerator<MeshTopology> Topologies
		{
			get
			{
				if(submeshes != null)
				{
					foreach(SubMesh mesh in submeshes)
					{
						yield return mesh.topology;
					}
				}
			}
		}

		public int SubMeshesCount
		{
			get
			{
				return submeshes != null ? submeshes.Length : 0;
			}
		}

		public int VerticesCount
		{
			get
			{
				return vertices != null ? vertices.Length : 0;
			}
		}

		public int FacesCount
		{
			get
			{
				int faces = 0;

				if(submeshes != null)
				{
					foreach(SubMesh mesh in submeshes)
					{
						if(mesh.triangles != null)
						{
							int nb_faces = mesh.triangles.Length;

							switch(mesh.topology)
							{
								case MeshTopology.Points:
									break;
								case MeshTopology.Lines:
									nb_faces /= 2;
									break;
								case MeshTopology.LineStrip:
									nb_faces--;
									break;
								case MeshTopology.Triangles:
									nb_faces /= 3;
									break;
								case MeshTopology.Quads:
									nb_faces /= 4;
									break;
							}

							faces += nb_faces;
						}
					}
				}

				return faces;
			}
		}
		#endregion
	}
}
