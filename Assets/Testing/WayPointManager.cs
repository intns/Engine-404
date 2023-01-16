using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class WayPointManager : MonoBehaviour
{
	public TEST_Waypoint _Home;

	[Header("Debugging")]
	[SerializeField] List<TEST_Waypoint> _Network;

	public static WayPointManager _Instance;

	void OnEnable()
	{
		_Instance = this;
	}

	void Awake()
	{
		_Network = GetComponentsInChildren<TEST_Waypoint>().ToList();
	}

	public TEST_Waypoint GetWaypointTowards(Vector3 currentPosition)
	{
		TEST_Waypoint target = _Network.OrderBy(curWP =>
		{
			return Vector3.Distance(currentPosition, curWP.transform.position);
		}).First();

		return target;
	}

	public void CalculateDistance(bool clear)
	{
		foreach (TEST_Waypoint way in _Network)
		{
			EditorUtility.SetDirty(way);
			if (way == _Home || clear)
			{
				way._Next = null;
				continue;
			}

			Debug.Log(way.name);
			float bestF = float.MaxValue;

			foreach (TEST_Waypoint destination in way._Destinations)
			{
				if (destination == _Home)
				{
					way._Next = _Home;
					break;
				}

				// wp - > dest
				float g = Vector3.Distance(way.transform.position, destination.transform.position);

				// dest - > home
				float h = Vector3.Distance(destination.transform.position, _Home.transform.position);
				float f = g + h;
				Debug.Log(destination.name + " G " + g + " H " + h + " F " + f);
				EditorUtility.SetDirty(way);

				if (f < bestF)
				{
					way._Next = destination;
					bestF = f;
				}

				Debug.Log(way._Next.name + " Chosen");
			}
		}
	}
}
