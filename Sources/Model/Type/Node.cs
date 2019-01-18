using UnityEngine;

namespace Armine.Model.Type
{
	public sealed partial class Node
	{
		public partial class GraphicMesh
		{
			public int meshIndex = -1;
			public int[] materialsIndexes = null;
		} 

		#region Members
		private string name = null;
		private string tag = null;
		private int layer = 0;
		private bool active = true;
		private HideFlags hideFlags = HideFlags.None;
		private Vector3 position = Vector3.zero;
		private Quaternion rotation = Quaternion.identity;
		private Vector3 scale = Vector3.one;
		private Node[] children = null;
		private GraphicMesh[] meshes = null;
        private UnityComponent[] components = null;
		private Metadata metadata = null;
		#endregion

		#region Getter / Setter
		public bool Active
		{
			get
			{
				return active;
			}
		}

		public Node[] Children
		{
			get
			{
				return children;
			}
		}
		#endregion
	}
}
