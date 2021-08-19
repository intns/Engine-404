using UnityEditor;
using UnityEngine;

public class SetupScene
{
	[MenuItem("Project 404/Setup New Scene")]
	public static void SetupNewScene_Editor()
	{
		// Destroy Scene and re-create
		GameObject[] objects = Object.FindObjectsOfType<GameObject>();
		for (int i = 0; i < objects.Length; i++)
		{
			Object.DestroyImmediate(objects[i]);
		}

		SceneHelper.SetupNewScene();
	}
}
