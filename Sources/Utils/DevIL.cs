#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

using System;
using System.Runtime.InteropServices;

namespace Armine.Utils
{
	internal class DevIL
	{
		#region DevIL definitions
		internal enum DataFormat
		{
			IL_COLOUR_INDEX = 0x1900,
			IL_COLOR_INDEX = 0x1900,
			IL_ALPHA = 0x1906,
			IL_RGB = 0x1907,
			IL_RGBA = 0x1908,
			IL_BGR = 0x80E0,
			IL_BGRA = 0x80E1,
			IL_LUMINANCE = 0x1909,
			IL_LUMINANCE_ALPHA = 0x190A,
		}

		internal enum DataType
		{
			IL_BYTE = 0x1400,
			IL_UNSIGNED_BYTE = 0x1401,
			IL_SHORT = 0x1402,
			IL_UNSIGNED_SHORT = 0x1403,
			IL_INT = 0x1404,
			IL_UNSIGNED_INT = 0x1405,
			IL_FLOAT = 0x1406,
			IL_DOUBLE = 0x140A,
			IL_HALF = 0x140B,
		}

		internal enum ImageType
		{
			IL_TYPE_UNKNOWN = 0x0000,
			IL_BMP = 0x0420, //!< Microsoft Windows Bitmap - .bmp extension
			IL_CUT = 0x0421, //!< Dr. Halo - .cut extension
			IL_DOOM = 0x0422, //!< DooM walls - no specific extension
			IL_DOOM_FLAT = 0x0423, //!< DooM flats - no specific extension
			IL_ICO = 0x0424, //!< Microsoft Windows Icons and Cursors - .ico and .cur extensions
			IL_JPG = 0x0425, //!< JPEG - .jpg, .jpe and .jpeg extensions
			IL_JFIF = 0x0425, //!<
			IL_ILBM = 0x0426, //!< Amiga IFF (FORM ILBM) - .iff, .ilbm, .lbm extensions
			IL_PCD = 0x0427, //!< Kodak PhotoCD - .pcd extension
			IL_PCX = 0x0428, //!< ZSoft PCX - .pcx extension
			IL_PIC = 0x0429, //!< PIC - .pic extension
			IL_PNG = 0x042A, //!< Portable Network Graphics - .png extension
			IL_PNM = 0x042B, //!< Portable Any Map - .pbm, .pgm, .ppm and .pnm extensions
			IL_SGI = 0x042C, //!< Silicon Graphics - .sgi, .bw, .rgb and .rgba extensions
			IL_TGA = 0x042D, //!< TrueVision Targa File - .tga, .vda, .icb and .vst extensions
			IL_TIF = 0x042E, //!< Tagged Image File Format - .tif and .tiff extensions
			IL_CHEAD = 0x042F, //!< C-Style Header - .h extension
			IL_RAW = 0x0430, //!< Raw Image Data - any extension
			IL_MDL = 0x0431, //!< Half-Life Model Texture - .mdl extension
			IL_WAL = 0x0432, //!< Quake 2 Texture - .wal extension
			IL_LIF = 0x0434, //!< Homeworld Texture - .lif extension
			IL_MNG = 0x0435, //!< Multiple-image Network Graphics - .mng extension
			IL_JNG = 0x0435, //!< 
			IL_GIF = 0x0436, //!< Graphics Interchange Format - .gif extension
			IL_DDS = 0x0437, //!< DirectDraw Surface - .dds extension
			IL_DCX = 0x0438, //!< ZSoft Multi-PCX - .dcx extension
			IL_PSD = 0x0439, //!< Adobe PhotoShop - .psd extension
			IL_EXIF = 0x043A, //!< 
			IL_PSP = 0x043B, //!< PaintShop Pro - .psp extension
			IL_PIX = 0x043C, //!< PIX - .pix extension
			IL_PXR = 0x043D, //!< Pixar - .pxr extension
			IL_XPM = 0x043E, //!< X Pixel Map - .xpm extension
			IL_HDR = 0x043F, //!< Radiance High Dynamic Range - .hdr extension
			IL_ICNS = 0x0440, //!< Macintosh Icon - .icns extension
			IL_JP2 = 0x0441, //!< Jpeg 2000 - .jp2 extension
			IL_EXR = 0x0442, //!< OpenEXR - .exr extension
			IL_WDP = 0x0443, //!< Microsoft HD Photo - .wdp and .hdp extension
			IL_VTF = 0x0444, //!< Valve Texture Format - .vtf extension
			IL_WBMP = 0x0445, //!< Wireless Bitmap - .wbmp extension
			IL_SUN = 0x0446, //!< Sun Raster - .sun, .ras, .rs, .im1, .im8, .im24 and .im32 extensions
			IL_IFF = 0x0447, //!< Interchange File Format - .iff extension
			IL_TPL = 0x0448, //!< Gamecube Texture - .tpl extension
			IL_FITS = 0x0449, //!< Flexible Image Transport System - .fit and .fits extensions
			IL_DICOM = 0x044A, //!< Digital Imaging and Communications in Medicine (DICOM) - .dcm and .dicom extensions
			IL_IWI = 0x044B, //!< Call of Duty Infinity Ward Image - .iwi extension
			IL_BLP = 0x044C, //!< Blizzard Texture Format - .blp extension
			IL_FTX = 0x044D, //!< Heavy Metal: FAKK2 Texture - .ftx extension
			IL_ROT = 0x044E, //!< Homeworld 2 - Relic Texture - .rot extension
			IL_TEXTURE = 0x044F, //!< Medieval II: Total War Texture - .texture extension
			IL_DPX = 0x0450, //!< Digital Picture Exchange - .dpx extension
			IL_UTX = 0x0451, //!< Unreal (and Unreal Tournament) Texture - .utx extension
			IL_MP3 = 0x0452, //!< MPEG-1 Audio Layer 3 - .mp3 extension
		}

