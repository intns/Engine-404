using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyDamageScript))]
public class FieryBlowhog : MonoBehaviour, IPikminAttack
{
	public enum States
	{
		Idle,
		MovingTowards,
		Dying,
		Dead
	}

	[Header("Components")]
	Transform _Transform = null;
	EnemyDamageScript _DamageScript = null;
	MovementEngine _MovementEngine = null;

	public PikminIntention IntentionType => PikminIntention.Attack;

	// [Header("Settings")]

	#region Unity Functions
	private void Awake()
	{
		_Transform = transform;
		_DamageScript = GetComponent<EnemyDamageScript>();
		_MovementEngine = GetComponent<MovementEngine>();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.V))
		{
			_MovementEngine.SmoothVelocity = Vector3.up * 20;
		}
		else if (Input.GetKeyDown(KeyCode.P))
		{
			_MovementEngine.SmoothVelocity = transform.forward * 20;
		}
	}
	#endregion

	#region IEnumerators
	#endregion

	#region Utility Functions
	#endregion

	#region Public Functions
	public void OnAttackStart(PikminAI pikmin)
	{
		_DamageScript._AttachedPikmin.Add(pikmin);
	}

	public void OnAttackEnd(PikminAI pikmin)
	{
		_DamageScript._AttachedPikmin.Remove(pikmin);
	}

	public void OnAttackRecieve(float damage)
	{
		if (this == null || _DamageScript == null)
		{
			return;
		}

		_DamageScript.SubtractHealth(damage);
		_DamageScript._HWScript._CurrentHealth = _DamageScript.GetCurrentHealth();
	}
	#endregion
}
