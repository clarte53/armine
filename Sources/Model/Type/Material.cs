using System;
using System.Collections.Generic;
using UnityEngine;

namespace Armine.Model.Type
{
	public sealed partial class Material
	{
		public sealed partial class TextureParams
		{
			#region Members
			public uint index;
			public Vector2 offset = Vector2.zero;
			public Vector2 scale = Vector2.one;
			#endregion

			#region Constructors
			public TextureParams()
			{
				// For deserialization purposes
			}

			public TextureParams(uint idx)
			{
				index = idx;
			}

			public TextureParams(uint idx, Vector2 off, Vector2 sca) : this(idx)
			{
				offset = off;
				scale = sca;
			}
			#endregion
		}

		#region Members
		private string name = null;
		private string shader = null;
		private int renderQueue = -1; // Opaque
		private HideFlags hideFlags = HideFlags.None;
		private MaterialGlobalIlluminationFlags globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
		private string[] keywords = null;
		private bool[] passes = null;
		private Dictionary<string, int> ints = null;
		private Dictionary<string, float> floats = null;
		private Dictionary<string, Vector3> vectors = null;
		private Dictionary<string, Color> colors = null;
		private Dictionary<string, TextureParams> textures = null; // Can be accessed from multiple threads, so be carefull and use locks
		#endregion

		#region Getter / Setter
		public string Name
		{
			get
			{
				return name;
			}
		}

		public float? GetFloat(string name)
		{
			float? result = null;

			if(floats != null)
			{
				float value;

				if(floats.TryGetValue(name, out value))
				{
					result = value;
				}
			}

			return result;
		}

		public Color? GetColor(string name)
		{
			Color? result = null;

			if(colors != null)
			{
				Color value;

				if(colors.TryGetValue(name, out value))
				{
					result = value;
				}
			}

			return result;
		}

		public void AddTextureParams(string name, TextureParams texture)
		{
			lock(textures)
			{
				textures.Add(name, texture);
			}
		}

		public TextureParams GetTextureParams(string name)
		{
			TextureParams param = null;

			lock(textures)
			{
				if(!textures.TryGetValue(name, out param))
				{
					param = null;
				}
			}

			return param;
		}
		#endregion
	}
}
