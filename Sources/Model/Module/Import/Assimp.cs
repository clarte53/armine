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
	/// <summary>
	/// Assimp importer module.
	/// </summary>
	public class Assimp : IImporter
	{
		/// <summary>
		/// Exception raised when trying to access properties of unsupported type.
		/// </summary>
		public class UnsupportedTypeException : Exception
		{
			/// <summary>
			/// Constructor of UnsupportedTypeException.
			/// </summary>
			/// <param name="message">The error message associated with this exception.</param>
			public UnsupportedTypeException(string message) : base(message)
			{

			}
		}

		/// <summary>
		/// Exception raised when trying to access properties that does not exist.
		/// </summary>
		public class NotDefinedException : Exception
		{
			/// <summary>
			/// Constructor of NotDefinedException.
			/// </summary>
			/// <param name="message">The error message associated with this exception.</param>
			public NotDefinedException(string message) : base(message)
			{

			}
		}

		/// <summary>
		/// Structure containing the current info required by the different import methods.
		/// </summary>
		public class Context
		{
			#region Members
			/// <summary>
			/// The importer manager used for import.
			/// </summary>
			public Importer importer;

			/// <summary>
			/// A thread pool to execute asynchronous tasks.
			/// </summary>
			/// <remarks>
			/// A dedicated pool is used as many tasks are launched in batches, and the importer need to be able
			/// to await for the completion of all tasks in a batch before moving forward to the next steps of import.
			/// </remarks>
			public Pool threads;

			/// <summary>
			/// The progress callback used to notify the caller of import progress.
			/// </summary>
			public Progress progress;

			/// <summary>
			/// The scene object representing the imported data in he internal format used for exchage between modules.
			/// </summary>
			public Type.Scene scene;

			/// <summary>
			/// The name of the file currently imported.
			/// </summary>
			public string filename;

			/// <summary>
			/// The path of the file currently imported.
			/// </summary>
			public string path;

			/// <summary>
			/// The next available ID to use when associating gameobjects with unique IDs.
			/// </summary>
            public uint id;
			#endregion

			#region Constructors
			/// <summary>
			/// Constructor for context structure.
			/// </summary>
			/// <param name="i"></param>
			public Context(Importer i)
			{
				threads = new Pool();
				progress = new Progress();

				importer = i;

				Clean();
			}
			#endregion

			#region Public methods
			/// <summary>
			/// Clean the content of the structure fields to get ready for next import.
			/// </summary>
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
		/// <summary>
		/// Required post processing steps.
		/// </summary>
		private const aiPostProcessSteps mandatoryPostProcessSteps =
			aiPostProcessSteps.aiProcess_ValidateDataStructure | // perform a full validation of the loader's output
			aiPostProcessSteps.aiProcess_MakeLeftHanded | // set the correct coordinate system for Unity (left handed)
			aiPostProcessSteps.aiProcess_FlipWindingOrder | // set the output face winding order to be CW (important for back face culling !)
			aiPostProcessSteps.aiProcess_SplitLargeMeshes | // split large, unrenderable meshes into submeshes (65000 vertices max per mesh in Unity)
			aiPostProcessSteps.aiProcess_Triangulate | // triangulate polygons with more than 3 edges (required for import into Unity)
			aiPostProcessSteps.aiProcess_SortByPType | // make 'clean' meshes which consist of a single type of primitives (useful to remove point & line faces)
			(aiPostProcessSteps) 0;

		/// <summary>
		/// Forbiden post processing steps.
		/// </summary>
		private const aiPostProcessSteps forbidenPostProcessSteps =
			aiPostProcessSteps.aiProcess_FlipUVs |
			(aiPostProcessSteps) 0;

		/// <summary>
		/// User defined post processing steps.
		/// </summary>
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
		/// <summary>
		/// Constructor of Assimp importer module.
		/// </summary>
		/// <param name="importer"></param>
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

		/// <summary>
		/// Create a file where to output the  logs of assimp import process.
		/// </summary>
		/// <param name="filename">The name of the file wher to write the logs.</param>
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

		/// <summary>
		/// Release the ressources used by the importer.
		/// </summary>
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
		/// <summary>
		/// Provide the list of file extensions supported by this importer module.
		/// </summary>
		/// <returns>The list of supported extensions.</returns>
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

		/// <summary>
		/// Import data asynchronously from a source file.
		/// </summary>
		/// <param name="filename">The name of the file to import from.</param>
		/// <param name="return_callback">The calback used to notify the caller when the import is completed.</param>
		/// <param name="progress_callback">The callback to regularly notify the caller of the import progress.</param>
		/// <returns>An iterator to use inside a coroutine.</returns>
		public IEnumerator ImportFromFile(string filename, ImporterReturnCallback return_callback, ProgressCallback progress_callback)
		{
			aiPostProcessSteps steps;
			string extension;

			SetProperties(filename, out extension, out steps);

			byte[] ascii_data = Encoding.Convert(Encoding.Unicode, Encoding.GetEncoding("iso-8859-1"), Encoding.Unicode.GetBytes(filename));

			return Type.Scene.FromAssimp(context, Importer, () => Importer.ReadFile(ascii_data, (uint) ascii_data.Length, steps), return_callback, progress_callback);
		}

		/// <summary>
		/// Import data asynchronously from a byte array.
		/// </summary>
		/// <param name="filename">The name of the file corresponding to the imported data. The extension is used to determine which codec use.</param>
		/// <param name="data">The data to import from.</param>
		/// <param name="return_callback">The calback used to notify the caller when the import is completed.</param>
		/// <param name="progress_callback">The callback to regularly notify the caller of the import progress.</param>
		/// <returns>An iterator to use inside a coroutine.</returns>
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

		/// <summary>
		/// Get the value of a given property.
		/// </summary>
		/// <typeparam name="T">The type of the value te get.</typeparam>
		/// <param name="property">The name of the property to get.</param>
		/// <returns>The value of the property.</returns>
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
		/// <summary>
		/// Reset all properties and postprocess steps to the default values.
		/// </summary>
		public void ResetToDefaultOptions()
		{
			// Reset options to default values
			SetProperty("GLOB_MEASURE_TIME", false);
			SetProperty("IMPORT_NO_SKELETON_MESHES", false);
			SetProperty("FAVOUR_SPEED", false);
			SetProperty("GLOB_MULTITHREADING", -1);
			SetProperty("PP_FID_ANIM_ACCURACY", 0.0f);
			SetProperty("PP_FD_REMOVE", false);
			SetProperty("PP_ICL_PTCACHE_SIZE", assimp_swig.PP_ICL_PTCACHE_SIZE);
			SetProperty("PP_SBBC_MAX_BONES", assimp_swig.AI_SBBC_DEFAULT_MAX_BONES);
			SetProperty("PP_SBP_REMOVE", 0);
			SetProperty("PP_OG_EXCLUSIVE_LIST", "");
			SetProperty("PP_GSN_MAX_SMOOTHING_ANGLE", 175.0f);
			SetProperty("PP_CT_MAX_SMOOTHING_ANGLE", 45.0f);
			SetProperty("PP_CT_TEXTURE_CHANNEL_INDEX", 0);
			SetProperty("PP_TUV_EVALUATE", 0x2 | 0x1 | 0x4);
			SetProperty("PP_PTV_KEEP_HIERARCHY", false);
			SetProperty("PP_PTV_NORMALIZE", false);
			SetProperty("PP_PTV_ADD_ROOT_TRANSFORMATION", false);
			SetProperty("PP_PTV_ROOT_TRANSFORMATION", "0 0 0 0 0 0 1 1 1");
			SetProperty("PP_RRM_EXCLUDE_LIST", "");
			SetProperty("PP_RVC_FLAGS", 0);
			SetProperty("PP_DB_ALL_OR_NONE", false);
			SetProperty("PP_DB_THRESHOLD", (float) assimp_swig.AI_DEBONE_THRESHOLD);
			SetProperty("PP_LBW_MAX_WEIGHTS", assimp_swig.AI_LMW_MAX_WEIGHTS);
			SetProperty("IMPORT_FBX_READ_ALL_GEOMETRY_LAYERS", true);
			SetProperty("IMPORT_FBX_READ_ALL_MATERIALS", false);
			SetProperty("IMPORT_FBX_READ_MATERIALS", true);
			SetProperty("IMPORT_FBX_READ_CAMERAS", true);
			SetProperty("IMPORT_FBX_READ_LIGHTS", true);
			SetProperty("IMPORT_FBX_READ_ANIMATIONS", true);
			SetProperty("IMPORT_FBX_STRICT_MODE", false);
			SetProperty("IMPORT_FBX_PRESERVE_PIVOTS", true);
			SetProperty("IMPORT_FBX_OPTIMIZE_EMPTY_ANIMATION_CURVES", true);
			SetProperty("IMPORT_COLLADA_IGNORE_UP_DIRECTION", false);
			SetProperty("IMPORT_3DXML_USE_NODE_MATERIALS", true);
			SetProperty("IMPORT_3DXML_USE_COMPLEX_MATERIALS", true);

			// Unset post process steps
			postProcessSteps = 0;
		}

		/// <summary>
		/// Set a int property value.
		/// </summary>
		/// <param name="property">The name of the property to set.</param>
		/// <param name="value">The value of the property to set.</param>
		public void SetProperty(string property, int value)
		{
			Importer.SetPropertyInteger(property, value);
		}

		/// <summary>
		/// Set a float property value.
		/// </summary>
		/// <param name="property">The name of the property to set.</param>
		/// <param name="value">The value of the property to set.</param>
		public void SetProperty(string property, float value)
		{
			Importer.SetPropertyFloat(property, value);
		}

		/// <summary>
		/// Set a bool property value.
		/// </summary>
		/// <param name="property">The name of the property to set.</param>
		/// <param name="value">The value of the property to set.</param>
		public void SetProperty(string property, bool value)
		{
			Importer.SetPropertyBool(property, value);
		}

		/// <summary>
		/// Set a string property value.
		/// </summary>
		/// <param name="property">The name of the property to set.</param>
		/// <param name="value">The value of the property to set.</param>
		public void SetProperty(string property, string value)
		{
			Importer.SetPropertyString(property, value);
		}

		/// <summary>
		/// Set a matrix property value.
		/// </summary>
		/// <param name="property">The name of the property to set.</param>
		/// <param name="value">The value of the property to set.</param>
		public void SetProperty(string property, aiMatrix4x4 value)
		{
			Importer.SetPropertyMatrix(property, value);
		}
		#endregion

		#region Flags
		/// <summary>
		/// Enable or disable a set of postprocess steps.
		/// </summary>
		/// <param name="flags">The postprocess steps to set, as bit flags.</param>
		/// <param name="state">The desired state for the postprocess steps. True will enable the steps, false will disable them.</param>
		public void ChangeFlag(aiPostProcessSteps flags, bool state)
		{
			postProcessSteps = (aiPostProcessSteps) Option.Flags.Toogle((int) postProcessSteps, (int) flags, state);
		}

		/// <summary>
		/// Test if at least one of the postprocess steps in a set is enabled or not.
		/// </summary>
		/// <param name="flags">The postprocess steps to check, as bit flags.</param>
		/// <returns>True if at least one of the postprocess steps is enabled, false otherwise.</returns>
		public bool IsFlagSet(aiPostProcessSteps flags)
		{
			return Option.Flags.IsSet((int) postProcessSteps, (int) flags);
		}

		/// <summary>
		/// Compute the actual postprocess steps that will be used for import, including user defined steps, mandatory steps and without forbidden steps.
		/// </summary>
		/// <param name="flags">The requested postprocess steps to use, as bit flags.</param>
		/// <returns>The postprocess steps that will be used, based on requested steps, mandatory steps and without forbidden steps.</returns>
		public static aiPostProcessSteps UsedSteps(aiPostProcessSteps flags)
		{
			return (flags | mandatoryPostProcessSteps) & ~forbidenPostProcessSteps;
		}

		/// <summary>
		/// Check if to set of postprocess steps are identical.
		/// </summary>
		/// <remarks>
		/// The postprocess steps are normalized before comparison to take into account the actual steps that would be used on import for both.
		/// </remarks>
		/// <param name="flags1">The first set of postprocess steps to compare, as bit flags.</param>
		/// <param name="flags2">The second set of postprocess steps to compare, as bit flags.</param>
		/// <returns>True if both set of postprocess steps are equivalent, false otherwise.</returns>
		public static bool CompareFlags(aiPostProcessSteps flags1, aiPostProcessSteps flags2)
		{
			return UsedSteps(flags1) == UsedSteps(flags2);
		}
		#endregion
	}
}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
