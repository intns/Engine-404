using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(Waypoint))]
[CanEditMultipleObjects]
public class WaypointEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		Waypoint way = (Waypoint)target;

		if (GUILayout.Button("Set as Destination"))
		{
			if ((way._Type & WaypointType.Onion) != 0
			    || (way._Type & WaypointType.Ship) != 0)
			{
				way._Connections.Clear();
				way._Next = null;
			}
			else
			{
				Debug.LogWarning("Unable to set destination, as it isn't a custom waypoint type");
			}
		}

		if (GUILayout.Button("Set Closest Node"))
		{
			way.CalculateClosest();
		}

		if (GUILayout.Button("Generate ID"))
		{
			way.GenerateID();
		}
	}
}
#endif