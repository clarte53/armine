#if UNITY_EDITOR_WIN

using System;
using System.Collections;
using Armine.UI.File;
using UnityEditor;
using UnityEngine;

namespace Armine.Editor.Windows
{
	[Serializable]
	internal class NativeSaverEditor : Selector
	{
		public override void DisplaySelector(string directory, string filename, string extensions)
		{
			string file = EditorUtility.SaveFilePanel("Save file", directory, filename, extensions);

			if(HasFile())
			{
				files[0] = file;
			}
			else
			{
				files.Add(file);
			}

			Modified = true;
		}

		public override void DisplayConfiguration(string filename, string extensions, bool multiple_selection, params GUILayoutOption[] options)
		{
			base.DisplayConfiguration(filename, extensions, false, options);
		}
	}

	public class Exporter : EditorWindow
	{
		#region Members
		[NonSerialized]
		private Model.Exporter exporter;

		[SerializeField]
		private NativeSaverEditor fileSelector;

		[SerializeField]
		private GameObject rootObject;

		[SerializeField]
		private bool saveAsPrefab;
		#endregion

		#region Menus
		[MenuItem("Armine/Export %e", false, 1)]
		static public void ShowExport()
		{
			EditorWindow.GetWindow(typeof(Exporter), false, "Export");
		}
		
		[MenuItem("Armine/Export %e", true)]
		static public bool ValidateShowExport()
		{
			return Utils.License.IsLicensed() && Utils.License.ExportIsPermitted();
		}
		#endregion

		#region MonoBehaviour callbacks
		public void OnGUI()
		{
			if(exporter == null)
			{
				exporter = new Model.Exporter();
			}
			
			if(fileSelector == null)
			{
				fileSelector = new NativeSaverEditor();
			}

			if(Utils.License.IsLicensed())
			{
				GUILayout.BeginVertical();
			
				GUILayout.Label("Select the root GameObject to export:");

				rootObject = (GameObject) EditorGUILayout.ObjectField(rootObject, typeof(GameObject), true);

				if(rootObject != null)
				{
					saveAsPrefab = GUILayout.Toggle(saveAsPrefab, "Save as Prefab");

					GUILayout.FlexibleSpace();

					if(GUILayout.Button("Export"))
					{
						if(Utils.License.ExportIsPermitted())
						{
							string path = fileSelector.Load();

							string[] extensions_list = exporter.SupportedExtensions;

							for(int i = 0; i < extensions_list.Length; i++)
							{
								extensions_list[i] = extensions_list[i].Insert(0, "*.");
							}

							string extensions = string.Format(";{0}", string.Join(";", extensions_list));

							if(saveAsPrefab)
							{
								extensions = "prefab";
							}

							fileSelector.DisplaySelector(path, string.Format("{0}.{1}", rootObject.name, Constants.binaryExtension), extensions);

							if(fileSelector.Modified && fileSelector.HasFile())
							{
								string file = fileSelector.First();

								if(!string.IsNullOrEmpty(file))
								{
									fileSelector.Save();

									if(saveAsPrefab)
									{
										Tools.PrefabExporter prefab = new Tools.PrefabExporter();

										prefab.Save(rootObject, file);
									}
									else
									{
										IEnumerator it = Export(exporter, file);

										while(it.MoveNext())
											;
									}
								}
							}
						}
						else
						{
							Debug.LogError("Export is not possible with this license.");
						}
					}
				}

				GUILayout.EndVertical();
			}
		}
		#endregion

		#region Coroutines
		private IEnumerator Export(Model.Exporter exporter, string filename)
		{
			ProgressBar progress = new ProgressBar("Export", "Please wait during models export.");

			bool success = false;

			IEnumerator it = exporter.Export(rootObject, filename, s => success = s, percentage => progress.Update(percentage));

			while(it.MoveNext())
			{
				yield return it.Current;
			}

			progress.Stop();

			if(! success)
			{
				EditorUtility.DisplayDialog("Error", string.Format("Export to '{0}' failed.", filename), "OK");
			}
		}
		#endregion
	}
}

#endif // UNITY_EDITOR_WIN
