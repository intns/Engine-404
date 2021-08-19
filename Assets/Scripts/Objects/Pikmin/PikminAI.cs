/*
 * PikminAI.cs
 * Created by: Ambrosia, Kman
 * Created on: 30/4/2020 (dd/mm/yy)
 */

using UnityEngine;

public enum PikminStates
{
	Idle,
	RunningTowards,
	Attacking,

	Carrying,

	// Holding/Throwing States
	BeingHeld,
	Thrown,

	Dead,
	Waiting,
}

// Immediate states after running towards another object/position
public enum PikminIntention
{
	Attack,
	Carry,
	PullWeeds,
	Idle,
}

public class PikminAI : MonoBehaviour, IHealth
{
	[Header("Components")]
	// Holds everything that makes a Pikmin unique
	public PikminObject _Data = null;
	[SerializeField] private LayerMask _PikminInteractableMask = 0;

	[Header("VFX")]
	[SerializeField] private GameObject _DeathParticle = null;
	[HideInInspector] public Transform _FormationPosition = null;

	[Header("Head Types")]
	[SerializeField] private Transform _LeafSpawn = null;
	[SerializeField] private PikminMaturity _StartingMaturity = PikminMaturity.Leaf;
	private GameObject[] _HeadModels = null;
	private PikminMaturity _CurrentMaturity = PikminMaturity.Leaf;

	#region Debugging Variables

	[Header("Debugging")]
	[SerializeField] private PikminStates _CurrentState = PikminStates.Idle;

	[Header("Idle")]
	[SerializeField] private PikminIntention _Intention = PikminIntention.Idle;
	[SerializeField] private Transform _TargetObject = null;
	[SerializeField] private Collider _TargetObjectCollider = null;

	[Header("Attacking")]
	[SerializeField] private IPikminAttack _Attacking = null;
	[SerializeField] private Transform _AttackingTransform = null;
	[SerializeField] private float _AttackTimer = 0;
	[SerializeField] private float _AttackJumpTimer = 0;

	[Header("Carrying")]
	[SerializeField] private IPikminCarry _Carrying = null;

	[Header("Stats")]
	[SerializeField] private PikminStatSpecifier _CurrentStatSpecifier = default;
	[SerializeField] private float _CurrentMoveSpeed = 0;

	[Header("Misc")]
	public bool _InSquad = false;
	[SerializeField] private float _RagdollTime = 0;
	[SerializeField] private Vector3 _MovementVector = Vector3.zero;
	[SerializeField] private float _PlayerPushScale = 30;
	[SerializeField] private float _PikminPushScale = 15;
	private Vector3 _LatchOffset = Vector3.zero;
	#endregion

	// Components
	private AudioSource _AudioSource = null;
	private Animator _Animator = null;
	private Rigidbody _Rigidbody = null;
	private CapsuleCollider _Collider = null;

	#region Interface Methods

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

	#region Unity Methods
	private void OnEnable()
	{
		GameObject fObject = new GameObject($"{name}_formation_pos");
		_FormationPosition = fObject.transform;
	}

	private void Awake()
	{
		_Rigidbody = GetComponent<Rigidbody>();
		_AudioSource = GetComponent<AudioSource>();
		_Animator = GetComponent<Animator>();
		_Collider = GetComponent<CapsuleCollider>();

		_CurrentStatSpecifier = PikminStatSpecifier.OnField;
		PikminStatsManager.Add(_Data._PikminColour, _CurrentMaturity, _CurrentStatSpecifier);

		_HeadModels = new GameObject[(int)PikminMaturity.Flower + 1];
		_HeadModels[0] = Instantiate(_Data._Leaf, _LeafSpawn);
		_HeadModels[1] = Instantiate(_Data._Bud, _LeafSpawn);
		_HeadModels[2] = Instantiate(_Data._Flower, _LeafSpawn);
		SetMaturity(_StartingMaturity);
	}

	private void Start()
	{
		_FormationPosition.SetParent(Globals._Player._PikminController._FormationCenter.transform);
	}

