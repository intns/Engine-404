using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Ship : MonoBehaviour
{
	public static Ship _Instance = null;

	[Header("Components")]
	[SerializeField] VisualEffect _FireVFX;

	[HideInInspector]
	public Transform _Transform = null;

	// [Header("Settings")]

	#region Unity Functions
	private void OnEnable()
	{
		_Instance = this;
		SetEngineFlamesVFX(false);
	}

	private void Awake()
	{
		_Transform = transform;
	}
	#endregion

	#region IEnumerators
	#endregion

	#region Utility Functions
	#endregion

	#region Public Functions
	/// <summary>
	/// Starts / Stops flames underneath ship
	/// </summary>
	/// <param name="state">Whether to start or stop the flames</param>
	public void SetEngineFlamesVFX(bool state)
	{
		if (state)
		{
			_FireVFX.Play();
		}
		else
		{
			_FireVFX.Stop();
		}
	}
	#endregion
}
