#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/*
    File browser for selecting files or folders at runtime.
 */
namespace Armine.UI.File
{
	//------------------------------------------------------------
	// Implementation of Armine.Model.UI.File.Selector
	//------------------------------------------------------------
	[Serializable]
	public class BrowserSelector : Selector
	{
		[SerializeField]
		private Browser fileBrowser;

		protected virtual void Repaint()
		{

		}

		private void FileSelectedCallback(string path)
		{
			fileBrowser = null;

			if(path != null)
			{
				files.AddRange(path.Split(';'));
				Modified = true;
			}
		}

		public override void DisplaySelector(string directory, string filename, string extensions)
		{
			if(fileBrowser == null)
			{
				files.Clear();

				fileBrowser = new Browser(FileSelectedCallback);

				fileBrowser.DirectorySelectionPattern = "*";
				fileBrowser.FileSelectionPattern = extensions;
				fileBrowser.SetNewDirectory(directory);
				fileBrowser.SwitchDirectoryNow();
			}
		}

		public override void UpdateSelector()
		{
			if(fileBrowser != null)
			{
				if(fileBrowser.isDirty)
				{
					Repaint();
				}
				
				fileBrowser.OnGUI();
			}
		}
	}
	//------------------------------------------------------------

	[Serializable]
	internal enum BrowserType
	{
		FILE,
		DIRECTORY,
		LOGICAL_DRIVE
	}

	[Serializable]
	internal class Browser
	{
		// Called when the user clicks cancel or select
		internal delegate void FinishedCallback(string path);
		// Defaults to working directory
		internal string CurrentDirectory
		{
			get
			{
				return currentDir;
			}
			set
			{
				SetNewDirectory(value);
				SwitchDirectoryNow();
			}
		}

		[SerializeField]
		private string currentDir;

		// Optional pattern for filtering selectable files/folders. See:
		// http://msdn.microsoft.com/en-us/library/wz42302f(v=VS.90).aspx
		// and
		// http://msdn.microsoft.com/en-us/library/6ff71z1w(v=VS.90).aspx
		internal string FileSelectionPattern
		{
			get
			{
				return filePattern;
			}
			set
			{
				filePattern = value;
				ReadDirectoryContents(currentDir, out currentDirectoryParts);
			}
		}

		[SerializeField]
		private string filePattern;

		internal string DirectorySelectionPattern
		{
			get
			{
				return directoryPattern;
			}
			set
			{
				directoryPattern = value;
				ReadDirectoryContents(currentDir, out currentDirectoryParts);
			}
		}

		[SerializeField]
		private string directoryPattern;

		// Optional image for directories
		internal Texture2D DirectoryImage
		{
			get
			{
				return dirImage;
			}
			set
			{
				dirImage = value;
				BuildContent();
			}
		}

		[SerializeField]
		private Texture2D dirImage;

		// Optional image for files
		internal Texture2D FileImage
		{
			get
			{
				return filenameImage;
			}
			set
			{
				filenameImage = value;
				BuildContent();
			}
		}

		[SerializeField]
		private Texture2D filenameImage;

		// Browser type. Defaults to File, but can be set to Folder
		internal BrowserType BrowserType
		{
			get
			{
				return type;
			}
			set
			{
				type = value;
				ReadDirectoryContents(currentDir, out currentDirectoryParts);
			}
		}

		[SerializeField]
		private BrowserType type;

		[SerializeField]
		private string newDirectory;

		[SerializeField]
		private string[] newDirectoryParts;

		[SerializeField]
		private string[] currentDirectoryParts;

		[SerializeField]
		private string[] files;

		[SerializeField]
		private GUIContent[] filesWithImages;

		[SerializeField]
		private List<int> selectedFile;

		[SerializeField]
		private string[] nonMatchingFiles;

		[SerializeField]
		private GUIContent[] nonMatchingFilesWithImages;

		[SerializeField]
		private List<int> selectedNonMatchingDirectory;

		[SerializeField]
		private string[] directories;

		[SerializeField]
		private GUIContent[] directoriesWithImages;

		[SerializeField]
		private List<int> selectedDirectory;

		[SerializeField]
		private string[] nonMatchingDirectories;

		[SerializeField]
		private GUIContent[] nonMatchingDirectoriesWithImages;

		[SerializeField]
		private bool currentDirectoryMatches;

		[SerializeField]
		internal bool isDirty;

