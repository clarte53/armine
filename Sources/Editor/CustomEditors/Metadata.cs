#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Armine.Editor.CustomEditors
{
	[Serializable]
	[CustomEditor(typeof(Model.Metadata))]
	public class Metadata : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			Model.Metadata metadata = (Model.Metadata) target;

			if(metadata.data != null)
			{
				foreach(KeyValuePair<string, object> pair in metadata.data)
				{
					EditorGUILayout.BeginHorizontal();
					{
						EditorGUILayout.SelectableLabel(pair.Key, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(EditorGUIUtility.labelWidth - 4));
						EditorGUILayout.SelectableLabel(pair.Value != null  ? pair.Value.ToString() : "", EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
					}
					EditorGUILayout.EndHorizontal();
				}
			}

			if(GUI.changed)
			{
				EditorUtility.SetDirty(metadata);
			}
		}
	}
}

#endif // UNITY_EDITOR
