using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WayPointManager))]
public class EditorWaypointManager : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		WayPointManager way = (WayPointManager)target;

		if (GUILayout.Button("Calculate Distance"))
		{
			way.CalculateDistance(false);
			Debug.Log("Complete");
		}

		if (GUILayout.Button("Clear Paths"))
		{
			way.CalculateDistance(true);
			Debug.Log("ClearedPaths");
		}

		if (GUILayout.Button("Generate IDs"))
		{
			TEST_Waypoint[] wps = way.GetComponentsInChildren<TEST_Waypoint>();
			foreach (TEST_Waypoint wp in wps)
			{
				wp.GenerateID();
			}
		}
	}
}

[CustomEditor(typeof(TEST_Waypoint))]
public class EditorWaypoint : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		TEST_Waypoint way = (TEST_Waypoint)target;

		if (GUILayout.Button("Calculate Closest Node"))
		{
			way.CalculateClosest();
		}

		if (GUILayout.Button("Generate ID"))
		{
			way.GenerateID();
		}
	}
}