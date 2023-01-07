using UnityEngine;

public class PelletPosy : Entity
{
	[Header("Settings")]
	public float _TimeToSprout = 2.5f;

	Animator _Animator = null;

	public new void Awake()
	{
		base.Awake();

		_Animator = GetComponent<Animator>();
	}

	#region Pikmin Attacking
	public new void OnAttackEnd(PikminAI pikmin)
	{
		base.OnAttackEnd(pikmin);
		
		if (_Animator != null)
		{
			_Animator.SetBool("hit", false);
		}
	}

	public new void OnAttackRecieve(float damage)
	{
		if (this == null || _Animator == null)
		{
			return;
		}

		base.OnAttackRecieve(damage);

		if (_Animator.GetBool("hit") == false)
		{
			_Animator.SetBool("hit", true);
		}
	}
	#endregion
}
