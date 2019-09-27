Armine
===============

3D file import/export module for Unity based on Assimp (https://github.com/assimp/assimp).
Depends on [clarte-utils](https://github.com/clarte53/clarte-utils.git "clarte-utils") module.

Armine is a 3D model data import / export hub between multiple sources.
The sources included by default are:
- Unity engine, a serialized binary version of Unity scenes;
- Assimp library, that can load multiple 3D models formats.

Loading is designed to be performed at runtime in an asynchronous fashion and
uses as much CPU cores as available to speed up the loading process.

Getting started
===============

First, this repository as well as [clarte-utils](https://github.com/clarte53/clarte-utils.git "clarte-utils")
repository must be cloned into your Unity project as submodules inside the "Assets"
directory. The required commit hash of the clarte-utils repository can be found
in the file "dependencies.xml". More recent commits might also work, but have not
extensively been tested.

clarte-utils is not directly provided as a submodule of Armine to avoid problems
of multiple inclusion of the same submodule (in case of multiple modules depending
on the same dependency). Therefore, the task to maintain coherency between armine
and clarte-utils repositories is left to the developers.

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

Similarly, exporter is defined as Armine.Model.Exporter.

Configuration
===============

When using the Assimp source, import can be configured to take advantage of
loading options and postprocessing features of Assimp.

To set Assimp options, one can use the methods defined in Armine.Model.Importer.Assimp as follows:
```
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
Options are automatically saved to playerprefs, so next imports will automatically
use the same options. 

To avoid this default behavior, call the following method:
```
// Reset options to default values
importer.Assimp.ResetToDefaultOptions();
```

Architecture
===============

Armine is designed to import and export 3D models files from / to multiple sources.
As such, to simplify the interdependancies between sources, Armine is primarily
designed as a hub between multiple plugins (sources), with a common intermediary
format as the exchange protocol between the plugins.

Because the main objective of the project is to load 3D model data into the Unity engine,
the intermediary format is designed to be as fast as possible to load in Unity.
In particular, Unity imposes strong restrictions on parallelism of tasks.
As a consequence, the intermediary format almost perfectly mimics the internal
Unity format. However, this format provides the means for true asynchronous
loading in the other plugins. Therefore, most of the loading process is asynchronous,
while only a small part is executed in a coroutine to finalize the transition to Unity thread.

The plugin system is designed around two main concepts:
- Inheritance from IImporter or IExporter interfaces to create new source plugins,
then registration of the plugin to the Importer or Exporter with the AddModule() method.
- Extension of the partial classes in Armine.Model.Type to get access to the
(often private) exchange types internal data. Typically the following two methods
should be added to every types, where X is the name of the plugin, T1 and T2 the
exchange type and the source type respectively:
    - public static T1 FromX(...)
    - public T2 ToX(...)