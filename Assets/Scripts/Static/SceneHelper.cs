using UnityEditor;
using UnityEngine;

public static class SceneHelper
{
	public static void SetupNewScene(ref GameObject player, ref GameObject sceneMaster)
	{
		// Create Scene Master Prefab
		sceneMaster = (GameObject)PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/pref_SceneMaster") as GameObject);

		// Create Directional Light and assign it to the day manager
		GameObject light = new GameObject("Directional Light");
		Light lightComp = light.AddComponent<Light>();
		lightComp.intensity = 7.5f;
		lightComp.type = LightType.Directional;
		light.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(50, -30, 0));
		sceneMaster.GetComponentInChildren<DayTimeManager>()._SunLight = light.transform;

		// Create Player Prefab
		player = PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/Player/pref_Player_Set") as GameObject) as GameObject;
	}

	public static void SetupNewScene()
	{
		// Create Scene Master Prefab
		GameObject sceneMaster = (GameObject)PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/pref_SceneMaster") as GameObject);

		// Create Directional Light and assign it to the day manager
		GameObject light = new GameObject("Directional Light");
		Light lightComp = light.AddComponent<Light>();
		lightComp.intensity = 7.5f;
		lightComp.type = LightType.Directional;
		light.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(50, -30, 0));
		sceneMaster.GetComponentInChildren<DayTimeManager>()._SunLight = light.transform;

		// Create Player Prefab
		PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/Player/pref_Player_Set") as GameObject);

		// Create surface for the Player to walk on
		GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
		floor.transform.position = Vector3.down;
		floor.transform.localScale = new Vector3(5, 1, 5);
		floor.name = "Floor";
		floor.layer = LayerMask.NameToLayer("Map");
	}
}