		internal enum OriginMode
		{
			IL_ORIGIN_LOWER_LEFT = 0x0601,
			IL_ORIGIN_UPPER_LEFT = 0x0602,
		}

		internal enum StateMode
		{
			IL_ORIGIN_SET = 0x0600,
			IL_FILE_OVERWRITE = 0x0620,
			IL_CONV_PAL = 0x0630,
		}

		internal enum ErrorType
		{
			IL_NO_ERROR = 0x0000,
			IL_INVALID_ENUM = 0x0501,
			IL_OUT_OF_MEMORY = 0x0502,
			IL_FORMAT_NOT_SUPPORTED = 0x0503,
			IL_INTERNAL_ERROR = 0x0504,
			IL_INVALID_VALUE = 0x0505,
			IL_ILLEGAL_OPERATION = 0x0506,
			IL_ILLEGAL_FILE_VALUE = 0x0507,
			IL_INVALID_FILE_HEADER = 0x0508,
			IL_INVALID_PARAM = 0x0509,
			IL_COULD_NOT_OPEN_FILE = 0x050A,
			IL_INVALID_EXTENSION = 0x050B,
			IL_FILE_ALREADY_EXISTS = 0x050C,
			IL_OUT_FORMAT_SAME = 0x050D,
			IL_STACK_OVERFLOW = 0x050E,
			IL_STACK_UNDERFLOW = 0x050F,
			IL_INVALID_CONVERSION = 0x0510,
			IL_BAD_DIMENSIONS = 0x0511,
			IL_FILE_READ_ERROR = 0x0512,
			IL_FILE_WRITE_ERROR = 0x0512,
			IL_LIB_GIF_ERROR = 0x05E1,
			IL_LIB_JPEG_ERROR = 0x05E2,
			IL_LIB_PNG_ERROR = 0x05E3,
			IL_LIB_TIFF_ERROR = 0x05E4,
			IL_LIB_MNG_ERROR = 0x05E5,
			IL_LIB_JP2_ERROR = 0x05E6,
			IL_LIB_EXR_ERROR = 0x05E7,
			IL_UNKNOWN_ERROR = 0x05FF,
		};

		internal enum Values
		{
			IL_VERSION_NUM = 0x0DE2,
			IL_IMAGE_WIDTH = 0x0DE4,
			IL_IMAGE_HEIGHT = 0x0DE5,
			IL_IMAGE_DEPTH = 0x0DE6,
			IL_IMAGE_SIZE_OF_DATA = 0x0DE7,
			//L_IMAGE_BPP = 0x0DE8,
			IL_IMAGE_BYTES_PER_PIXEL = 0x0DE8,
			IL_IMAGE_BPP = 0x0DE8,
			IL_IMAGE_BITS_PER_PIXEL = 0x0DE9,
			IL_IMAGE_FORMAT = 0x0DEA,
			IL_IMAGE_TYPE = 0x0DEB,
			IL_PALETTE_TYPE = 0x0DEC,
			IL_PALETTE_SIZE = 0x0DED,
			IL_PALETTE_BPP = 0x0DEE,
			IL_PALETTE_NUM_COLS = 0x0DEF,
			IL_PALETTE_BASE_TYPE = 0x0DF0,
			IL_NUM_FACES = 0x0DE1,
			IL_NUM_IMAGES = 0x0DF1,
			IL_NUM_MIPMAPS = 0x0DF2,
			IL_NUM_LAYERS = 0x0DF3,
			IL_ACTIVE_IMAGE = 0x0DF4,
			IL_ACTIVE_MIPMAP = 0x0DF5,
			IL_ACTIVE_LAYER = 0x0DF6,
			IL_ACTIVE_FACE = 0x0E00,
			IL_CUR_IMAGE = 0x0DF7,
			IL_IMAGE_DURATION = 0x0DF8,
			IL_IMAGE_PLANESIZE = 0x0DF9,
			IL_IMAGE_BPC = 0x0DFA,
			IL_IMAGE_OFFX = 0x0DFB,
			IL_IMAGE_OFFY = 0x0DFC,
			IL_IMAGE_CUBEFLAGS = 0x0DFD,
			IL_IMAGE_ORIGIN = 0x0DFE,
			IL_IMAGE_CHANNELS = 0x0DFF,
		}

