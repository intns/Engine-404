using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WP_Obj : MonoBehaviour
{
	[Header("Debugging")]
	[SerializeField] bool _GetClosest = false;
	[SerializeField] bool _Enable = false;
	TEST_Waypoint _CurrentWP = null;

	void Update()
	{
		if (_CurrentWP != null && _Enable)
		{
			transform.position = Vector3.MoveTowards(transform.position, _CurrentWP.transform.position, 3.5f * Time.deltaTime);
			if (MathUtil.DistanceTo(transform.position, _CurrentWP.transform.position) < 1.0f)
			{
				_CurrentWP = _CurrentWP._Next;
			}
		}
	}

	void OnDrawGizmosSelected()
	{
		if (_GetClosest)
		{
			_CurrentWP = WayPointManager._Instance.GetWaypointTowards(transform.position);
			_GetClosest = false;
		}

		if (_CurrentWP != null)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawLine(transform.position, _CurrentWP.transform.position);
		}
	}
}
