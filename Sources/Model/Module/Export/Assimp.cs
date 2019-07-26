#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Armine.Model.Type;
using Armine.Utils;
using CLARTE.Threads;
using Assimp;

namespace Armine.Model.Module.Export
{
	/// <summary>
	/// Assimp exporter module.
	/// </summary>
	public class Assimp : IExporter
	{
		/// <summary>
		/// Structure containing the current info required by the different export methods.
		/// </summary>
		public class Context
		{
			#region Members
			/// <summary>
			/// The Assimp exporter used for export.
			/// </summary>
			public global::Assimp.Exporter exporter;

			/// <summary>
			/// A thread pool to execute asynchronous tasks.
			/// </summary>
			/// <remarks>
			/// A dedicated pool is used as many tasks are launched in batches, and the importer need to be able
			/// to await for the completion of all tasks in a batch before moving forward to the next steps of export.
			/// </remarks>
			public Pool threads;

			/// <summary>
			/// The progress callback used to notify the caller of export progress.
			/// </summary>
			public Progress progress;

			/// <summary>
			/// The Assimp scene object representing the exported data.
			/// </summary>
			public aiScene scene;

			/// <summary>
			/// A dictionary for mapping meshes instances to index identifiers.
			/// </summary>
			public Dictionary<Mesh, uint> meshes;
			#endregion

			#region Constructors
			/// <summary>
			/// Constructor for context structure.
			/// </summary>
			/// <param name="e">The Assimp exporter to use for export.</param>
			public Context(global::Assimp.Exporter e)
			{
				threads = new Pool();
				progress = new Progress();
				meshes = new Dictionary<Mesh, uint>();

				exporter = e;

				Clean();
			}
			#endregion

			#region Public methods
			/// <summary>
			/// Clean the content of the structure fields to get ready for next export.
			/// </summary>
			public void Clean()
			{
				scene = null;

				meshes.Clear();

				progress.Clean();
			}
			#endregion
		}

		/// <summary>
		/// Intermediate representation to associate meshes and submeshes indexes to material indexes.
		/// </summary>
		public struct Mesh
		{
			#region Members
			/// <summary>
			/// The index of the mesh.
			/// </summary>
			public int mesh;

			/// <summary>
			/// The index of the submesh.
			/// </summary>
			public int submesh;

			/// <summary>
			/// The index of the material.
			/// </summary>
			public int material;
			#endregion

			#region Constructors
			/// <summary>
			/// Constructor of the intermediate mesh descriptor structure.
			/// </summary>
			/// <param name="mesh_index"></param>
			/// <param name="submesh_index"></param>
			/// <param name="material_index"></param>
			public Mesh(int mesh_index, int submesh_index, int material_index)
			{
				mesh = mesh_index;
				submesh = submesh_index;
				material = material_index;
			}
			#endregion

			#region Comparison functions
			/// <summary>
			/// Generate hash code for the descriptor.
			/// </summary>
			/// <returns>The generated hash code.</returns>
			public override int GetHashCode()
			{
				const int int_size = 32;
				const int offset1 = 19;
				const int offset2 = 24; // ie no collisions if nb mesh < 524288, submesh < 32 and nb materials < 256

				int a = mesh.GetHashCode();
				int b = submesh.GetHashCode();
				int c = material.GetHashCode();

				return a ^ ((b << offset1) | (b >> (int_size - offset1))) ^ ((c << offset2) | (c >> (int_size - offset2)));
			}

			/// <summary>
			/// Test if two objects are equals.
			/// </summary>
			/// <param name="other">The other object to comparate against.</param>
			/// <returns>True if both objects are Mesh descriptors and have the same values, false otherwise.</returns>
			public override bool Equals(object other)
			{
				return other is Mesh && this == (Mesh) other;
			}

			/// <summary>
			/// Compare two descriptors component wise.
			/// </summary>
			/// <param name="a">The first descriptor to compare.</param>
			/// <param name="b">The second descriptor to compare.</param>
			/// <returns>True if both descriptors have the same values.</returns>
			public static bool operator ==(Mesh a, Mesh b)
			{
				return a.mesh == b.mesh && a.submesh == b.submesh && a.material == b.material;
			}

			/// <summary>
			/// Test if two descriptors have different values.
			/// </summary>
			/// <param name="a">The first descriptor to compare.</param>
			/// <param name="b">The second descriptor to compare.</param>
			/// <returns>True if both descriptors have at least one different value, false otherwise.</returns>
			public static bool operator !=(Mesh a, Mesh b)
			{
				return !(a == b);
			}
			#endregion
		}

		#region Members
		private const aiPostProcessSteps mandatoryPostProcessSteps =
			aiPostProcessSteps.aiProcess_MakeLeftHanded |
			aiPostProcessSteps.aiProcess_FlipWindingOrder |
			aiPostProcessSteps.aiProcess_ValidateDataStructure |
			(aiPostProcessSteps) 0;

