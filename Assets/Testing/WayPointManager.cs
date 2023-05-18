using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WayPointManager : MonoBehaviour
{
	public static WayPointManager _Instance;
	public LayerMask _MapMask;

	[Header("Debugging")]
	[SerializeField] List<Waypoint> _Network;

	void Awake()
	{
		_Network = GetComponentsInChildren<Waypoint>().ToList();
	}

	void OnEnable()
	{
		_Instance = this;
	}

	public void CalculateDistances(bool clear)
	{
		foreach (Waypoint waypoint in _Network)
		{
			if (clear)
			{
				waypoint._Next = null;
				continue;
			}

			Queue<Waypoint> bestPath = FindBestDestination(waypoint, WaypointType.Path);
			waypoint._Next = bestPath.Count > 0 ? bestPath.Peek() : null;
		}
	}

	public Waypoint GetClosestWaypoint(Vector3 currentPosition, HashSet<Waypoint> excludedWaypoints = null)
	{
		excludedWaypoints ??= new();

		return _Network.Where(waypoint => !excludedWaypoints.Contains(waypoint))
		               .OrderBy(waypoint => Vector3.Distance(currentPosition, waypoint.transform.position))
		               .FirstOrDefault();
	}

	public Queue<Waypoint> FindBestDestination(Waypoint startWaypoint, Waypoint destinationWaypoint, WaypointType type)
	{
		var visited = new HashSet<Waypoint>();
		var queue = new Queue<Waypoint>();
		var parentMap = new Dictionary<Waypoint, Waypoint>();

		queue.Enqueue(startWaypoint);
		visited.Add(startWaypoint);

		while (queue.Count > 0)
		{
			Waypoint current = queue.Dequeue();

			if (current == destinationWaypoint)
			{
				return ReconstructPath(current, parentMap);
			}

			foreach (Waypoint neighbor in current._Connections.Where(neighbor => visited.Add(neighbor)))
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


	public Queue<Waypoint> FindBestDestination(Waypoint waypoint, WaypointType type)
	{
		var visited = new HashSet<Waypoint>();
		var queue = new Queue<Waypoint>();
		var parentMap = new Dictionary<Waypoint, Waypoint>();

		queue.Enqueue(waypoint);

		while (queue.Count > 0)
		{
			Waypoint current = queue.Dequeue();

			if ((current._Type & type) != 0)
			{
				return ReconstructPath(current, parentMap);
			}

			foreach (Waypoint neighbor in current._Connections.Where(neighbor => visited.Add(neighbor)))
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


	Queue<Waypoint> ReconstructPath(Waypoint destination, Dictionary<Waypoint, Waypoint> parentMap)
	{
		var waypoints = new List<Waypoint>();
		Waypoint waypointToAdd = destination;

		while (waypointToAdd != null)
		{
			waypoints.Insert(0, waypointToAdd);
			waypointToAdd = parentMap.GetValueOrDefault(waypointToAdd);
		}

		return new(waypoints);
	}
}
