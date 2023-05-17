using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class TEST_Waypoint : MonoBehaviour
{
	public List<TEST_Waypoint> _Destinations;
	public TEST_Waypoint _Next;

	void OnDrawGizmos()
	{
		Handles.Label(transform.position + Vector3.up, name);
		Gizmos.DrawSphere(transform.position, transform.localScale.magnitude/2.0f);

		if (Selection.Contains(gameObject))
		{
			Gizmos.color = Color.green;
			if (_Next != null)
			{
				 Gizmos.DrawLine(transform.position, _Next.transform.position);
			}

			int bail = 0;
			for (TEST_Waypoint n = this; n != null && n._Next != null; n = n._Next)
			{
				if (bail++ > 50)
				{
					break;
				}

				 Gizmos.DrawLine(n.transform.position, n._Next.transform.position);
			}
		}

		foreach (TEST_Waypoint marker in _Destinations)
		{
			Gizmos.color = Selection.Contains(gameObject) ? Color.red : Color.blue;

			if (!Selection.Contains(marker.gameObject)) {
				 Gizmos.DrawLine(transform.position + Vector3.up, marker.transform.position + Vector3.up);
			}
		}
	}

	public void CalculateClosest()
	{
		List<Transform> children = transform.parent.GetComponentsInChildren<Transform>().Where(c => c != transform).ToList();
		Transform closest = MathUtil.GetClosestTransform(transform.position, children, out int index);
		TEST_Waypoint closestWP = closest.GetComponent<TEST_Waypoint>();

		_Destinations = new(1)
		{
			closestWP
		};
		_Next = closestWP;

		EditorUtility.SetDirty(this);
	}

	public void GenerateID()
	{
		name = $"WP_{transform.GetSiblingIndex()}";
	}
}
