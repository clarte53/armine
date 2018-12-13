using UnityEngine;

namespace Armine.Model.Type
{
	public partial class Metadata
	{
		#region Import
		public static Metadata FromUnity(GameObject go)
		{
			Metadata metadata = null;

			if(go != null)
			{
				Model.Metadata meta = go.GetComponent<Model.Metadata>();

				if(meta != null && meta.data != null)
				{
					metadata = new Metadata();

					metadata.data = meta.data;
				}
			}

			return metadata;
		}
		#endregion

		#region Export
		public void ToUnity(GameObject go)
		{
			if(data != null)
			{
				go.AddComponent<Model.Metadata>().data = data;
			}
		}
		#endregion
	}
}
