using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
	public static List<Waypoint> _MapLinks = new();

	public List<Waypoint> _Links = null;

	void OnEnable() => _MapLinks.Add(this);
	void OnDisable() => _MapLinks.Remove(this);

	void OnDrawGizmos()
	{
		foreach (Waypoint waypoint in _Links)
		{
			Gizmos.DrawLine(transform.position, waypoint.transform.position);
		}
	}
}
