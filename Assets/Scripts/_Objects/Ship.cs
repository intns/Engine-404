using UnityEngine;
using UnityEngine.VFX;

public class Ship : MonoBehaviour
{
	public static Ship _Instance;

	[Header("Components")]
	[SerializeField] VisualEffect _FireVFX;

	[HideInInspector]
	public Transform _Transform;

	#region Public Functions

	/// <summary>
	///   Starts / Stops flames underneath ship
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

	// [Header("Settings")]

	#region Unity Functions

	void OnEnable()
	{
		_Instance = this;
		SetEngineFlamesVFX(false);
	}

	void Awake()
	{
		_Transform = transform;
	}

	#endregion
}
