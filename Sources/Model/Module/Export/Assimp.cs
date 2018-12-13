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
	public class Assimp : IExporter
	{
		public class Context
		{
			#region Members
			public global::Assimp.Exporter exporter;
			public Pool threads;
			public Progress progress;
			public aiScene scene;
			public Dictionary<Mesh, uint> meshes;
			#endregion

			#region Constructors
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
			public void Clean()
			{
				scene = null;

				meshes.Clear();

				progress.Clean();
			}
			#endregion
		}

		public struct Mesh
		{
			#region Members
			public int mesh;
			public int submesh;
			public int material;
			#endregion

			#region Constructors
			public Mesh(int mesh_index, int submesh_index, int material_index)
			{
				mesh = mesh_index;
				submesh = submesh_index;
				material = material_index;
			}
			#endregion

			#region Comparison functions
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

			public override bool Equals(object other)
			{
				return other is Mesh && this == (Mesh) other;
			}

			public static bool operator ==(Mesh a, Mesh b)
			{
				return a.mesh == b.mesh && a.submesh == b.submesh && a.material == b.material;
			}

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

		public IEnumerator ExportToFile(Scene scene, string filename, ExporterSuccessCallback return_callback, ProgressCallback progress_callback)
		{
			return scene.ToAssimp(context, filename, UsedSteps(postProcessSteps), return_callback, progress_callback);
		}

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
		public void ChangeFlag(aiPostProcessSteps step, bool state)
		{
			postProcessSteps = (aiPostProcessSteps) Option.Flags.Toogle((int) postProcessSteps, (int) step, state);
		}

		public bool IsFlagSet(aiPostProcessSteps step)
		{
			return Option.Flags.IsSet((int) postProcessSteps, (int) step);
		}

		public static aiPostProcessSteps UsedSteps(aiPostProcessSteps flags)
		{
			return (flags | mandatoryPostProcessSteps) & ~forbidenPostProcessSteps;
		}
		#endregion
	}
}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
