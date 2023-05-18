using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WayPointManager))]
public class WaypointManagerEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		WayPointManager way = (WayPointManager)target;

		if (GUILayout.Button("Generate IDs"))
		{
			Waypoint[] wps = way.GetComponentsInChildren<Waypoint>();

			// Reorder the waypoints in the hierarchy
			foreach (Waypoint wp in wps)
			{
				wp.GenerateID();
			}

			Array.Sort(wps, (wp1, wp2) => string.Compare(wp1.name, wp2.name, StringComparison.Ordinal));
			for (int i = 0; i < wps.Length; i++)
			{
				wps[i].transform.SetSiblingIndex(i);
			}

			foreach (Waypoint wp in wps)
			{
				wp.GenerateID();
			}
		}

		if (GUILayout.Button("Place On Map"))
		{
			Waypoint[] wps = way.GetComponentsInChildren<Waypoint>();

			foreach (Waypoint wp in wps)
			{
				if (Physics.Raycast(
					    wp.transform.position + Vector3.up * 1000.0f,
					    Vector3.down,
					    out RaycastHit info,
					    float.PositiveInfinity,
					    way._MapMask,
					    QueryTriggerInteraction.Ignore
				    ))
				{
					wp.transform.position = info.point + Vector3.up * 2.5f;
					EditorUtility.SetDirty(wp.transform);
				}
			}
		}
	}
}

[CustomEditor(typeof(Waypoint))]
[CanEditMultipleObjects]
public class EditorWaypoint : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		Waypoint way = (Waypoint)target;

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
