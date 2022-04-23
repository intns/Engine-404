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

public class PikminAI : MonoBehaviour, IHealth, IEntityInfo
{
	[Header("Components")]
	// Holds everything that makes a Pikmin unique
	public PikminObject _Data = null;

	[Header("Pushing away things")]
	[SerializeField] private float _PlayerPushScale = 30;
	[SerializeField] private float _PikminPushScale = 15;

	[SerializeField] private LayerMask _PikminInteractableMask = 0;
	[SerializeField] private TrailRenderer _ThrowTrailRenderer = null;
	[SerializeField] private LayerMask _PlayerAndPikminLayer = 0;

	[Header("VFX")]
	[SerializeField] private GameObject _DeathParticle = null;
	[HideInInspector] public Transform _FormationPosition = null;

	[Header("Head Types")]
	[SerializeField] private Transform _LeafSpawn = null;
	[SerializeField] private PikminMaturity _StartingMaturity = PikminMaturity.Leaf;
	private GameObject[] _HeadModels = null;
	public PikminMaturity _CurrentMaturity { get; private set; } = PikminMaturity.Leaf;

	#region Debugging Variables

	[Space, Header("Debugging")]
	[SerializeField] private PikminStates _CurrentState = PikminStates.Idle;

	[Space, Header("Idle")]
	[SerializeField] private Transform _TargetObject = null;
	[SerializeField] private Collider _TargetObjectCollider = null;
	[SerializeField] private PikminIntention _Intention = PikminIntention.Idle;

	[Space, Header("Attacking")]
	[SerializeField] private IPikminAttack _Attacking = null;
	[SerializeField] private Transform _AttackingTransform = null;
	[SerializeField] private float _AttackJumpTimer = 0;

	[Space, Header("Carrying")]
	[SerializeField] private IPikminCarry _Carrying = null;

	[Space, Header("Stats")]
	[SerializeField] private PikminStatSpecifier _CurrentStatSpecifier = default;
	[SerializeField] private float _CurrentMoveSpeed = 0;

	[Space, Header("Misc")]
	public bool _InSquad = false;
	[SerializeField] private float _RagdollTime = 0;
	[SerializeField] private Vector3 _MovementVector = Vector3.zero;

	[SerializeField] private Transform _LatchedTransform = null;
	public Vector3 _LatchedOffset = Vector3.zero;
	#endregion

