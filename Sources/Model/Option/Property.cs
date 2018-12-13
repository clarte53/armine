#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

using System;
using System.Collections.Generic;
using Assimp;
using UnityEngine;

namespace Armine.Model.Option
{
	#region Various enums
	[Serializable]
	internal enum Preset {
		None,
		Fast,
		Quality,
		Best,
		Custom,
	}

	[Flags, Serializable]
	internal enum PrimitiveType
	{
		Point = 0x1,
		Line = 0x2,
		Triangle = 0x4,
		Polygon = 0x8,
	}

	[Flags, Serializable]
	internal enum UVTrans
	{
		Rotation = 0x2,
		Scalling = 0x1,
		Translation = 0x4,
	}

	[Flags, Serializable]
	internal enum Components
	{
		Normals = 0x2,
		Tangents_and_Bitangents = 0x4,
		Colors = 0x8,
		Texture_Coordinates = 0x10,
		Bone_Weights = 0x20,
		Animations = 0x40,
		Textures = 0x80,
		Lights = 0x100,
		Cameras = 0x200,
		Meshes = 0x400,
		Materials = 0x800,
	}
	#endregion

	[Serializable]
	public class Property
	{
		#region Property enums
		[Serializable]
		public enum Type
		{
			INT,
			BOOL,
			FLOAT,
			STRING,
			MATRIX,
			FLAG,
			GROUP,
		}

		[Serializable]
		public enum SelfDefinedOption
		{
			NONE = -1,
			PRESETS,
			SHOW_ALL_POST_PROCESS_STEPS,
			ACTIVATE_LOGGING,
		}

		[Serializable]
		public enum Category
		{
			None = -1,
			General_Options,
			Presets,
			Preprocess_Steps,
			Extension_Related_Options,
		}
		#endregion

		[Serializable]
		public class Data
		{
			#region Members
			public bool userDefined;
			public bool changed;
			public bool isStepFlag;
			public string propertyFlag;
			public string optionText;
			public string currentValue;
			public Property.Type propertyType;
			public string associatedEnumType;
			public aiPostProcessSteps processStep;
			public Category propertyCategory;
			public SelfDefinedOption customOption;
			#endregion

			#region Constructors
			public Data()
			{
				userDefined = true;
				changed = false;
				isStepFlag = false;
				
				propertyFlag = "";
				optionText = "";
				currentValue = "";
				propertyType = Type.GROUP;
				associatedEnumType = "";
				processStep = (aiPostProcessSteps) 0;
				propertyCategory = Category.None;;
				customOption = SelfDefinedOption.NONE;
			}

			internal Data(string option_text, Property.Type type, Category category, System.Object init_value, bool user_defined) : this()
			{
				userDefined = user_defined;
				optionText = option_text;
				currentValue = init_value.ToString();
				propertyType = type;
				associatedEnumType = typeof(void).FullName;
				propertyCategory = category;;
			}

			internal Data(Importer importer, string property_flag, string option_text, Property.Type type, Category category, System.Object init_value, bool user_defined) :
				this(option_text, type, category, InitValue(importer, property_flag, type, init_value), user_defined)
			{
				propertyFlag = property_flag;
			}

			internal Data(Importer importer, string property_flag, string option_text, Property.Type type, Category category, System.Object init_value) :
				this(importer, property_flag, option_text, type, category, init_value, true)
			{

			}

			internal Data(Importer importer, string property_flag, string option_text, Property.Type type, Category category, System.Type enum_type, System.Object init_value, bool user_defined) :
				this(importer, property_flag, option_text, type, category, (int) init_value, user_defined)
			{
				associatedEnumType = enum_type.ToString();
			}

			internal Data(Importer importer, string property_flag, string option_text, Property.Type type, Category category, System.Type enum_type, System.Object init_value) :
				this(importer, property_flag, option_text, type, category, enum_type, (int) init_value, true)
			{

			}

			internal Data(Importer importer, aiPostProcessSteps step, string option_text, Category category, bool user_defined) :
				this(option_text, Property.Type.BOOL, category, InitValue(importer, step), user_defined)
			{
				isStepFlag = true;
				processStep = step;
			}

			internal Data(Importer importer, aiPostProcessSteps step, string option_text, Category category) :
				this(importer, step, option_text, category, true)
			{

			}

			internal Data(SelfDefinedOption option, Property.Type type, Category category, System.Object init_value, bool user_defined) :
				this(Enum.GetName(typeof(SelfDefinedOption), option), type, category, InitValue(option, type, init_value), user_defined)
			{
				customOption = option;
			}

			internal Data(SelfDefinedOption option, Property.Type type, Category category, System.Object init_value) :
				this(option, type, category, init_value, true)
			{

			}

			internal Data(SelfDefinedOption option, Property.Type type, Category category, System.Type enum_type, System.Object init_value) :
				this(Enum.GetName(typeof(SelfDefinedOption), option), type, category, InitValue(option, type, (int) init_value), true)
			{
				customOption = option;
				associatedEnumType = enum_type.ToString();
			}
			#endregion

