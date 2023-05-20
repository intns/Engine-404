using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

[RequireComponent(
	typeof(PlayerPikminController),
	typeof(AnimationController),
	typeof(PlayerMovementController)
)]
public class Player : MonoBehaviour, IHealth, IInteraction
{
	public static Player _Instance;

	public PlayerMovementController _MovementController;
	public PlayerPikminController _PikminController;
	public PlayerUIController _UIController;
	public WhistleController _WhistleController;

	[Header("Components")]
	[SerializeField] LineRenderer _WhistleLine;
	[SerializeField] GameObject _ModelObject;

	[Header("Settings")]
	[SerializeField] float _MaxHealth = 100;
	[SerializeField] float _CurrentHealth = 100;
	[Space]
	[SerializeField] float _AttackSphereRadius = 2.5f;
	[SerializeField] float _AttackDamage = 2.5f;
	[SerializeField] float _AttackCooldown = 1.5f;

	[Header("Animations")]
	[SerializeField] AnimationController _AnimController;

	[SerializeField] AnimationClip _WalkAnimation;
	[SerializeField] AnimationClip _DieAnimation;
	[SerializeField] AnimationClip _DamageAnimation;
	[SerializeField] AnimationClip _IdleAnimation;
	[SerializeField] AnimationClip _AttackAnimation;

	[SerializeField] CharacterController _Controller;

	float _AttackTimer;
	float _DamageCooldown;
	bool _IsHit;
	bool _Walking;