	private void Update()
	{
		if (GameManager._IsPaused)
		{
			return;
		}

		switch (_CurrentState)
		{
			case PikminStates.Idle:
				HandleIdle();
				break;
			case PikminStates.Attacking:
				transform.localPosition = _LatchOffset;
				HandleAttacking();
				break;
			case PikminStates.Dead:
				HandleDeath();
				break;

			case PikminStates.Carrying:
			case PikminStates.RunningTowards:
			case PikminStates.BeingHeld:
			case PikminStates.Thrown:
			case PikminStates.Waiting:
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

		if (_CurrentState == PikminStates.BeingHeld ||
			_CurrentState == PikminStates.Thrown ||
			_CurrentState == PikminStates.Attacking ||
			_CurrentState == PikminStates.Carrying)
		{
			return;
		}

		if (_CurrentState == PikminStates.RunningTowards)
		{
			if (!_InSquad && _TargetObject == null)
			{
				ChangeState(PikminStates.Idle);
			}
			else if (_InSquad)
			{
				MoveTowards(_FormationPosition.position);
			}
			else
			{
				MoveTowardsTarget();
			}

			if (_Intention == PikminIntention.Attack && _TargetObject != null)
			{
				Vector3 directionToObj = ClosestPointOnTarget() - transform.position;
				if (Mathf.Abs(directionToObj.y) >= 0.25f)
				{
					ChangeState(PikminStates.Idle);
				}
				if (Physics.Raycast(transform.position, directionToObj, out RaycastHit hit, 2.5f))
				{
					if (hit.collider != _TargetObjectCollider && hit.collider.CompareTag("Pikmin"))
					{
						// Make the Pikmin move to the right a little to avoid jumping into the other Pikmin
						_MovementVector += transform.right * 2.5f;
					}
				}

				_AttackJumpTimer -= Time.deltaTime;
				try
				{
					Vector3 closestPoint = ClosestPointOnTarget();
					if (_AttackJumpTimer <= 0 &&
						MathUtil.DistanceTo(transform.position, closestPoint) <= _Data._AttackDistToJump &&
						Physics.Raycast(transform.position, directionToObj, out hit, 2.5f) &&
						hit.collider == _TargetObjectCollider)
					{
						_MovementVector = new Vector3(_Rigidbody.velocity.x, _Data._AttackJumpPower, _Rigidbody.velocity.z);
						_AttackJumpTimer = _Data._AttackJumpTimer;
					}
				}
				catch
				{
					Debug.Log("Error");
				}
			}
		}

		Collider[] objects = Physics.OverlapSphere(transform.position, 1);
		foreach (Collider collider in objects)
		{
			if (collider.CompareTag("Pikmin"))
			{
				Vector3 direction = transform.position - collider.transform.position;
				direction.y = 0;

				_MovementVector += _PikminPushScale * Time.deltaTime * direction.normalized;
			}
			else if (collider.CompareTag("Player"))
			{
				Vector3 direction = transform.position - collider.transform.position;
				direction.y = 0;

				_MovementVector += _PlayerPushScale * Time.deltaTime * direction.normalized;

				if (!_InSquad)
				{
					AddToSquad();
				}
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

		_Animator.SetBool("Thrown", _CurrentState == PikminStates.Thrown);

		if (_CurrentState == PikminStates.Idle || _CurrentState == PikminStates.RunningTowards)
		{
			Vector2 horizonalVelocity = new Vector2(_Rigidbody.velocity.x, _Rigidbody.velocity.z);
			_Animator.SetBool("Walking", horizonalVelocity.magnitude >= 1.5f);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (_CurrentState == PikminStates.Thrown)
		{
			if (collision.gameObject.CompareTag("PikminInteract"))
			{
				_TargetObject = collision.transform;
				_TargetObjectCollider = collision.collider;
				_Intention = collision.gameObject.GetComponentInParent<IPikminInteractable>().IntentionType;
				CarryoutIntention();
				return;
			}

			if (!collision.gameObject.CompareTag("Player"))
			{
				ChangeState(PikminStates.Idle);
			}
		}

		if (_CurrentState == PikminStates.RunningTowards && !_InSquad)
		{
			if (collision.gameObject.CompareTag("PikminInteract"))
			{
				_TargetObject = collision.transform;
				_TargetObjectCollider = collision.collider;
				_Intention = collision.gameObject.GetComponentInParent<IPikminInteractable>().IntentionType;
				CarryoutIntention();
			}
		}

		if (!_InSquad && collision.transform.CompareTag("Player"))
		{
			AddToSquad();
		}
	}

	#endregion

	#region States
	private void CarryoutIntention()
	{
		PikminStates previousState = _CurrentState;

		// Run intention-specific logic (attack = OnAttackStart for the target object)
		switch (_Intention)
		{
			case PikminIntention.Attack:
				_AttackingTransform = _TargetObject;

				_Attacking = _TargetObject.GetComponentInParent<IPikminAttack>();
				_Attacking.OnAttackStart(this);

				LatchOnto(_AttackingTransform);

				ChangeState(PikminStates.Attacking);
				break;
			case PikminIntention.Carry:
				_Carrying = _TargetObject.GetComponent<IPikminCarry>();
				_Carrying.OnCarryStart(this);
				break;
			case PikminIntention.PullWeeds:
				break;
			case PikminIntention.Idle:
				ChangeState(PikminStates.Idle);
				break;
			default:
				break;
		}

		if (previousState == _CurrentState && _CurrentState != PikminStates.Idle)
		{
			ChangeState(PikminStates.Idle);
		}

		_Intention = PikminIntention.Idle;
	}

	private void HandleIdle()
	{
		// Look for a target object
		Collider[] objects = Physics.OverlapSphere(transform.position, _Data._SearchRadius, _PikminInteractableMask);
		foreach (Collider collider in objects)
		{
			float yOffset = Mathf.Abs(collider.ClosestPoint(transform.position).y - transform.position.y);
			if (yOffset > 0.25f)
			{
				continue;
			}

			IPikminInteractable interactableComponent = collider.GetComponentInParent<IPikminInteractable>();
			_Intention = interactableComponent.IntentionType;

			if (_Intention == PikminIntention.Carry)
			{
				_Carrying = collider.GetComponent<IPikminCarry>();
				if (!_Carrying.PikminSpotAvailable())
				{
					continue;
				}
			}

			// We can move to the target object, and it is an interactable, so set our target object
			ChangeState(PikminStates.RunningTowards);
			_TargetObject = collider.transform;
			_TargetObjectCollider = collider;
		}
	}

	private void HandleDeath()
	{
		if (_InSquad)
		{
			RemoveFromSquad(PikminStates.Dead);
		}

		if (_RagdollTime > 0)
		{
			_Rigidbody.constraints = RigidbodyConstraints.None;
			_Rigidbody.isKinematic = false;
			_Rigidbody.useGravity = true;
			_RagdollTime -= Time.deltaTime;
			return;
		}

		PikminStatsManager.Remove(_Data._PikminColour, _CurrentMaturity, _CurrentStatSpecifier);

		AudioSource.PlayClipAtPoint(_Data._DeathNoise, transform.position, _Data._AudioVolume);

		// Create the soul gameobject, and play the death noise
		ParticleSystem soul = Instantiate(_DeathParticle, transform.position, Quaternion.Euler(-90, 0, 0)).GetComponent<ParticleSystem>();
		ParticleSystem.MainModule soulEffect = soul.main;
		soulEffect.startColor = _Data._DeathSpiritPikminColour;
		Destroy(soul.gameObject, 5);

		// Remove the object
		Destroy(gameObject);
	}

	private void HandleAttacking()
	{
		// The object we were attacking has died, so we can go back to being idle
		if (_Attacking == null)
		{
			ChangeState(PikminStates.Idle);
			return;
		}

		// Add to the timer and attack if we've gone past the timer
		_AttackTimer += Time.deltaTime;
		if (_AttackTimer >= _Data._AttackDelay)
		{
			_Attacking.OnAttackRecieve(_Data._AttackDamage);
			_AttackTimer = 0;
		}
	}

	#endregion

	#region Misc
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
		transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), _Data._RotationSpeed * Time.deltaTime);

		if (stopEarly && MathUtil.DistanceTo(transform.position, position, false) < 0.5f)
		{
			_CurrentMoveSpeed = Mathf.SmoothStep(_CurrentMoveSpeed, 0, 0.25f);
		}
		else
		{
			// To prevent instant, janky movement we step towards the resultant max speed according to _Acceleration
			_CurrentMoveSpeed = Mathf.SmoothStep(_CurrentMoveSpeed, _Data._MaxMovementSpeed, _Data._AccelerationSpeed * Time.deltaTime);
		}

		Vector3 newVelocity = delta.normalized * _CurrentMoveSpeed;
		newVelocity.y = _Rigidbody.velocity.y;
		_MovementVector = newVelocity;
	}

	private void MoveTowardsTarget()
	{
		MoveTowards(ClosestPointOnTarget(), false);
	}

	private Vector3 ClosestPointOnTarget()
	{
		// Check if there is a collider for the target object we're running to
		if (_TargetObjectCollider != null)
		{
			// Our target is the closest point on the collider
			return _TargetObjectCollider.ClosestPoint(transform.position);
		}

		return _TargetObject.position;
	}

	#endregion

	#region Public Functions

	private void SetMaturity(PikminMaturity m)
	{
		PikminStatsManager.Remove(_Data._PikminColour, _CurrentMaturity, _InSquad ? PikminStatSpecifier.InSquad : PikminStatSpecifier.OnField);
		_CurrentMaturity = m;
		PikminStatsManager.Add(_Data._PikminColour, _CurrentMaturity, _InSquad ? PikminStatSpecifier.InSquad : PikminStatSpecifier.OnField);

		// Get the headtype we want to enable
		int type = (int)_CurrentMaturity;
		// Iterate over all of the heads, and activate the one that matches the type we want
		for (int i = 0; i < (int)PikminMaturity.Flower + 1; i++)
		{
			_HeadModels[i].SetActive(i == type);
		}
	}

	public void ChangeState(PikminStates newState)
	{
		// There's no saving pikmin from death
		if (_CurrentState == PikminStates.Dead)
		{
			return;
		}

		if (_CurrentState == PikminStates.RunningTowards || _CurrentState == PikminStates.Idle && _TargetObject != null)
		{
			_TargetObject = null;
			_TargetObjectCollider = null;
		}
		else if (_CurrentState == PikminStates.Attacking)
		{
			// Reset latching variables
			transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
			LatchOnto(null);

			_MovementVector = !Physics.Raycast(transform.position, Vector3.down, 0.5f) ? Vector3.down * 5 : Vector3.zero;

			_AttackTimer = 0;
			_AttackingTransform = null;

			_Attacking?.OnAttackEnd(this);
		}
		else if (_CurrentState == PikminStates.Carrying)
		{
			LatchOnto(null);
			_Carrying?.OnCarryLeave(this);
		}

		_CurrentState = newState;
	}

	public void StartThrowHold()
	{
		_Rigidbody.isKinematic = true;
		_Rigidbody.useGravity = false;
		_Collider.enabled = false;

		if (_Animator.GetBool("Walking") == true)
		{
			_Animator.SetBool("Walking", false);
		}

		_Animator.SetBool("Holding", true);

		ChangeState(PikminStates.BeingHeld);
	}

	// We've been thrown!
	public void EndThrowHold()
	{
		_Rigidbody.isKinematic = false;
		_Rigidbody.useGravity = true;
		_Collider.enabled = true;

		_Animator.SetBool("Holding", false);

		RemoveFromSquad(PikminStates.Thrown);
	}

	public void StartRunTowards(Transform obj)
	{
		_TargetObject = obj;
		ChangeState(PikminStates.RunningTowards);
	}

	public void LatchOnto(Transform obj)
	{
		transform.parent = obj;
		_LatchOffset = obj == null ? Vector3.zero : transform.localPosition;
		_Rigidbody.isKinematic = obj != null;
		_Collider.isTrigger = obj != null;
		if (obj == null)
		{
			transform.localScale = Vector3.one;
		}
	}

	public void AddToSquad()
	{
		if (!_InSquad && _CurrentState != PikminStates.Dead && _CurrentState != PikminStates.Thrown)
		{
			_InSquad = true;
			ChangeState(PikminStates.RunningTowards);

			PikminStatsManager.AddToSquad(this, _Data._PikminColour, _CurrentMaturity);
			PikminStatsManager.ReassignFormation();
		}
	}

	public void RemoveFromSquad(PikminStates to = PikminStates.Idle)
	{
		if (_InSquad)
		{
			_InSquad = false;
			_TargetObject = null;
			ChangeState(to);

			PikminStatsManager.RemoveFromSquad(this, _Data._PikminColour, _CurrentMaturity);
			PikminStatsManager.ReassignFormation();
		}
	}

	public void Die(float ragdollTimer = 0)
	{
		_RagdollTime = ragdollTimer;
		ChangeState(PikminStates.Dead);
	}

	public void WaterEnter()
	{
		Debug.Log("Entered water");
	}

	public void WaterLeave()
	{
		Debug.Log("Left water");
	}

	#endregion
}
