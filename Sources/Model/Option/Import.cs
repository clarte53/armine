#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

using System;
using System.Collections.Generic;
using Assimp;
using UnityEngine;

namespace Armine.Model.Option
{
	/// <summary>
	/// Contains all the options configuration 
	/// data that we want to give access to in the 
	/// Importer menu
	/// </summary>
	[Serializable]
	public class Import
	{
		#region Constants
		private const string propertiesName = "properties";
		private const string componentsName = "components";
		#endregion

		#region Members
		public bool initialized;
		
		public List<Property> optionsList;
		#endregion

		#region Constructors
		internal Import()
		{
			initialized = false;

			optionsList = new List<Property>();
		}
		#endregion

		#region GetterSetter
		internal List<Property> Properties
		{
			get
			{
				return optionsList;
			}
		}
		#endregion

		#region Init
		public void Init(Importer importer, IList<string> filenames)
		{
			if(! initialized)
			{
				SetOptions(importer, filenames);

				initialized = true;
			}
		}
		#endregion

		#region Flags
		internal aiPostProcessSteps GetFlags()
		{
			aiPostProcessSteps flags = (aiPostProcessSteps) 0; 
			
			for(int i = 0; i < optionsList.Count; i++)
			{
				flags |= GetFlags(optionsList[i]);
			}
			
			return flags;
		}

		private aiPostProcessSteps GetFlags(Property property)
		{
			aiPostProcessSteps flags = GetFlags(property.data); 
			
			if(property.subProperties != null)
			{
				foreach(Property.Data subproperty in property.subProperties)
				{
					flags |= GetFlags(subproperty);
				}
			}
			
			return flags;
		}

		private aiPostProcessSteps GetFlags(Property.Data data)
		{
			aiPostProcessSteps flags = (aiPostProcessSteps) 0; 

			// Is the option related to an assimp property or the activation 
			if(data.isStepFlag)
			{
				flags = (aiPostProcessSteps) Flags.Toogle((int) flags, (int) data.processStep, Convert.ToBoolean(data.currentValue));
			}

			return flags;
		}
		
		internal void SetFlags(aiPostProcessSteps flags)
		{
			for(int i = 0; i < optionsList.Count; i++)
			{
				SetFlags(optionsList[i], flags);
			}
		}

		private void SetFlags(Property property, aiPostProcessSteps flags)
		{
			if(property.subProperties != null)
			{
				foreach(Property.Data subproperty in property.subProperties)
				{
					if(SetFlags(subproperty, flags))
					{
						property.SetChanged(true);
					}
				}
			}

			if(SetFlags(property.data, flags))
			{
				property.SetChanged(true);
			}
		}

		private bool SetFlags(Property.Data data, aiPostProcessSteps flags)
		{
			bool set = false;

			// Is the option related to an assimp property or the activation 
			if(data.isStepFlag)
			{
				string value = Flags.IsSet((int) flags, (int) data.processStep).ToString();
				
				if(data.currentValue.CompareTo(value) != 0)
				{
					data.currentValue = value;

					set = true;
				}
			}

			return set;
		}
		#endregion

		#region PlayerPrefs
		internal void Save()
		{
			Save(optionsList);
		}

		private void Save(List<Property> properties)
		{
			if(properties != null)
			{
				bool changed = false;

				foreach(Property property in properties)
				{
					foreach(Property.Data data in property.subProperties)
					{
						if(data.changed)
						{
							Save(data);

							changed = true;
						}
					}

					if(property.data.changed)
					{
						Save(property.data);

						changed = true;
					}

					if(changed)
					{
						property.SetChanged(false);

						changed = false;
					}
				}
			}
		}

		private void Save(Property.Data data)
		{
			if(data.customOption != Property.SelfDefinedOption.NONE)
			{
				PlayerPrefs.SetString(Enum.GetName(typeof(Property.SelfDefinedOption), data.customOption), data.currentValue);
			}
			else if(data.processStep != 0)
			{
				PlayerPrefs.SetString(Enum.GetName(typeof(aiPostProcessSteps), data.processStep), data.currentValue);
			}
			else if(data.propertyFlag.Length != 0)
			{
				PlayerPrefs.SetString(data.propertyFlag, data.currentValue);
			}
		}
		#endregion

