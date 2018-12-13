Armine
===============
3D file import/export module for Unity based on Assimp (https://github.com/assimp/assimp)
Depends on [clarte-utils](ssh://git@gitlab.clarte.asso.fr:53000/modules/clarte-utils.git "clarte-utils") module (GitHub url will be added soon for external reference).

Basic usage may be as simple as calling Armine.Model.Importer.Import(string filename).

A more usable, asynchronous example may be found below:
```
using Armine.Model;
using UnityEngine;

public class Load : MonoBehaviour
{
	[SerializeField]
	string filename = "c:\\toto.obj";

	private Importer armineImporter;

	void Start ()
	{
		armineImporter = new Importer();

		try
		{
			StartCoroutine(armineImporter.Import(filename, armineImporterReturnCallback, armineImporterProgressCallback));
		}
		catch(System.Exception ex)
		{
			Debug.LogException(ex, this);
		}
	}

	// Callback on import completion
	private void armineImporterReturnCallback(GameObject obj)
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
	private void armineImporterProgressCallback(float progress)
	{
		Debug.Log("Loading " + (100.0f * progress).ToString("F0") + "% done");
	}
}
```