using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//-------------------------------------------------------------------------------
// Namespace Armine.Model
//-------------------------------------------------------------------------------
namespace Armine.Model
{
	/// <summary>
	/// Importer class.
	/// 
	/// This class register and manage all existing importers modules.
	/// </summary>
	public sealed class Importer : Module.Manager<Module.IImporter>
	{
		#region Members
		internal const float unityLoadingPercentage = 0.15f;

		private Dictionary<string, byte[]> registeredTextures;
		private bool importing;
		#endregion

		#region Constructors
		/// <summary>
		/// Constructor of importer class.
		/// </summary>
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
		/// <summary>
		/// Get or set the with to use for line geometries.
		/// </summary>
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
		/// <summary>
		/// Get the Assimp importer.
		/// </summary>
		public Module.Import.Assimp Assimp
		{
			get
			{
				return (Module.Import.Assimp) modules[Constants.assimpModule];
			}
		}
#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

		/// <summary>
		/// Get the binary importer.
		/// </summary>
		public Module.Import.Binary Binary
		{
			get
			{
				return (Module.Import.Binary) modules[Constants.binaryModule];
			}
		}
		#endregion

		#region Texture registration
		/// <summary>
		/// Register an external texture. This utility helps making the link to external textures referenced
		/// by a file name, even when the file is not directly available / accessible.
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="data"></param>
		/// <returns></returns>
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

		/// <summary>
		/// Get the texture referenced by a given filename, including those previously registered.
		/// </summary>
		/// <param name="filename">The file name to reference the texture.</param>
		/// <returns>The texture as a byte array (no decoding done).</returns>
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
		/// <summary>
		/// Delegate for functions to use to notify completion of an import task.
		/// </summary>
		/// <param name="root"></param>
		public delegate void ReturnCallback(GameObject root);

		/// <summary>
		/// Import a file synchronously.
		/// </summary>
		/// <param name="filename">The name and path of the file to import.</param>
		/// <returns>The root gameobject of the imported geometries.</returns>
		public GameObject Import(string filename)
		{
			GameObject result = null;

			IEnumerator it = Import(filename, go => result = go);

			while(it.MoveNext());

			return result;
		}

		/// <summary>
		/// Import a file synchronously from a given byte array as source.
		/// </summary>
		/// <remarks>
		/// The file name is used mainly to determine wich importer to use based on the extension,
		/// as well as for logging and error reporting purposes.
		/// </remarks>
		/// <param name="filename">The name of the file to import.</param>
		/// <param name="data">The array containg the data to import.</param>
		/// <returns>The root gameobject of the imported geometries.</returns>
		public GameObject Import(string filename, byte[] data)
		{
			GameObject result = null;

			IEnumerator it = Import(filename, data, go => result = go);

			while(it.MoveNext());

			return result;
		}
		#endregion

		#region Import implementation
		/// <summary>
		/// Import a file asynchronously.
		/// </summary>
		/// <remarks>
		/// This method must be used as a coroutine. Failure to do so whould result in no import whatsoever. In particular,
		/// for successful import, the returned iterator must be itered over until it's end.
		/// </remarks>
		/// <param name="filename">The name and path of the file to import.</param>
		/// <param name="return_callback">The callback that will be called on import completion.</param>
		/// <param name="progress_callback">The callback that will be called periodically during import to notify current progress.</param>
		/// <returns>An iterator to use in a coroutine.</returns>
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

		/// <summary>
		/// Import a file asynchronously from a given byte array as source.
		/// </summary>
		/// <remarks>
		/// This method must be used as a coroutine. Failure to do so whould result in no import whatsoever. In particular,
		/// for successful import, the returned iterator must be itered over until it's end.
		/// 
		/// The file name is used mainly to determine wich importer to use based on the extension,
		/// as well as for logging and error reporting purposes.
		/// </remarks>
		/// <param name="filename">The name of the file to import.</param>
		/// <param name="data">The array containg the data to import.</param>
		/// <param name="return_callback">The callback that will be called on import completion.</param>
		/// <param name="progress_callback">The callback that will be called periodically during import to notify current progress.</param>
		/// <returns>An iterator to use in a coroutine.</returns>
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

			// Ready to accept new imports
			lock(this)
			{
				importing = false;
			}
		}
		#endregion
	}
}
