#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Armine.Utils
{
	internal class DependenciesLoader : IDisposable
	{
		#region Members
		protected const string pluginsFolder = "Plugins";

		protected Dictionary<IntPtr, string> loadedLibraries;
		protected bool disposed;
		#endregion

		#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		[Flags]
		enum LoadLibraryFlags : uint
		{     
			DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
			LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
			LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
			LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
			LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
			LOAD_LIBRARY_SEARCH_APPLICATION_DIR = 0x00000200,
			LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000,
			LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100,
			LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800,
			LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400,
			LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
		}
		
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr LoadLibraryEx(string filename, IntPtr reserved, LoadLibraryFlags flags);
		
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool FreeLibrary(IntPtr module);
		#endif

		#region Constructors / Destructors
		internal DependenciesLoader()
		{
			try
			{
				string base_path = Application.dataPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
				string arch = null;

				#if UNITY_EDITOR
				//  Editor has 64 bit or 32 build target 
				if(IntPtr.Size == 4)
				{
					arch = "x86";
				}
				else if(IntPtr.Size == 8)
				{
					arch = "x86_64";
				}
				#endif

				List<string> directories = new List<string>();

				foreach(string plugin_dir in Directory.GetDirectories(base_path, pluginsFolder, SearchOption.AllDirectories))
				{
					if(! string.IsNullOrEmpty(arch))
					{
						foreach(string arch_dir in Directory.GetDirectories(plugin_dir, arch, SearchOption.TopDirectoryOnly))
						{
							directories.Add(arch_dir);
						}
					}
					else
					{
						directories.Add(plugin_dir);
					}
				}

				Queue<FileInfo> queued_libraries = new Queue<FileInfo>();
				
				foreach(string directory in directories)
				{
					#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
					foreach(string file in Directory.GetFiles(directory))
					{
						FileInfo file_info = new FileInfo(file);
						
						if(file_info.Extension.TrimStart('.').ToLower() == "dll" && ! file_info.Name.Contains("MiddleVR"))
						{
							queued_libraries.Enqueue(file_info);
						}
					}
					#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
					#if UNITY_STANDALONE_LINUX
					const string env_path = "LD_LIBRARY_PATH";
					#else
					const string env_path = "DYLD_FALLBACK_LIBRARY_PATH";
					#endif
					
					string current_path = Environment.GetEnvironmentVariable(env_path, EnvironmentVariableTarget.Process);
					
					if(! current_path.Contains(directory))
					{
						Environment.SetEnvironmentVariable(env_path, current_path + Path.PathSeparator + directory, EnvironmentVariableTarget.Process);
					}
					#else
					throw new System.NotImplementedException("The current platform does not support setting the dynamic libraries path.");
					#endif
				}
				
				if(queued_libraries != null && queued_libraries.Count > 0)
				{
					loadedLibraries = new Dictionary<IntPtr, string>();
					
					int remaining = queued_libraries.Count;
					int counter = 0;
					
					// Try to load each dll in turn, until all dlls are loaded, or all remaining dll can not be loaded (eliminate order effect)
					while(queued_libraries.Count > 0 && remaining > counter)
					{
						FileInfo file_info = queued_libraries.Dequeue();
						
						IntPtr ptr = LoadLibraryEx(file_info.FullName, IntPtr.Zero, LoadLibraryFlags.LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR);
						
						if(ptr != IntPtr.Zero)
						{
							loadedLibraries.Add(ptr, file_info.Name);
							
							//Debug.LogFormat(string.Format("Loaded library '{0}'", file_info.Name));
						}
						else
						{
							queued_libraries.Enqueue(file_info);
						}
						
						if(queued_libraries.Count != remaining)
						{
							remaining = queued_libraries.Count;
							counter = 0;
						}
						else{
							counter++;
						}
					}
					
					while(queued_libraries.Count > 0)
					{
						FileInfo file_info = queued_libraries.Dequeue();
						
						int error_code = Marshal.GetLastWin32Error();
						
						Debug.LogErrorFormat(string.Format("Failed to load library '{1}' (ErrorCode: {0})", error_code, file_info.Name));
					}
				}
			}
			catch(Exception)
			{
				// We don't do anything. Users will have to move dlls manually like before
			}
		}

		~DependenciesLoader()
		{
			if(! disposed)
			{
				Dispose();
			}
		}
		#endregion

		#region IDisposable implementation
		public void Dispose()
		{
			if(disposed)
			{
				return;
			}

			#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			if(loadedLibraries != null)
			{
				foreach(KeyValuePair<IntPtr, string> library in loadedLibraries)
				{
					if(FreeLibrary(library.Key))
					{
						//Debug.LogFormat ("Unloaded library '{0}'", library.Value);
					}
				}
			}
			#endif
			
			disposed = true;
		}
		#endregion
	}
}

#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