		#region Apply configuration
		public void ApplyConfiguration(Importer importer)
		{
			if(initialized)
			{
				for(int i = 0; i < optionsList.Count; i++)
				{
					ApplyConfiguration(importer, optionsList[i]);
				}
			}
		}

		private void ApplyConfiguration(Importer importer, Property property)
		{
			ApplyConfiguration(importer, property.data);

			if(property.subProperties != null)
			{
				foreach(Property.Data subproperty in property.subProperties)
				{
					ApplyConfiguration(importer, subproperty);
				}
			}
		}

		private void ApplyConfiguration(Importer importer, Property.Data data)
		{
			switch(data.propertyCategory)
			{
				case Property.Category.General_Options:
					switch(data.customOption)
					{
						case Property.SelfDefinedOption.ACTIVATE_LOGGING:
							bool? activate_logging = Property.Data.GetBool(data);

							if(activate_logging.HasValue && activate_logging.Value)
							{
								importer.Assimp.SetLogFile("import.log");
							}

							break;

						case Property.SelfDefinedOption.NONE:
							ApplyAssimpConfiguration(importer, data);
							break;
					}

					break;

				case Property.Category.Preprocess_Steps:
				case Property.Category.Extension_Related_Options:
					ApplyAssimpConfiguration(importer, data);
					break;
			}
		}

		private void ApplyAssimpConfiguration(Importer importer, Property.Data data)
		{
			if(data.isStepFlag)
			{
				bool value_step;
				
				if(Boolean.TryParse(data.currentValue, out value_step))
				{
					importer.Assimp.ChangeFlag(data.processStep, value_step);
				}
			}
			else
			{
				switch(data.propertyType)
				{
					case Property.Type.BOOL:
						bool? value_bool = Property.Data.GetBool(data);
						
						if(value_bool.HasValue)
						{
							importer.Assimp.SetProperty(data.propertyFlag, value_bool.Value);
						}
						
						break;

					case Property.Type.FLAG:
					case Property.Type.INT:
						int? value_int = Property.Data.GetInt(data);
						
						if(value_int.HasValue)
						{
							importer.Assimp.SetProperty(data.propertyFlag, value_int.Value);
						}
						
						break;

					case Property.Type.FLOAT:
						float? value_float = Property.Data.GetFloat(data);
						
						if(value_float.HasValue)
						{
							importer.Assimp.SetProperty(data.propertyFlag, value_float.Value);
						}
						
						break;

					case Property.Type.STRING:
						importer.Assimp.SetProperty(data.propertyFlag, data.currentValue);
						
						break;

					case Property.Type.MATRIX:
						const int size = 9;
						
						List<string> values_matrix_str = UI.List.Parse(data.currentValue);
						
						if(values_matrix_str.Count == size)
						{
							float[] value_matrix = new float[size];
							
							for(int i = 0; i < size; i++)
							{
								if(!float.TryParse(values_matrix_str[i], out value_matrix[i]))
								{
									Debug.LogWarning("Invalid value \"" + data.optionText + " = " + values_matrix_str[i] + "\": should be a float.");
								}
							}
							
							Vector3 pos = new Vector3(value_matrix[0], value_matrix[1], value_matrix[2]);
							Quaternion rot = new Quaternion();
							rot.eulerAngles = new Vector3(value_matrix[3], value_matrix[4], value_matrix[5]);
							Vector3 scale = new Vector3(value_matrix[6], value_matrix[7], value_matrix[8]);
							
							using(aiVector3D scaling = Type.Assimp.Convert.UnityToAssimp.Vector3(scale))
							{
								using(aiQuaternion rotation = Type.Assimp.Convert.UnityToAssimp.Quaternion(rot))
								{
									using(aiVector3D position = Type.Assimp.Convert.UnityToAssimp.Vector3(pos))
									{
										using(aiMatrix4x4 matrix = new aiMatrix4x4(scaling, rotation, position))
										{
											importer.Assimp.SetProperty(data.propertyFlag, matrix);
										}
									}
								}
							}
						}
						
						break;

					default:
						break;
				}
			}
		}
		#endregion

