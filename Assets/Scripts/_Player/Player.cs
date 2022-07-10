/*
 * Player.cs
 * Created by: Ambrosia
 * Created on: 8/2/2020 (dd/mm/yy)
 * Last update by : Senka
 * Last update on : 9/7/2022
 * Created for: having a generalised manager for the seperate Player scripts
 */

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerPikminController),
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
	[SerializeField] private Animator _Animator;
	[SerializeField] private CharacterController _Controller;
	bool _IsHit = false;

	private void OnEnable()
	{
		_Instance = this;
	}

	private void Awake()
	{
		_MovementController = GetComponent<PlayerMovementController>();
		_PikminController = GetComponent<PlayerPikminController>();

		// Resets the health back to the max if changed in the editor
		_CurrentHealth = _MaxHealth;
	}

	private void Update()
	{
		if (!GameManager._IsPaused)
		{
			if (_IsHit)
			{
				_Animator.ResetTrigger("Damage");
				_IsHit = false;
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
		}
	}

	private void Die()
	{
		DayTimeManager._Instance.FinishDay();
	}

	public void Pause(bool toPause, PauseType type = PauseType.Full)
	{
		// Time.timeScale = toPause ? 0 : 1;
		_Animator.SetBool("Walk", false);
		_Animator.ResetTrigger("Damage");

		GameManager._IsPaused = toPause;
		GameManager._PauseType = type;
		_MovementController._Paralysed = toPause;
	}

	// When start key is pressed
	private void OnStart() {
		Die();
	}

	// Happens whenever the movement joystick/buttons change values
	private void OnMovement(InputValue value) {
		// If the player is moving in any direction and the game isn't paused, play the movement animation
		_Animator.SetBool("Walk", (!_MovementController._Paralysed) && (!GameManager._IsPaused) && ((value.Get<Vector2>().x != 0) || (value.Get<Vector2>().y != 0)));
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

	public float SubtractHealth(float take)
	{
		_Animator.SetTrigger("Damage");
		_IsHit = true;
		return _CurrentHealth -= take;
	}

	public void SetHealth(float set)
	{
		if (_CurrentHealth > set)
		{
			_Animator.SetTrigger("Damage");
			_IsHit = true;
		}

		_CurrentHealth = set;
	}

	#endregion
}
