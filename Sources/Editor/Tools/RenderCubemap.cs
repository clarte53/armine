#if UNITY_EDITOR_WIN

using UnityEditor;
using UnityEngine;

namespace Armine.Editor.Tools
{
	internal class RenderCubemap : EditorWindow
	{
		[SerializeField]
		private Cubemap cubemapOutput;

		[SerializeField]
		private Transform viewPoint;

		[MenuItem("Armine/Tools/Render Cubemap", false, 11)]
		private static void ShowCubemap()
		{
			EditorWindow.GetWindow(typeof(RenderCubemap), false, "Cubemap");
		}
		
		[MenuItem("Armine/Tools/Render Cubemap", true)]
		private static bool ValidateShowCubemap()
		{
			return Utils.License.IsLicensed() && Utils.License.ToolScriptsArePermited();
		}

		private void OnGUI()
		{
			if(Utils.License.IsLicensed() && Utils.License.ToolScriptsArePermited())
			{
				GUILayout.BeginVertical();

				GUILayout.Label("Render scene into Cubemap: ");

				GUIContent[] labels = {
					new GUIContent("Cubemap: "),
					new GUIContent("Position: ")
				};

				float width = 0.0f;
				foreach(GUIContent label in labels)
				{
					float min;
					float max;

					GUI.skin.label.CalcMinMaxWidth(label, out min, out max);

					width = System.Math.Max(width, min);
				}
				
				GUILayout.BeginHorizontal();

				GUILayout.BeginVertical(GUILayout.Width(width));
				foreach(GUIContent label in labels)
				{
					GUILayout.Label(label);
				}
				GUILayout.EndVertical();

				GUILayout.BeginVertical();
				cubemapOutput = (Cubemap) EditorGUILayout.ObjectField(cubemapOutput, typeof(Cubemap), true);
				viewPoint = (Transform) EditorGUILayout.ObjectField(viewPoint, typeof(Transform), true);
				GUILayout.EndVertical();

				GUILayout.EndHorizontal();

				bool create = GUILayout.Button("Create scene");

				bool enabled = GUI.enabled;
				GUI.enabled = cubemapOutput != null && viewPoint != null;

				bool render = GUILayout.Button("Render Cubemap");

				GUI.enabled = enabled;

				GUILayout.EndVertical();

				if(create)
				{
					CreateScene();
				}

				if(render && cubemapOutput != null && viewPoint != null)
				{
					Render();

					Utils.License.DecrementToolCount();
				}
			}
		}

		private void CreateScene()
		{
			// Create a new scene
			#if UNITY_5_3_OR_NEWER || UNITY_5_3 // Because UNITY_X_Y_OR_NEWER is defined starting Unity 5.3.4
			UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
			#else
			EditorApplication.NewScene();
			#endif

			GameObject light = new GameObject("Light");

			light.AddComponent<Light>();

			light.transform.Translate(0.0f, 2.0f, 0.0f);

			const float width = 10.0f;
			const float height = 4.0f;
			const int steps = 50; // Min: 3

			float scale = (float) ((double) width * System.Math.Sin(System.Math.PI / (double) steps));
			float distance = (float) (0.5 * (double) width * System.Math.Cos(System.Math.PI / (double) steps));

			Material darker = new Material(Shader.Find("Diffuse"));
			darker.SetColor("_Color", new Color32(128, 128, 128, 255));

			Material lighter = new Material(Shader.Find("Diffuse"));
			lighter.SetColor("_Color", new Color32(255, 255, 255, 255));

			GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
			floor.name = "Floor";
			floor.GetComponent<MeshRenderer>().sharedMaterial = darker;
			floor.transform.Rotate(90.0f, 0.0f, 0.0f);
			floor.transform.localScale = new Vector3(width, width, 1.0f);

			GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Quad);
			ceiling.name = "Ceiling";
			ceiling.GetComponent<MeshRenderer>().sharedMaterial = lighter;
			ceiling.transform.Translate(0.0f, height, 0.0f);
			ceiling.transform.Rotate(-90.0f, 0.0f, 0.0f);
			ceiling.transform.localScale = new Vector3(width, width, 1.0f);

			GameObject walls = new GameObject();
			walls.name = "Walls";

			for(int i = 0; i < steps; i++)
			{
				GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
				wall.name = "Wall_" + (i + 1);
				wall.GetComponent<MeshRenderer>().sharedMaterial = darker;

				wall.transform.Rotate(0.0f, (float) i * 360.0f / (float) steps, 0.0f);
				wall.transform.Translate(0.0f, height / 2.0f, distance);
				wall.transform.localScale = new Vector3(scale, height, 1.0f);

				wall.transform.parent = walls.transform;
			}

			viewPoint = light.transform;
		}

		private void Render()
		{
			// Get the main camera for rendering
			GameObject camera = GameObject.Find("Main Camera");

			// Is the main camera already exist?
			bool use_main_camera = true;

			if(camera == null)
			{
				// Create a new temporary camera
				camera = new GameObject("Main Camera");

				// Add the camera
				camera.AddComponent<Camera>();

				// We use a custom camera
				use_main_camera = false;
			}

			// Save camera original transformation
			Transform transform = camera.transform;

			// Place the camera on the object
			camera.transform.position = viewPoint.position;
			camera.transform.rotation = Quaternion.identity;
			camera.transform.localScale = Vector3.one;

			// Render into cubemap		
			camera.GetComponent<Camera>().RenderToCubemap(cubemapOutput);

			if(use_main_camera)
			{
				// Restore original camera transformation
				camera.transform.position = transform.position;
				camera.transform.rotation = transform.rotation;
				camera.transform.localScale = transform.localScale;
			}
			else
			{
				// Destroy temporary camera
				DestroyImmediate(camera);
			}
		}
	}
}

#endif // UNITY_EDITOR_WIN
