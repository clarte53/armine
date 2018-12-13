#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Armine.UI.File
{
	[Serializable]
	internal class BrowserLayout
	{
		[SerializeField]
		private static bool multiSelection;

		[SerializeField]
		private static int multiSelectionStartIndex = -1;

		internal delegate void DoubleClickCallback(int[] index);

		internal static List<int> SelectionList(List<int> selected, GUIContent[] list)
		{
			return SelectionList(selected, list, "List Item", null);
		}

		internal static List<int> SelectionList(List<int> selected, GUIContent[] list, GUIStyle element_style)
		{
			return SelectionList(selected, list, element_style, null);
		}

		internal static List<int> SelectionList(List<int> selected, GUIContent[] list, DoubleClickCallback callback)
		{
			return SelectionList(selected, list, "List Item", callback);
		}

		internal static List<int> SelectionList(List<int> selected, GUIContent[] list, GUIStyle element_style, DoubleClickCallback callback)
		{
			for(int i = 0; i < list.Length; ++i)
			{
				Rect element_rect = GUILayoutUtility.GetRect(list[i], element_style);

				bool hover = element_rect.Contains(Event.current.mousePosition);

				if(hover && Event.current.type == EventType.MouseDown && Event.current.control)
				{
					if(multiSelection)
					{
						if(selected.Contains(i))
						{
							selected.Remove(i);
						}
						else
						{
							selected.Add(i);
						}
					}
					Event.current.Use();
				}
				else if(hover && Event.current.type == EventType.MouseDown && Event.current.shift)
				{
					if(selected.Count == 0)
					{
						selected.Add(i);
					}
					else
					{
						int nbelementselected = Math.Abs(i - selected[0]) + 1;
						int id_file_to_add;

						for(int id = 0; id < nbelementselected; id++)
						{
							id_file_to_add = id + Math.Min(i, selected[0]);
							if(!selected.Contains(id_file_to_add))
							{
								selected.Add(id_file_to_add);
							}
						}
						multiSelection = true;
					}
					Event.current.Use();
				}
				else if(hover && Event.current.type == EventType.MouseDown && Event.current.clickCount == 1)
				{
					selected = new List<int>{ i };
					Event.current.Use();
				}
				else if(hover && callback != null && Event.current.type == EventType.MouseDown && Event.current.clickCount == 2)
				{
					if(!multiSelection)
					{
						callback(new int[1]{i});
					}
					else
					{
						callback(selected.ToArray());
					}

					Event.current.Use(); 
				}
				if(Event.current.type == EventType.Repaint)
				{
					bool is_selected = false;

					if(selected != null)
					{
						foreach(int indice in selected)
						{
							if(indice == i)
							{
								is_selected = true;
								hover = true;
								break;
							}
						}
					}

					element_style.Draw(element_rect, list[i], hover, is_selected, is_selected, false);
				}
			}

			return selected;
		}

		internal static List<int> SelectionList(List<int> selected, string[] list)
		{
			return SelectionList(selected, list, "List Item", null);
		}

		internal static List<int> SelectionList(List<int> selected, string[] list, GUIStyle element_style)
		{
			return SelectionList(selected, list, element_style, null);
		}

		internal static List<int> SelectionList(List<int> selected, string[] list, DoubleClickCallback callback)
		{
			return SelectionList(selected, list, "List Item", callback);
		}

		internal static List<int> SelectionList(List<int> selected, string[] list, GUIStyle element_style, DoubleClickCallback callback)
		{
			for(int i = 0; i < list.Length; ++i)
			{
				Rect element_rect = GUILayoutUtility.GetRect(new GUIContent(list[i]), element_style);

				bool hover = element_rect.Contains(Event.current.mousePosition);

				if(hover && Event.current.type == EventType.MouseDown && Input.GetKey("Shift"))
				{
					if(multiSelectionStartIndex == -1)
					{
						multiSelectionStartIndex = i;
					}
					else
					{
						selected.Clear();

						int nb_element_selected = i - multiSelectionStartIndex;

						for(int id = 0; id < nb_element_selected; id++)
						{
							if(!selected.Contains(id))
							{
								selected.Add(i + multiSelectionStartIndex);
							}
						}
					}

					Event.current.Use();
				}
				else if(hover && Event.current.type == EventType.MouseDown)
				{
					selected.Clear();
					selected.Add(i);

					Event.current.Use();
				}
				else if(hover && callback != null && Event.current.type == EventType.MouseUp && Event.current.clickCount == 2)
				{
					if(multiSelection)
					{
						callback(new int[1]{i});
					}
					else
					{
						callback(selected.ToArray());
					}

					Event.current.Use();
				}

				if(Event.current.type == EventType.Repaint)
				{
					bool is_selected = false;

					foreach(int indice in selected)
					{
						if(indice == i)
						{
							is_selected = true;
							break;
						}
					}

					element_style.Draw(element_rect, list[i], hover, is_selected, is_selected, false);
				}
			}
			return  selected;
		}
	}
}

#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