		private GUIStyle CentredText
		{

			get
			{
				if(textCentered == null)
				{
					textCentered = new GUIStyle(GUI.skin.label);
					textCentered.alignment = TextAnchor.MiddleLeft;
					textCentered.fixedHeight = GUI.skin.button.fixedHeight;
				}
				return textCentered;
			}
		}

		[SerializeField]
		private GUIStyle textCentered;

		[SerializeField]
		private Vector2 scrollPosition;

		[SerializeField]
		private FinishedCallback callback;

		// Browsers need at least a rect, name and callback
		internal Browser(FinishedCallback finished_callback)
		{
			type = BrowserType.FILE;
			callback = finished_callback;

			SetNewDirectory(Directory.GetCurrentDirectory());

			SwitchDirectoryNow();
		}

		internal void SetNewDirectory(string directory)
		{
			newDirectory = directory;
		}

		internal void SwitchDirectoryNow()
		{
			if(newDirectory == null || currentDir == newDirectory)
			{
				return;
			}

			scrollPosition = Vector2.zero;
			selectedDirectory = selectedNonMatchingDirectory = selectedFile = new List<int>();

			try
			{
				ReadDirectoryContents(newDirectory, out newDirectoryParts);
				currentDir = newDirectory;
				currentDirectoryParts = newDirectoryParts;
				newDirectoryParts = null;
			}
			catch(UnauthorizedAccessException e)
			{
				Debug.Log("You don't have the right to access this folder apparently" + e.Message);
			}
			catch(DirectoryNotFoundException e)
			{
				Debug.Log("Cannot find the repository " + e.Message);
			}
			finally
			{
				newDirectory = null;
			}

			isDirty = true;
		}

		private void ReadDirectoryContents(string current_directory, out string[] new_directory_parts)
		{
			if(current_directory == "/")
			{
				new_directory_parts = new string[] { "" };
				currentDirectoryMatches = false;
			}
			else
			{
				string[] drives = Directory.GetLogicalDrives();

				if(Array.IndexOf(drives, current_directory) != -1)
				{
					BrowserType = BrowserType.LOGICAL_DRIVE;
					//currentDirectory = " Pick Logical Drive to Browse";
				}

				//currentDirectory= currentDirectory.Trim(Path.GetInvalidFileNameChars());
				//currentDirectory = currentDirectory.Replace(" ", "_");
				Path.GetInvalidFileNameChars();

				new_directory_parts = current_directory.Split(Path.DirectorySeparatorChar);

				if(DirectorySelectionPattern != null)
				{
					string[] generation = new string[0];

					if(BrowserType != BrowserType.LOGICAL_DRIVE)
					{
						generation = Directory.GetDirectories(Path.GetDirectoryName(current_directory), DirectorySelectionPattern);
					}
					currentDirectoryMatches = Array.IndexOf(generation, current_directory) >= 0;
				}
				else
				{
					currentDirectoryMatches = false;
				}
			}

			if(BrowserType == BrowserType.FILE || DirectorySelectionPattern == null)
			{
				directories = Directory.GetDirectories(current_directory);
				nonMatchingDirectories = new string[0];
			}
			else if(BrowserType == BrowserType.LOGICAL_DRIVE)
			{
				directories = Directory.GetLogicalDrives();
				nonMatchingDirectories = new string[0];
				nonMatchingFiles = new string[0];
			}
			else
			{
				directories = Directory.GetDirectories(current_directory, DirectorySelectionPattern);
				string[] all_directory = Directory.GetDirectories(current_directory);
				var non_matching_directories = new List<string>();

				foreach(string directoryPath in all_directory)
				{
					if(Array.IndexOf(directories, directoryPath) < 0)
					{
						non_matching_directories.Add(directoryPath);
					}
				}

				nonMatchingDirectories = non_matching_directories.ToArray();

				for(int i = 0; i < nonMatchingDirectories.Length; ++i)
				{
					int last_separator = nonMatchingDirectories[i].LastIndexOf(Path.DirectorySeparatorChar);
					nonMatchingDirectories[i] = nonMatchingDirectories[i].Substring(last_separator + 1);
				}

				Array.Sort(nonMatchingDirectories);

			}

			if((BrowserType != BrowserType.LOGICAL_DRIVE))
			{
				for(int i = 0; i < directories.Length; ++i)
				{
					directories[i] = directories[i].Substring(directories[i].LastIndexOf(Path.DirectorySeparatorChar) + 1);
				}
			}

			if(BrowserType == BrowserType.DIRECTORY || FileSelectionPattern == null)
			{
				files = Directory.GetFiles(current_directory);
				nonMatchingFiles = new string[0];
			}
			else
			{
				// ArrayList will hold all file names
				System.Collections.ArrayList al_files = new System.Collections.ArrayList();
				
				// Create an array of filter string
				string[] multiple_filters = FileSelectionPattern.Split(';');
				
				// for each filter find mathing file names
				foreach(string FileFilter in multiple_filters)
				{
					// add found file names to array list
					al_files.AddRange(Directory.GetFiles(current_directory, FileFilter, SearchOption.TopDirectoryOnly));
				}
				
				// get the string array of relevant file names
				files = (string[]) al_files.ToArray(typeof(string));

				var non_matching_files = new List<string>();

				foreach(string filePath in Directory.GetFiles(current_directory))
				{
					if(Array.IndexOf(files, filePath) < 0)
					{
						non_matching_files.Add(filePath);
					}
				}

				nonMatchingFiles = non_matching_files.ToArray();

				for(int i = 0; i < nonMatchingFiles.Length; ++i)
				{
					nonMatchingFiles[i] = Path.GetFileName(nonMatchingFiles[i]);
				}

				Array.Sort(nonMatchingFiles);
			}

			for(int i = 0; i < files.Length; ++i)
			{
				files[i] = Path.GetFileName(files[i]);
			}

			Array.Sort(files);

			BuildContent();
		}

