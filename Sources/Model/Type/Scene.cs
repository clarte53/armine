namespace Armine.Model.Type
{
	public sealed partial class Scene
	{
		#region Members
		public Node root_node = null;
		public Mesh[] meshes = null;
		public Material[] materials = null;
		public Texture[] textures = null;
		#endregion

		#region Progress
		private static uint CountNodes(Node node)
		{
			uint result = 1;

			if(node.Children != null)
			{
				int children_size = node.Children.Length;

				for(int i = 0; i < children_size; i++)
				{
					result += CountNodes(node.Children[i]);
				}
			}

			return result;
		}
		#endregion
	}
}