		// Forbiden post processing steps
		private const aiPostProcessSteps forbidenPostProcessSteps =
			aiPostProcessSteps.aiProcess_FlipUVs |
			(aiPostProcessSteps) 0;

		// User defined post processing steps
		private aiPostProcessSteps postProcessSteps =
			(aiPostProcessSteps) 0;

		private static string[] extensions = null;

		private global::Assimp.Exporter assimpExporter;
		private Context context;
		#endregion

		#region Constructors
		/// <summary>
		/// Constructor of Assimp exporter module.
		/// </summary>
		public Assimp()
		{
			context = new Context(Exporter);
		}
		#endregion

		#region Getter / setter
		private global::Assimp.Exporter Exporter
		{
			get
			{
				if(isDisposed)
				{
					throw new ObjectDisposedException(GetType().FullName);
				}

				if(assimpExporter == null)
				{
					assimpExporter = new global::Assimp.Exporter();
				}

				return assimpExporter;
			}
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

					if(assimpExporter != null)
					{
						assimpExporter.Dispose();
						assimpExporter = null;
					}

					if(context != null)
					{
						context.threads.Dispose();
						context.threads = null;
						context.exporter = null;

						context.meshes.Clear();
					}
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
		/// Release the ressources used by the exporter.
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

		#region IExporter implementation
		/// <summary>
		/// Provide the list of file extensions supported by this exporter module.
		/// </summary>
		/// <returns>The list of supported extensions.</returns>
		public string[] GetSupportedExtensions()
		{
			if(extensions == null)
			{
				uint export_format_count = Exporter.GetExportFormatCount();

				extensions = new string[export_format_count];

				for(uint i = 0; i < export_format_count; i++)
				{
					using(aiExportFormatDesc desc = Exporter.GetExportFormatDescription(i))
					{
						extensions[i] = desc.fileExtension.ToLower();
					}
				}
			}

			return extensions;
		}

		/// <summary>
		/// Export data asynchronously to a destination file.
		/// </summary>
		/// <param name="scene">The scene representation to export.</param>
		/// <param name="filename">The file to export to.</param>
		/// <param name="return_callback">The calback used to notify the caller when the export is completed.</param>
		/// <param name="progress_callback">The callback to regularly notify the caller of the export progress.</param>
		/// <returns>An iterator to use inside a coroutine.</returns>
		public IEnumerator ExportToFile(Scene scene, string filename, ExporterSuccessCallback return_callback, ProgressCallback progress_callback)
		{
			return scene.ToAssimp(context, filename, UsedSteps(postProcessSteps), return_callback, progress_callback);
		}

		/// <summary>
		/// Export data asynchronously to a byte array.
		/// </summary>
		/// <param name="scene">The scene representation to export.</param>
		/// <param name="filename">The name of the file corresponding to the exported data. The extension is used to determine which codec use.</param>
		/// <param name="return_callback">The calback used to notify the caller when the export is completed.</param>
		/// <param name="progress_callback">The callback to regularly notify the caller of the export progress.</param>
		/// <returns>An iterator to use inside a coroutine.</returns>
		public IEnumerator ExportToBytes(Scene scene, string filename, ExporterReturnCallback return_callback, ProgressCallback progress_callback)
		{
			// Assimp currently does not support export to byte array directly
			bool success = false;

			// Create a temporary file to export the data
			string tmp_file = Path.GetTempPath() + Path.GetFileName(filename);

			// Export to the temporary file
			IEnumerator it = ExportToFile(scene, tmp_file, result => success = result, progress_callback);

			while(it.MoveNext())
			{
				yield return it.Current;
			}

			FileInfo file_info = new FileInfo(tmp_file);
			
			if(file_info.Exists)
			{
				// Read the exported data and send it back to the caller
				if(success && return_callback != null)
				{
					return_callback(File.ReadAllBytes(file_info.FullName));
				}

				// Remove the temporary file if it exists
				file_info.Delete();
			}
		}
		#endregion

		#region Flags
		/// <summary>
		/// Enable or disable a set of postprocess steps.
		/// </summary>
		/// <param name="flags">The postprocess steps to set, as bit flags.</param>
		/// <param name="state">The desired state for the postprocess steps. True will enable the steps, false will disable them.</param>
		public void ChangeFlag(aiPostProcessSteps step, bool state)
		{
			postProcessSteps = (aiPostProcessSteps) Option.Flags.Toogle((int) postProcessSteps, (int) step, state);
		}

		/// <summary>
		/// Test if at least one of the postprocess steps in a set is enabled or not.
		/// </summary>
		/// <param name="flags">The postprocess steps to check, as bit flags.</param>
		/// <returns>True if at least one of the postprocess steps is enabled, false otherwise.</returns>
		public bool IsFlagSet(aiPostProcessSteps step)
		{
			return Option.Flags.IsSet((int) postProcessSteps, (int) step);
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
		#endregion
	}
}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