		private void BuildContent()
		{
			directoriesWithImages = new GUIContent[directories.Length];
			for(int i = 0; i < directoriesWithImages.Length; ++i)
			{
				directoriesWithImages[i] = new GUIContent(directories[i], DirectoryImage);
			}
			nonMatchingDirectoriesWithImages = new GUIContent[nonMatchingDirectories.Length];
			for(int i = 0; i < nonMatchingDirectoriesWithImages.Length; ++i)
			{
				nonMatchingDirectoriesWithImages[i] = new GUIContent(nonMatchingDirectories[i], DirectoryImage);
			}
			filesWithImages = new GUIContent[files.Length];
			for(int i = 0; i < filesWithImages.Length; ++i)
			{
				filesWithImages[i] = new GUIContent(files[i], FileImage);
			}
			nonMatchingFilesWithImages = new GUIContent[nonMatchingFiles.Length];
			for(int i = 0; i < nonMatchingFilesWithImages.Length; ++i)
			{
				nonMatchingFilesWithImages[i] = new GUIContent(nonMatchingFiles[i], FileImage);
			}
		}

		internal void OnGUI()
		{
			GUISkin old_skin = GUI.skin;
			GUI.skin = (GUISkin) Resources.Load("Skins/Browser");

			GUI.skin.horizontalScrollbar = old_skin.horizontalScrollbar;
			GUI.skin.horizontalScrollbarLeftButton = old_skin.horizontalScrollbarLeftButton;
			GUI.skin.horizontalScrollbarRightButton = old_skin.horizontalScrollbarRightButton;
			GUI.skin.horizontalScrollbarThumb = old_skin.horizontalScrollbarThumb;
			GUI.skin.verticalScrollbar = old_skin.verticalScrollbar;
			GUI.skin.verticalScrollbarUpButton = old_skin.verticalScrollbarUpButton;
			GUI.skin.verticalScrollbarDownButton = old_skin.verticalScrollbarDownButton;
			GUI.skin.verticalScrollbarThumb = old_skin.verticalScrollbarThumb;

			//GUILayout.BeginArea(m_screenRect, m_name, GUI.skin.window);
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal(GUI.skin.GetStyle("box"), GUILayout.Height(GUI.skin.label.CalcHeight(new GUIContent(""), 1)));

			for(int parent_index = 0; parent_index < currentDirectoryParts.Length; ++parent_index)
			{
				if(parent_index == currentDirectoryParts.Length - 1)
				{
					GUILayout.Label(currentDirectoryParts[parent_index], GUI.skin.GetStyle("ClickableText"));
				}
				else if(GUILayout.Button(currentDirectoryParts[parent_index], GUI.skin.GetStyle("ClickableText")))
				{
					string parent_directory_name = currentDir;

					for(int i = currentDirectoryParts.Length - 1; i > parent_index; --i)
					{
						parent_directory_name = Path.GetDirectoryName(parent_directory_name);
					}

					SetNewDirectory(parent_directory_name);
				}

				GUILayout.Label("\\");
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box);

			if(BrowserType == BrowserType.LOGICAL_DRIVE)
			{
				selectedDirectory = BrowserLayout.SelectionList(selectedDirectory, directoriesWithImages, LogicalDriveDoubleClickCallback);
			}
			else
			{
				selectedDirectory = BrowserLayout.SelectionList(selectedDirectory, directoriesWithImages, DirectoryDoubleClickCallback);
			}

			if(selectedDirectory.Count >= 1)
			{
				selectedFile.Clear();
				selectedNonMatchingDirectory.Clear(); 
			}

			selectedNonMatchingDirectory = BrowserLayout.SelectionList(selectedNonMatchingDirectory, nonMatchingDirectoriesWithImages, NonMatchingDirectoryDoubleClickCallback);

			if(selectedNonMatchingDirectory.Count >= 1)
			{
				selectedDirectory.Clear();
				selectedFile.Clear();
			}

			GUI.enabled = BrowserType == BrowserType.FILE;
			selectedFile = BrowserLayout.SelectionList(selectedFile, filesWithImages, FileDoubleClickCallback);
			GUI.enabled = true;

			if(selectedFile.Count >= 1)
			{
				selectedDirectory.Clear();
				selectedNonMatchingDirectory.Clear();
			}

			GUI.enabled = false;
			BrowserLayout.SelectionList(null, nonMatchingFilesWithImages);
			GUI.enabled = true;

			GUILayout.EndScrollView();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if(GUILayout.Button("Cancel", GUILayout.Width(50)))
			{
				callback(null);
			}
			if(BrowserType == BrowserType.FILE)
			{
				GUI.enabled = selectedFile.Count >= 1;
			}
			else
			{
				if(FileSelectionPattern == null && DirectorySelectionPattern == null)
				{
					GUI.enabled = selectedDirectory.Count > 0;
				}
				else
				{
					GUI.enabled = selectedDirectory.Count > 0 || (currentDirectoryMatches && selectedNonMatchingDirectory.Count == 0 && selectedFile.Count == 0);
				}
			}

			if(GUILayout.Button("Select", GUILayout.Width(50)))
			{
				if(BrowserType == BrowserType.FILE)
				{
					string result_path = "";

					foreach(int indice in  selectedFile)
					{
						if(result_path != "")
						{
							result_path = String.Concat(result_path, ";", Path.Combine(currentDir, files[indice]));
						}
						else
						{
							result_path = Path.Combine(currentDir, files[indice]);
						}
					}
					callback(result_path);
				}
				else
				{
					if(selectedDirectory.Count > 0)
					{
						callback(Path.Combine(currentDir, directories[0]));
					}
					else
					{
						callback(currentDir);
					}
				}
			}

			GUI.enabled = true;

			GUILayout.EndHorizontal();
			GUILayout.EndVertical();

			if(Event.current.type == EventType.Repaint)
			{
				SwitchDirectoryNow();
			}

			GUI.skin = old_skin;
		}

		private void FileDoubleClickCallback(int[] i)
		{
			if(BrowserType == BrowserType.FILE)
			{
				string path = "";

				foreach(int indice in i)
				{
					if(path != "")
					{
						path = string.Concat(";", Path.Combine(currentDir, files[indice]));
					}
					else
					{
						path = Path.Combine(currentDir, files[indice]);
					}
				}

				callback(path);
			}
		}

		private void DirectoryDoubleClickCallback(IList<int> i)
		{
			SetNewDirectory(Path.Combine(currentDir, directories[i[0]]));
		}

		private void LogicalDriveDoubleClickCallback(IList<int> i)
		{
			///GetDrives is Not Implemented :/

			DriveInfo[] infos = DriveInfo.GetDrives();
			foreach(DriveInfo DInfo in infos)
			{
				if(DInfo.Name == directories[i[0]])
				{
					currentDir = DInfo.RootDirectory.Name;
				}
			}
		}

		private void NonMatchingDirectoryDoubleClickCallback(IList<int> i)
		{
			SetNewDirectory(Path.Combine(currentDir, nonMatchingDirectories[i[0]]));
		}
	}
}

#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
