#if UNITY_EDITOR_WIN

using System;
using UnityEditor;
using UnityEngine;

namespace Armine.Editor.Windows
{
	[Serializable]
	public class ProgressBar
	{
		#region Members
		[SerializeField]
		private string windowTitle;

		[SerializeField]
		private string progressInfo;
		#endregion

		public ProgressBar(string title, string info)
		{
			windowTitle = title;
			progressInfo = info;
		}

		public void Update(float percentage)
		{
			EditorUtility.DisplayProgressBar(windowTitle, progressInfo, percentage);
		}

		public void Stop()
		{
			EditorUtility.ClearProgressBar();
		}
	}
}

#endif // UNITY_EDITOR_WIN
