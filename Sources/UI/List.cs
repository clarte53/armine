#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

using System.Collections.Generic;
using UnityEngine;

namespace Armine.UI
{
	internal class List
	{
		#region Configuration
		internal static void DisplayConfiguration(IList<string> values, params GUILayoutOption[] options)
		{
			int remove_at_index = -1;
			for(int i = 0; i < values.Count; ++i)
			{
				if(values[i].Length != 0)
				{
					values[i] = GUILayout.TextField(values[i], options);
				}
				else
				{
					remove_at_index = i;
				}
			}
			
			if(remove_at_index >= 0)
			{
				values.RemoveAt(remove_at_index);
			}
			
			string new_component = GUILayout.TextField("", options);
			
			if(new_component != "")
			{
				values.Add(new_component);
			}
		}
		#endregion

		#region Serialization
		internal static List<string> Parse(string values)
		{
			List<string> result = new List<string>();

			if(values != null)
			{
				bool complete = false;
				bool quoted = false;
				string value = "";

				for(int i = 0; i < values.Length; ++i)
				{
					if(values[i] == '\'')
					{
						quoted = ! quoted;

						complete = ! quoted;

						if(complete && i + 1 < values.Length && values[i + 1] == ' ')
						{
							++i;
						}
					}
					else if(values[i] == ' ' && ! quoted)
					{
						complete = true;
					}
					else
					{
						value += values[i];
					}

					if(complete)
					{
						result.Add(value);

						value = "";
						complete = false;
					}
				}

				if(value.Length != 0)
				{
					result.Add(value);
				}
			}

			return result;
		}

		internal static string Serialize(IList<string> values)
		{
			string result = "";

			if(values != null)
			{
				for(int i = 0; i < values.Count; ++i)
				{
					result += "\'" + values[i] + "\'";

					if(i != values.Count - 1)
					{
						result += " ";
					}
				}
			}

			return result;
		}
		#endregion
	}
}

#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
