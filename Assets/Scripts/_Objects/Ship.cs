using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{
	public static Ship _Instance = null;

	[Header("Components")]
	[HideInInspector]
	public Transform _Transform = null;

	// [Header("Settings")]

	#region Unity Functions
	private void OnEnable()
	{
		_Instance = this;
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
	#endregion
}