		#region Options
		private void SetOptions(Importer importer, IList<string> filenames)
		{
			if(importer != null)
			{
				optionsList = new List<Property> {
					new Property(
						new Property.Data(
							Property.SelfDefinedOption.SHOW_ALL_POST_PROCESS_STEPS,
							Property.Type.BOOL,
							Property.Category.General_Options,
							false
						)
					),
					new Property(
						new Property.Data(
							Property.SelfDefinedOption.ACTIVATE_LOGGING,
							Property.Type.BOOL,
							Property.Category.General_Options,
							false
						)
					),
					new Property(
						new Property.Data(
							importer,
							"GLOB_MEASURE_TIME",
							"Activate performance logging",
							Property.Type.BOOL,
							Property.Category.General_Options,
							false
						)
					),
					new Property(
						new Property.Data(
							importer,
							"IMPORT_NO_SKELETON_MESHES",
							"No dummy meshes for skeletons",
							Property.Type.BOOL,
							Property.Category.General_Options,
							false
						)
					),
					new Property(
						new Property.Data(
							importer,
							"FAVOUR_SPEED",
							"Favour loading speed",
							Property.Type.BOOL,
							Property.Category.General_Options,
							false
						)
					),
					new Property(
						new Property.Data(
							importer,
							"GLOB_MULTITHREADING",
							"Set multithreading policy",
							Property.Type.INT,
							Property.Category.General_Options,
							-1,
							false
						)
					),
					new Property(
						new Property.Data(
							Property.SelfDefinedOption.PRESETS,
							Property.Type.GROUP,
							Property.Category.Presets,
							""
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_ValidateDataStructure,
							"Validate Data Structure",
							Property.Category.Preprocess_Steps,
						  false
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_FindInvalidData,
							"Find Invalid Data",
							Property.Category.Preprocess_Steps
						),
						new Property.Data(
							importer,
							"PP_FID_ANIM_ACCURACY",
							"Animations accuracy",
							Property.Type.FLOAT,
							Property.Category.Preprocess_Steps,
							0.0f
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_FindDegenerates,
							"Find degenerated polygons",
							Property.Category.Preprocess_Steps
						),
						new Property.Data(
							importer,
							"PP_FD_REMOVE",
							"Remove degenerated polygons",
							Property.Type.BOOL,
							Property.Category.Preprocess_Steps,
							false
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_FindInstances,
							"Find Instances",
							Property.Category.Preprocess_Steps
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_MakeLeftHanded,
							"Convert to left handed coordinate system",
							Property.Category.Preprocess_Steps,
							false
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_FlipWindingOrder,
							"Set face winding order to counter clockwise (CW)",
							Property.Category.Preprocess_Steps,
							false
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_FlipUVs,
							"Flip UVs",
							Property.Category.Preprocess_Steps,
							false
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_Triangulate,
							"Triangulate polygonal faces",
							Property.Category.Preprocess_Steps,
							false
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_JoinIdenticalVertices,
							"Join identical vertices",
							Property.Category.Preprocess_Steps
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_ImproveCacheLocality,
							"Improve cache locality",
							Property.Category.Preprocess_Steps
						),
						new Property.Data(
							importer,
							"PP_ICL_PTCACHE_SIZE",
							"Cache size in vertices",
							Property.Type.INT,
							Property.Category.Preprocess_Steps,
							assimp_swig.PP_ICL_PTCACHE_SIZE
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_SplitLargeMeshes,
							"Split large meshes",
							Property.Category.Preprocess_Steps,
							false
						),
						new Property.Data(
							importer,
							"PP_SLM_TRIANGLE_LIMIT",
							"Triangle limit per mesh",
							Property.Type.INT,
							Property.Category.Preprocess_Steps,
							assimp_swig.AI_SLM_DEFAULT_MAX_TRIANGLES,
							false
						),
						new Property.Data(
							importer,
							"PP_SLM_VERTEX_LIMIT",
							"Vertex limit per mesh",
							Property.Type.INT,
							Property.Category.Preprocess_Steps,
							assimp_swig.AI_SLM_DEFAULT_MAX_VERTICES,
							false
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_SplitByBoneCount,
							"Split by bone count",
							Property.Category.Preprocess_Steps
						),
						new Property.Data(
							importer,
							"PP_SBBC_MAX_BONES",
							"Bone limit per mesh",
							Property.Type.INT,
							Property.Category.Preprocess_Steps,
							assimp_swig.AI_SBBC_DEFAULT_MAX_BONES
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_SortByPType,
							"Sort and remove meshes by polygon types",
							Property.Category.Preprocess_Steps,
							false
						),
						new Property.Data(
							importer,
							"PP_SBP_REMOVE",
							"Remove meshes based on their primitive type",
							Property.Type.FLAG,
							Property.Category.Preprocess_Steps,
							typeof(PrimitiveType),
							0
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_OptimizeMeshes,
							"Optimize meshes",
							Property.Category.Preprocess_Steps
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_OptimizeGraph,
							"Optimize graph",
							Property.Category.Preprocess_Steps
						),
						new Property.Data(
							importer,
							"PP_OG_EXCLUDE_LIST",
							"List of preserved nodes",
							Property.Type.STRING,
							Property.Category.Preprocess_Steps,
							""
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_FixInfacingNormals,
							"Fix infacing normals",
							Property.Category.Preprocess_Steps
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_GenNormals,
							"Generate Normals",
							Property.Category.Preprocess_Steps
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_GenSmoothNormals,
							"Generate Smooth Normals",
							Property.Category.Preprocess_Steps
						),
						new Property.Data(
							importer,
							"PP_GSN_MAX_SMOOTHING_ANGLE",
							"Max angle between normals",
							Property.Type.FLOAT,
							Property.Category.Preprocess_Steps,
							175.0f
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_CalcTangentSpace,
							"Compute Tangents space",
							Property.Category.Preprocess_Steps
						),
						new Property.Data(
							importer,
							"PP_CT_MAX_SMOOTHING_ANGLE",
							"Smoothing angle",
							Property.Type.FLOAT,
							Property.Category.Preprocess_Steps,
					    45.0f
						),
						new Property.Data(
							importer,
							"PP_CT_TEXTURE_CHANNEL_INDEX",
							"Source UV channel",
							Property.Type.INT,
							Property.Category.Preprocess_Steps,
							0
					  )
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_GenUVCoords,
							"Generate UV Coordinates",
							Property.Category.Preprocess_Steps
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_TransformUVCoords,
							"Transform UV coordinates",
							Property.Category.Preprocess_Steps
						),
						new Property.Data(
							importer,
							"PP_TUV_EVALUATE",
							"Transformations",
							Property.Type.FLAG,
							Property.Category.Preprocess_Steps,
							typeof(UVTrans),
							UVTrans.Rotation | UVTrans.Scalling | UVTrans.Translation
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_PreTransformVertices,
							"Pre-transform vertices",
							Property.Category.Preprocess_Steps
						),
						new Property.Data(
							importer,
							"PP_PTV_KEEP_HIERARCHY",
							"Keep the hierarchy",
							Property.Type.BOOL,
							Property.Category.Preprocess_Steps,
							false
						),
						new Property.Data(
							importer,
							"PP_PTV_NORMALIZE",
							"Normalize scale",
							Property.Type.BOOL,
							Property.Category.Preprocess_Steps,
							false
						),
						new Property.Data(
							importer,
							"PP_PTV_ADD_ROOT_TRANSFORMATION",
							"Add root transformation",
							Property.Type.BOOL,
							Property.Category.Preprocess_Steps,
							false
						),
						new Property.Data(
							importer,
							"PP_PTV_ROOT_TRANSFORMATION",
							"Root transformation",
							Property.Type.MATRIX,
							Property.Category.Preprocess_Steps,
							"0 0 0 0 0 0 1 1 1"
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_RemoveRedundantMaterials,
							"Remove redundant materials",
							Property.Category.Preprocess_Steps
						),
						new Property.Data(
							importer,
							"PP_RRM_EXCLUDE_LIST",
							"List of preserved materials",
							Property.Type.STRING,
							Property.Category.Preprocess_Steps,
							""
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_RemoveComponent,
							"Remove components",
							Property.Category.Preprocess_Steps
						),
						new Property.Data(
							importer,
							"PP_RVC_FLAGS",
							"Removed components",
							Property.Type.FLAG,
							Property.Category.Preprocess_Steps,
							typeof(Components),
							0
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_Debone,
							"Remove bones",
							Property.Category.Preprocess_Steps
						),
						new Property.Data(
							importer,
							"PP_DB_ALL_OR_NONE",
							"Debone all or nothing",
							Property.Type.BOOL,
							Property.Category.Preprocess_Steps,
							false
						),
						new Property.Data(
							importer,
							"PP_DB_THRESHOLD",
							"Deboning threshold",
							Property.Type.FLOAT,
							Property.Category.Preprocess_Steps,
							assimp_swig.AI_DEBONE_THRESHOLD
						)
					),
					new Property(
						new Property.Data(
							importer,
							aiPostProcessSteps.aiProcess_LimitBoneWeights,
							"Limit Bone Weights",
							Property.Category.Preprocess_Steps
						),
						new Property.Data(
							importer,
							"PP_LBW_MAX_WEIGHTS",
							"Nb of bones per vertex",
							Property.Type.INT,
							Property.Category.Preprocess_Steps,
							assimp_swig.AI_LMW_MAX_WEIGHTS
						)
					)
				};

				List<string> extensions = new List<string>();
					
				foreach(string file in filenames)
				{
					if(System.IO.File.Exists(file))
					{
						string[] file_components = file.Split('.');

						if(file_components.Length >= 2)
						{
							string file_extension = file_components[file_components.Length - 1];
							file_extension = file_extension.ToLower();

							if(!extensions.Contains(file_extension))
							{
								extensions.Add(file_extension);

								switch(file_extension)
								{
									case "fbx":
										FBXOptions(importer);

										break;
									case "dae":
										DAEOptions(importer);

										break;
									case "ifc":
										IFCOptions(importer);

										break;
									case "3dxml":
										Dassault3DXMLOptions(importer);

										break;
								}
							}
						}
					}
				}
			}
		}

