/*
 * Player.cs
 * Created by: Ambrosia
 * Created on: 8/2/2020 (dd/mm/yy)
 * Last update by : Senka
 * Last update on : 9/7/2022
 * Created for: having a generalised manager for the seperate Player scripts
 */

using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerPikminController),
									typeof(AnimationController),
									typeof(PlayerMovementController))]
public class Player : MonoBehaviour, IHealth
{
	public static Player _Instance;
	public PlayerMovementController _MovementController = null;
	public PlayerPikminController _PikminController = null;
	public PlayerUIController _UIController = null;
	public WhistleController _WhistleController = null;
	public GameObject _ModelObject = null;

	[Header("Components")]
	[SerializeField] LineRenderer _WhistleLine = null;

	[Header("Settings")]
	[SerializeField] float _MaxHealth = 100;
	[SerializeField] float _CurrentHealth = 100;
	[Space]
	[SerializeField] float _AttackSphereRadius = 2.5f;
	[SerializeField] float _AttackDamage = 2.5f;
	[SerializeField] float _AttackCooldown = 1.5f;

	[Header("Animations")]
	[SerializeField] AnimationController _AnimController;

	public static class PlayerAnimation
	{
		public const int Walk = 0;
		public const int Die = 1;
		public const int Damage = 2;
		public const int Idle = 3;
		public const int Attack = 4;
	}

	[SerializeField] AnimationClip _WalkAnimation;
	[SerializeField] AnimationClip _DieAnimation;
	[SerializeField] AnimationClip _DamageAnimation;
	[SerializeField] AnimationClip _IdleAnimation;
	[SerializeField] AnimationClip _AttackAnimation;

	[SerializeField] CharacterController _Controller;
	bool _IsHit = false;
	bool _Walking = false;

	float _AttackTimer = 0.0f;
	float _DamageCooldown = 0.0f;

	void OnEnable()
	{
		_Instance = this;
	}

	void Awake()
	{
		_MovementController = GetComponent<PlayerMovementController>();
		_PikminController = GetComponent<PlayerPikminController>();

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

			// BUGFIX: Whistle line clips backwards and all sorts if you don't check the length
			if (MathUtil.DistanceTo(transform.position, _WhistleController._Reticle.position) < 5)
			{
				_WhistleLine.SetPosition(0, Vector3.zero);
				_WhistleLine.SetPosition(1, Vector3.zero);
			}
			else
			{
				// Offset the whistle line from the player position and then the reticle position
				Vector3 dir1 = MathUtil.DirectionFromTo(transform.position, _WhistleController._Reticle.position, true);
				Vector3 dir2 = MathUtil.DirectionFromTo(_WhistleController._Reticle.position, transform.position, true);

				_WhistleLine.SetPosition(0, transform.position + dir1);
				_WhistleLine.SetPosition(1, _WhistleController._Reticle.position + dir2);
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

	void OnDrawGizmosSelected()
	{
		Gizmos.DrawWireSphere(transform.position + transform.forward, _AttackSphereRadius);
		Handles.Label(transform.position + transform.forward + Vector3.up * _AttackSphereRadius, $"Attack Damage: {_AttackDamage}");
	}

	public void Die()
	{
		DayTimeManager._Instance.FinishDay();
	}

	public void PikminExtinction()
	{
		StartCoroutine(IE_PikminExtinctionSequence());
	}

	public void Squish()
	{
		_MovementController._Paralysed = true;
	}

	public void Pause(PauseType type)
	{
		_AnimController.ChangeState(PlayerAnimation.Idle);

		if (type == PauseType.Unpaused)
		{
			_UIController.FadeInUI(true);
		}
		else
		{
			_UIController.FadeOutUI();
		}

		_MovementController._Paralysed = type != PauseType.Unpaused;
		GameManager.PauseType = type;
	}

	// When start key is pressed
	public void OnStart()
	{
		Die();
	}

	// Happens whenever the movement joystick/buttons change values
	public void OnMovement(InputAction.CallbackContext context)
	{
		// If NOT paralyzed AND NOT paused AND IS moving any direction
		_Walking = (!_MovementController._Paralysed) && (!GameManager.IsPaused) && ((context.ReadValue<Vector2>().x != 0) || (context.ReadValue<Vector2>().y != 0));
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
		if (c != null)
		{
			c.SubtractHealth(_AttackDamage);

			_AttackTimer = _AttackCooldown;
		}
	}

	#region Animation Callbacks

	#endregion

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
		Camera.main.GetComponent<CameraFollow>().Shake(50.0f);
		_DamageCooldown = 3.0f;
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

	#endregion

	#region IEnumerators
	IEnumerator IE_PikminExtinctionSequence()
	{
		Pause(PauseType.Paused);
		_UIController.FadeOutUI();

		yield return new WaitForSecondsRealtime(3.5f);

		Die();
	}
	#endregion
}
