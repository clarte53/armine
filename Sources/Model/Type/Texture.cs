using System;
using UnityEngine;

namespace Armine.Model.Type
{
	public sealed partial class Texture
	{
		#region Members
		private string filename = null;
		private int width = 0;
		private int height = 0;
		private TextureFormat format = TextureFormat.RGBA32;
		private byte[] data = null;
		#endregion

		#region Constructors
		public Texture()
		{
			// For serialization purposes only. Do not use this!
		}

		// Create texture with given name, size & color
		private Texture(string file,int w, int h, Color32 color) : this(file, new byte[4 * Math.Max(w, 0) * Math.Max(h, 0)], w, h, TextureFormat.RGBA32)
		{
			// Set every value to 255 (opaque white)
			for(int i = 0; i < data.Length; i += 4)
			{
				data[i + 0] = color.r;
				data[i + 1] = color.g;
				data[i + 2] = color.b;
				data[i + 3] = color.a;
			}
		}

		private Texture(string file, byte[] d, int w, int h, TextureFormat f = TextureFormat.RGBA32)
		{
			Reset(file, d, w, h, f);
		}

		private void Reset(string file, byte[] d, int w, int h, TextureFormat f = TextureFormat.RGBA32)
		{
			if(d == null)
			{
				throw new ArgumentNullException("d", "The decoded texture is null.");
			}
			else if(w < 0 || h < 0)
			{
				throw new ArgumentException(string.Format("The height or width of the texture is invalid. Got '({0} x {1})'.", w, h));
			}
			
			filename = file;
			width = w;
			height = h;
			format = f;
			data = d;
		}
		#endregion

		#region Modifications
		public static int Index(int x, int y, int width)
		{
			return y * width + x;
		}

		public static Color GetColor(byte[] texture_data, int index)
		{
			index *= 4;

			return new Color32(
				texture_data[index + 0],
				texture_data[index + 1],
				texture_data[index + 2],
				texture_data[index + 3]
			);
		}

		public static void SetColor(byte[] texture_data, int index, Color32 color)
		{
			index *= 4;

			texture_data[index + 0] = color.r;
			texture_data[index + 1] = color.g;
			texture_data[index + 2] = color.b;
			texture_data[index + 3] = color.a;
		}

		public Texture Convolution(double[,] filter)
		{
			if(data != null)
			{
				int fw = filter.GetLength(0);
				int fh = filter.GetLength(1);

				// Create arrays to store the data
                byte[] result = new byte[data.Length];

				double factor = 0.0;
				for(int i = 0; i < fw; i++)
				{
					for(int j = 0; j < fh; j++)
					{
						factor += filter[i, j];
					}
				}

				if(Math.Abs(factor - 0.0) < 1e-6)
				{
					factor = 1.0;
				}
				else
				{
					factor = 1.0 / factor;
				}

				for(int x = 0; x < width; x++)
				{
					for(int y = 0; y < height; y++)
					{
						Color c = new Color(0.0f, 0.0f, 0.0f, 0.0f);

						for(int i = 0; i < fw; i++)
						{
							for(int j = 0; j < fh; j++)
							{
								int px = (x - fw / 2 + i + width) % width;
								int py = (y - fh / 2 + j + height) % height;

								Color cp = GetColor(data, Index(px, py, width));

								double filter_value = filter[i, j];

								c.r += (float) (factor * cp.r * filter_value);
								c.g += (float) (factor * cp.g * filter_value);
								c.b += (float) (factor * cp.b * filter_value);
								c.a += (float) (factor * cp.a * filter_value);
							}
						}

						SetColor(result, Index(x, y, width), c);
					}
				}

				// Copy the result back to the bitmap
				data = result;
			}

			return this;
		}

		public Texture Blur(double blur)
		{
			double[,] filter = {
				{0.5 * blur, blur, 0.5 * blur},
				{      blur, 1.0,        blur},
				{0.5 * blur, blur, 0.5 * blur}
			};

			Convolution(filter);

			return this;
		}

		public Texture HeightmapToNormals(double bumpiness)
		{
			// TODO: check if the heightmap is grayscale or if only one component is used (cf assimp_viewer/Material.cpp line 503 (CMaterialManager::HMtoNMIfNecessary()))

			if(data != null)
			{
				// Create arrays to store the data
				byte[] result = new byte[data.Length];

				for(int x = 0; x < width; x++)
				{
					for(int y = 0; y < height; y++)
					{
						// TODO: use the correct component if not grayscale
						double hxp = GetColor(data, Index((x + width + 1) % width, y, width)).grayscale;
						double hxm = GetColor(data, Index((x + width - 1) % width, y, width)).grayscale;
						double hyp = GetColor(data, Index(x, (y + height + 1) % height, width)).grayscale;
						double hym = GetColor(data, Index(x, (y + height - 1) % height, width)).grayscale;

						double nx = -bumpiness * (hxp - hxm);
						double ny = -bumpiness * (hyp - hym);
						double nz = bumpiness * bumpiness;

						// normalization factor     
						double norm = 1.0 / Math.Sqrt(nx * nx + ny * ny + nz * nz);

						// calc and range compress it
						Color32 color = new Color(
							(float) (nx * norm * 0.5 + 0.5),
							(float) (ny * norm * 0.5 + 0.5),
							(float) (nz * norm * 0.5 + 0.5)
						);

						// store it
						SetColor(result, Index(x, y, width), color);
					}
				}

				// Copy the result back to the bitmap
				data = result;
			}

			return this;
		}

		public Texture AddToAlpha(Texture alpha, Func<Color, float> op)
		{
			if(data != null)
			{
				int wa = alpha.width;
				int ha = alpha.height;

				// Create arrays to store the data
				byte[] pa = alpha.data;

				for(int x = 0; x < width; x++)
				{
					for(int y = 0; y < height; y++)
					{
						int i = Index(x, y, width);
						int ia = Index(
							Mathf.RoundToInt((float) x * (float) wa / (float) width),
							Mathf.RoundToInt((float) y * (float) ha / (float) height),
							wa
						);

						Color c = GetColor(data, i);

						SetColor(data, i, new Color(c.r, c.g, c.b, op(GetColor(pa, ia))));
					}
				}

				// Copy the result back to the bitmap
				// Already done, modfification was done in-place
			}

			return this;
		}
		#endregion
	}
}
