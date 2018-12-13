#if UNITY_EDITOR_WIN

using UnityEditor;
using UnityEngine;

namespace Armine.Editor.Tools
{
	internal class InvertNormals
	{
		[MenuItem("Armine/Tools/Invert Normals", false, 10)]
		private static void ShowInvert()
		{
			Invert();
		}
		
		[MenuItem("Armine/Tools/Invert Normals", true)]
		private static bool ValidateShowInvert()
		{
			return Utils.License.IsLicensed() && Utils.License.ToolScriptsArePermited() && Selection.activeGameObject != null;
		}

		private static void Invert()
		{
			if(Utils.License.IsLicensed() && Utils.License.ToolScriptsArePermited())
			{
				GameObject active_object = Selection.activeGameObject;
				if(! active_object)
				{
					return;
				}
		        
				MeshFilter mesh_filter = active_object.GetComponent<MeshFilter>();
				if(! mesh_filter)
				{
					return;
				}

				Mesh shared_mesh = mesh_filter.sharedMesh;
				if(! shared_mesh)
				{
					return;
				}

				Vector3[] normals = shared_mesh.normals;
				for(int i = 0; i < normals.Length; i++)
				{
					normals[i] = -normals[i];
				}
				shared_mesh.normals = normals;

				for(int i = 0; i < shared_mesh.subMeshCount; i++)
				{
					int[] faces = shared_mesh.GetTriangles(i);

					for(int j = 0; j < faces.Length; j += 3)
					{
						int tmp = faces[j];
						faces[j] = faces[j + 1];
						faces[j + 1] = tmp;
					}

					shared_mesh.SetTriangles(faces, i);
				}

				Utils.License.DecrementToolCount();
			}
		}
	}
}

#endif // UNITY_EDITOR_WIN
