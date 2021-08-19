using UnityEngine;

[RequireComponent(typeof(EnemyDamageScript))]
public class Bulborb : MonoBehaviour, IPikminAttack
{
	private Animator _Animator = null;
	private EnemyDamageScript _DamageScript = null;

	private void Awake()
	{
		_Animator = GetComponent<Animator>();
		_DamageScript = GetComponent<EnemyDamageScript>();
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
		_DamageScript.SubtractHealth(damage);
		_DamageScript._HWScript._CurrentHealth = _DamageScript.GetCurrentHealth();
	}
	#endregion
}
