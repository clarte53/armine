#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Armine.UI.File
{
	[Serializable]
	public abstract class Selector
	{
		#region Constants
		protected const string directoryPref = "LastOpenedDirectory";
		#endregion

		#region Members
		[SerializeField]
		protected List<string> files;

		[SerializeField]
		protected bool modified;
		#endregion
		
		#region Constructors
		public Selector()
		{
			files = new List<string>();
		}
		#endregion

		#region Abstract methods
		public abstract void DisplaySelector(string directory, string filename, string extensions);

		public virtual void UpdateSelector()
		{

		}
		#endregion

		#region GetterSetter
		public List<string> Filenames
		{
			get
			{
				return files;
			}
		}

		public bool Modified
		{
			get
			{
				return modified;
			}

			set
			{
				modified = value;
			}
		}

		public bool HasFile()
		{
			return files.Count > 0;
		}

		public string First()
		{
			return (HasFile() ? files[0] : "");
		}

		public string Last()
		{
			return (HasFile() ? files[files.Count - 1] : "");
		}
		#endregion

		#region PlayerPrefs
		public string Load()
		{
			string path = System.IO.Directory.GetCurrentDirectory();
			
			if(PlayerPrefs.HasKey(directoryPref))
			{
				string path_pref = PlayerPrefs.GetString(directoryPref);
				
				if(System.IO.Directory.Exists(path_pref))
				{
					path = path_pref;
				}
			}

			return path;
		}

		public void Save()
		{
			if(Modified && HasFile())
			{
				string file = Last();
				
				if(file.Length != 0)
				{
					PlayerPrefs.SetString(directoryPref, System.IO.Path.GetDirectoryName(file));
				}
			}
		}
		#endregion

		#region Configuration
		public void DisplayConfiguration(string extensions, bool multiple_selection, params GUILayoutOption[] options)
		{
			DisplayConfiguration("", extensions, multiple_selection, options);
		}

		public virtual void DisplayConfiguration(string filename, string extensions, bool multiple_selection, params GUILayoutOption[] options)
		{
			if(files != null)
			{
				GUILayout.BeginVertical();

				if(multiple_selection)
				{
					List.DisplayConfiguration(files, options);
				}
				else
				{
					if(HasFile())
					{
						files[0] = GUILayout.TextField(files[0], options);
					}
					else
					{
						string file = GUILayout.TextField("", options);
						
						if(file.Length != 0)
						{
							files.Add(file);
						}
					}
				}

				GUILayout.BeginHorizontal();

				bool browse = GUILayout.Button("Browse");

				if(GUILayout.Button("Clear"))
				{
					files.Clear();

					modified = true;
				}

				GUILayout.EndHorizontal();

				if(browse)
				{
					string path = Load();
					
					DisplaySelector(path, filename, extensions);
				}

				UpdateSelector();

				Save();

				GUILayout.EndVertical();
			}
		}
		#endregion
	}
}

#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
