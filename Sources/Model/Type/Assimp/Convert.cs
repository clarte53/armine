#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

using System.Collections.Generic;
using Assimp;
using UnityEngine;

//-------------------------------------------------------------------------------
// Namespace Armine.Utils
//-------------------------------------------------------------------------------
namespace Armine.Model.Type.Assimp
{
	//-------------------------------------------------------------------------------
	// Class Convert
	//-------------------------------------------------------------------------------
	internal class Convert
	{
		internal const string unityRenderModeName = "_Mode";
		internal const string unityMetallicValueName = "_Metallic";
		internal const string unityGlossinessValueName = "_Glossiness";

		internal const string unityDiffuseColorName = "_Color";
		internal const string unitySpecularColorName = "_SpecColor";
		internal const string unityEmissiveColorName = "_EmissionColor";

		internal const string unityMainTexName = "_MainTex";
		internal const string unityMetallicGlossName = "_MetallicGlossMap";
		internal const string unitySpecGlossName = "_SpecGlossMap";
		internal const string unityBumpName = "_BumpMap";

		internal static readonly Dictionary<string, aiTextureType> textureTypes = new Dictionary<string, aiTextureType> {
			{unityMainTexName, aiTextureType.aiTextureType_DIFFUSE},
			{unityMetallicGlossName, aiTextureType.aiTextureType_NONE /*aiTextureType.aiTextureType_REFLECTION*/}, // Reflection texture is the image that should be reflected, not the level of reflectivity of each pixel...
			{unitySpecGlossName, aiTextureType.aiTextureType_SPECULAR},
			{unityBumpName, aiTextureType.aiTextureType_NORMALS},
			{"_ParallaxMap", aiTextureType.aiTextureType_DISPLACEMENT},
			{"_OcclusionMap", aiTextureType.aiTextureType_LIGHTMAP},
			{"_EmissionMap", aiTextureType.aiTextureType_EMISSIVE},
			//{"_???", aiTextureType.aiTextureType_AMBIENT},

			// aiTextureType_SHININESS is encoded in alpha chanel of _SpecGlossMap and _MetallicGlossMap (after convertion)
			// aiTextureType_OPACITY is encoded in alpha chanel of _MainTex
			// aiTextureType_HEIGHT is also a bump mapping texture, but it needs to be converted into normals
		};

		//-------------------------------------------------------------------------------
		internal delegate bool GetColor(aiColor4D inout);

		internal static Dictionary<string, GetColor> GetColors(aiMaterial material)
		{
			Dictionary<string, GetColor> color_types = new Dictionary<string, GetColor> {
				{unityDiffuseColorName, material.GetColorDiffuse},
				{unitySpecularColorName, material.GetColorSpecular},
				{unityEmissiveColorName, material.GetColorEmissive},
				//{"_???", material.GetColorAmbient},
				//{"_???", material.GetColorTransparent};
				//{"_???", material.GetColorReflective},
			};

			return color_types;
		}

		internal delegate bool SetColor(aiColor4D inout);

		internal static Dictionary<string, SetColor> SetColors(aiMaterial material)
		{
			Dictionary<string, SetColor> color_types = new Dictionary<string, SetColor> {
				{unityDiffuseColorName, material.SetColorDiffuse},
				{unitySpecularColorName, material.SetColorSpecular},
				{unityEmissiveColorName, material.SetColorEmissive},
				//{"_???", material.SetColorAmbient},
				//{"_???", material.SetColorTransparent},
				//{"_???", material.SetColorReflective},
			};

			return color_types;
		}

		//-------------------------------------------------------------------------------
		internal static string Name(aiString name, string context)
		{
			if(name.Length == 0)
			{
				return "unnamed_" + context;
			}
			else
			{
				return name.ToString();
			}
		}

		//-------------------------------------------------------------------------------
		// Class AssimpToUnity
		//-------------------------------------------------------------------------------
		internal class AssimpToUnity
		{

			internal delegate U Conversion<T, U>(T assimp) where T : System.IDisposable;

			internal static U[] Array<T, U>(Conversion<T, U> conv, Interface.DynamicArray<T> assimp) where T : System.IDisposable
			{
				U[] unity = null;

				if(conv != null)
				{
					uint size = assimp.Size();

					unity = new U[size];

					for(uint i = 0; i < size; i++)
					{
						using(T assimp_element = assimp.Get(i))
						{
							unity[i] = conv(assimp_element);
						}
					}
				}

				return unity;
			}

