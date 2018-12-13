#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

using System;
using System.Collections.Generic;
using Assimp;
using UnityEngine;
using Armine.Model.Option;

namespace Armine.UI
{
	[Serializable]
	public class Menu
	{
		#region Members
		[NonSerialized]
		public static readonly GUIStyle foldout;

		[NonSerialized]
		public static readonly GUIStyle indent;

		[SerializeField]
		private Import options;

		[SerializeField]
		private bool showAll;

		[SerializeField]
		private bool[] opened;

		[SerializeField]
		private Vector2 scrollPosition;
		#endregion

		#region Constructor
		static Menu()
		{
			foldout = new GUIStyle();
			foldout.normal.background = Resources.Load("Textures/ToggleOff") as Texture2D;
			foldout.onNormal.background = Resources.Load("Textures/ToggleOn") as Texture2D;
			foldout.alignment = TextAnchor.MiddleLeft;
			foldout.imagePosition = ImagePosition.ImageLeft;
			foldout.fixedHeight = 12;
			foldout.fixedWidth = 12;
			foldout.contentOffset = new Vector2(14, 0);
			foldout.margin = new RectOffset(2, 2, 2, 2);

			indent = new GUIStyle();
			indent.margin = new RectOffset(0, 0, 0, 0);
		}

		public Menu()
		{
			options = new Import();

			showAll = false;

			opened = new bool[Enum.GetValues(typeof(Property.Category)).Length];
		}
		#endregion

		#region GetterSetter
		public Import Options
		{
			get
			{
				return options;
			}
		}
		#endregion

		#region Indent
		public static void BeginIndent()
		{
			GUILayout.BeginHorizontal(indent);
			GUILayout.Space(20);
			GUILayout.BeginVertical(indent);
		}

		public static void EndIndent()
		{
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}
		#endregion

		#region Configuration
		/// <summary>
		/// Display a menu in a tree view style , with each category 
		/// fitted in a foldout button
		/// </summary>
		/// <param name="optionList"> Property list containing the options that need to be displayed in the menu </param>
		/// <returns>bool: hasChanged true if at least one property has been changed </returns>
		public void DisplayConfiguration()
		{
			scrollPosition = GUILayout.BeginScrollView(scrollPosition);

			List<Property> option_list = options.Properties;

			//If name has changed, we reload the option list that fit its expansion type
			if(option_list != null)
			{
				foreach(Property.Category category in Enum.GetValues(typeof(Property.Category)))
				{
					if(category > Property.Category.None)
					{
						List<Property> category_option_list = option_list.FindAll(x => x.data.propertyCategory == category);
						
						if(category_option_list.Count > 0)
						{
							int index = Convert.ToInt32(category);
							
							// use a foldout element 
							opened[index] = GUILayout.Toggle(opened[index], Enum.GetName(typeof(Property.Category), category).Replace("_", " "), foldout);

							DisplayMenu(category_option_list, opened[index]);
						}
					}
				}
			}

			GUILayout.EndScrollView();
		}

		/// <summary>
		///  Display a list of option based on the property list passed in parameter. 
		/// </summary>
		/// <param name="properties">List[Property]: list of Properties data structures describing the content of the option list.</param>
		/// <param name="enabled">bool: Set if the list is enabled to modification or not</param>
		private void DisplayMenu(IList<Property> properties, bool enabled)
		{
			if(enabled)
			{
				for(int i = 0; i < properties.Count; i++)
				{
					Property property = properties[i];

					if(showAll || property.data.userDefined)
					{
						// In case we want to display all the post-processing steps, we make inactive those who are not modifiable
						bool user_defined = GUI.enabled;
						if(! property.data.userDefined)
						{
							GUI.enabled = false;
						}

						BeginIndent();

						if(property.data.customOption != Property.SelfDefinedOption.NONE)
						{
							DisplayCustomMenu(property);
						}
						else
						{
							bool changed = DisplayMenuItem(property.data, enabled);

							if(property.subProperties != null && property.subProperties.Count > 0)
							{
								bool is_enabled = (property.data.propertyType == Property.Type.BOOL ? Convert.ToBoolean(property.data.currentValue) : enabled);

								if(is_enabled)
								{
									BeginIndent();

									foreach(Property.Data data in property.subProperties)
									{
										GUI.enabled = data.userDefined;

										bool res = DisplayMenuItem(data, is_enabled);

										changed = changed || res;

										GUI.enabled = false;
									}

									EndIndent();
								}
							}

							if(changed)
							{
								property.SetChanged(true);
							}
						}

						EndIndent();

						properties[i] = property;

						GUI.enabled = user_defined;
					}
				}

				options.Save();
			}
		}

