using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WayPointManager : MonoBehaviour
{
	public static WayPointManager _Instance;
	public LayerMask _MapMask;

	[Header("Debugging")]
	[SerializeField] List<TEST_Waypoint> _Network;

	void Awake()
	{
		_Network = GetComponentsInChildren<TEST_Waypoint>().ToList();
	}

	void OnEnable()
	{
		_Instance = this;
	}

	public void CalculateDistances(bool clear)
	{
		foreach (TEST_Waypoint waypoint in _Network)
		{
			if (clear)
			{
				waypoint._Next = null;
				continue;
			}

			Queue<TEST_Waypoint> bestPath = FindBestDestination(waypoint, WaypointType.Path);
			waypoint._Next = bestPath.Count > 0 ? bestPath.Peek() : null;
		}
	}

	public TEST_Waypoint GetClosestWaypoint(Vector3 currentPosition, HashSet<TEST_Waypoint> excludedWaypoints = null)
	{
		excludedWaypoints ??= new();

		return _Network.Where(waypoint => !excludedWaypoints.Contains(waypoint))
		               .OrderBy(waypoint => Vector3.Distance(currentPosition, waypoint.transform.position))
		               .FirstOrDefault();
	}

	public Queue<TEST_Waypoint> FindBestDestination(TEST_Waypoint startWaypoint, TEST_Waypoint destinationWaypoint, WaypointType type)
	{
		var visited = new HashSet<TEST_Waypoint>();
		var queue = new Queue<TEST_Waypoint>();
		var parentMap = new Dictionary<TEST_Waypoint, TEST_Waypoint>();

		queue.Enqueue(startWaypoint);
		visited.Add(startWaypoint);

		while (queue.Count > 0)
		{
			TEST_Waypoint current = queue.Dequeue();

			if (current == destinationWaypoint)
			{
				return ReconstructPath(current, parentMap);
			}

			foreach (TEST_Waypoint neighbor in current._Destinations.Where(neighbor => visited.Add(neighbor)))
			{
				queue.Enqueue(neighbor);
				parentMap[neighbor] = current;
			}

			if (current._Next == null || !visited.Add(current._Next))
			{
				continue;
			}

			queue.Enqueue(current._Next);
			parentMap[current._Next] = current;
		}

		return null;
	}


	public Queue<TEST_Waypoint> FindBestDestination(TEST_Waypoint waypoint, WaypointType type)
	{
		var visited = new HashSet<TEST_Waypoint>();
		var queue = new Queue<TEST_Waypoint>();
		var parentMap = new Dictionary<TEST_Waypoint, TEST_Waypoint>();

		queue.Enqueue(waypoint);

		while (queue.Count > 0)
		{
			TEST_Waypoint current = queue.Dequeue();

			if ((current._Type & type) != 0)
			{
				return ReconstructPath(current, parentMap);
			}

			foreach (TEST_Waypoint neighbor in current._Destinations.Where(neighbor => visited.Add(neighbor)))
			{
				queue.Enqueue(neighbor);
				parentMap[neighbor] = current;
			}

			if (current._Next == null || !visited.Add(current._Next))
			{
				continue;
			}

			queue.Enqueue(current._Next);
			parentMap[current._Next] = current;
		}

		return null;
	}


	Queue<TEST_Waypoint> ReconstructPath(TEST_Waypoint destination, Dictionary<TEST_Waypoint, TEST_Waypoint> parentMap)
	{
		var waypoints = new List<TEST_Waypoint>();
		TEST_Waypoint waypointToAdd = destination;

		while (waypointToAdd != null)
		{
			waypoints.Insert(0, waypointToAdd);
			waypointToAdd = parentMap.GetValueOrDefault(waypointToAdd);
		}

		return new(waypoints);
	}
}
