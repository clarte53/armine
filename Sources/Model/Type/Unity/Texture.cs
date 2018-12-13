using UnityEngine;

namespace Armine.Model.Type
{
	public partial class Texture
	{
		#region Members
		private Texture2D unityTexture = null;
		#endregion

		#region Import
		public static Texture FromUnity(Texture2D unity_texture)
		{
			Texture texture = null;

			if(unity_texture != null && unity_texture is Texture2D)
			{
				texture = new Texture(unity_texture.name, unity_texture.GetRawTextureData(), unity_texture.width, unity_texture.height, unity_texture.format);

				texture.unityTexture = unity_texture;
			}

			return texture;
		}
		#endregion

		#region Export
		public Texture2D ToUnity(Utils.Progress progress = null)
		{
			if(unityTexture == null)
			{
				if(data != null)
				{
					unityTexture = new Texture2D(width, height, format, false);
					unityTexture.LoadRawTextureData(data);
					unityTexture.name = filename;
					unityTexture.Apply();
				}
				else
				{
					unityTexture = new Texture2D(1, 1);
					unityTexture.name = "ERROR";
					unityTexture.Apply();
				}

				if(progress != null)
				{
					progress.Update(1);
				}
			}

			return unityTexture;
		}
		#endregion
	}
}
