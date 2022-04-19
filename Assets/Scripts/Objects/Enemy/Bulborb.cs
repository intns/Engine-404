using UnityEngine;

[RequireComponent(typeof(EnemyDamageScript), typeof(Rigidbody))]
public class Bulborb : MonoBehaviour, IPikminAttack, IHealth
{
	enum BulborbStates
	{
		Sleeping,     // Sleeping
		MoveTowards,  // Moving towards a target
		Attack,       // Attacking an object
		Dead          // Dead
	}

	enum BulborbIntention
	{
		Attack,
		Sleep
	}

	[Header("Settings")]
	[SerializeField] float _DetectionRadius = 5.0f;
	[SerializeField] float _RotationSpeed = 2;
	[SerializeField] float _MaxSpeed = 5;
	[SerializeField] float _Acceleration = 8;
	[SerializeField] float _BulborbDeathVolume = 0.5f;

	[Space]
	[SerializeField] LayerMask _PlayerAndPikminLayer;

	[Header("References")]
	[SerializeField] private AudioClip _BulborbDeathNoise;

	[Header("Debug")]
	[SerializeField] BulborbStates _CurrentState = BulborbStates.Sleeping;
	[SerializeField] BulborbIntention _CurrentIntention = BulborbIntention.Sleep;
	[SerializeField] float _CurrentMoveSpeed = 0;
	[SerializeField] Vector3 _MovementVector;
	[SerializeField] GameObject _SpawnObj;

	[SerializeField] private Transform _TargetObject;
	[SerializeField] private Collider _TargetObjectCollider;

	private Animator _Animator = null;
	private Rigidbody _Rigidbody = null;
	private EnemyDamageScript _DamageScript = null;

	private void Awake()
	{
		_Animator = GetComponent<Animator>();
		_Rigidbody = GetComponent<Rigidbody>();
		_DamageScript = GetComponent<EnemyDamageScript>();

		_SpawnObj = new GameObject(name + " Spawn Point");
		_SpawnObj.transform.SetPositionAndRotation(transform.position, transform.rotation);
		_CurrentState = BulborbStates.Sleeping;
	}

	private void Update()
	{
		if (GameManager._IsPaused)
		{
			return;
		}

		switch (_CurrentState)
		{
			case BulborbStates.Sleeping:
				HandleSleeping();
				break;
			case BulborbStates.Attack:
				ChangeState(BulborbStates.Sleeping);
				break;
			case BulborbStates.Dead:
				HandleDeath();
				break;
			default:
				break;
		}
	}

	private void FixedUpdate()
	{
		if (GameManager._IsPaused)
		{
			return;
		}

		if (_CurrentState != BulborbStates.MoveTowards)
		{
			return;
		}

		// Only state possible is move towards

		// If the object we were following is not around anymore
		if (_TargetObject == null)
		{
			// Return back to spawn position
			MoveTowards(_SpawnObj.transform.position);

			if (MathUtil.DistanceTo(transform.position, _SpawnObj.transform.position, false) < 0.25f)
			{
				transform.rotation = _SpawnObj.transform.rotation;
				ChangeState(BulborbStates.Sleeping);
			}
		}
		else
		{
			Vector3 closestPoint = ClosestPointOnTarget(_TargetObject, _TargetObjectCollider);
			MoveTowards(closestPoint);

			float dist = MathUtil.DistanceTo(transform.position, closestPoint, false);
			if (dist < 0.5f)
			{
				Debug.Log("Attempting attack", this);
				ChangeState(BulborbStates.Attack);
			}

			if (MathUtil.DistanceTo(transform.position, _SpawnObj.transform.position, false) > 25)
			{
				_TargetObject = null;
			}
		}

		float storedY = _Rigidbody.velocity.y;
		_Rigidbody.velocity = _MovementVector;
		_MovementVector = new Vector3(0, storedY, 0);
	}

