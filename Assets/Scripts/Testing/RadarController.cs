using System.Collections.Generic;
using UnityEngine;

public enum RadarObjectType
{
	Player,
	Pikmin,
	Onion,
	Ship,
};

// TODO!
public class RadarController : MonoBehaviour
{
	public static RadarController _Instance;

	public List<EntityBase> _RadarObjects;

	void Awake()
	{
		_Instance = this;
	}
}