			//-------------------------------------------------------------------------------
			internal static int[] Face(Interface.DynamicArray<aiFace> assimp, out MeshTopology topology)
			{
				uint size = assimp.Size();

				topology = MeshTopology.Triangles;
				uint nb_faces = 3;

				if(size > 0)
				{
					// Get the topology from the first face (because option aiProcess_SortByPType is mandatory, all faces have the same topology)
					using(aiFace face = assimp.Get(0))
					{
						using(aiUIntArray indices = face.Indices)
						{
							switch(indices.Size())
							{
								case 1:
									topology = MeshTopology.Points;
									nb_faces = 1;
									break;
								case 2:
									topology = MeshTopology.Lines;
									nb_faces = 2;
									break;
								default:
									// Because option aiProcess_Triangulate is mandatory
									topology = MeshTopology.Triangles;
									nb_faces = 3;
									break;
							}
						}
					}
				}

				int[] unity = new int[size * nb_faces];

				for(uint i = 0; i < size; i++)
				{
					using(aiFace face = assimp.Get(i))
					{
						using(aiUIntArray indices = face.Indices)
						{
							if(indices.Size() >= nb_faces)
							{
								for(uint j = 0; j < nb_faces; j++)
								{
									unity[i * nb_faces + j] = (int) indices.Get(j);
								}

								if(indices.Size() > nb_faces)
								{
									Debug.LogError("Too many vertices to compose a face. Some data is lost.");
								}
							}
							else
							{
								Debug.LogError("Not enough vertices to compose a face.");
							}
						}
					}
				}

				return unity;
			}

			//-------------------------------------------------------------------------------
			internal static Vector3 Vector3(aiVector3D assimp)
			{
				return new Vector3(assimp.x, assimp.y, assimp.z);
			}

			internal static Quaternion Quaternion(aiQuaternion assimp)
			{
				return new Quaternion(assimp.x, assimp.y, assimp.z, assimp.w);
			}

			internal static Vector4 Tangent(aiVector3D assimp)
			{
				return new Vector4(assimp.x, assimp.y, assimp.z, 1);
			}

			internal static Vector2 UV(aiVector3D assimp)
			{
				return new Vector2(assimp.x, assimp.y);
			}

			internal static Color Color(aiColor4D assimp)
			{
				return new Color(assimp.r, assimp.g, assimp.b, assimp.a);
			}
		}

		//-------------------------------------------------------------------------------
		// Class UnityToAssimp
		//-------------------------------------------------------------------------------
		internal class UnityToAssimp
		{

			internal delegate U Conversion<T, U>(T unity) where U : Interface.Unmanagable<U>, System.IDisposable;

			internal static void Array<T, U>(Conversion<T, U> conv, T[] unity, Interface.Array<U> assimp) where U : Interface.Unmanagable<U>, System.IDisposable
			{
				int size = unity.Length;

				assimp.Clear();

				for(uint i = 0; i < size; i++)
				{
					using(U assimp_element = conv(unity[i]))
					{
						assimp.Set(i, assimp_element.Unmanaged());
					}
				}
			}

			//-------------------------------------------------------------------------------
			internal static void Face(int[] unity, MeshTopology topology, Interface.Array<aiFace> assimp)
			{
				uint nb_faces = 0;

				switch(topology)
				{
					case MeshTopology.Points:
						nb_faces = 1;
						break;
					case MeshTopology.Lines:
						nb_faces = 2;
						break;
					case MeshTopology.Triangles:
						nb_faces = 3;
						break;
					case MeshTopology.Quads:
						nb_faces = 4;
						break;
					default:
						Debug.LogErrorFormat("Unsupported topology '{0}' in assimp export.", topology);
						break;
				}

				assimp.Clear();

				if(nb_faces > 0)
				{
					long size = unity.Length / nb_faces;

					uint i = 0;
					for(; i < size; i++)
					{
						using(aiFace face = new aiFace())
						{
							using(aiUIntArray indices = face.Indices)
							{
								for(uint j = 0; j < nb_faces; j++)
								{
									indices.Set(j, (uint) unity[i * nb_faces + j]);
								}

								assimp.Set(i, face.Unmanaged());
							}
						}
					}

					if(i * nb_faces != unity.Length)
					{
						Debug.LogError("Invalid number of vertices to compose the faces.");
					}
				}
			}

			//-------------------------------------------------------------------------------
			internal static aiVector3D Vector3(Vector3 unity)
			{
				return new aiVector3D(unity.x, unity.y, unity.z);
			}

			internal static aiQuaternion Quaternion(Quaternion unity)
			{
				return new aiQuaternion(unity.w, unity.x, unity.y, unity.z);
			}

			internal static aiVector3D Tangent(Vector4 unity)
			{
				return new aiVector3D(unity.x, unity.y, unity.z);
			}

			internal static aiVector3D UV(Vector2 unity)
			{
				return new aiVector3D(unity.x, unity.y, 0);
			}

			internal static aiColor4D Color(Color unity)
			{
				return new aiColor4D(unity.r, unity.g, unity.b, unity.a);
			}
		}
	}
}

#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
