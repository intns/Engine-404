/*
 * PlayerMovementController.cs
 * Created by: Ambrosia
 * Created on: 7/2/2020 (dd/mm/yy)
 * Created for: controlling the movement of the Player
 */

using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] private Transform _WhistleTransform = null;
	private CharacterController _Controller;
	private Camera _MainCamera;

	[Header("Settings")]
	[SerializeField] private float _MovementSpeed = 3;
	[SerializeField] private Vector2 _MovementDeadzone = Vector2.one / 0.1f;
	[SerializeField] private float _SlideLimit = 25;
	[SerializeField] private float _SlideSpeed = 5;
	[SerializeField] private float _RotationSpeed = 3;
	[SerializeField] private float _Gravity = -Physics.gravity.y;

	[SerializeField] private float _LookAtWhistleTime = 2.5f;

	[HideInInspector] public Quaternion _RotationBeforeIdle = Quaternion.identity;
	[HideInInspector] public bool _Paralysed = false;
	[HideInInspector] public Vector3 _gVelocity => _Velocity;

	private Vector3 _PreviousPosition;
	private Vector3 _Velocity;
	private float _IdleTimer = 0;
	private RaycastHit _SlideHit;
	private Vector3 _CharacterContactPoint;
	private float _SlideRayDist;
	private Vector3 _BaseHeight;

	private void Awake()
	{
		_Controller = GetComponent<CharacterController>();
		_BaseHeight = Vector3.up * (_Controller.height / 2);
		_SlideRayDist = (_Controller.height * .5f) + _Controller.radius;
		_SlideLimit = _Controller.slopeLimit - .1f;
		_MainCamera = Camera.main;
		_PreviousPosition = transform.position;
	}

	private void Update()
	{
		if (_Paralysed)
		{
			return;
		}

		// Add time to the idle timer
		_IdleTimer += Time.deltaTime;

		// Check if we've been idle long enough to look at the whistle
		if (_IdleTimer > _LookAtWhistleTime)
		{
			Vector3 finalLookPosition = _WhistleTransform.position - transform.position;
			finalLookPosition.y = 0;

			if (finalLookPosition != Vector3.zero)
			{
				transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(finalLookPosition), _RotationSpeed * Time.deltaTime);
			}
		}
		else
		{
			_RotationBeforeIdle = transform.rotation;
		}

		// If we're not grounded and not on a slope
		if (!IsGrounded())
		{
			// Apply gravity
			_Controller.Move(_Gravity * Time.deltaTime * Vector3.down);
		}

		// Get input from the 'Horizontal' and 'Vertical' axis, and normalize it
		// so as to not the player move quicker when going diagonally
		Vector3 mDirection = new Vector3(Input.GetAxis("Horizontal"),
			0,
			Input.GetAxis("Vertical")).normalized;

		// If the player has even touched the H and V axis
		if (Mathf.Abs(mDirection.x) <= _MovementDeadzone.x && Mathf.Abs(mDirection.z) <= _MovementDeadzone.y)
		{
			return;
		}

		// We've moved, so the idle timer gets reset
		_IdleTimer = 0;

		// Make the movement vector relative to the camera's position/rotation
		// and remove any Y momentum gained from doing the TransformDirection
		mDirection = _MainCamera.transform.TransformDirection(mDirection);
		mDirection.y = 0;

		// Rotate and move the player
		transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(mDirection), _RotationSpeed * Time.deltaTime);
		_Controller.Move(_MovementSpeed * Time.deltaTime * mDirection.normalized);
	}

	private void FixedUpdate()
	{
		if (_Paralysed)
		{
			return;
		}
		// Calculate the velocity of the Player using the previous frame as a base point for the calculation
		_Velocity = (transform.position - _PreviousPosition) / Time.fixedDeltaTime;
		_PreviousPosition = transform.position;

		bool sliding = false;
		// See if surface immediately below should be slid down. We use this normally rather than a ControllerColliderHit point,
		// because that interferes with step climbing amongst other annoyances
		if (Physics.Raycast(transform.position, -Vector3.up, out _SlideHit, _SlideRayDist))
		{
			if (Vector3.Angle(_SlideHit.normal, Vector3.up) > _SlideLimit)
			{
				sliding = true;
			}
		}
		// However, just raycasting straight down from the center can fail when on steep slopes
		// So if the above raycast didn't catch anything, raycast down from the stored ControllerColliderHit point instead
		else
		{
			Physics.Raycast(_CharacterContactPoint + Vector3.up, -Vector3.up, out _SlideHit);
			if (Vector3.Angle(_SlideHit.normal, Vector3.up) > _SlideLimit)
			{
				sliding = true;
			}
		}

		if (sliding)
		{
			Vector3 normal = _SlideHit.normal;
			Vector3 direction = new Vector3(normal.x, -normal.y, normal.z);
			Vector3.OrthoNormalize(ref normal, ref direction);
			normal.Normalize();
			direction *= _SlideSpeed;
			direction.y -= Physics.gravity.y * Time.deltaTime;
			_Controller.Move(direction * Time.deltaTime);
			return;
		}
	}

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		_CharacterContactPoint = hit.point;
	}

	private bool IsGrounded()
	{
		// Calculate the bottom position of the character controller
		// then check if there is any collider beneath us
		if (Physics.Raycast(transform.position - _BaseHeight, Vector3.down, out RaycastHit hit, 1f))
		{
			// Handle special case of water
			if (hit.transform.CompareTag("Water"))
			{
				return false;
			}

			// Check if the raycast hit a floor,
			// and then check the distance between the floor and the player
			if (hit.normal == Vector3.up)
			{
				return hit.distance <= 0.2;
			}

			// Move down but only the distance away, this cancels out the bouncing
			// effect that you can achieve by removing this function
			_Controller.Move(Vector3.down * hit.distance);
			return true;
		}

		// We couldn't ground ourselves whilst travelling down a slope and 
		// the controller says we're not grounded so return false
		return false;
	}
}
