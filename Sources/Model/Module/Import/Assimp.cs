#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections;
using System.IO;
using System.Text;
using Armine.Utils;
using Assimp;
using CLARTE.Threads;

namespace Armine.Model.Module.Import
{
	public class Assimp : IImporter
	{
		//-------------------------------------------------------------------------------
		// Class UnsupportedTypeException
		//-------------------------------------------------------------------------------
		public class UnsupportedTypeException : Exception
		{
			public UnsupportedTypeException(string message) : base(message)
			{

			}
		}

		//-------------------------------------------------------------------------------
		// Class NotDefinedException
		//-------------------------------------------------------------------------------
		public class NotDefinedException : Exception
		{
			public NotDefinedException(string message) : base(message)
			{

			}
		}

		public class Context
		{
			#region Members
			public Importer importer;
			public Pool threads;
			public Progress progress;
			public Type.Scene scene;
			public string filename;
			public string path;
            public uint id;
			#endregion

			#region Constructors
			public Context(Importer i)
			{
				threads = new Pool();
				progress = new Progress();

				importer = i;

				Clean();
			}
			#endregion

			#region Public methods
			public void Clean()
			{
				filename = null;
				path = null;

				scene = null;

                id = 0;

                progress.Clean();
			}
			#endregion
		}

		#region Members
		// Required post processing steps
		private const aiPostProcessSteps mandatoryPostProcessSteps =
			aiPostProcessSteps.aiProcess_ValidateDataStructure | // perform a full validation of the loader's output
			aiPostProcessSteps.aiProcess_MakeLeftHanded | // set the correct coordinate system for Unity (left handed)
			aiPostProcessSteps.aiProcess_FlipWindingOrder | // set the output face winding order to be CW (important for back face culling !)
			aiPostProcessSteps.aiProcess_SplitLargeMeshes | // split large, unrenderable meshes into submeshes (65000 vertices max per mesh in Unity)
			aiPostProcessSteps.aiProcess_Triangulate | // triangulate polygons with more than 3 edges (required for import into Unity)
			aiPostProcessSteps.aiProcess_SortByPType | // make 'clean' meshes which consist of a single type of primitives (useful to remove point & line faces)
			(aiPostProcessSteps) 0;

		// Forbiden post processing steps
		private const aiPostProcessSteps forbidenPostProcessSteps =
			aiPostProcessSteps.aiProcess_FlipUVs |
			(aiPostProcessSteps) 0;

		// User defined post processing steps
		private aiPostProcessSteps postProcessSteps =
			//aiPostProcessSteps.aiProcess_PreTransformVertices | // pre-transform vertices into world coordinates
			//aiPostProcessSteps.aiProcess_CalcTangentSpace | // calculate tangents and bitangents if possible
			//aiPostProcessSteps.aiProcess_FindInvalidData | // detect invalid model data, such as invalid normal vectors
			//aiPostProcessSteps.aiProcess_GenUVCoords | // convert spherical, cylindrical, box and planar mapping to proper UVs
			//aiPostProcessSteps.aiProcess_TransformUVCoords | // preprocess UV transformations (scaling, translation ...)
			//aiPostProcessSteps.aiProcess_FindInstances | // search for instanced meshes and remove them by references to one master
			//aiPostProcessSteps.aiProcess_FindDegenerates | // remove degenerated polygons from the import
			//aiPostProcessSteps.aiProcess_LimitBoneWeights | // limit bone weights to 4 per vertex
			//aiPostProcessSteps.aiProcess_OptimizeMeshes | // join small meshes, if possible;
			//aiPostProcessSteps.aiProcess_GenSmoothNormals | // generate smooth normal vectors if not existing
			//aiPostProcessSteps.aiProcess_ImproveCacheLocality | // improve the cache locality of the output vertices
			//aiPostProcessSteps.aiProcess_RemoveRedundantMaterials | // remove redundant materials
			//aiPostProcessSteps.aiProcess_JoinIdenticalVertices | // join identical vertices/ optimize indexing
			(aiPostProcessSteps) 0;

