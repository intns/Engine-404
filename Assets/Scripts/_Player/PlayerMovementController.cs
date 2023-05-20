using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementController : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] Transform _WhistleTransform;

	[Header("Settings")]
	[SerializeField] float _MovementSpeed = 3;
	[SerializeField] float _RotationSpeed = 3;
	[SerializeField] float _MovementDeadzone = 0.1f;

	[SerializeField]
	[Tooltip("The time the player has to be idle before looking at the whistle.")]
	float _LookAtWhistleTime = 2.5f;

	[SerializeField] float _Weight = 0.01f;

	[Header("Sliding Settings")]
	[SerializeField]
	[Tooltip("The speed at which the player slides.")]
	float _SlideSpeed = 2;

	[SerializeField]
	[Tooltip("The maximum speed at which the player can slide.")]
	float _MaxSlideSpeed = 10;

	[SerializeField]
	[Range(1.0f, 180.0f)]
	[Tooltip("The minimum slope angle at which the player can slide.")]
	float _MinSlopeAngle = 25;

	[SerializeField]
	[Range(0.01f, 1.0f)]
	[Tooltip("The friction applied to the player when sliding.")]
	float _SlideFriction = 0.15f;

	[HideInInspector] public Quaternion _RotationBeforeIdle = Quaternion.identity;
	[HideInInspector] public bool _Paralysed;
	CharacterController _Controller;

	Vector3 _FinalMovement = Vector3.zero;
	Vector3 _HitNormal;

	float _IdleTimer;
	bool _IsSliding;
	Camera _MainCamera;
	Vector3 _Movement;

	Vector3 _SlideDirection = Vector3.zero;

	void Awake()
	{
		_Controller = GetComponent<CharacterController>();
		_MainCamera = Camera.main;
	}

	void Update()
	{
		if (_Paralysed || GameManager.IsPaused)
		{
			return;
		}

		UpdateLookAtWhistle();
		ApplyGravity();
		ApplySlide();
		UpdateMovement();

		if (_FinalMovement != Vector3.zero)
		{
			_Controller.Move(_FinalMovement);
		}
	}

	void OnControllerColliderHit(ControllerColliderHit hit)
	{
		_HitNormal = hit.normal;

		Rigidbody otherRigidbody = hit.collider.attachedRigidbody;

		if (otherRigidbody != null)
		{
			// Check if the collided object should be ignored based on its tag
			if (hit.collider.CompareTag("PikminInteract"))
			{
				// Apply an opposite force to cancel the pushing effect
				Vector3 pushDirection = -hit.moveDirection;
				otherRigidbody.AddForce(pushDirection * hit.moveLength, ForceMode.Impulse);

				_Controller.Move(MathUtil.DirectionFromTo(hit.collider.ClosestPoint(transform.position), transform.position) / 2.0f);
			}
		}
	}

	// Happens whenever the movement joystick/buttons change values
	public void OnMovement(InputAction.CallbackContext context)
	{
		// The movement directions are already normalized
		_Movement = new(context.ReadValue<Vector2>().x, _Movement.y, context.ReadValue<Vector2>().y);
	}

	/// <summary>
	///   Applies gravity to a character controller, adjusting its vertical movement based on weight and grounded state.
	/// </summary>
	void ApplyGravity()
	{
		_Controller.Move(new(0, _Movement.y, 0));

		float gravityForce = Physics.gravity.y * _Weight * Time.deltaTime;

		if (_Controller.isGrounded)
		{
			_Movement.y = gravityForce;
		}
		else
		{
			_Movement.y += gravityForce;
		}
	}

	/// <summary>
	///   Applies sliding effect on slopes by adjusting movement using slide direction, speed, and friction.
	/// </summary>
	void ApplySlide()
	{
		if (!_Controller.isGrounded || Vector3.Angle(_HitNormal, Vector3.up) <= _MinSlopeAngle)
		{
			_IsSliding = false;
			return;
		}

		_IsSliding = true;

		// Calculate slide direction
		_SlideDirection = Vector3.ProjectOnPlane(Vector3.down, _HitNormal).normalized;

		// Calculate slide speed and friction
		float slopeAngle = Vector3.Angle(_HitNormal, Vector3.up);
		float slideSpeedFactor = Mathf.InverseLerp(_MinSlopeAngle, _Controller.slopeLimit, slopeAngle);
		float slideSpeed = Mathf.Lerp(_SlideSpeed, _MaxSlideSpeed, slideSpeedFactor);
		Vector3 slideMovement = _SlideDirection * slideSpeed;

		// Apply friction to reduce slide speed over time
		slideMovement *= Mathf.Clamp01(1f / (1f + _SlideFriction * Time.deltaTime));

		// Apply slide movement
		_Controller.Move(slideMovement * Time.deltaTime);
	}

	/// <summary>
	///   Smoothly rotates the character towards the whistle's position if enough idle time has passed.
	/// </summary>
	void UpdateLookAtWhistle()
	{
		// Add time to the idle timer
		_IdleTimer += Time.deltaTime;

		// Check if we've been idle long enough to look at the whistle
		if (_IdleTimer > _LookAtWhistleTime)
		{
			Vector3 dir = MathUtil.DirectionFromTo(transform.position, _WhistleTransform.position);

			if (dir != Vector3.zero)
			{
				transform.rotation = Quaternion.Lerp(
					transform.rotation,
					Quaternion.LookRotation(dir),
					_RotationSpeed * Time.deltaTime
				);
			}
		}
		else
		{
			_RotationBeforeIdle = transform.rotation;
		}
	}

	/// <summary>
	///   Updates player movement based on input. If no Pikmin are being held, the player rotates towards the movement
	///   direction.
	/// </summary>
	void UpdateMovement()
	{
		Vector3 movementDirection = _Movement;
		movementDirection.y = 0f;

		// If the player has not moved enough, return
		if (movementDirection.magnitude <= _MovementDeadzone)
		{
			return;
		}

		if (_IsSliding)
		{
			movementDirection -= Vector3.Project(movementDirection, _SlideDirection);
		}

		// We've moved, so the idle timer gets reset
		_IdleTimer = 0;

		movementDirection = _MainCamera.transform.TransformDirection(movementDirection);
		movementDirection.y = 0f;

		// Only rotate if there isn't a Pikmin in hand, as it has priority
		if (Player._Instance._PikminController._PikminInHand == null)
		{
			transform.rotation = Quaternion.Slerp(
				transform.rotation,
				Quaternion.LookRotation(movementDirection),
				_RotationSpeed * Time.deltaTime
			);
		}

		// Move the controller
		_Controller.Move(_MovementSpeed * Time.deltaTime * movementDirection);
	}
}