		private bool DisplayMenuItem(Property.Data data, bool enabled)
		{
			bool changed = false;

			if(enabled)
			{
				string value = data.currentValue;
				
				switch(data.propertyType)
				{
					case Property.Type.BOOL:
						bool? toogle_value = Property.Data.GetBool(data);
						
						if(toogle_value.HasValue)
						{
							value = GUILayout.Toggle(toogle_value.Value, data.optionText).ToString();
						}
						else
						{
							value = Boolean.FalseString;
						}
						
						break;
						
					case Property.Type.FLAG:
						int? flag_value = DisplayFlags(data);
						
						if(flag_value.HasValue)
						{
							value = ((int) flag_value.Value).ToString();
						}
						
						break;
						
					case Property.Type.INT:
					case Property.Type.FLOAT:
						GUILayout.BeginHorizontal();
						GUILayout.Label(data.optionText);
						
						value = GUILayout.TextField(data.currentValue, GUILayout.Width(75));
						
						GUILayout.EndHorizontal();
						
						break;
						
					case Property.Type.STRING:
						GUILayout.BeginHorizontal();
						GUILayout.Label(data.optionText);
						
						List<string> value_list = List.Parse(data.currentValue);
						
						GUILayout.FlexibleSpace();
						GUILayout.BeginVertical();
						List.DisplayConfiguration(value_list, GUILayout.Width(75));
						GUILayout.EndVertical();
						
						value = List.Serialize(value_list);
						
						GUILayout.EndHorizontal();
						
						break;
						
					case Property.Type.MATRIX:
						const int size = 9;
						
						List<string> values_str = List.Parse(data.currentValue);
						
						if(values_str.Count != size)
						{
							Debug.LogError(values_str.Count);
							values_str = new List<string>{"0", "0", "0", "0", "0", "0", "1", "1", "1"};
						}
						
						GUILayout.BeginHorizontal();
						
						GUILayout.BeginVertical();
						
						GUILayout.Label("Position");
						GUILayout.Label("Rotation");
						GUILayout.Label("Scale");
						
						GUILayout.EndVertical();
						
						GUILayout.BeginVertical();
						
						for(int j = 0; j < size; j++)
						{
							if(j % 3 == 0)
							{
								GUILayout.BeginHorizontal();
							}
							
							values_str[j] = GUILayout.TextField(values_str[j], GUILayout.Width(50));
							
							if(j % 3 == 2)
							{
								GUILayout.EndHorizontal();
							}
						}
						
						GUILayout.EndVertical();
						GUILayout.EndHorizontal();
						
						value = List.Serialize(values_str);				
						
						break;
				}
				
				if(data.currentValue.CompareTo(value) != 0)
				{
					data.currentValue = value;
					
					changed = true;
				}
			}

			return changed;
		}

		private int? DisplayFlags(Property.Data data)
		{
			GUILayout.BeginVertical();
			
			int? flag_value = Property.Data.GetInt(data);
			
			if(! flag_value.HasValue)
			{
				flag_value = 0;
			}

			System.Type enum_type = System.Type.GetType(data.associatedEnumType);
			string[] names = Enum.GetNames(enum_type);
			int[] values = (int[]) Enum.GetValues(enum_type);
			
			for(int j = 0; j < names.Length; j++)
			{
				flag_value = Flags.Toogle(flag_value.Value, values[j], GUILayout.Toggle(Flags.IsSet(flag_value.Value, values[j]), names[j].Replace("_", " ")));
			}
			
			GUILayout.EndVertical();

			return flag_value;
		}
		
		// <summary>
		/// Add a property related to unity options ( and not assimp property or 
		/// preprocessor step. 
		/// </summary>
		/// <param name="option">  Options type enum </param>
		private void DisplayCustomMenu(Property property)
		{
			switch(property.data.customOption)
			{
				case Property.SelfDefinedOption.PRESETS:
					// Get the user modifed list of flags
					aiPostProcessSteps steps = options.GetFlags();
					
					// Test if the current steps fit a pre-configuration
					Preset current_preset = (
						Model.Module.Import.Assimp.CompareFlags(steps, aiPostProcessSteps.aiProcessPreset_TargetRealtime_Fast) ? Preset.Fast : (
							Model.Module.Import.Assimp.CompareFlags(steps, aiPostProcessSteps.aiProcessPreset_TargetRealtime_Quality) ? Preset.Quality : (
								Model.Module.Import.Assimp.CompareFlags(steps, aiPostProcessSteps.aiProcessPreset_TargetRealtime_MaxQuality) ? Preset.Best : (
									Model.Module.Import.Assimp.CompareFlags(steps, 0) ? Preset.None : Preset.Custom
								)
							)
						)
					);

					// Display the preset checkboxes & save the current selected preset
					foreach(Preset preset in Enum.GetValues(typeof(Preset)))
					{
						string name = Enum.GetName(typeof(Preset), preset);
						bool custom = (name.CompareTo("Custom") == 0);
						
						if(custom) GUI.enabled = false;
						if(GUILayout.Toggle(current_preset == preset, name)) {
							current_preset = preset;
						}
						if(custom) GUI.enabled = true;
					}

					// Set the flags in the properties based on the selected preset
					switch(current_preset)
					{
						case Preset.None:
							steps = 0;
							break;
						case Preset.Fast:
							steps = aiPostProcessSteps.aiProcessPreset_TargetRealtime_Fast;
							break;
						case Preset.Quality:
							steps = aiPostProcessSteps.aiProcessPreset_TargetRealtime_Quality;
							break;
						case Preset.Best:
							steps = aiPostProcessSteps.aiProcessPreset_TargetRealtime_MaxQuality;
							break;
						default:
						case Preset.Custom:
							steps = options.GetFlags();
							break;
					}
					options.SetFlags(Model.Module.Import.Assimp.UsedSteps(steps));
					
					break;

				case Property.SelfDefinedOption.SHOW_ALL_POST_PROCESS_STEPS:
					bool? show_all = Property.Data.GetBool(property.data);

					if(show_all.HasValue)
					{
						showAll = show_all.Value;

						showAll = GUILayout.Toggle(showAll, "Show all post processing steps");

						if(showAll != show_all.Value)
						{
							property.SetChanged(true);
						}

						property.data.currentValue = showAll.ToString();
					}
					
					break;
				
				case Property.SelfDefinedOption.ACTIVATE_LOGGING:
					bool? activate_logging = Property.Data.GetBool(property.data);
					
					if(activate_logging.HasValue)
					{
						bool activated = GUILayout.Toggle(activate_logging.Value, "Activate logging");

						if(activated != activate_logging.Value)
						{
							property.SetChanged(true);
						}
						
						property.data.currentValue = activated.ToString();
					}

					break;
			}
		}
		#endregion
	}
}

#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
