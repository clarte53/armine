Armine
===============

3D file import/export module for Unity based on Assimp (https://github.com/assimp/assimp).
Depends on [clarte-utils](https://github.com/clarte53/armine.git "clarte-utils")
module.

Armine is a 3D model data import / export hub between multiple sources.
The sources included by default are Unity engine, a serialized binary
version of unity scenes and the librairy Assimp that can load multiple 3D models
formats.

Loading is designed to be asynchronous and use multiple CPU cores as much as
possible to speed up the loading process.

Getting started
===============

First, this repository as well as [clarte-utils](https://github.com/clarte53/armine.git "clarte-utils")
repository must be added to your Unity project as submodules inside the
"Assets" directory. The required commit hash of the clarte-utils repository can
be found in the file "dependencies.xml". 

This repository was not added as a submodule itself to avoid problems of
multiple submodules each using their own version of clarte-utils as submodule.
Therefore, the task to maintain coherency between armine and clarte-utils
repositories is left to the developpers.

Usage
===============

Basic usage may be as simple as calling Armine.Model.Importer.Import(string filename).

A more usable, asynchronous example may be found below:
```
using Armine.Model;
using UnityEngine;

public class Load : MonoBehaviour
{
	public string filename = "c:\\model.obj";

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
importer.Assimp.ResetToDefaultOptions();

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


Architecture
===============

Armine is designed to import and export 3D models files from / to multiple sources.
As such, to simplify the interdependancies between the sources, Armine is
designed primarily as a hub between multiple plugins (sources), with a common
intermediary format as the exchange protocol between the plugins.

Because the main objective of the project is to load 3D model data into the
Unity engine, the intermediary format is designed to be as fast as possible to
load in Unity. In particular, Unity imposes strong restictions on parallelism
of tasks. As a consequence, the intermediary format almost perfectly mimic the
internal Unity format. However, this format provides the means for true
asynchronous loading in the other plugins. Therefore, most of the loading
process is asynchronous, while only a small part is executed in a coroutine
to finalize the transition to Unity thread.

The plugin system is designed around two main concepts:
- Inheritance from IImporter or IExporter interfaces to create new source
plugins, then registration of the plugin to the Importer or Exporter with the
AddModule() method.
- Extension of the partial classes in Armine.Model.Type to get access to the
(often private) exchange types internal data. Typically the following two
methods should be added to every types, where X is the name of the plugin, T1
and T2 the exchange type and the source type respectively:
    - public static T1 FromX(...)
    - public T2 ToX(...)

