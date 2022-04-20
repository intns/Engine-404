/*
 * Player.cs
 * Created by: Ambrosia
 * Created on: 8/2/2020 (dd/mm/yy)
 * Created for: having a generalised manager for the seperate Player scripts
 */

using UnityEngine;

[RequireComponent(typeof(PlayerPikminController),
	typeof(PlayerMovementController))]
public class Player : MonoBehaviour, IHealth, IEntityInfo
{
	public static Player _Instance;
	[HideInInspector] public PlayerMovementController _MovementController = null;
	[HideInInspector] public PlayerPikminController _PikminController = null;
	[HideInInspector] public PlayerUIController _UIController = null;

	[Header("Settings")]
	[SerializeField] private float _MaxHealth = 100;
	[SerializeField] private float _CurrentHealth = 100;

	private void OnEnable()
	{
		_Instance = this;
	}

	private void Awake()
	{
		_MovementController = GetComponent<PlayerMovementController>();
		_PikminController = GetComponent<PlayerPikminController>();
		_UIController = GetComponent<PlayerUIController>();

		// Resets the health back to the max if changed in the editor
		_CurrentHealth = _MaxHealth;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha8))
		{
			PikminStatsManager.Print();
		}

		// Handle health-related functions
		if (_CurrentHealth <= 0)
		{
			Die();
		}

		// Handle exiting the game/program
		if (Input.GetButtonDown("Start Button"))
		{
			Debug.Break();
			Application.Quit();
		}
	}

	private void Die()
	{
		Debug.Log("Player is dead!");
		Debug.Break();
	}

	public void Pause(bool toPause)
	{
		// Time.timeScale = toPause ? 0 : 1;
		GameManager._IsPaused = toPause;
		_MovementController._Paralysed = toPause;
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
		return _CurrentHealth -= take;
	}

	public void SetHealth(float set)
	{
		_CurrentHealth = set;
	}

	#endregion

	#region Entity Implementations
	public EntityInfo GetEntityInfo()
	{
		return EntityInfo.Player;
	}
	#endregion
}
