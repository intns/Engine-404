/*
 * PlayerMovementController.cs
 * Created by: Ambrosia
 * Created on: 7/2/2020 (dd/mm/yy)
 * Last update by : Senka
 * Last update on : 9/7/2022
 * Created for: controlling the movement of the Player
 */

using UnityEngine;
using UnityEngine.InputSystem;

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
	[SerializeField] LayerMask _MapMask;

	[SerializeField] private float _LookAtWhistleTime = 2.5f;

	[HideInInspector] public Quaternion _RotationBeforeIdle = Quaternion.identity;
	[HideInInspector] public bool _Paralysed = false;

	private float _IdleTimer = 0;
	private Vector2 _Movement;
	private RaycastHit _SlideHit;
	private Vector3 _CharacterContactPoint;
	private Vector3 _HitNormal;
	private float _SlideRayDist;
	private Vector3 _BaseHeight;

	private void Awake()
	{
		_Controller = GetComponent<CharacterController>();
		_BaseHeight = Vector3.up * (_Controller.height / 2);
		_SlideRayDist = (_Controller.height * .5f) + _Controller.radius;
		_SlideLimit = _Controller.slopeLimit - .1f;
		_MainCamera = Camera.main;
	}

	private void Update()
	{
		if (_Paralysed || GameManager._IsPaused)
		{
			return;
		}

		// Add time to the idle timer
		_IdleTimer += Time.deltaTime;

		// Check if we've been idle long enough to look at the whistle
		if (_IdleTimer > _LookAtWhistleTime)
		{
			Vector3 dir = MathUtil.DirectionFromTo(transform.position, _WhistleTransform.position);
			if (dir != Vector3.zero)
			{
				transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(dir), _RotationSpeed * Time.deltaTime);
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

		// Get current movement and make it a Vector3
		Vector3 mDirection = new Vector3(_Movement.x, 0, _Movement.y);

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

		Vector3 movement = mDirection.normalized * _MovementSpeed;

		bool sliding = false;
		// See if surface immediately below should be slid down. We use this normally rather than a ControllerColliderHit point,
		// because that interferes with step climbing amongst other annoyances
		if (Physics.SphereCast(transform.position, 0.75f, Vector3.down, out _SlideHit, _SlideRayDist, _MapMask, QueryTriggerInteraction.Ignore)
			&& Vector3.Angle(_SlideHit.normal, Vector3.up) > _SlideLimit)
		{
				sliding = true;
		}
		// However, just raycasting straight down from the center can fail when on steep slopes
		// So if the above raycast didn't catch anything, raycast down from the stored ControllerColliderHit point instead
		else
		{
			if (Physics.Raycast(_CharacterContactPoint + Vector3.up, -Vector3.up, out _SlideHit, _SlideRayDist, _MapMask, QueryTriggerInteraction.Ignore)
				&& Vector3.Angle(_SlideHit.normal, Vector3.up) > _SlideLimit)
			{
				sliding = true;
			}
			else if (Physics.SphereCast(transform.position, 1.5f, mDirection.normalized + (Vector3.down / 2), out _SlideHit, _SlideRayDist, _MapMask, QueryTriggerInteraction.Ignore)
				&& Vector3.Angle(_SlideHit.normal, Vector3.up) > _SlideLimit)
			{
				sliding = true;
			}
		}

		if (sliding)
		{
			movement.x += (1f - _SlideHit.normal.y) * _SlideHit.normal.x * _SlideSpeed;
			movement.z += (1f - _SlideHit.normal.y) * _SlideHit.normal.z * _SlideSpeed;
		}
		else if (Vector3.Angle(Vector3.up, _HitNormal) > _Controller.slopeLimit)
		{
			movement.x += (1f - _HitNormal.y) * _HitNormal.x * _SlideSpeed;
			movement.z += (1f - _HitNormal.y) * _HitNormal.z * _SlideSpeed;
		}

		_Controller.Move(movement * Time.deltaTime);
	}

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		_CharacterContactPoint = hit.point;
		_HitNormal = hit.normal;
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

	// Happens whenever the movement joystick/buttons change values
	public void OnMovement(InputAction.CallbackContext context) {
		// Stores the current movement (already normalized)
		_Movement = context.ReadValue<Vector2>();
	}
}
