/*
 * CameraFollow.cs
 * Created by: Neo, Ambrosia, SkrienaSenka
 * Created on: 6/2/2020 (dd/mm/yy)
 * Created for: following a target with incrementable offset and field of view
 */

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

[Serializable]
public class CameraPositionData
{
	public float _FOV = 75f;
	public Vector2 _Offset = Vector2.one;
}

[RequireComponent(typeof(AudioSource))]
public class CameraFollow : MonoBehaviour
{
	public static CameraFollow _Instance;
	[Header("Components")]
	[SerializeField] CameraPositionData[] _DefaultData;
	[SerializeField] bool _UseTopView = true;
	[SerializeField] CameraPositionData[] _TopViewData;
	[Space]
	[SerializeField] AudioClip _ZoomAudio;

	[Header("Settings")]
	[SerializeField] [Tooltip("Speed used to track the player or goal object")]
	float _LookatFollowSpeed = 1;
	[SerializeField] [Tooltip("Speed used to change the orbit radius")]
	float _OrbitChangeSpeed = 1;
	[SerializeField] [Tooltip("Speed at which the offset height changes")]
	float _HeightChangeSpeed = 3.5f;
	[SerializeField] [Tooltip("Speed used to change the FOV")]
	float _FOVChangeSpeed = 1;

	[Header("Trigger Settings")]
	[SerializeField] [Tooltip("Speed of the interpolation used for triggers")]
	float _TriggerRotationSpeed = 1;
	[SerializeField] [Tooltip("How sensitive the triggers are, as speed")]
	float _TriggerSensitivity = 1;

	[Header("Reset Settings")]
	[SerializeField] [Tooltip("Speed of rotation for the reset sequence")]
	float _ResetRotationSpeed = 1;
	[SerializeField] [Tooltip("How long it takes for the reset sequence to complete")]
	float _ResetLength = 1;
	AudioSource _AudioSource;
	/*[SerializeField] */
	float _ControllerTriggerState; // State of controller triggers for reset

	// Misc. data
	CameraPositionData _CurrentHolder;
	/*[Space()]*/
	/*[SerializeField] */
	float _CurrentRotation; // Current rotation in degrees
	/*[SerializeField] */
	float _GroundOffset; // How far off the ground
	/*[Space()]*/
	/*[SerializeField] */
	int _HolderIndex; // Current struct index
	/*[SerializeField] */
	bool _IsTopView; // Is top view or not
	/*[SerializeField] */
	Vector3 _LookatPosition; // The position we're looking at
	Camera _MainCamera;

	//[Header("Debugging")]
	/*[SerializeField] */
	float _OrbitRadius; // How far away to orbit
	Transform _PlayerPosition;
	/*[Space()]*/
	/*[SerializeField] */
	float _ResetTimer; // Rotation reset timer
	/*[SerializeField] */
	float _TargetRotation; // Current rotation in degrees

	void Start()
	{
		_MainCamera = Camera.main;
		_PlayerPosition = Player._Instance.transform;
		_AudioSource = GetComponent<AudioSource>();

		if (_DefaultData.Length != _TopViewData.Length)
		{
			Debug.LogError("Top View holders must have the same length as default holders!");
			Debug.Break();
		}

		_HolderIndex = 0;
		_CurrentHolder = _DefaultData[_HolderIndex];

		_MainCamera.fieldOfView = _CurrentHolder._FOV;
		_OrbitRadius = _CurrentHolder._Offset.x;
		_GroundOffset = _CurrentHolder._Offset.y + _PlayerPosition.position.y;

		_TargetRotation = _CurrentRotation = 270 - _PlayerPosition.eulerAngles.y;

		_LookatPosition = _PlayerPosition.position;
		transform.LookAt(_LookatPosition);
	}