		//FBX specifics Options;
		private void FBXOptions(Importer importer)
		{
			if(importer != null)
			{
				List<Property> fbx_options = new List<Property> {
					new Property(
						new Property.Data(
							importer,
							"IMPORT_FBX_READ_ALL_GEOMETRY_LAYERS",
							"Read all geometry layers",
							Property.Type.BOOL,
							Property.Category.Extension_Related_Options,
							true
						)
					),
					new Property(
						new Property.Data(
							importer,
							"IMPORT_FBX_READ_ALL_MATERIALS",
							"Read all materials", 
							Property.Type.BOOL,
							Property.Category.Extension_Related_Options,
							false
						)
					),
					new Property(
						new Property.Data(
							importer,
							"IMPORT_FBX_READ_MATERIALS",
							"Read materials", 
							Property.Type.BOOL,
							Property.Category.Extension_Related_Options,
							true
						)
					),
					new Property(
						new Property.Data(
							importer,
							"IMPORT_FBX_READ_CAMERAS",
							"Read cameras",
							Property.Type.BOOL,
							Property.Category.Extension_Related_Options,
							true
						)
					),
					new Property(
						new Property.Data(
							importer,
							"IMPORT_FBX_READ_LIGHTS",
							"Read lights",
							Property.Type.BOOL,
							Property.Category.Extension_Related_Options,
							true
						)
					),
					new Property(
						new Property.Data(
							importer,
							"IMPORT_FBX_READ_ANIMATIONS",
							"Read animations",
							Property.Type.BOOL,
							Property.Category.Extension_Related_Options,
							true
						)
					),
					new Property(
						new Property.Data(
							importer,
							"IMPORT_FBX_STRICT_MODE",
							"Strict FBX 2013 mode",
							Property.Type.BOOL,
							Property.Category.Extension_Related_Options,
							false
						)
					),
					new Property(
						new Property.Data(
							importer,
							"IMPORT_FBX_PRESERVE_PIVOTS",
							"Preserve pivots",
							Property.Type.BOOL,
							Property.Category.Extension_Related_Options,
							true
						)
					),
					new Property(
						new Property.Data(
							importer,
							"IMPORT_FBX_OPTIMIZE_EMPTY_ANIMATION_CURVES",
							"Optimize empty animations",
							Property.Type.BOOL,
							Property.Category.Extension_Related_Options,
							true
						)
					)
				};

				optionsList.AddRange(fbx_options);
			}
		}

