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
		}

		if (GUILayout.Button("Clear Paths"))
		{
			way.CalculateDistance(true);
		}

		if (GUILayout.Button("Generate IDs"))
		{
			TEST_Waypoint[] wps = way.GetComponentsInChildren<TEST_Waypoint>();
			foreach (TEST_Waypoint wp in wps)
			{
				if (way._Home == wp)
				{
					wp.name = "WP_HOME";
					continue;
				}

				wp.GenerateID();
			}
		}

		if (GUILayout.Button("Place On Map"))
		{
			TEST_Waypoint[] wps = way.GetComponentsInChildren<TEST_Waypoint>();
			foreach (TEST_Waypoint wp in wps)
			{
				if (Physics.Raycast(wp.transform.position + Vector3.up * 1000.0f, Vector3.down, out RaycastHit info, float.PositiveInfinity, way._MapMask, QueryTriggerInteraction.Ignore))
				{
					wp.transform.position = info.point + Vector3.up * 2.5f;
					EditorUtility.SetDirty(wp.transform);
				}
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