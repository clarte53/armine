using UnityEngine;

namespace CLARTE.Shaders.Standard
{
	public class Utility : MonoBehaviour
	{
		public enum WorkflowMode
		{
			SPECULAR,
			METALLIC,
			DIELECTRIC
		}
		
		public enum BlendMode
		{
			OPAQUE,
			CUTOUT,
			FADE,
			TRANSPARENT
		}

		#region Public methods
		public static bool IsStandardShader(Shader shader)
		{
			return shader.name.StartsWith("Standard", System.StringComparison.Ordinal) || shader.name.StartsWith("CLARTE/Standard/", System.StringComparison.Ordinal);
		}

		public static bool AssignStandardShaderToMaterial(Material material, Shader new_shader)
		{
			if(material == null || new_shader == null)
			{
				return false;
			}

			Shader old_shader = material.shader;

			bool standard = IsStandardShader(old_shader);
			bool new_standard = IsStandardShader(new_shader);
			bool legacy = old_shader.name.StartsWith("Legacy Shaders/", System.StringComparison.Ordinal) || old_shader.name.StartsWith("CLARTE/", System.StringComparison.Ordinal);
			bool unlit_cutout = old_shader.name.StartsWith("Unlit/Transparent", System.StringComparison.Ordinal);

			if(old_shader == null || ! (new_standard && (standard || legacy || unlit_cutout)))
			{
				return false;
			}

			material.shader = new_shader;

			if(! standard)
			{
				BlendMode blend_mode = BlendMode.OPAQUE;

				if((legacy && old_shader.name.Contains("/Transparent/Cutout/")) || unlit_cutout)
				{
					blend_mode = BlendMode.CUTOUT;
				}
				else if(legacy && old_shader.name.Contains("/Transparent/"))
				{
					blend_mode = BlendMode.FADE;
				}
				
				material.SetFloat("_Mode", (float) blend_mode);

				MaterialChanged(material);
			}

			return true;
		}

		public static void MaterialChanged(Material material)
		{
			if(IsStandardShader(material.shader))
			{
				WorkflowMode workflow_mode = DetermineWorkflow(material);
				
				SetupMaterialWithBlendMode(material, (BlendMode) material.GetFloat("_Mode"));
				
				SetMaterialKeywords(material, workflow_mode);
			}
		}
		#endregion

		#region Material properties handling
		private static WorkflowMode DetermineWorkflow(Material material)
		{
			WorkflowMode workflow_mode;
			
			if(material.HasProperty("_SpecGlossMap") && material.HasProperty("_SpecColor"))
			{
				workflow_mode = WorkflowMode.SPECULAR;
			}
			else if(material.HasProperty("_MetallicGlossMap") && material.HasProperty("_Metallic"))
			{
				workflow_mode = WorkflowMode.METALLIC;
			}
			else
			{
				workflow_mode = WorkflowMode.DIELECTRIC;
			}
			
			return workflow_mode;
		}

		private static void SetupMaterialWithBlendMode(Material material, BlendMode blend_mode)
		{
			switch(blend_mode)
			{
				case BlendMode.OPAQUE:
					material.SetInt("_SrcBlend", 1);
					material.SetInt("_DstBlend", 0);
					material.SetInt("_ZWrite", 1);
					material.DisableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = -1;
					break;
				case BlendMode.CUTOUT:
					material.SetInt("_SrcBlend", 1);
					material.SetInt("_DstBlend", 0);
					material.SetInt("_ZWrite", 1);
					material.EnableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = 2450;
					break;
				case BlendMode.FADE:
					material.SetInt("_SrcBlend", 5);
					material.SetInt("_DstBlend", 10);
					material.SetInt("_ZWrite", 0);
					material.DisableKeyword("_ALPHATEST_ON");
					material.EnableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = 3000;
					break;
				case BlendMode.TRANSPARENT:
					material.SetInt("_SrcBlend", 1);
					material.SetInt("_DstBlend", 10);
					material.SetInt("_ZWrite", 0);
					material.DisableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = 3000;
					break;
			}
		}
		
		private static void SetMaterialKeywords(Material material, WorkflowMode workflow_mode)
		{
			SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap") || material.GetTexture("_DetailNormalMap"));

			if(workflow_mode == WorkflowMode.SPECULAR)
			{
				SetKeyword(material, "_SPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
			}
			else if(workflow_mode == WorkflowMode.METALLIC)
			{
				SetKeyword(material, "_METALLICGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
			}

			SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));
			SetKeyword(material, "_DETAIL_MULX2", material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap"));

			bool flag = ShouldEmissionBeEnabled(material.GetColor("_EmissionColor"));

			SetKeyword(material, "_EMISSION", flag);

			MaterialGlobalIlluminationFlags material_global_illumination_flags = material.globalIlluminationFlags;

			if((material_global_illumination_flags & (MaterialGlobalIlluminationFlags.RealtimeEmissive | MaterialGlobalIlluminationFlags.BakedEmissive)) != MaterialGlobalIlluminationFlags.None)
			{
				material_global_illumination_flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;

				if(! flag)
				{
					material_global_illumination_flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
				}

				material.globalIlluminationFlags = material_global_illumination_flags;
			}
		}

		private static void SetKeyword(Material material, string keyword, bool state)
		{
			if(state)
			{
				material.EnableKeyword(keyword);
			}
			else
			{
				material.DisableKeyword(keyword);
			}
		}
		#endregion

		#region Emissive handling
		public static bool ShouldEmissionBeEnabled(Color color)
		{
			return color.grayscale > 0.000392156857f;
		}
		#endregion
	}
}
