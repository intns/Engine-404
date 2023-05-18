using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[Flags]
public enum WaypointType
{
	Path = 1 << 0,
	Onion = 1 << 1,
	Ship = 1 << 2,
}

public class Waypoint : MonoBehaviour
{
	[Header("Settings")]
	public WaypointType _Type = WaypointType.Path;

	[FormerlySerializedAs("_Destinations")]
	[Header("Components")]
	public List<Waypoint> _Connections;
	public Waypoint _Next;

	void OnDrawGizmos()
	{
		Vector3 position = transform.position;
		Handles.Label(position + Vector3.up, name);
		Gizmos.DrawSphere(position, transform.localScale.magnitude / 2.0f);

		if (Selection.Contains(gameObject))
		{
			Gizmos.color = Color.green;

			if (_Next != null)
			{
				Gizmos.DrawLine(transform.position, _Next.transform.position);
			}

			int bail = 0;

			for (Waypoint n = this; n != null && n._Next != null; n = n._Next)
			{
				if (bail++ > 50)
				{
					break;
				}

				Gizmos.DrawLine(n.transform.position, n._Next.transform.position);
			}
		}

		foreach (Waypoint marker in _Connections)
		{
			Gizmos.color = Selection.Contains(gameObject) ? Color.red : Color.blue;

			if (!Selection.Contains(marker.gameObject))
			{
				Gizmos.DrawLine(transform.position + Vector3.up, marker.transform.position + Vector3.up);
			}
		}
	}

	public void CalculateClosest()
	{
		List<Transform> children
			= transform.parent.GetComponentsInChildren<Transform>().Where(c => c != transform).ToList();
		Transform closest = MathUtil.GetClosestTransform(transform.position, children, out int index);
		TEST_Waypoint closestWP = closest.GetComponent<TEST_Waypoint>();

		_Destinations = new(1) { closestWP };
		_Next = closestWP;

		EditorUtility.SetDirty(this);
	}

	public void GenerateID()
	{
		string prefix = "WP";

		if (_Type == WaypointType.Onion)
		{
			prefix += "_Onion";
		}
		else if (_Type == WaypointType.Ship)
		{
			prefix += "_Ship";
		}

		string typeName = _Type.ToString();
		string index = transform.GetSiblingIndex().ToString();
		name = $"{prefix}_{typeName}_{index}";
	}
}
