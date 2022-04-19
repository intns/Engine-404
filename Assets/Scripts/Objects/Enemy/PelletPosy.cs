using UnityEngine;

[RequireComponent(typeof(EnemyDamageScript))]
public class PelletPosy : MonoBehaviour, IPikminAttack
{
	private Animator _Animator = null;
	private EnemyDamageScript _DamageScript = null;

	private void Awake()
	{
		_Animator = GetComponent<Animator>();
		_DamageScript = GetComponent<EnemyDamageScript>();
	}

	private void Update()
	{
		if (_DamageScript._AttachedPikmin.Count == 0 && _Animator.GetBool("hit"))
		{
			_Animator.SetBool("hit", true);
		}
	}

	#region Pikmin Attacking Implementation
	public PikminIntention IntentionType => PikminIntention.Attack;

	public void OnAttackEnd(PikminAI pikmin)
	{
		_DamageScript._AttachedPikmin.Remove(pikmin);
	}

	public void OnAttackStart(PikminAI pikmin)
	{
		_DamageScript._AttachedPikmin.Add(pikmin);
	}

	public void OnAttackRecieve(float damage)
	{
		if (this == null || _Animator == null || _DamageScript == null)
		{
			return;
		}

		if (_Animator.GetBool("hit") == false)
		{
			_Animator.SetBool("hit", true);
		}

		// Should be called last in case the 
		_DamageScript.SubtractHealth(damage);
		_DamageScript._HWScript._CurrentHealth = _DamageScript.GetCurrentHealth();
	}
	#endregion
}
