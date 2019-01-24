using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Armine.Utils;
using UnityEngine;

//-------------------------------------------------------------------------------
// Namespace Armine.Model
//-------------------------------------------------------------------------------
namespace Armine.Model
{
	//-------------------------------------------------------------------------------
	// Class Importer
	//-------------------------------------------------------------------------------
	public sealed class Importer : Module.Manager<Module.IImporter>
	{
		//-------------------------------------------------------------------------------
		// Class InvalidDataException
		//-------------------------------------------------------------------------------
		public class InvalidDataException : Exception
		{
			public InvalidDataException(string message) : base(message)
			{
				
			}
		}

		#region Members
		internal const float unityLoadingPercentage = 0.15f;

		private Dictionary<string, byte[]> registeredTextures;
		private bool importing;
		#endregion

		#region Constructors
		public Importer()
		{
			importing = false;

			registeredTextures = new Dictionary<string, byte[]>();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			AddModule(Constants.assimpModule, new Module.Import.Assimp(this));
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			AddModule(Constants.binaryModule, new Module.Import.Binary());
		}
		#endregion

		#region Getter / Setter
		public static float LineWidth
		{
			get
			{
				return Type.Material.unityLineWidth;
			}

			set
			{
				Type.Material.unityLineWidth = value;
			}
		}

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		public Module.Import.Assimp Assimp
		{
			get
			{
				return (Module.Import.Assimp) modules[Constants.assimpModule];
			}
		}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

		public Module.Import.Binary Binary
		{
			get
			{
				return (Module.Import.Binary) modules[Constants.binaryModule];
			}
		}
		#endregion

		#region Texture registration
		public bool RegisterTexture(string filename, byte[] data)
		{
			if(isDisposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			bool success = true;
			
			try
			{
				registeredTextures.Add(filename, data);
			}
			catch(Exception)
			{
				success = false;
			}
			
			return success;
		}

		public byte[] GetTexture(string filename)
		{
			if(isDisposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			byte[] texture;

			if(!registeredTextures.TryGetValue(filename, out texture))
			{
				texture = null;
			}

			return texture;
		}
		#endregion

		#region Import overloads
		public delegate void ReturnCallback(GameObject root);

		public GameObject Import(string filename)
		{
			GameObject result = null;

			IEnumerator it = Import(filename, go => result = go);

			while(it.MoveNext());

			return result;
		}

		public GameObject Import(string filename, byte[] data)
		{
			GameObject result = null;

			IEnumerator it = Import(filename, data, go => result = go);

			while(it.MoveNext());

			return result;
		}
		#endregion

		#region Import implementation
		public IEnumerator Import(string filename, ReturnCallback return_callback, Module.ProgressCallback progress_callback = null)
		{
			if(isDisposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			Module.ProgressCallback progress1 = null;
			Module.ProgressCallback progress2 = null;

			if(progress_callback != null)
			{
				progress1 = p => progress_callback(p * (1f - unityLoadingPercentage));
				progress2 = p => progress_callback(p * unityLoadingPercentage + (1f - unityLoadingPercentage));
			}

			string extension = Path.GetExtension(filename).Remove(0, 1).ToLower();

			Module.IImporter module;
			
			if(extensionHandler.TryGetValue(extension, out module))
			{
				return Import(result => module.ImportFromFile(filename, result, progress1), filename, return_callback, progress2);
			}
			else
			{
				Debug.LogErrorFormat("Unsupported format with extension '{0}'. No importer is registered for this format.", extension);
			}

			return null;
		}

		public IEnumerator Import(string filename, byte[] data, ReturnCallback return_callback, Module.ProgressCallback progress_callback = null)
		{
			if(isDisposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			Module.ProgressCallback progress1 = null;
			Module.ProgressCallback progress2 = null;

			if(progress_callback != null)
			{
				progress1 = p => progress_callback(p * (1f - unityLoadingPercentage));
				progress2 = p => progress_callback(p * unityLoadingPercentage + (1f - unityLoadingPercentage));
			}

			string extension = Path.GetExtension(filename).Remove(0, 1).ToLower();

			Module.IImporter module;

			if(extensionHandler.TryGetValue(extension, out module))
			{
				return Import(result => module.ImportFromBytes(filename, data, result, progress1), filename, return_callback, progress2);
			}
			else
			{
				Debug.LogErrorFormat("Unsupported format with extension '{0}'. No importer is registered for this format.", extension);
			}

			return null;
		}

		private IEnumerator Import(Func<Module.ImporterReturnCallback, IEnumerator> importer, string filename, ReturnCallback return_callback, Module.ProgressCallback progress_callback = null)
		{
			bool waiting;

			do
			{
				lock(this)
				{
					if(importing)
					{
						waiting = true;
					}
					else
					{
						importing = true;

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
			if(License.ImportIsPermitted())
#else
			if(true)
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			{
				if(importer != null)
				{
					int refresh_rate = Screen.currentResolution.refreshRate;
					float max_frame_duration = 1000.0f * 0.75f * (1.0f / (float) (refresh_rate >= 20 ? refresh_rate : 60)); // In milliseconds. Use only 75% of the available time to avoid missing vsync events

					// Create timer to break the code that must be executed in unity thread into chunks that will fit into the required target framerate
					System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

					DateTime start = DateTime.Now;

					Type.Scene scene = null;

					// Import data
					IEnumerator it = importer(s => scene = s);

					while(it.MoveNext())
					{
						yield return it.Current;
					}

					if(scene != null)
					{
						// Convert data to Unity format
						it = scene.ToUnity(progress_callback);

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

						DateTime end = DateTime.Now;

						// Add diagnostic info
						if(scene.UnityRoot != null)
						{
							int vertices_loaded = 0;
							int faces_loaded = 0;

							foreach(Type.Mesh mesh in scene.meshes)
							{
								vertices_loaded += mesh.VerticesCount;
								faces_loaded += mesh.FacesCount;
							}

							scene.UnityRoot.AddComponent<Info>().Init(filename, end.Subtract(start), vertices_loaded, faces_loaded, scene.IdMapping.Id2Go);
						}

						if(return_callback != null)
						{
							return_callback(scene.UnityRoot);
						}

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
						License.DecrementImportCount();
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
					}
					else
					{
						Debug.LogErrorFormat("Import of '{0}' failed.", filename);
					}
				}
				else
				{
					Debug.LogError("Invalid null importer.");
				}
			}
#pragma warning disable 0162
			else
			{
				Debug.LogError("Import is not possible with this license.");
			}
#pragma warning restore 0162

			// Ready to accept new imports
			lock(this)
			{
				importing = false;
			}
		}
		#endregion
	}
}
