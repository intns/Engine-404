using UnityEngine;
using UnityEngine.VFX;

public class PuffStoolRewrite : Entity
{
	#region FSM

	#endregion

	[Header("Components")]
	[SerializeField] VisualEffect _DeathVFX = null;

	[Header("Settings")]
	[SerializeField] float _MovementSpeed = 5.0f;

	[Header("Debugging")]
	[SerializeField] Vector3 _MoveDirection = Vector3.zero;

	Rigidbody _Rigidbody = null;
	Animator _Animator = null;

	public new void Awake()
	{
		base.Awake();

		_Animator = GetComponent<Animator>();
		_Rigidbody = GetComponent<Rigidbody>();
	}

	public void FixedUpdate()
	{
		if (_MoveDirection != Vector3.zero)
		{
			_Rigidbody.MovePosition(_Rigidbody.position + _MoveDirection.normalized * _MovementSpeed);
		}
	}

	#region Pikmin Attacking
	public new void OnAttackEnd(PikminAI pikmin)
	{
		base.OnAttackEnd(pikmin);
	}

	public new void OnAttackRecieve(float damage, Transform hitPart)
	{
		if (_Animator == null)
		{
			return;
		}

		base.OnAttackRecieve(damage, hitPart);
	}
	#endregion

	#region Public Functions
	public void VFX_Death_Start()
	{
		_DeathVFX.Play();
	}

	public void VFX_Death_Stop()
	{
		_DeathVFX.Stop();
	}
	#endregion
}