	void Awake()
	{
		_MovementController = GetComponent<PlayerMovementController>();
		_PikminController = GetComponent<PlayerPikminController>();

		Assert.IsNotNull(_MovementController);
		Assert.IsNotNull(_PikminController);
		Assert.IsNotNull(_UIController);
		Assert.IsNotNull(_WhistleController);

		Debug.Assert(PlayerAnimation.Walk == _AnimController.AddState(_WalkAnimation));
		Debug.Assert(PlayerAnimation.Die == _AnimController.AddState(_DieAnimation));
		Debug.Assert(PlayerAnimation.Damage == _AnimController.AddState(_DamageAnimation));
		Debug.Assert(PlayerAnimation.Idle == _AnimController.AddState(_IdleAnimation));
		Debug.Assert(PlayerAnimation.Attack == _AnimController.AddState(_AttackAnimation));

		// Resets the health back to the max if changed in the editor
		_CurrentHealth = _MaxHealth;
		_DamageCooldown = 0.0f;

		// Lock to the floor to start the scene
		if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit))
		{
			transform.position = hit.point;
		}
	}

	void Update()
	{
		if (_DamageCooldown >= 0)
		{
			_DamageCooldown -= Time.deltaTime;
		}

		if (!GameManager.IsPaused)
		{
			if (_IsHit)
			{
				_AnimController.ChangeState(PlayerAnimation.Damage, true);
				_IsHit = false;
			}
			else if (_Walking)
			{
				_AnimController.ChangeState(PlayerAnimation.Walk);
			}
			else
			{
				_AnimController.ChangeState(PlayerAnimation.Idle);
			}

			// Handle health-related functions
			if (_CurrentHealth <= 0)
			{
				Die();
			}
		}
		else
		{
			_WhistleLine.SetPosition(0, Vector3.zero);
			_WhistleLine.SetPosition(1, Vector3.zero);

			_AnimController.ChangeState(PlayerAnimation.Idle, true);
		}

		if (_AttackTimer > 0)
		{
			_AttackTimer -= Time.deltaTime;
		}
	}

	void OnEnable()
	{
		_Instance = this;
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.DrawWireSphere(transform.position + transform.forward, _AttackSphereRadius);

		Handles.Label(
			transform.position + transform.forward + Vector3.up * _AttackSphereRadius,
			$"Attack Damage: {_AttackDamage}"
		);
	}

	public void SetModelVisibility(bool isVisible)
	{
		_ModelObject.SetActive(isVisible);
	}

	public void Die()
	{
		DayTimeManager._Instance.FinishDay();
	}

	// Happens whenever the movement joystick/buttons change values
	public void OnMovement(InputAction.CallbackContext context)
	{
		// If NOT paralyzed AND NOT paused AND IS moving any direction
		_Walking = !_MovementController._Paralysed && !GameManager.IsPaused
		                                           && (context.ReadValue<Vector2>().x != 0
		                                               || context.ReadValue<Vector2>().y != 0);
	}

	public void OnPrimaryAction(InputAction.CallbackContext context)
	{
		if (!context.started || GameManager.IsPaused || _CurrentHealth <= 0 || _MovementController._Paralysed
		    || !_PikminController._CanPlayerAttack || _AttackTimer > 0)
		{
			return;
		}

		// TODO: Make animation play
		_AnimController.ChangeState(PlayerAnimation.Attack, true, true);

		// Attack!
		if (!Physics.SphereCast(transform.position, _AttackSphereRadius, transform.forward, out RaycastHit info)
		    || info.transform.CompareTag("Player") || info.transform.CompareTag("Pikmin"))
		{
			return;
		}

		IHealth c = info.transform.GetComponentInParent<IHealth>();

		if (c == null)
		{
			return;
		}

		c.SubtractHealth(_AttackDamage);

		_AttackTimer = _AttackCooldown;
	}

	// When start key is pressed
	public void OnStart()
	{
		Die();
	}

	public void Pause(PauseType type, bool fadeUI = true)
	{
		_AnimController.ChangeState(PlayerAnimation.Idle);

		if (type == PauseType.Unpaused)
		{
			if (fadeUI)
			{
				_UIController.FadeInUI(true);
			}

			_WhistleLine.enabled = true;
		}
		else
		{
			if (fadeUI)
			{
				_UIController.FadeInUI();
			}

			_WhistleLine.enabled = false;
		}

		_MovementController._Paralysed = type != PauseType.Unpaused;
		GameManager.PauseType = type;
	}

	public void PikminExtinction()
	{
		StartCoroutine(IE_PikminExtinctionSequence());
	}

	public void SetWhistleLine(Vector3 destination)
	{
		// BUGFIX: Whistle line clips backwards and all sorts if you don't check the length
		if (MathUtil.DistanceTo(transform.position, destination) < 5.0f)
		{
			_WhistleLine.SetPosition(0, Vector3.zero);
			_WhistleLine.SetPosition(1, Vector3.zero);
		}
		else
		{
			// Offset the whistle line from the player position and then the reticle position
			Vector3 dir1 = MathUtil.DirectionFromTo(transform.position, destination, true);
			Vector3 dir2 = MathUtil.DirectionFromTo(destination, transform.position, true);

			_WhistleLine.SetPosition(0, transform.position + dir1);
			_WhistleLine.SetPosition(1, destination + dir2);
		}
	}

	#region IEnumerators

	IEnumerator IE_PikminExtinctionSequence()
	{
		Pause(PauseType.Paused);
		_UIController.FadeOutUI();

		yield return new WaitForSecondsRealtime(3.5f);

		Die();
	}

	#endregion

	public static class PlayerAnimation
	{
		public const int Walk = 0;
		public const int Die = 1;
		public const int Damage = 2;
		public const int Idle = 3;
		public const int Attack = 4;
	}

	#region Health Implementation

	// 'Getter' functions
	public float GetCurrentHealth()
	{
		return _CurrentHealth;
	}

	public float GetMaxHealth()
	{
		return _MaxHealth;
	}

	// 'Setter' functions
	public float AddHealth(float give)
	{
		return _CurrentHealth += give;
	}

	void DamageHealth()
	{
		_IsHit = true;
		_AnimController.ChangeState(PlayerAnimation.Damage, true, true);
		CameraFollow._Instance.Shake(5.0f);
		_DamageCooldown = 2.5f;
	}

	public float SubtractHealth(float take)
	{
		if (_DamageCooldown < 0)
		{
			DamageHealth();
			_CurrentHealth -= take;
		}

		return _CurrentHealth;
	}

	public void SetHealth(float set)
	{
		if (_CurrentHealth > set)
		{
			DamageHealth();
			_CurrentHealth = set;
		}
	}

	public void ActFire()
	{
		if (_DamageCooldown < 0)
		{
			CameraFollow._Instance.Shake(1.0f);
		}

		SubtractHealth(_MaxHealth / 6.0f);
	}

	public void ActSquish()
	{
		SubtractHealth(_MaxHealth / 4.0f);
	}

	public void ActWater()
	{
	}

	#endregion
}