		//DAE specifics Options;	
		private void DAEOptions(Importer importer)
		{
			if(importer != null)
			{
				List<Property> dae_options = new List<Property>{
					new Property(
						new Property.Data(
							importer,
							"IMPORT_COLLADA_IGNORE_UP_DIRECTION",
							"Ignore up direction",
							Property.Type.BOOL,
							Property.Category.Extension_Related_Options,
							false
						)
					)
				};

				optionsList.AddRange(dae_options);
			}
		}

		//IFC specifics Options;	
		private void IFCOptions(Importer importer)
		{
			if(importer != null)
			{
				List<Property> ifc_options = new List<Property>{
					new Property(
						new Property.Data(
							importer,
							"IMPORT_IFC_SKIP_SPACE_REPRESENTATIONS",
							"Skips IfcSpace elements",
							Property.Type.BOOL,
							Property.Category.Extension_Related_Options,
							true
						)
					),
					new Property(
						new Property.Data(
							importer,
							"IMPORT_IFC_CUSTOM_TRIANGULATION",
							"Use IFC special triangulation",
							Property.Type.BOOL,
							Property.Category.Extension_Related_Options,
							true
						)
					),
					new Property(
						new Property.Data(
							importer,
							"IMPORT_IFC_SMOOTHING_ANGLE",
							"Smoothing angle",
							Property.Type.FLOAT,
							Property.Category.Extension_Related_Options,
							assimp_swig.AI_IMPORT_IFC_DEFAULT_SMOOTHING_ANGLE
						)
					),
					new Property(
						new Property.Data(
							importer,
							"IMPORT_IFC_CYLINDRICAL_TESSELLATION",
							"Cylindrical tessellation",
							Property.Type.INT,
							Property.Category.Extension_Related_Options,
							assimp_swig.AI_IMPORT_IFC_DEFAULT_CYLINDRICAL_TESSELLATION
						)
					)
				};
				
				optionsList.AddRange(ifc_options);
			}
		}

		//3DXML specifics Options;	
		private void Dassault3DXMLOptions(Importer importer)
		{
			if(importer != null)
			{
				List<Property> dassault_3dxml_options = new List<Property>{
					new Property(
						new Property.Data(
							importer,
							"IMPORT_3DXML_USE_COMPLEX_MATERIALS",
							"Use complex materials",
							Property.Type.BOOL,
							Property.Category.Extension_Related_Options,
							false
						)
					),
					new Property(
						new Property.Data(
							importer,
							"IMPORT_3DXML_USE_NODE_MATERIALS",
							"Use hierarchical materials",
							Property.Type.BOOL,
							Property.Category.Extension_Related_Options,
							false
						)
					),
					new Property(
						new Property.Data(
							importer,
							"IMPORT_3DXML_USE_REFERENCES_NAMES",
							"Use names of references instead of instances",
							Property.Type.BOOL,
							Property.Category.Extension_Related_Options,
							false
						)
					)
				};
				
				optionsList.AddRange(dassault_3dxml_options);
			}
		}
		#endregion
	}
}

#endif // UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