	// Components
	private AudioSource _AudioSource = null;
	private Animator _Animator = null;
	private Rigidbody _Rigidbody = null;
	private CapsuleCollider _Collider = null;
	private Transform _Transform = null;

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
		_Transform = transform;
	}

	private void Awake()
	{
		_Rigidbody = GetComponent<Rigidbody>();
		_AudioSource = GetComponent<AudioSource>();
		_Animator = GetComponent<Animator>();
		_Collider = GetComponent<CapsuleCollider>();

		_ThrowTrailRenderer.enabled = false;

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
		_FormationPosition.SetParent(Player._Instance._PikminController._FormationCenter.transform);
	}

	private void Update()
	{
		if (GameManager._IsPaused)
		{
			return;
		}

		MaintainLatch();

		switch (_CurrentState)
		{
			case PikminStates.Idle:
				HandleIdle();
				break;
			case PikminStates.Attacking:
				HandleAttacking();
				break;
			case PikminStates.Dead:
				HandleDeath();
				break;

			case PikminStates.Carrying:
			case PikminStates.Thrown:
			case PikminStates.RunningTowards:
			case PikminStates.BeingHeld:
			case PikminStates.Waiting:
				break;
			default:
				break;
		}

		Collider[] colls = Physics.OverlapSphere(_Transform.position, 0.5f, _PikminInteractableMask);
		foreach (Collider col in colls)
		{
			OnCollisionHandle(col);
		}
	}

	private void FixedUpdate()
	{
		if (GameManager._IsPaused)
		{
			return;
		}

		MaintainLatch();

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
				Vector3 directionToObj = ClosestPointOnTarget(_TargetObject, _TargetObjectCollider, _Data._SearchRadius) - _Transform.position;
				if (Mathf.Abs(directionToObj.y) > 0.25f)
				{
					ChangeState(PikminStates.Idle);
				}
			}
		}

		Collider[] objects = Physics.OverlapSphere(_Transform.position, 0.5f, _PlayerAndPikminLayer);
		foreach (Collider collider in objects)
		{
			if (collider.CompareTag("Pikmin"))
			{
				Vector3 direction = _Transform.position - collider.transform.position;
				direction.y = 0;

				_MovementVector += _PikminPushScale * Time.deltaTime * direction.normalized;
			}
			else if (collider.CompareTag("Player"))
			{
				Vector3 direction = _Transform.position - collider.transform.position;
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

		MaintainLatch();

		_Animator.SetBool("Thrown", _CurrentState == PikminStates.Thrown);
		_Animator.SetBool("Attacking", _CurrentState == PikminStates.Attacking);

		switch (_CurrentState)
		{
			case PikminStates.Idle:
			case PikminStates.RunningTowards:
				{
					Vector2 horizonalVelocity = new Vector2(_Rigidbody.velocity.x, _Rigidbody.velocity.z);
					_Animator.SetBool("Walking", horizonalVelocity.magnitude >= 1.5f);
					break;
				}

			case PikminStates.Carrying:
				_Animator.SetBool("Walking", true);
				break;
		}
	}

	private void OnCollisionHandle(Collider collision)
	{
		if (_CurrentState == PikminStates.Thrown
			|| (_CurrentState == PikminStates.RunningTowards && !_InSquad))
		{
			if (collision.CompareTag("PikminInteract"))
			{
				_TargetObject = collision.transform;
				_TargetObjectCollider = collision;
				_Intention = collision.gameObject.GetComponentInParent<IPikminInteractable>().IntentionType;
				CarryoutIntention();
				return;
			}
			else if (!collision.CompareTag("Player"))
			{
				ChangeState(PikminStates.Idle);
			}
		}
		else if (!_InSquad && collision.transform.CompareTag("Player"))
		{
			AddToSquad();
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		OnCollisionHandle(collision.collider);
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
				_Carrying = _TargetObject.GetComponentInParent<IPikminCarry>();
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
		Collider[] objects = Physics.OverlapSphere(_Transform.position, _Data._SearchRadius, _PikminInteractableMask);
		foreach (Collider collider in objects)
		{
			// Determine if the collider is on the same level as us
			Vector3 direction = collider.ClosestPoint(_Transform.position) - _Transform.position;
			direction.y = Mathf.Clamp(direction.y, -1.5f, 1.5f);
			if (!Physics.Raycast(_Transform.position, direction, _Data._SearchRadius, _PikminInteractableMask))
			{
				continue;
			}

			IPikminInteractable interactableComponent = collider.GetComponentInParent<IPikminInteractable>();
			_Intention = interactableComponent.IntentionType;

			if (_Intention == PikminIntention.Carry)
			{
				_Carrying = collider.GetComponentInParent<IPikminCarry>();
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
		RemoveFromSquad(PikminStates.Dead);

		if (_RagdollTime > 0)
		{
			_Rigidbody.constraints = RigidbodyConstraints.None;
			_Rigidbody.isKinematic = false;
			_Rigidbody.useGravity = true;
			_RagdollTime -= Time.deltaTime;
			return;
		}

		PikminStatsManager.Remove(_Data._PikminColour, _CurrentMaturity, _CurrentStatSpecifier);
		AudioSource.PlayClipAtPoint(_Data._DeathNoise, _Transform.position, _Data._AudioVolume);

		// Create the soul gameobject, and play the death noise
		if (_DeathParticle != null)
		{
			ParticleSystem soul = Instantiate(_DeathParticle, _Transform.position, Quaternion.Euler(-90, 0, 0)).GetComponent<ParticleSystem>();
			ParticleSystem.MainModule soulEffect = soul.main;
			soulEffect.startColor = _Data._DeathSpiritPikminColour;
			Destroy(soul.gameObject, 5);
		}

		// Remove the object
		Destroy(gameObject);
	}

	private void HandleAttacking()
	{
		// The object we were attacking has died, so we can go back to being idle
		if (_Attacking == null || _AttackingTransform == null)
		{
			ChangeState(PikminStates.Idle);
			return;
		}
	}

	#endregion

	#region Misc
	private void MoveTowards(Vector3 position, bool stopEarly = true)
	{
		// If we're on top of something we should be interacting with, we'll slide off it
		if (Physics.Raycast(_Transform.position, Vector3.down, out RaycastHit hit, 1.5f, _PikminInteractableMask))
		{
			Vector3 direction = _Transform.position - hit.transform.position;
			direction.y = 0;

			_MovementVector += 75 * Time.deltaTime * direction;
			return;
		}

		// Rotate to look at the object we're moving towards
		Vector3 delta = (position - _Transform.position).normalized;
		delta.y = 0;
		_Transform.rotation = Quaternion.Slerp(_Transform.rotation, Quaternion.LookRotation(delta), _Data._RotationSpeed * Time.deltaTime);

		if (stopEarly && MathUtil.DistanceTo(_Transform.position, position, false) < 0.5f)
		{
			_CurrentMoveSpeed = Mathf.SmoothStep(_CurrentMoveSpeed, 0, 0.25f);
		}
		else
		{
			// To prevent instant, janky movement we step towards the resultant max speed according to _Acceleration
			_CurrentMoveSpeed = Mathf.SmoothStep(_CurrentMoveSpeed, _Data._MaxMovementSpeed, _Data._AccelerationSpeed * Time.fixedDeltaTime);
		}

		Vector3 newVelocity = delta.normalized * _CurrentMoveSpeed;
		newVelocity.y = _Rigidbody.velocity.y;
		_MovementVector = newVelocity;
	}

	private void MoveTowardsTarget()
	{
		MoveTowards(ClosestPointOnTarget(_TargetObject, _TargetObjectCollider, _Data._SearchRadius), false);
	}

	private Vector3 ClosestPointOnTarget(Transform target, Collider collider = null, float maxDistance = float.PositiveInfinity)
	{
		// Check if there is a collider for the target object we're running to
		if (collider != null)
		{
			Vector3 direction = collider.ClosestPoint(_Transform.position) - _Transform.position;
			direction.y = 0;
			if (Physics.Raycast(_Transform.position, direction, out RaycastHit hit, maxDistance))
			{
				return hit.point;
			}
			else
			{
				return collider.ClosestPoint(_Transform.position);
			}
		}

		return target.position;
	}

	private void MaintainLatch()
	{
		if (_LatchedTransform != null)
		{
			Vector3 finalPos = ClosestPointOnTarget(_LatchedTransform, _LatchedTransform.GetComponent<Collider>()) + _Transform.forward;
			_Transform.SetPositionAndRotation(_LatchedTransform.position + _LatchedOffset,
				Quaternion.LookRotation(finalPos - _Transform.position));
		}
	}

	#endregion

	#region Public Functions

	public void SetMaturity(PikminMaturity m)
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

		switch (_CurrentState)
		{
			case PikminStates.RunningTowards:
			case PikminStates.Idle when _TargetObject != null:
				_TargetObject = null;
				_TargetObjectCollider = null;
				break;
			case PikminStates.Attacking:
				// Reset latching variables
				_Transform.rotation = Quaternion.Euler(0, _Transform.rotation.eulerAngles.y, 0);
				LatchOnto(null);

				_MovementVector = !Physics.Raycast(_Transform.position, Vector3.down, 0.5f) ? Vector3.down * 5 : Vector3.zero;
				_Animator.SetBool("Attacking", false);

				_AttackingTransform = null;

				_Attacking?.OnAttackEnd(this);
				break;
			case PikminStates.Carrying:
				LatchOnto(null);
				_Carrying?.OnCarryLeave(this);
				break;
			case PikminStates.Thrown:
				_ThrowTrailRenderer.enabled = false;
				break;
		}

		_CurrentState = newState;

		if (newState == PikminStates.Thrown)
		{
			_ThrowTrailRenderer.enabled = true;
		}
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

	public void LatchOnto(Transform obj)
	{
		_LatchedTransform = obj;
		_TargetObject = obj;

		if (obj != null)
		{
			_Rigidbody.isKinematic = true;
			_Collider.isTrigger = true;

			_TargetObjectCollider = obj.GetComponent<Collider>();
			_LatchedOffset = _Transform.position - _LatchedTransform.position;
			Vector3 finalPos = obj.position + _LatchedOffset;
			_Transform.rotation = Quaternion.LookRotation(finalPos - _Transform.position);
		}
		else
		{
			_Rigidbody.isKinematic = false;
			_Collider.isTrigger = false;
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
		if (_Data._PikminColour != PikminColour.Blue)
		{
			Die(0.5f);
		}
	}

	public void WaterLeave()
	{
		Debug.Log("Left water");
	}

	// CALLED BY ANIMATION - PIKMIN HIT
	public void HandleAttackingAnimationHit()
	{
		if (_Attacking != null)
		{
			_Attacking.OnAttackRecieve(_Data._AttackDamage);
		}
	}

	#endregion

	#region Entity Implementations
	public EntityInfo GetEntityInfo()
	{
		return EntityInfo.Piki;
	}
	#endregion
}
