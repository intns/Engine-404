using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyDamageScript))]
public class PuffStool : MonoBehaviour, IPikminAttack
{
	enum States
	{
		Idle,
		Walking,

		StunStart,
		Stunned,
		StunEnd,

		Attack,
	}

	[Header("Components")]
	Transform _Transform = null;
	Animator _Animator = null;
	MovementEngine _MovementEngine = null;
	EnemyDamageScript _DamageScript = null;

	[Header("Settings")]
	[SerializeField] float _Speed = 15;
	[Space]
	[SerializeField] float _TimePerAttack = 5;
	[Space]
	[SerializeField] float _TimeForStun = 3.5f;
	[SerializeField] float _HealthForStun = 100;
	[Space]
	[SerializeField] float _DetectionSphere = 5;
	[SerializeField] float _DeathSphere = 2.5f;
	[SerializeField] LayerMask _PlayerAndPikminMask;

	[Header("Debugging")]
	[SerializeField] States _CurrentState = States.Idle;
	[SerializeField] Transform _TargetObject;
	[SerializeField] float _AttackTimer = 0;
	[SerializeField] float _StunTimer = 0;
	[SerializeField] float _CurrentHealthForStun = 0;

	string _CurrentAnimState = "";
	const string ANIM_Idle = "Idle";
	const string ANIM_Walk = "Walk";
	const string ANIM_Attack = "Attack";
	const string ANIM_StunStart = "StunStart";
	const string ANIM_StunEnd = "StunEnd";
	const string ANIM_Stunned = "Stunned";

	#region Unity Functions
	private void Awake()
	{
		_Transform = transform;
		_Animator = GetComponent<Animator>();
		_MovementEngine = GetComponent<MovementEngine>();
		_DamageScript = GetComponent<EnemyDamageScript>();
		_CurrentHealthForStun = _HealthForStun;
	}

	private void Update()
	{
		switch (_CurrentState)
		{
			case States.Idle:
				{
					Collider closestObj = MathUtil.GetClosestCollider(_Transform.position, Physics.OverlapSphere(_Transform.position, _DetectionSphere, _PlayerAndPikminMask));
					if (closestObj != null)
					{
						_TargetObject = closestObj.transform;
						ChangeState(States.Walking);
					}

					ChangeAnimationState(ANIM_Idle);
					break;
				}
			case States.Walking:
				{
					if (_TargetObject == null)
					{
						ChangeState(States.Idle);
						break;
					}
					else
					{
						ChangeAnimationState(ANIM_Walk);
					}

					Vector3 direction = MathUtil.DirectionFromTo(_TargetObject.position, _Transform.position);

					_MovementEngine.SmoothVelocity = _Speed * direction;
					_Transform.rotation = Quaternion.Slerp(_Transform.rotation, Quaternion.LookRotation(direction), 10 * Time.deltaTime);

					float distanceToTarget = float.PositiveInfinity;

					Collider[] objects = Physics.OverlapSphere(_Transform.position, _DetectionSphere, _PlayerAndPikminMask);
					Collider closestObj = MathUtil.GetClosestCollider(_Transform.position, objects);
					if (closestObj != null)
					{
						float curDist = MathUtil.DistanceTo(_Transform.position, _TargetObject.position);
						float closestDist = MathUtil.DistanceTo(_Transform.position, closestObj.transform.position);
						if (closestDist < curDist)
						{
							_TargetObject = closestObj.transform;
							distanceToTarget = closestDist;
						}
						else
						{
							distanceToTarget = curDist;
						}
					}
					else
					{
						_TargetObject = null;
					}

					// Attack!
					if (distanceToTarget < 3)
					{
						_AttackTimer += Time.deltaTime;

						if (_AttackTimer >= _TimePerAttack)
						{
							_AttackTimer = 0;
							ChangeState(States.Attack);
						}
					}

					break;
				}
			case States.Stunned:
				{
					ChangeAnimationState(ANIM_Stunned);
					_MovementEngine.SetVelocity(Vector3.zero);

					_StunTimer += Time.deltaTime;
					if (_StunTimer >= _TimeForStun)
					{
						while (_DamageScript._AttachedPikmin.Count > 0)
						{
							PikminAI pik = _DamageScript._AttachedPikmin[0];
							if (pik != null)
							{
								pik.ChangeState(PikminStates.Idle);
								pik._AddedVelocity = MathUtil.DirectionFromTo(_Transform.position, pik.transform.position) * 50;
							}
						}

						ChangeState(States.StunEnd);
					}

					break;
				}
			case States.Attack:
				{
					_MovementEngine.SetVelocity(Vector3.zero);
				}
				break;

			case States.StunEnd:
			case States.StunStart:
				_MovementEngine.SetVelocity(Vector3.zero);
				break;
			default:
				break;
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.DrawWireSphere(transform.position, _DetectionSphere);
	}
	#endregion

	#region IEnumerators
	#endregion

	#region Utility Functions
	private void ChangeState(States newState)
	{
		switch (_CurrentState)
		{
			case States.Walking when _TargetObject != null:
				_TargetObject = null;
				break;
			case States.Stunned:
				_StunTimer = 0;
				_CurrentHealthForStun = _HealthForStun;
				break;
			case States.StunStart:
			case States.StunEnd:
			default:
				break;
		}

		_CurrentState = newState;

		if (newState == States.StunStart)
		{
			ChangeAnimationState(ANIM_StunStart);
		}
		else if (newState == States.StunEnd)
		{
			ChangeAnimationState(ANIM_StunEnd);
		}
		else if (newState == States.Attack)
		{
			ChangeAnimationState(ANIM_Attack);
		}
	}

	private void ChangeAnimationState(string newState)
	{
		if (_CurrentAnimState == newState)
		{
			return;
		}

		_CurrentAnimState = newState;
		_Animator.Play(newState);
	}
	#endregion

	#region Public Functions
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

		_CurrentHealthForStun -= damage;
		if (_CurrentHealthForStun <= 0)
		{
			if (_CurrentState != States.StunStart
				&& _CurrentState != States.Stunned
				&& _CurrentState != States.StunEnd
				&& _CurrentState != States.Attack)
			{
				ChangeState(States.StunStart);
				_CurrentHealthForStun = _HealthForStun;
			}
		}

		// Should be called last in case the 
		_DamageScript.SubtractHealth(damage);
		_DamageScript._HWScript._CurrentHealth = _DamageScript.GetCurrentHealth();
	}

	public void ANIM_OnStunStart_End()
	{
		ChangeState(States.Stunned);
	}

	public void ANIM_OnStunEnd_End()
	{
		ChangeState(States.Attack);
	}

	public void ANIM_OnAttack_End()
	{
		ChangeState(States.Idle);
	}

	public void ANIM_OnAttack_Do()
	{
		Collider[] objects = Physics.OverlapSphere(_Transform.position, _DetectionSphere, _PlayerAndPikminMask);

		foreach (var coll in objects)
		{
			if (Vector3.Distance(coll.transform.position, _Transform.position) < 2.5f)
			{
				PikminAI ai = coll.GetComponent<PikminAI>();
				if (ai != null)
				{
					ai.Die(0);
				}

				if (coll.transform == _TargetObject)
				{
					_TargetObject = null;
				}
			}
		}
	}
	#endregion
}
