using UnityEngine;

[RequireComponent(typeof(EnemyDamageScript))]
public class Bulborb : MonoBehaviour, IPikminAttack
{
	enum States
	{
		Sleeping,
		Wake,
		Attack,
	};

	[Header("Settings")]
	[SerializeField] float _DetectionRadius = 5.0f;

	[Header("Debug")]
	[SerializeField] Transform _Target = null;
	[SerializeField] States _CurrentState = States.Sleeping;

	private Animator _Animator = null;
	private EnemyDamageScript _DamageScript = null;

	private void Awake()
	{
		_Animator = GetComponent<Animator>();
		_DamageScript = GetComponent<EnemyDamageScript>();
		_CurrentState = States.Sleeping;
	}

	private void Update()
	{
		if (GameManager._IsPaused)
		{
			return;
		}

		switch (_CurrentState)
		{
			case States.Sleeping:
				break;
			case States.Wake:
				break;
			case States.Attack:
				break;
			default:
				break;
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.DrawSphere(transform.position + transform.forward, _DetectionRadius);
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