		[DllImport("DevIL.dll")]
		internal static extern void ilInit();

		[DllImport("DevIL.dll")]
		internal static extern uint ilGenImage();

		[DllImport("DevIL.dll")]
		internal static extern void ilBindImage(uint Image);

		[DllImport("DevIL.dll")]
		internal static extern ImageType ilDetermineType(string FileName);

		[DllImport("DevIL.dll")]
		internal static extern ImageType ilDetermineTypeL(IntPtr Lump, uint Size);

		[DllImport("DevIL.dll")]
		internal static extern bool ilOriginFunc(OriginMode Mode);

		[DllImport("DevIL.dll")]
		internal static extern bool ilEnable(StateMode Mode);

		[DllImport("DevIL.dll")]
		internal static extern bool ilDisable(StateMode Mode);

		[DllImport("DevIL.dll")]
		internal static extern bool ilLoadL(ImageType Type, IntPtr Lump, uint Size);

		[DllImport("DevIL.dll")]
		internal static extern int ilGetInteger(Values Mode);

		[DllImport("DevIL.dll")]
		internal static extern bool ilConvertImage(DataFormat DestFormat, DataType DestType);

		[DllImport("DevIL.dll")]
		internal static extern IntPtr ilGetData();

		[DllImport("DevIL.dll")]
		internal static extern void ilDeleteImage(uint Num);

		[DllImport("DevIL.dll")]
		internal static extern ErrorType ilGetError();
		#endregion

		#region Members
		protected static object mutex = new object();
		protected static bool initialized = false;
		#endregion

		#region Loading
		internal static bool Load(string filename, byte[] data, out byte[] decoded, out int width, out int height)
		{
			decoded = null;
			width = 0;
			height = 0;

			IntPtr data_ptr = IntPtr.Zero;

			try
			{
				// Allocate some memory into unmanaged memory to hold raw image data
				data_ptr = Marshal.AllocHGlobal(data.Length);

				// Copy raw image data into unmanaged memory
				Marshal.Copy(data, 0, data_ptr, data.Length);

				lock(mutex) // Because DevIL is not thread safe. Therefore, we must force synchronicity for this part.
				{
					if(!initialized)
					{
						// Initialize DevIL
						ilInit();
					}

					// Create a new image
					uint image_id = ilGenImage();

					// Set this image as the active one
					ilBindImage(image_id);

					// Get the type of the image file
					ImageType type = ilDetermineType(filename);

					// If the extension was not recognized, try to infer type from file header
					if(type == ImageType.IL_TYPE_UNKNOWN)
					{
						type = ilDetermineTypeL(data_ptr, (uint) data.Length);
					}

					// If the file is supported
					if(type != ImageType.IL_TYPE_UNKNOWN)
					{
						// Set the orientation of the loaded file
						ilOriginFunc(OriginMode.IL_ORIGIN_LOWER_LEFT);
						ilEnable(StateMode.IL_ORIGIN_SET);

						// Load the file
						if(ilLoadL(type, data_ptr, (uint) data.Length))
						{
							// Get the file specs
							width = ilGetInteger(Values.IL_IMAGE_WIDTH);
							height = ilGetInteger(Values.IL_IMAGE_HEIGHT);

							// Convert the image into ARGB32 format to simplify import process
							ilConvertImage(DataFormat.IL_RGBA, DataType.IL_UNSIGNED_BYTE);

							// Allocated some managed memory to store the decoded image
							decoded = new byte[4 * height * width];

							// Copy raw image data back to byte array
							Marshal.Copy(ilGetData(), decoded, 0, decoded.Length);
						}
					}

					// Delete used image
					ilDeleteImage(image_id);
				}
			}
			catch(Exception e)
			{
				UnityEngine.Debug.LogErrorFormat("Error while importing image '{0}': {1}\n{2}", filename, e.Message, e.StackTrace);
			}

			// Free the allocated unmanaged memory
			if(data_ptr != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(data_ptr);
			}

			if(decoded == null)
			{
				lock(mutex) // Because DevIL is not thread safe. Therefore, we must force synchronicity for this part.
				{
					UnityEngine.Debug.LogErrorFormat("Error while importing image '{0}': {1}", filename, ilGetError().ToString());
				}
			}

			return (decoded != null);
		}
		#endregion
	}
}

#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
