Armine
===============

3D file import/export module for Unity based on Assimp (https://github.com/assimp/assimp)
Depends on [clarte-utils](ssh://git@gitlab.clarte.asso.fr:53000/modules/clarte-utils.git "clarte-utils")
module (GitHub url will be added soon for external reference).

Armine can be described as a 3D model data import / export hub between multiple
sources. The sources included by default are Unity engine, a serialized binary
version of unity scenes and the librairy Assimp that can load multiple 3D models
formats.

Loading is desgned to be asynchronous and use multiple CPU cores as much as
possible to speed up the loading process.

Usage
===============

Basic usage may be as simple as calling Armine.Model.Importer.Import(string filename).

A more usable, asynchronous example may be found below:
```
using Armine.Model;
using UnityEngine;

public class Load : MonoBehaviour
{
	[SerializeField]
	string filename = "c:\\model.obj";

	private Importer importer;

	void Start ()
	{
		importer = new Importer();

		try
		{
			StartCoroutine(importer.Import(filename, ReturnCallback, ProgressCallback));
		}
		catch(System.Exception ex)
		{
			Debug.LogException(ex, this);
		}
	}

	// Callback on import completion
	private void ReturnCallback(GameObject obj)
	{
		if(obj != null)
		{
			Debug.Log(obj.name + " successfully loaded");
		}
		else
		{
			Debug.LogError("Failed to load " + obj.name);
		}
	}

	// Callback on progress update
	private void ProgressCallback(float progress)
	{
		Debug.Log("Loading " + (100.0f * progress).ToString("F0") + "% done");
	}
}
```

Similarely, exporter is defined as Armine.Model.Exporter.


Configuration
===============

Loading from 3D model files with Assimp can be configured to take advantage of
loading options and postprocess features of Assimp.

Options are automatically saved to playerprefs, so next imports will
automatically use the same options. To set Armine options, one can use the
methods defined in Armine.Model.Importer.Assimp as such:
```
// Reset options to default values
importer.Assimp.SetProperty("GLOB_MEASURE_TIME", false);
importer.Assimp.SetProperty("IMPORT_NO_SKELETON_MESHES", false);
importer.Assimp.SetProperty("FAVOUR_SPEED", false);
importer.Assimp.SetProperty("GLOB_MULTITHREADING", -1);
importer.Assimp.SetProperty("PP_FID_ANIM_ACCURACY", 0.0f);
importer.Assimp.SetProperty("PP_FD_REMOVE", false);
importer.Assimp.SetProperty("PP_ICL_PTCACHE_SIZE", Assimp.assimp_swig.PP_ICL_PTCACHE_SIZE);
importer.Assimp.SetProperty("PP_SBBC_MAX_BONES", Assimp.assimp_swig.AI_SBBC_DEFAULT_MAX_BONES);
importer.Assimp.SetProperty("PP_SBP_REMOVE", 0);
importer.Assimp.SetProperty("PP_OG_EXCLUSIVE_LIST", "");
importer.Assimp.SetProperty("PP_GSN_MAX_SMOOTHING_ANGLE", 175.0f);
importer.Assimp.SetProperty("PP_CT_MAX_SMOOTHING_ANGLE", 45.0f);
importer.Assimp.SetProperty("PP_CT_TEXTURE_CHANNEL_INDEX", 0);
importer.Assimp.SetProperty("PP_TUV_EVALUATE", 0x2 | 0x1 | 0x4);
importer.Assimp.SetProperty("PP_PTV_KEEP_HIERARCHY", false);
importer.Assimp.SetProperty("PP_PTV_NORMALIZE", false);
importer.Assimp.SetProperty("PP_PTV_ADD_ROOT_TRANSFORMATION", false);
importer.Assimp.SetProperty("PP_PTV_ROOT_TRANSFORMATION", "0 0 0 0 0 0 1 1 1");
importer.Assimp.SetProperty("PP_RRM_EXCLUDE_LIST", "");
importer.Assimp.SetProperty("PP_RVC_FLAGS", 0);
importer.Assimp.SetProperty("PP_DB_ALL_OR_NONE", false);
importer.Assimp.SetProperty("PP_DB_THRESHOLD", (float) Assimp.assimp_swig.AI_DEBONE_THRESHOLD);
importer.Assimp.SetProperty("PP_LBW_MAX_WEIGHTS", Assimp.assimp_swig.AI_LMW_MAX_WEIGHTS);
importer.Assimp.SetProperty("IMPORT_FBX_READ_ALL_GEOMETRY_LAYERS", true);
importer.Assimp.SetProperty("IMPORT_FBX_READ_ALL_MATERIALS", false);
importer.Assimp.SetProperty("IMPORT_FBX_READ_MATERIALS", true);
importer.Assimp.SetProperty("IMPORT_FBX_READ_CAMERAS", true);
importer.Assimp.SetProperty("IMPORT_FBX_READ_LIGHTS", true);
importer.Assimp.SetProperty("IMPORT_FBX_READ_ANIMATIONS", true);
importer.Assimp.SetProperty("IMPORT_FBX_STRICT_MODE", false);
importer.Assimp.SetProperty("IMPORT_FBX_PRESERVE_PIVOTS", true);
importer.Assimp.SetProperty("IMPORT_FBX_OPTIMIZE_EMPTY_ANIMATION_CURVES", true);
importer.Assimp.SetProperty("IMPORT_COLLADA_IGNORE_UP_DIRECTION", false);
importer.Assimp.SetProperty("IMPORT_3DXML_USE_NODE_MATERIALS", true);
importer.Assimp.SetProperty("IMPORT_3DXML_USE_COMPLEX_MATERIALS", true);

// Set some options
importer.Assimp.SetProperty("PP_GSN_MAX_SMOOTHING_ANGLE", 80.0f);
importer.Assimp.SetProperty(
	"PP_RVC_FLAGS",
	(int) (
		global::Armine.Model.Option.Components.Colors |
		global::Armine.Model.Option.Components.Bone_Weights |
		global::Armine.Model.Option.Components.Animations |
		global::Armine.Model.Option.Components.Lights |
		global::Armine.Model.Option.Components.Cameras
	)
);

// Use quality profile to set post process steps to use 
importer.Assimp.ChangeFlag(Assimp.aiPostProcessSteps.aiProcessPreset_TargetRealtime_MaxQuality, true);

// Set specific post process steps to create a custom profile
importer.Assimp.ChangeFlag(Assimp.aiPostProcessSteps.aiProcess_GenSmoothNormals, true);
importer.Assimp.ChangeFlag(Assimp.aiPostProcessSteps.aiProcess_RemoveComponent, true);
```
