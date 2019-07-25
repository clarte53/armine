using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Armine.UI.File;
using UnityEditor;
using UnityEngine;

namespace Armine.Editor.Windows
{
	[Serializable]
	internal class BrowserSelectorEditor : BrowserSelector
	{
		protected override void Repaint()
		{
			HandleUtility.Repaint();
		}
	}

	[Serializable]
	internal class NativeSelectorEditor : Selector
	{
		public override void DisplaySelector(string directory, string filename, string extensions)
		{
			// For some obscure reason, OpenFilePanel can't handle too many extensions...
			if(extensions.Length > 30)
			{
				extensions = "*.*";
			}

			files.Add(EditorUtility.OpenFilePanel("Open file", directory, extensions));
			Modified = true;
		}
	}

	public class Importer : EditorWindow, ISerializationCallbackReceiver
	{
		#region Members
		[NonSerialized]
		private Model.Importer importer;

		[SerializeField]
		private NativeSelectorEditor fileSelector;

		[SerializeField]
		private UI.Menu importMenu;

		[SerializeField]
		private List<GameObject> rootObjects;

		[SerializeField]
		private bool displayAssimpOptions;

		[NonSerialized]
		private TimeSpan duration;

		[SerializeField]
		private string durationStr;

		[SerializeField]
		private int vertices;

		[SerializeField]
		private int faces;
		#endregion

		#region Menus
		[MenuItem("Armine/Import %i", false, 0)]
		static public void ShowImport()
		{
			GetWindow(typeof(Importer), false, "Import");
		}
		#endregion

		#region Serialization callback
		public void OnBeforeSerialize()
		{
			durationStr = duration.Ticks.ToString();
		}
		
		public void OnAfterDeserialize()
		{
			long ticks;
			
			if(long.TryParse(durationStr, out ticks))
			{
				duration = new TimeSpan(ticks);
			}

			durationStr = null;
		}
		#endregion

		#region MonoBehaviour callbacks
		public void OnGUI()
		{
			//Initialisation/ reinit to avoid null object on runtime launch			
			if(importer == null)
			{
				importer = new Model.Importer();
			}

			if(fileSelector == null)
			{
				fileSelector = new NativeSelectorEditor();
			}

			if(importMenu == null)
			{
				importMenu = new UI.Menu();
			}

			if(rootObjects == null)
			{
				rootObjects = new List<GameObject>();
			}

			GUILayout.BeginVertical();

			GUILayout.Label("Select files to import:");

			string[] extensions = importer.SupportedExtensions;

			for(int i = 0; i < extensions.Length; i++)
			{
				extensions[i] = extensions[i].Insert(0, "*.");
			}

			fileSelector.DisplayConfiguration(string.Join(";", extensions), true);
				
			if(fileSelector.Filenames != null)
			{
				if(fileSelector.Modified)
				{
					displayAssimpOptions = false;

					foreach(string file in fileSelector.Filenames)
					{
						if(Path.GetExtension(file).Remove(0, 1).ToLower() != Constants.binaryExtension)
						{
							displayAssimpOptions = true;

							break;
						}
					}

					importMenu.Options.Init(importer, fileSelector.Filenames);
					fileSelector.Modified = false;
				}

				if(displayAssimpOptions)
				{
					importMenu.DisplayConfiguration();
				}
				else
				{
					GUILayout.FlexibleSpace();
				}

				if(GUILayout.Button("Import"))
				{
					rootObjects.Clear();
					duration = new TimeSpan(0);
					vertices = 0;
					faces = 0;

					importMenu.Options.ApplyConfiguration(importer);

					// Use serialization to create a deep copy clone
					Model.Option.Import options;
					using(Stream stream = new MemoryStream())
					{
						IFormatter formatter = new BinaryFormatter();

						formatter.Serialize(stream, importMenu.Options);

						stream.Seek(0, SeekOrigin.Begin);

						options = (Model.Option.Import) formatter.Deserialize(stream);
					}

					if(options != null)
					{
						IEnumerator it = Import(importer, options);

						while(it.MoveNext());
					}
					else
					{
						Debug.LogError("Impossible to save import options.");
					}
				}

				bool display_stats = false;
				foreach(GameObject root in rootObjects)
				{
					if(root != null)
					{
						EditorGUILayout.ObjectField(root, typeof(GameObject), false);

						display_stats = true;
					}
				}

				if(display_stats)
				{
					EditorGUILayout.LabelField("Vertices: " + vertices);
					EditorGUILayout.LabelField("Faces: " + faces);
					EditorGUILayout.LabelField("Duration: " + duration);
				}
			}

			GUILayout.EndVertical();
		}

		public void OnDestroy()
		{
			rootObjects = null;

			importMenu = null;

			fileSelector = null;

			if(importer != null)
			{
				importer.Dispose();
			}
		}
		#endregion

		#region Coroutines
		private IEnumerator Import(Model.Importer importer, Model.Option.Import options)
		{
			ProgressBar progress = new ProgressBar("Import", "Please wait during models import.");

			foreach(string file in fileSelector.Filenames)
			{
				GameObject root = null;

				IEnumerator it = importer.Import(file, go => root = go, percentage => progress.Update(percentage));
				
				while(it.MoveNext())
				{
					yield return it.Current;
				}

				if(root != null)
				{
					rootObjects.Add(root);

					Model.Info info = root.GetComponent<Model.Info>();

					info.options = options;

					vertices += info.vertices;
					faces += info.faces;
					duration += info.duration;
				}
				else
				{
					EditorUtility.DisplayDialog("Error", string.Format("Import of '{0}' failed.", file), "OK");
				}
			}

			progress.Stop();
		}
		#endregion
	}
}
