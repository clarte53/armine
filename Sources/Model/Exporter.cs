using System;
using System.Collections;
using System.IO;
using Armine.Utils;
using UnityEngine;

//-------------------------------------------------------------------------------
// Namespace Armine.Model
//-------------------------------------------------------------------------------
namespace Armine.Model
{
	//-------------------------------------------------------------------------------
	// Class Exporter
	//-------------------------------------------------------------------------------
	public sealed class Exporter : Module.Manager<Module.IExporter>
	{
		#region Members
		private bool exporting;
		#endregion

		#region Constructors
		public Exporter()
		{
			exporting = false;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			AddModule(Constants.assimpModule, new Module.Export.Assimp());
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			AddModule(Constants.binaryModule, new Module.Export.Binary());
		}
		#endregion

		#region Getter / Setter
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		public Module.Export.Assimp Assimp
		{
			get
			{
				return (Module.Export.Assimp) modules[Constants.assimpModule];
			}
		}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

		public Module.Export.Binary Binary
		{
			get
			{
				return (Module.Export.Binary) modules[Constants.binaryModule];
			}
		}
		#endregion

		#region Export
		public bool Export(GameObject root, string filename)
		{
			bool result = false;

			IEnumerator it = Export(root, filename, sucess => result = sucess);

			while(it.MoveNext());

			return result;
		}

		public IEnumerator Export(GameObject root, string filename, Module.ExporterSuccessCallback return_callback, Module.ProgressCallback progress_callback = null)
		{
			if(isDisposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			Module.ProgressCallback progress1 = null;
			Module.ProgressCallback progress2 = null;

			if(progress_callback != null)
			{
				progress1 = p => progress_callback(p * Importer.unityLoadingPercentage);
				progress2 = p => progress_callback(p * (1f - Importer.unityLoadingPercentage) + Importer.unityLoadingPercentage);
			}

			string extension = Path.GetExtension(filename).Remove(0, 1).ToLower();

			Module.IExporter module;

			if(extensionHandler.TryGetValue(extension, out module))
			{
				return Export((scene, success) => module.ExportToFile(scene, filename, success, progress2), root, filename, return_callback, progress1);
			}
			else
			{
				Debug.LogErrorFormat("Unsupported format with extension '{0}'. No exporter is registered for this format.", extension);
			}

			return null;
		}

		private IEnumerator Export(Func<Type.Scene, Module.ExporterSuccessCallback, IEnumerator> exporter, GameObject root, string filename, Module.ExporterSuccessCallback return_callback, Module.ProgressCallback progress_callback = null)
		{
			bool success = false;

			bool waiting;

			do
			{
				lock(this)
				{
					if(exporting)
					{
						waiting = true;
					}
					else
					{
						exporting = true;

						waiting = false;
					}
				}

				if(waiting)
				{
					yield return null;
				}
			}
			while(waiting);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			if(License.ExportIsPermitted())
#else
			if(true)
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			{
				if(exporter != null)
				{
					int refresh_rate = Screen.currentResolution.refreshRate;
					float max_frame_duration = 1000.0f * 0.75f * (1.0f / (float) (refresh_rate >= 20 ? refresh_rate : 60)); // In milliseconds. Use only 75% of the available time to avoid missing vsync events

					// Create timer to break the code that must be executed in unity thread into chunks that will fit into the required target framerate
					System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

					DateTime start = DateTime.Now;

					Type.Scene scene = null;

					// Get data from Unity
					IEnumerator it = Type.Scene.FromUnity(root, s => scene = s, progress_callback);

					timer.Start();

					// Split code executed in unity thread into chunks that allow to maintain targeted framerate,
					// without loosing unnecessary time by yielding every time possible (because 1 yield <=> 1 frame)
					while(it.MoveNext())
					{
						if(timer.ElapsedMilliseconds >= max_frame_duration)
						{
							yield return null;

							timer.Reset();
							timer.Start();
						}
					}

					if(scene != null)
					{
						// Export data to final format
						it = exporter(scene, s => success = s);

						while(it.MoveNext())
						{
							yield return it.Current;
						}

						DateTime end = DateTime.Now;

						if(success)
						{
							Debug.LogFormat("Export successful: {0}.", end.Subtract(start));
						}
						else
						{
							Debug.LogErrorFormat("Export to '{0}' failed.", filename);
						}
					}
					else
					{
						Debug.LogErrorFormat("Export to '{0}' failed.", filename);
					}

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
					License.DecrementExportCount();
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
				}
				else
				{
					Debug.LogError("Invalid null exporter.");
				}
			}
#pragma warning disable 0162
			else
			{
				Debug.Log("Export is not possible with this license.");
			}
#pragma warning restore 0162

			// Ready to accept new exports
			lock(this)
			{
				exporting = false;
			}

			if(return_callback != null)
			{
				return_callback(success);
			}
		}
		#endregion
	}
}