		private static DependenciesLoader loader = null;
		private static string[] extensions = null;

		private global::Assimp.Importer assimpImporter;
		private Context context;
		#endregion

		#region Constructors
		public Assimp(Importer importer)
		{
			if(loader == null)
			{
				loader = new DependenciesLoader();
			}

			context = new Context(importer);

			SetDefaultProperties();
		}
		#endregion

		#region Getter / setter
		private global::Assimp.Importer Importer
		{
			get
			{
				if(isDisposed)
				{
					throw new ObjectDisposedException(GetType().FullName);
				}

				if(assimpImporter == null)
				{
					assimpImporter = new global::Assimp.Importer();
				}

				return assimpImporter;
			}
		}

		public void SetLogFile(string filename)
		{
			if(!DefaultLogger.isNullLogger())
			{
				DefaultLogger.kill();
			}

			DefaultLogger.create(filename, global::Assimp.Logger.LogSeverity.VERBOSE, aiDefaultLogStream.aiDefaultLogStream_FILE);
		}

		private void SetProperties(string filename, out string extension, out aiPostProcessSteps steps)
		{
			context.filename = Path.GetFileName(filename);
			context.path = Path.GetDirectoryName(filename);

			extension = Path.GetExtension(filename).Remove(0, 1).ToLower();

			steps = UsedSteps(postProcessSteps);

			// Validate data structure is redundant for assbin format and can lead to unjustified errors
			if(extension == "assbin")
			{
				steps = steps & ~aiPostProcessSteps.aiProcess_ValidateDataStructure;
			}

			SetDefaultProperties();
		}

		private void SetDefaultProperties()
		{
			SetProperty("PP_SLM_TRIANGLE_LIMIT", 65534);
			SetProperty("PP_SLM_VERTEX_LIMIT", 65534);
		}
		#endregion

		#region IDisposable Support
		private bool isDisposed = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if(!isDisposed)
			{
				if(disposing)
				{
					// TODO: delete managed state (managed objects).

					if(assimpImporter != null)
					{
						assimpImporter.Dispose();
					}

					if(context != null)
					{
						context.threads.Dispose();
					}

					DefaultLogger.kill();
				}

				// TODO: free unmanaged resources (unmanaged objects) and replace finalizer below.
				// TODO: set fields of large size with null value.

				isDisposed = true;
			}
		}

		// TODO: replace finalizer only if the above Dispose(bool disposing) function as code to free unmanaged resources.
		//~Assimp()
		//{
		//	Dispose(false);
		//}

		public void Dispose()
		{
			// Pass true in dispose method to clean managed resources too and say GC to skip finalize in next line.
			Dispose(true);

			// If dispose is called already then say GC to skip finalize on this instance.
			// TODO: uncomment next line if finalizer is replaced above.
			// GC.SuppressFinalize(this);
		}
		#endregion

		#region IImporter implementation
		public string[] GetSupportedExtensions()
		{
			if(extensions == null)
			{
				extensions = Importer.GetExtensions().Split(';');

				for(int i = 0; i < extensions.Length; i++)
				{
					extensions[i] = extensions[i].Remove(0, 2).ToLower();
				}
			}

			return extensions;
		}

		public IEnumerator ImportFromFile(string filename, ImporterReturnCallback return_callback, ProgressCallback progress_callback)
		{
			aiPostProcessSteps steps;
			string extension;

			SetProperties(filename, out extension, out steps);

			byte[] ascii_data = Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding("iso-8859-1"), Encoding.Unicode.GetBytes(filename));

