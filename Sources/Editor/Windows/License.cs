#if UNITY_EDITOR_WIN

using System.IO;
using Armine.UI.File;
using UnityEditor;
using UnityEngine;

namespace Armine.Editor.Windows
{
	internal class License : EditorWindow
	{
		#region Members
		[SerializeField]
		private Selector fileSelector;
		
		[SerializeField]
		private bool displayLicense;
		#endregion
		
		#region GetterSetter
		public static string DeviceID
		{
			get
			{
				return Utils.License.DeviceID();
			}
		}
		
		public static string Key
		{
			set
			{
				Utils.License.SetLicense(value);
			}
		}
		#endregion

		#region MonoBehaviour callbacks
		[MenuItem("Armine/License", false, 20)]
		static public void ShowLicense()
		{
			EditorWindow.GetWindow(typeof(License), false, "Armine License");
		}
		
		public void OnGUI()
		{
			//Initialisation/ reinit to avoid null object on runtime launch
			if(fileSelector == null)
			{
				fileSelector = new NativeSelectorEditor();
			}
			
			if(! Utils.License.IsLicensed())
			{
				GUILayout.BeginVertical();
				
				GUILayout.Label("Device key:");
				EditorGUILayout.TextField(DeviceID);
				
				GUILayout.Space(GUI.skin.label.CalcHeight(new GUIContent(""), 1));
				
				DisplayConfiguration();
				
				GUILayout.EndVertical();
			}
			else
			{
				ClearConfiguration();
			}
		}
		#endregion
		
		#region Configuration
		public void DisplayConfiguration()
		{
			GUILayout.BeginVertical();
			
			GUILayout.Label("License key:");
			
			fileSelector.DisplayConfiguration("txt", false);
			
			if(GUILayout.Button("Save"))
			{
				if(fileSelector.HasFile() && File.Exists(fileSelector.First()))
				{
					using(StreamReader stream = new StreamReader(fileSelector.First()))
					{
						Key = stream.ReadToEnd();
					}
				}
			}
			
			GUILayout.EndVertical();
		}
		
		public void ClearConfiguration()
		{
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			
			GUILayout.BeginVertical();
			GUILayout.Label("Status:");
			GUILayout.Label("Expiration date: ");
			GUILayout.Label("Device key:");
			GUILayout.EndVertical();
			
			GUILayout.BeginVertical();
			
			Color color = GUI.skin.label.normal.textColor;
			GUI.skin.label.normal.textColor = new Color(0.0f, 0.7f, 0.0f);
			GUILayout.Label("Valid license");
			GUI.skin.label.normal.textColor = color;
			
			GUILayout.Label(Utils.License.Date());
			GUILayout.Label(Utils.License.DeviceID());
			GUILayout.EndVertical();
			
			GUILayout.EndHorizontal();
			
			GUILayout.Space(GUI.skin.label.CalcHeight(new GUIContent(""), 1));
			
			displayLicense = GUILayout.Toggle(displayLicense, "License key", UI.Menu.foldout);
			
			if(displayLicense)
			{
				GUILayout.TextArea(Utils.License.GetLicense());
			}
			
			GUILayout.EndVertical();
		}
		#endregion
	}
}

#endif // UNITY_EDITOR_WIN