	void Update()
	{
		if (Player._Instance._MovementController._Paralysed || GameManager.IsPaused)
		{
			return;
		}

		float groundOffset = _CurrentHolder._Offset.y + _PlayerPosition.position.y;
		float orbitRadius = _CurrentHolder._Offset.x;

		_MainCamera.fieldOfView
			= Mathf.Lerp(_MainCamera.fieldOfView, _CurrentHolder._FOV, _FOVChangeSpeed * Time.deltaTime);
		_OrbitRadius = Mathf.Lerp(_OrbitRadius, orbitRadius, _OrbitChangeSpeed * Time.deltaTime);
		_GroundOffset = Mathf.Lerp(_GroundOffset, groundOffset, _HeightChangeSpeed * Time.deltaTime);

		// Check if the controller trigger is being pressed
		float factor = Mathf.Min(_TriggerRotationSpeed, _ResetRotationSpeed);

		if (_ControllerTriggerState != 0.0f)
		{
			// Calculate the new rotation angle based on the trigger input
			_TargetRotation = _CurrentRotation - _ControllerTriggerState * _TriggerSensitivity;

			// Can't reset while using the triggers to change direction
			_ResetTimer = 0;

			factor = _TriggerRotationSpeed;
		}
		else if (_ResetTimer > 0)
		{
			_ResetTimer -= Time.deltaTime;

			factor = _ResetRotationSpeed;
		}

		_CurrentRotation = Mathf.LerpAngle(_CurrentRotation, _TargetRotation, factor * Time.deltaTime);
		_LookatPosition = Vector3.Lerp(_LookatPosition, _PlayerPosition.position, _LookatFollowSpeed * Time.deltaTime);

		float xAngle = Quaternion
		               .LookRotation(MathUtil.DirectionFromTo(transform.position, _LookatPosition + Vector3.up * 2.5f, true))
		               .eulerAngles.x;

		Vector3 newEulerAngles = new(
			Mathf.Lerp(transform.eulerAngles.x, xAngle, 25 * Time.deltaTime),
			270 - _CurrentRotation,
			0
		);
		transform.eulerAngles = newEulerAngles;

		transform.position = _LookatPosition
		                     + MathUtil.XZToXYZ(
			                     MathUtil.PositionInUnit(Mathf.Deg2Rad * _CurrentRotation, _OrbitRadius),
			                     _GroundOffset
		                     );
	}

	void OnEnable()
	{
		_Instance = this;
	}

	public void OnResetCamera(InputAction.CallbackContext context)
	{
		if (Player._Instance._MovementController._Paralysed || GameManager.IsPaused)
		{
			return;
		}

		if (context.started && _ControllerTriggerState == 0.0f)
		{
			_ResetTimer = _ResetLength;
			_TargetRotation = 270 - _PlayerPosition.eulerAngles.y;
		}
	}

	public void OnRotateCamera(InputAction.CallbackContext context)
	{
		if (Player._Instance._MovementController._Paralysed || GameManager.IsPaused)
		{
			return;
		}

		_ControllerTriggerState = context.ReadValue<float>();
	}

	public void OnTopDownView(InputAction.CallbackContext context)
	{
		if (Player._Instance._MovementController._Paralysed || GameManager.IsPaused || !_UseTopView)
		{
			return;
		}

		if (context.started)
		{
			_IsTopView = !_IsTopView; // Invert the TopView 
			ApplyChangedZoomLevel(_IsTopView ? _TopViewData : _DefaultData);
		}
	}

	public void OnZoom(InputAction.CallbackContext context)
	{
		if (Player._Instance._MovementController._Paralysed || GameManager.IsPaused
		                                                    || !DemoSettings._DiscoveredOnionCutsceneDone)
		{
			return;
		}

		if (context.started)
		{
			_HolderIndex++;
			ApplyChangedZoomLevel(_IsTopView ? _TopViewData : _DefaultData);
		}
	}

	public void Shake(float intensity)
	{
		transform.SetPositionAndRotation(
			Vector3.Lerp(transform.position, Random.insideUnitSphere + transform.position, intensity / 1000),
			Quaternion.Lerp(transform.rotation, Random.rotationUniform, intensity / 1000)
		);
	}

	/// <summary>
	///   Changes zoom level based on the holder index, and plays audio
	/// </summary>
	/// <param name="currentHolder"></param>
	void ApplyChangedZoomLevel(CameraPositionData[] currentHolder)
	{
		_AudioSource.PlayOneShot(_ZoomAudio);

		if (_HolderIndex > currentHolder.Length - 1)
		{
			_HolderIndex = 0;
		}

		_CurrentHolder = currentHolder[_HolderIndex];
	}
}
