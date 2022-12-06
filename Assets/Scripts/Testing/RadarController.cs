using System.Collections;
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

	private void Awake()
	{
		_Instance = this;
	}
}