	private void LateUpdate()
	{
		if (GameManager._IsPaused)
		{
			return;
		}

		Vector2 horizonalVelocity = new Vector2(_Rigidbody.velocity.x, _Rigidbody.velocity.z);
		_Animator.SetBool("Walking", horizonalVelocity.magnitude >= 1.5f);

		_Animator.SetBool("Sleeping", _CurrentState == BulborbStates.Sleeping);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.DrawWireSphere(transform.position + transform.forward, _DetectionRadius);
	}

	#region Helper Functions
	private void ChangeState(BulborbStates nextState)
	{
		// There's no saving bulborb from death
		if (_CurrentState == BulborbStates.Dead)
		{
			return;
		}

		if (_CurrentState == BulborbStates.MoveTowards)
		{
			_TargetObject = null;
			_TargetObjectCollider = null;
		}

		_CurrentState = nextState;
	}

	private void MoveTowards(Vector3 position, bool stopEarly = true)
	{
		// We need to be atleast somewhat close to a surface before we can start moving again
		if (Physics.Raycast(transform.position - transform.up, Vector3.down, 1.5f))
		{
			return;
		}

		// Rotate to look at the object we're moving towards
		Vector3 delta = (position - transform.position).normalized;
		delta.y = 0;
		// TODO: How the fuck do I rotate the bulborb model?

		if (stopEarly && MathUtil.DistanceTo(transform.position, position, false) < 0.5f)
		{
			_CurrentMoveSpeed = Mathf.SmoothStep(_CurrentMoveSpeed, 0, 0.25f);
		}
		else
		{
			// To prevent instant, janky movement we step towards the resultant max speed according to _Acceleration
			_CurrentMoveSpeed = Mathf.SmoothStep(_CurrentMoveSpeed, _MaxSpeed, _Acceleration * Time.fixedDeltaTime);
		}

		Vector3 newVelocity = delta.normalized * _CurrentMoveSpeed;
		newVelocity.y = _Rigidbody.velocity.y;
		_MovementVector = newVelocity;
	}

	private Vector3 ClosestPointOnTarget(Transform target, Collider collider = null, float maxDistance = float.PositiveInfinity)
	{
		// Check if there is a collider for the target object we're running to
		if (collider != null)
		{
			Vector3 direction = collider.ClosestPoint(transform.position) - transform.position;
			direction.y = 0;
			if (Physics.Raycast(transform.position, direction, out RaycastHit hit, maxDistance))
			{
				return hit.point;
			}
			else
			{
				return collider.ClosestPoint(transform.position);
			}
		}

		return target.position;
	}
	#endregion

	#region Handling States
	private void HandleSleeping()
	{
		Collider[] colls = Physics.OverlapSphere(transform.position + transform.forward, _DetectionRadius, _PlayerAndPikminLayer);
		Collider closest = null;
		float currentDist = 0;
		foreach (Collider coll in colls)
		{
			if (closest == null)
			{
				closest = coll;
				currentDist = MathUtil.DistanceTo(transform.position, closest.transform.position, false);
				continue;
			}

			float dist = MathUtil.DistanceTo(transform.position, coll.transform.position, false);
			if (dist < currentDist)
			{
				closest = coll;
			}
		}

		if (closest != null)
		{
			ChangeState(BulborbStates.MoveTowards);
			_CurrentIntention = BulborbIntention.Attack;
			_TargetObject = closest.transform;
			_TargetObjectCollider = closest;
		}
	}

	private void HandleDeath()
	{
		AudioSource.PlayClipAtPoint(_BulborbDeathNoise, transform.position, _BulborbDeathVolume);
	}

	public void Die()
	{
		ChangeState(BulborbStates.Dead);
	}
	#endregion

	#region Interface Methods
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

	float IHealth.GetCurrentHealth()
	{
		return 1;
	}

	float IHealth.GetMaxHealth()
	{
		return 1;
	}

	// Empty implementation purposely
	void IHealth.SetHealth(float h) { }

	float IHealth.SubtractHealth(float h)
	{
		//Pikmin don't have health so they die no matter what
		Die();
		return 0;
	}

	float IHealth.AddHealth(float h)
	{
		return 1;
	}

	#endregion
}