			return Type.Scene.FromAssimp(context, Importer, () => Importer.ReadFile(ascii_data, (uint) ascii_data.Length, steps), return_callback, progress_callback);
		}

		public IEnumerator ImportFromBytes(string filename, byte[] data, ImporterReturnCallback return_callback, ProgressCallback progress_callback)
		{
			aiPostProcessSteps steps;
			string extension;

			SetProperties(filename, out extension, out steps);

			return Type.Scene.FromAssimp(context, Importer, () => Importer.ReadFileFromMemory(data, (uint) data.Length, steps, extension), return_callback, progress_callback);
		}
		#endregion

		#region Properties getters
		private int GetPropertyInt(string property)
		{
			const int error = -1;

			int value = Importer.GetPropertyInteger(property, error);

			if(value == error)
			{
				throw new NotDefinedException(string.Format("The property \"{0}\" is not defined.", property));
			}

			return value;
		}

		private float GetPropertyFloat(string property)
		{
			const float error = 10e10f;

			float value = Importer.GetPropertyFloat(property, error);

			if(Math.Abs(value - error) < 0.000001)
			{
				throw new NotDefinedException(string.Format("The property \"{0}\" is not defined.", property));
			}

			return value;
		}

		private bool GetPropertyBool(string property)
		{
			const bool error = false;

			return Importer.GetPropertyBool(property, error);
		}

		private string GetPropertyString(string property)
		{
			const string error = "";

			string value = Importer.GetPropertyString(property, error);

			if(value.CompareTo(error) == 0)
			{
				throw new NotDefinedException(string.Format("The property \"{0}\" is not defined.", property));
			}

			return value;
		}

		private aiMatrix4x4 GetPropertyMatrix(string property)
		{
			return Importer.GetPropertyMatrix(property);
		}

		public T GetProperty<T>(string property)
		{
			if(property != null)
			{
				if(typeof(T) == typeof(int))
				{
					return (T) Convert.ChangeType(GetPropertyInt(property), typeof(T));
				}
				else if(typeof(T) == typeof(float))
				{
					return (T) Convert.ChangeType(GetPropertyFloat(property), typeof(T));
				}
				else if(typeof(T) == typeof(bool))
				{
					return (T) Convert.ChangeType(GetPropertyBool(property), typeof(T));
				}
				else if(typeof(T) == typeof(string))
				{
					return (T) Convert.ChangeType(GetPropertyString(property), typeof(T));
				}
				else if(typeof(T) == typeof(aiMatrix4x4))
				{
					return (T) Convert.ChangeType(GetPropertyMatrix(property), typeof(T));
				}
				else
				{
					throw new UnsupportedTypeException(string.Format("Impossible to get the property {0}. Unsupported type {1}", property, typeof(T)));
				}
			}
			else
			{
				throw new UnsupportedTypeException("Impossible to get the property 'null'.");
			}
		}
		#endregion

		#region Properties setters
		public void SetProperty(string property, int value)
		{
			Importer.SetPropertyInteger(property, value);
		}

		public void SetProperty(string property, float value)
		{
			Importer.SetPropertyFloat(property, value);
		}

		public void SetProperty(string property, bool value)
		{
			Importer.SetPropertyBool(property, value);
		}

		public void SetProperty(string property, string value)
		{
			Importer.SetPropertyString(property, value);
		}

		public void SetProperty(string property, aiMatrix4x4 value)
		{
			Importer.SetPropertyMatrix(property, value);
		}
		#endregion

		#region Flags
		public void ChangeFlag(aiPostProcessSteps flags, bool state)
		{
			postProcessSteps = (aiPostProcessSteps) Option.Flags.Toogle((int) postProcessSteps, (int) flags, state);
		}

		public bool IsFlagSet(aiPostProcessSteps flags)
		{
			return Option.Flags.IsSet((int) postProcessSteps, (int) flags);
		}

		public static aiPostProcessSteps UsedSteps(aiPostProcessSteps flags)
		{
			return (flags | mandatoryPostProcessSteps) & ~forbidenPostProcessSteps;
		}

		public static bool CompareFlags(aiPostProcessSteps flags1, aiPostProcessSteps flags2)
		{
			return UsedSteps(flags1) == UsedSteps(flags2);
		}
		#endregion
	}
}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
