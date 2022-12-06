using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EntityBase : MonoBehaviour
{
	/// <summary>
	/// TODO: Use this function on entity spawn in scene
	/// </summary>
	/// <param name="type">The type of object this enemy is on the Radar</param>
	public virtual void SetupRadarObject()
	{
		RadarController._Instance._RadarObjects.Add(this);
	}
}
