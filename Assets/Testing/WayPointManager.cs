using UnityEngine;
using UnityEditor;

public class WayPointManager : MonoBehaviour
{
	public TEST_Waypoint _Home;
	public static WayPointManager _Instance;

	void OnEnable()
	{
		_Instance = this;
	}

	public void CalculateDistance(bool clear)
	{
		TEST_Waypoint[] Waypoints = GetComponentsInChildren<TEST_Waypoint>();
		foreach (TEST_Waypoint way in Waypoints)
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