			#region Init values
			private static string InitValue(Importer importer, aiPostProcessSteps step)
			{
				string step_name = Enum.GetName(typeof(aiPostProcessSteps), step);
				
				if(PlayerPrefs.HasKey(step_name))
				{
					string result = PlayerPrefs.GetString(step_name);
					return result;
				}
				else
				{
					return importer.Assimp.IsFlagSet(step).ToString();
				}
			}
			
			private static System.Object InitValue(Importer importer, string key_name, Property.Type type, System.Object default_value)
			{
				if(PlayerPrefs.HasKey(key_name))
				{
					return PlayerPrefs.GetString(key_name);
				}
				else
				{
					try
					{
						switch(type)
						{
							case Property.Type.FLAG:
							case Property.Type.INT:
								return importer.Assimp.GetProperty<int>(key_name);
							case Property.Type.BOOL:
								// Do not check importer value because we can not determine if a value is present, so use default value instead.
								//return importer.GetProperty<bool>(key_name);
								return default_value;
							case Property.Type.FLOAT:
								return importer.Assimp.GetProperty<float>(key_name);
							case Property.Type.STRING:
								return importer.Assimp.GetProperty<string>(key_name);
							case Property.Type.MATRIX:
								string value;

								using(aiMatrix4x4 matrix = importer.Assimp.GetProperty<aiMatrix4x4>(key_name))
					      {
									using(aiVector3D position = new aiVector3D())
									{
										using(aiVector3D scaling = new aiVector3D())
										{
											using(aiQuaternion rot = new aiQuaternion())
											{
												matrix.Decompose(scaling, rot, position);

												Quaternion rotation = Model.Type.Assimp.Convert.AssimpToUnity.Quaternion(rot);
												Vector3 angles = rotation.eulerAngles;

												value = UI.List.Serialize(new List<string>{
													position.x.ToString(), position.y.ToString(), position.z.ToString(),
													angles.x.ToString(), angles.y.ToString(), angles.z.ToString(),
													scaling.x.ToString(), scaling.y.ToString(), scaling.z.ToString()
												});
											}
										}
									}
								}

								return value;
						}
					}
					catch(Module.Import.Assimp.NotDefinedException)
					{
						if(type == Property.Type.FLAG)
						{
							return (int) default_value; // Must be cast explicitely to int to avoid ToString conversion of enums
						}
						else
						{
							return default_value;
						}
					}
				}
				
				Debug.LogWarning("Incorrect type of property (" + key_name + ", " + type + "): unable to initialize it.");
				
				return "";
			}

			private static System.Object InitValue(SelfDefinedOption option, Property.Type type, System.Object default_value)
			{
				string key_name = Enum.GetName(typeof(SelfDefinedOption), option);

				if(PlayerPrefs.HasKey(key_name))
				{
					return PlayerPrefs.GetString(key_name);
				}
				else
				{
					if(type == Property.Type.FLAG)
					{
						return (int) default_value; // Must be cast explicitely to int to avoid ToString conversion of enums
					}
					else
					{
						return default_value;
					}
				}
			}
			#endregion

			#region Value getters
			public static bool? GetBool(Data property)
			{
				bool? value = null;
				bool tmp;

				if(Boolean.TryParse(property.currentValue, out tmp))
				{
					value = tmp;
				}
				else if(property.currentValue.Length != 0)
				{
					Debug.LogWarning("Invalid value \"" + property.optionText + " = " + property.currentValue + "\": should be boolean.");
				}

				return value;
			}

			public static int? GetInt(Data property)
			{
				int? value = null;
				int tmp;

				if(Int32.TryParse(property.currentValue, out tmp))
				{
					value = tmp;
				}
				else if(property.currentValue.Length != 0)
				{
					Debug.LogWarning("Invalid value \"" + property.optionText + " = " + property.currentValue + "\": should be an integer.");
				}
				
				return value;
			}

			public static float? GetFloat(Data property)
			{
				float? value = null;
				float tmp;

				if(Single.TryParse(property.currentValue, out tmp))
				{
					value = tmp;
				}
				else if(property.currentValue.Length != 0)
				{
					Debug.LogWarning("Invalid value \"" + property.optionText + " = " + property.currentValue + "\": should be a float.");
				}
				
				return value;
			}
			#endregion
		}

		#region Members
		public Data data;
		public List<Data> subProperties;
		#endregion

		#region Constructors
		public Property()
		{
			data = null;
			subProperties = null;
		}

		public Property(Data property_data)
		{
			data = property_data;

			subProperties = new List<Property.Data>();
		}

		public Property(Data property_data, params Data[] properties)
		{
			data = property_data;

			subProperties = new List<Property.Data>(properties);
		}
		#endregion

		#region Set changed
		internal void SetChanged(bool value)
		{
			data.changed = value;
			
			foreach(Data subproperty in subProperties)
			{
				subproperty.changed = value;
			}
		}
		#endregion
	}
}

#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
