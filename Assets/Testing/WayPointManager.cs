using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class WayPointManager : MonoBehaviour
{
	public TEST_Waypoint _Home;
	public LayerMask _MapMask;

	[Header("Debugging")]
	[SerializeField] List<TEST_Waypoint> _Network;

	public static WayPointManager _Instance;

	private Dictionary<(TEST_Waypoint, TEST_Waypoint), float> _WaypointDistancesCache;

	void OnEnable()
	{
		_Instance = this;
	}

	void Awake()
	{
		_Network = GetComponentsInChildren<TEST_Waypoint>().ToList();
	}


	public TEST_Waypoint GetClosestWaypoint(Vector3 currentPosition, HashSet<TEST_Waypoint> excludedWaypoints = null)
	{
		excludedWaypoints ??= new HashSet<TEST_Waypoint>();

		return _Network.Where(waypoint => !excludedWaypoints.Contains(waypoint))
									 .OrderBy(waypoint => Vector3.Distance(currentPosition, waypoint.transform.position))
									 .FirstOrDefault();
	}

	public void CalculateDistances(bool clear)
	{
		_WaypointDistancesCache = new Dictionary<(TEST_Waypoint, TEST_Waypoint), float>();
		Vector3 homePosition = _Home.transform.position;

		foreach (TEST_Waypoint waypoint in _Network)
		{
			if (clear || waypoint == _Home)
			{
				waypoint._Next = null;
				continue;
			}

			TEST_Waypoint bestDestination = FindBestDestination(waypoint, homePosition);
			waypoint._Next = bestDestination;
		}
	}

	private TEST_Waypoint FindBestDestination(TEST_Waypoint waypoint, Vector3 homePosition)
	{
		return waypoint._Destinations
									 .Select(destination => (destination, CalculateFScore(waypoint, destination, homePosition)))
									 .OrderBy(pair => pair.Item2)
									 .FirstOrDefault().destination;
	}

	private float CalculateFScore(TEST_Waypoint waypoint, TEST_Waypoint destination, Vector3 homePosition)
	{
		float g = GetDistanceBetweenWaypoints(waypoint, destination);
		float h = Vector3.Distance(destination.transform.position, homePosition);
		return g + h;
	}

	private float GetDistanceBetweenWaypoints(TEST_Waypoint waypoint1, TEST_Waypoint waypoint2)
	{
		if (!_WaypointDistancesCache.TryGetValue((waypoint1, waypoint2), out float distance))
		{
			distance = Vector3.Distance(waypoint1.transform.position, waypoint2.transform.position);
			_WaypointDistancesCache[(waypoint1, waypoint2)] = distance;
			_WaypointDistancesCache[(waypoint2, waypoint1)] = distance;
		}

		return distance;
	}
}
