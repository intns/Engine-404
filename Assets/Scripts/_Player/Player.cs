/*
 * Player.cs
 * Created by: Ambrosia
 * Created on: 8/2/2020 (dd/mm/yy)
 * Last update by : Senka
 * Last update on : 9/7/2022
 * Created for: having a generalised manager for the seperate Player scripts
 */

using System.Collections;
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

	[Header("Components")]
	[SerializeField] private LineRenderer _WhistleLine = null;

	[Header("Settings")]
	[SerializeField] private float _MaxHealth = 100;
	[SerializeField] private float _CurrentHealth = 100;

	[Header("Animations")]
	[SerializeField] AnimationController _AnimController;

	public static class PlayerAnimation
	{
		public const int Walk = 0;
		public const int Die = 1;
		public const int Damage = 2;
		public const int Idle1 = 3;
		public const int Idle2 = 4;

		public static int Idle { get { return Idle1 + Random.Range(0, 1); } }
	}

	[SerializeField] AnimationClip _WalkAnimation;
[SerializeField] AnimationClip _DieAnimation;
[SerializeField] AnimationClip _DamageAnimation;
[SerializeField] AnimationClip _Idle1Animation;
[SerializeField] AnimationClip _Idle2Animation;

[SerializeField] private CharacterController _Controller;
bool _IsHit = false;
bool _Walking = false;

private void OnEnable()
{
	_Instance = this;
}

private void Awake()
{
	_MovementController = GetComponent<PlayerMovementController>();
	_PikminController = GetComponent<PlayerPikminController>();

	Debug.Assert(PlayerAnimation.Walk == _AnimController.AddState(_WalkAnimation));
	Debug.Assert(PlayerAnimation.Die == _AnimController.AddState(_DieAnimation));
	Debug.Assert(PlayerAnimation.Damage == _AnimController.AddState(_DamageAnimation));
	Debug.Assert(PlayerAnimation.Idle1 == _AnimController.AddState(_Idle1Animation));
	Debug.Assert(PlayerAnimation.Idle2 == _AnimController.AddState(_Idle2Animation));

	// Resets the health back to the max if changed in the editor
	_CurrentHealth = _MaxHealth;
}

private void Update()
{
	if (!GameManager.IsPaused)
	{
		if (_Walking)
		{
			_AnimController.ChangeState(PlayerAnimation.Walk);
		}
		else if (_IsHit)
		{
			_AnimController.ChangeState(PlayerAnimation.Damage, true);
			_IsHit = false;
		}
		else
		{
			_AnimController.ChangeState(PlayerAnimation.Idle, true);
		}

		// Handle health-related functions
		if (_CurrentHealth <= 0)
		{
			Die();
		}

		// Offset the whistle line from the player position and then the reticle position
		Vector3 dir1 = MathUtil.DirectionFromTo(transform.position, _WhistleController._Reticle.position, true);
		Vector3 dir2 = MathUtil.DirectionFromTo(_WhistleController._Reticle.position, transform.position, true);

		_WhistleLine.SetPosition(0, transform.position + dir1);
		_WhistleLine.SetPosition(1, _WhistleController._Reticle.position + dir2);
	}
	else
	{
		_WhistleLine.SetPosition(0, Vector3.zero);
		_WhistleLine.SetPosition(1, Vector3.zero);

		_AnimController.ChangeState(PlayerAnimation.Idle, true);
	}
}

public void Die()
{
	DayTimeManager._Instance.FinishDay();
}

public void PikminExtinction()
{
	StartCoroutine(IE_PikminExtinctionSequence());
}

public void Pause(PauseType type)
{
	_AnimController.ChangeState(PlayerAnimation.Idle);

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

public float SubtractHealth(float take)
{
	_IsHit = true;
	_AnimController.ChangeState(PlayerAnimation.Damage, true);
	return _CurrentHealth -= take;
}

public void SetHealth(float set)
{
	if (_CurrentHealth > set)
	{
		_AnimController.ChangeState(PlayerAnimation.Damage, true);
		_IsHit = true;
	}

	_CurrentHealth = set;
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
