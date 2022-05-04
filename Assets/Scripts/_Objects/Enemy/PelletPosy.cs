using UnityEngine;

// TODO: Rewrite this shit
[RequireComponent(typeof(EnemyDamageScript))]
public class PelletPosy : MonoBehaviour, IPikminAttack
{
	public enum State
	{
		Sprout,
		Bud,
		Pellet
	}

	[Header("Settings")]
	public float _TimeToSprout = 2.5f;

	[Header("Debugging")]
	[SerializeField] private State _State = State.Bud;

	private Animator _Animator = null;
	private EnemyDamageScript _DamageScript = null;

	private void Awake()
	{
		_Animator = GetComponent<Animator>();
		_DamageScript = GetComponent<EnemyDamageScript>();
	}

	// When a Pikmin touches the part of the Pellet that pushes
	// the Pikmin away, we will play an animation
	public void OnTouchPushCollider()
	{
		_Animator.SetTrigger("Touch");
	}

	#region Pikmin Attacking Implementation
	public PikminIntention IntentionType => PikminIntention.Attack;

	public void OnAttackEnd(PikminAI pikmin)
	{
		_DamageScript._AttachedPikmin.Remove(pikmin);

		if (_DamageScript._AttachedPikmin.Count == 0 && _Animator != null)
		{
			_Animator.SetBool("hit", false);
		}
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
