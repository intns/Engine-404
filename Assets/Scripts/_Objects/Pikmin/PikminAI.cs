/*
 * PikminAI.cs
 * Created by: Ambrosia, Helodity
 * Created on: 30/4/2020 (dd/mm/yy)
 */

using System;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public enum PikminStates
{
	Idle,
	RunTowards,

	StandingAttack,
	Attack,

	Carry,
	Push,
	SuckNectar,

	// Holding/Throwing States
	BeingHeld,
	Thrown,

	Dead,
	OnFire,
	Squish, // When a wolly-hop slams onto it
	Waiting,
}

// Immediate states after running towards another object/position
public enum PikminIntention
{
	Attack,
	Carry,
	Push,
	Idle, // AKA None
}

public class PikminAI : MonoBehaviour, IHealth, IComparable, IInteraction
{
	[Header("Components")]
	// Holds everything that makes a Pikmin unique
	public PikminObject _Data;
	[SerializeField] Transform _HeadBoneTransform;
	[SerializeField] VisualEffect _FireVFX;
	[SerializeField] VisualEffectAsset _AttackVFX;

	[Header("Behaviour")]
	[SerializeField] float _LatchNormalOffset = 0.5f;
	[Space]
	[SerializeField] float _StoppingDistance = 0.5f;

	[Header("Idle")]
	[SerializeField] float _IdleTickRate = 0.3f;

	[Header("Object Avoidance")]
	[SerializeField] float _AvoidSphereSize = 0.5f;
	[SerializeField] float _PlayerPushScale = 30.0f;
	[SerializeField] float _PikminPushScale = 15.0f;

	[SerializeField] LayerMask _InteractableMask = 0;
	[SerializeField] LayerMask _RunTowardsMask = 0;
	[SerializeField] TrailRenderer _ThrowTrailRenderer;
	[SerializeField] LayerMask _PlayerAndPikminLayer = 0;

	[Header("VFX")]
	[SerializeField] GameObject _DeathParticle;
	[HideInInspector] public Transform _FormationPosition;

	[Header("Head Types")]
	[SerializeField] Transform _LeafSpawn;
	[SerializeField] PikminMaturity _StartingMaturity = PikminMaturity.Leaf;
	Animator _Animator;

	// Components
	AudioSource _AudioSource;
	CapsuleCollider _Collider;
	GameObject[] _HeadModels;
	Transform _PlayerTransform;
	Rigidbody _Rigidbody;
	Transform _Transform;
	public PikminMaturity _CurrentMaturity { get; set; } = PikminMaturity.Size;

	#region Debugging Variables

	[Header("==Debugging==")]
	public PikminStates _CurrentState = PikminStates.Idle;

	[Space]
	[Header("Idle")]
	[SerializeField] Transform _TargetObject;
	[SerializeField] Collider _TargetObjectCollider;
	[SerializeField] PikminIntention _Intention = PikminIntention.Idle;
	[SerializeField] float _IdleTimer;

	// Carrying
	IPikminCarry _Carrying;

	[Space]
	[Header("Attacking")]
	[SerializeField] Transform _AttackingTransform;
	IPikminAttack _Attacking;

	[Space]
	[Header("Pushing")]
	IPikminPush _Pushing;
	[SerializeField] bool _PushReady;

	[Space]
	[Header("On Fire")]
	[SerializeField] float _FireTimer;
	[SerializeField] float _WanderAngle;

	[Space]
	[Header("Suck Nectar")]
	[SerializeField] float _SuckNectarTimer;
	[SerializeField] Transform _NectarTransform;

	[Space]
	[Header("Pressed")]
	[SerializeField] float _PressedTimer;

	[Space]
	[Header("Stats")]
	[SerializeField] PikminStatSpecifier _CurrentStatSpecifier;
	[SerializeField] float _CurrentMoveSpeed;

	[Space]
	[Header("Misc")]
	public Vector3 _AddedVelocity = Vector3.zero;
	[SerializeField] Vector3 _DirectionVector = Vector3.zero;
	[SerializeField] Vector3 _EulerAngles = Vector3.zero;
	[Space]
	[SerializeField] float _RagdollTime;
	[SerializeField] float _FaceDirectionAngle;
	[Space]
	[SerializeField] LayerMask _MapMask;
	[SerializeField] LayerMask _AllMask;
	[SerializeField] float _MinimumY = -500;
	[Space]
	[SerializeField] Transform _LatchedTransform;
	public Vector3 _LatchedOffset = Vector3.zero;
	[SerializeField] float _HeldAudioTimer;
	[SerializeField] float _ColliderTimer;
	[SerializeField] float _ColliderOriginRadius;
	[SerializeField] float _ColliderOriginHeight;

	VisualEffect _AttackVFXInstance;

	static class Animations
	{
		public static readonly int Walking = Animator.StringToHash("Walking");
		public static readonly int Attacking = Animator.StringToHash("Attacking");
		public static readonly int Thrown = Animator.StringToHash("Thrown");
		public static readonly int Holding = Animator.StringToHash("Holding");
	}

	public bool _InSquad { get; private set; }

	#endregion

	#region Interface Methods

	float IHealth.GetCurrentHealth()
	{
		return 1;
	}

	float IHealth.GetMaxHealth()
	{
		return 1;
	}

	void IHealth.SetHealth(float h)
	{
		if (h <= 0)
		{
			Die();
		}
	}

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

	void OnEnable()
	{
		_FormationPosition = new GameObject($"{name}_{GetInstanceID()}_formation_pos").transform;

		_AttackVFXInstance = new GameObject().AddComponent<VisualEffect>();
		_AttackVFXInstance.transform.localScale = Vector3.one * 3.0f;
		_AttackVFXInstance.visualEffectAsset = _AttackVFX;

		GameManager.OnPauseEvent += OnPauseEvent;
	}

	void OnDisable()
	{
		GameManager.OnPauseEvent -= OnPauseEvent;
	}

	void Awake()
	{
		_Rigidbody = GetComponent<Rigidbody>();
		_Animator = GetComponent<Animator>();

		_Collider = GetComponent<CapsuleCollider>();
		_ColliderOriginRadius = _Collider.radius;
		_ColliderOriginHeight = _Collider.height;

		_AudioSource = GetComponent<AudioSource>();
		_AudioSource.volume = _Data._AudioVolume;

		_Transform = transform;
		_EulerAngles = _Transform.eulerAngles;
		_FaceDirectionAngle = _EulerAngles.y;

		_ThrowTrailRenderer.enabled = false;
		_FireVFX.Stop();

		_IdleTimer = Random.Range(0.02f, _IdleTickRate - _IdleTickRate / 10);

		_CurrentStatSpecifier = PikminStatSpecifier.OnField;
		PikminStatsManager.Add(_Data._PikminColour, _CurrentMaturity, _CurrentStatSpecifier);

		_HeadModels = new GameObject[(int)PikminMaturity.Size];

		for (int i = 0; i < _HeadModels.Length; i++)
		{
			_HeadModels[i] = i switch
			{
				0 => Instantiate(_Data._Leaf),
				1 => Instantiate(_Data._Bud),
				2 => Instantiate(_Data._Flower),
				_ => _HeadModels[i],
			};

			_HeadModels[i].transform.parent = _LeafSpawn;
			_HeadModels[i].transform.localPosition = Vector3.zero;
		}

		SetMaturity(_StartingMaturity);
	}

	void OnPauseEvent(PauseType t)
	{
		if (t == PauseType.Paused)
		{
			if (_Animator == null || _Rigidbody == null)
			{
				return;
			}

			_Animator.SetBool(Animations.Walking, false);
			_Rigidbody.isKinematic = true;
		}
		else
		{
			if (_Rigidbody != null)
			{
				_Rigidbody.isKinematic = false;
			}
		}
	}

	void Start()
	{
		_FormationPosition.SetParent(Player._Instance._PikminController._FormationCenter.transform);
		_PlayerTransform = Player._Instance.transform;
	}

	void Update()
	{
		if (GameManager.IsPaused && GameManager.PauseType != PauseType.OnlyPikminActive)
		{
			return;
		}

		_EulerAngles = _Transform.eulerAngles;

		if (_Transform.position.y < _MinimumY)
		{
			_Rigidbody.velocity = Vector3.zero;
			_Transform.position = _PlayerTransform.position + Vector3.up * 5.0f;
		}

		MaintainLatch();

		switch (_CurrentState)
		{
			case PikminStates.Idle:
				HandleIdle();
				break;
			case PikminStates.Attack:
				HandleAttack();
				break;
			case PikminStates.Push:
				HandlePush();
				break;
			case PikminStates.SuckNectar:
				HandleSuckNectar();
				break;
			case PikminStates.Squish:
				HandleSquish();
				break;
			case PikminStates.Dead:
				HandleDeath();
				break;
			case PikminStates.BeingHeld:
				HandleBeingHeld();
				break;
			case PikminStates.Thrown:
				HandleThrown();
				break;

			case PikminStates.Carry:
			case PikminStates.RunTowards:
			case PikminStates.Waiting:
			default:
				break;
		}

		HandleAnimations();
	}

	void FixedUpdate()
	{
		if (GameManager.IsPaused && GameManager.PauseType != PauseType.OnlyPikminActive)
		{
			return;
		}

		MaintainLatch();

		switch (_CurrentState)
		{
			case PikminStates.BeingHeld:
			case PikminStates.Thrown:
			case PikminStates.Attack:
			case PikminStates.Squish:
			case PikminStates.Dead:
			case PikminStates.Carry:
				return;
			case PikminStates.OnFire:
				HandleOnFire();
				break;
			case PikminStates.RunTowards:
				HandleRunningTowards();
				break;
		}

		if (_CurrentState == PikminStates.RunTowards && _Intention != PikminIntention.Attack
		    || _CurrentState == PikminStates.Idle)
		{
			RepelPikminAndPlayers();
		}

		if (!Latch_IsLatchedOntoObject() || _CurrentState == PikminStates.Push)
		{
			float newYRotation
				= Mathf.LerpAngle(_EulerAngles.y, _FaceDirectionAngle, _Data._RotationSpeed * Time.fixedDeltaTime);
			_Transform.rotation = Quaternion.Euler(0f, newYRotation, 0f);
		}

		float storedY = _Rigidbody.velocity.y;
		_Rigidbody.velocity = _DirectionVector + _AddedVelocity;
		_DirectionVector = Vector3.up * storedY;

		_AddedVelocity = Vector3.Lerp(_AddedVelocity, Vector3.zero, 10 * Time.fixedDeltaTime);
	}

	void OnCollisionEnter(Collision collision)
	{
		OnCollisionHandle(collision);
	}

	void OnCollisionStay(Collision collision)
	{
		OnCollisionHandle(collision);
	}

	void OnCollisionExit(Collision collision)
	{
		OnCollisionHandle(collision);
	}

	#endregion

	#region States

	void HandleAnimations()
	{
		_Animator.SetBool(Animations.Thrown, _CurrentState == PikminStates.Thrown);
		_Animator.SetBool(Animations.Attacking, _CurrentState == PikminStates.Attack);

		switch (_CurrentState)
		{
			case PikminStates.Idle:
			case PikminStates.RunTowards:
			{
				Vector2 horizonalVelocity = new(_Rigidbody.velocity.x, _Rigidbody.velocity.z);
				_Animator.SetBool(Animations.Walking, horizonalVelocity.magnitude >= 0.1f);
				break;
			}

			case PikminStates.OnFire:
			case PikminStates.Push:
			case PikminStates.Carry:
				_Animator.SetBool(Animations.Walking, true);
				break;
		}
	}

	void CarryoutIntention()
	{
		PikminStates previousState = _CurrentState;

		// Run intention-specific logic (attack = OnAttackStart for the target object)
		switch (_Intention)
		{
			case PikminIntention.Attack:
				_Attacking = _TargetObject.GetComponentInParent<IPikminAttack>();

				if (!_Attacking.IsAttackAvailable())
				{
					_Intention = PikminIntention.Idle;
					ChangeState(PikminStates.Idle);
					return;
				}

				_Attacking.OnAttackStart(this);
				_AttackingTransform = _TargetObject;

				LatchOnto(_AttackingTransform);
				ChangeState(PikminStates.Attack);
				break;
			case PikminIntention.Carry:
				_Carrying = _TargetObject.GetComponentInParent<IPikminCarry>();
				_Carrying.OnCarryStart(this);
				break;
			case PikminIntention.Push:
				if (!IsGrounded())
				{
					return;
				}

				_Pushing = _TargetObject.GetComponentInParent<IPikminPush>();

				if (!_Pushing.OnPikminAdded(this))
				{
					_Intention = PikminIntention.Idle;
					ChangeState(PikminStates.Idle);
					return;
				}

				break;
			case PikminIntention.Idle:
				ChangeState(PikminStates.Idle);
				break;
		}

		if (previousState == _CurrentState && _CurrentState != PikminStates.Idle)
		{
			ChangeState(PikminStates.Idle);
		}

		_Intention = PikminIntention.Idle;
	}

	void HandleIdle()
	{
		// Don't do anything until we hit the floor!
		if (!IsGrounded())
		{
			return;
		}

		// Checks if we're in squad anyways, so it's a safe check to make sure
		// we're not actually in squad (known bug)
		RemoveFromSquad();

		_IdleTimer += Time.deltaTime;

		if (_IdleTimer < _IdleTickRate)
		{
			return;
		}

		_IdleTimer = 0;

		// Scan for the closest target object and then run towards it
		Collider closestCol = null;
		float curClosestDist = float.PositiveInfinity;

		Collider[] objects
			= Physics.OverlapSphere(_Transform.position, _Data._SearchRadius, _InteractableMask | _RunTowardsMask);

		foreach (Collider col in objects)
		{
			IPikminInteractable interactableComponent = col.GetComponentInParent<IPikminInteractable>();

			if (interactableComponent == null)
			{
				continue;
			}

			PikminIntention currentIntention = interactableComponent.IntentionType;

			// If we've found an attackable object, but there is no attack avaiable
			// (Usually because of death or inactivation), ignore it
			if (currentIntention == PikminIntention.Attack &&
			    !col.GetComponentInParent<IPikminAttack>().IsAttackAvailable())
			{
				continue;
			}

			// Determine if we can see the item
			Vector3 direction = MathUtil.DirectionFromTo(_Transform.position, col.ClosestPoint(_Transform.position));

			if (!Physics.Raycast(
				    _Transform.position,
				    direction,
				    out RaycastHit hit,
				    _Data._SearchRadius,
				    _AllMask,
				    QueryTriggerInteraction.Ignore
			    )
			    || hit.collider != col)
			{
				// Fall back to global origin
				direction = MathUtil.DirectionFromTo(_Transform.position, col.transform.position);

				if (!Physics.Raycast(
					    _Transform.position,
					    direction,
					    out hit,
					    _Data._SearchRadius,
					    _AllMask,
					    QueryTriggerInteraction.Ignore
				    )
				    || hit.collider != col)
				{
					// Check if the object is too high up
					if (Mathf.Abs(_Transform.position.y - col.transform.position.y) > 1.5f)
					{
						continue;
					}
				}
			}

			switch (currentIntention)
			{
				case PikminIntention.Push:
				{
					IPikminPush toPush = col.GetComponentInParent<IPikminPush>();

					if (!toPush.IsPikminSpotAvailable())
					{
						continue;
					}

					_Pushing = toPush;
					break;
				}

				case PikminIntention.Carry:
				{
					IPikminCarry toCarry = col.GetComponentInParent<IPikminCarry>();

					if (!toCarry.IsPikminSpotAvailable())
					{
						continue;
					}

					_Carrying = toCarry;
					break;
				}
			}

			float distance = MathUtil.DistanceTo(_Transform.position, col.transform.position);

			if (distance < curClosestDist)
			{
				closestCol = col;
				curClosestDist = distance;
				_Intention = currentIntention;
			}
		}

		if (closestCol == null)
		{
			return;
		}

		// We can move to the target object, and it is an interactable, so set our target object
		ChangeState(PikminStates.RunTowards);
		_TargetObject = closestCol.transform;
		_TargetObjectCollider = closestCol;
	}

	void HandleRunningTowards()
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

		// If whatever we're running after doesn't have any spots for us to go to,
		// then we'll just go back to idle
		switch (_Intention)
		{
			case PikminIntention.Carry when _Carrying != null:
				if (!_Carrying.IsPikminSpotAvailable())
				{
					_Intention = PikminIntention.Idle;
					ChangeState(PikminStates.Idle);
				}

				break;
			case PikminIntention.Push when _Pushing != null:
				if (!_Pushing.IsPikminSpotAvailable())
				{
					_Intention = PikminIntention.Idle;
					ChangeState(PikminStates.Idle);
				}

				break;
		}
	}

	void HandleAttack()
	{
		// The object we were attacking has died, so we can go back to being idle
		if (_Attacking != null && _AttackingTransform != null)
		{
			return;
		}

		ChangeState(PikminStates.Idle);
	}

	void HandlePush()
	{
		if (_Pushing == null)
		{
			ChangeState(PikminStates.Idle);
			return;
		}

		Vector3 destination = _Pushing.GetPushPosition(this);

		// GetPushPosition may change our state, so we'll exit early if that's the case
		if (_CurrentState != PikminStates.Push)
		{
			return;
		}

		if (MathUtil.DistanceTo(_Transform.position, destination, false) <= 0.02f)
		{
			MoveTowards(destination, true, false);
			_FaceDirectionAngle = Quaternion.LookRotation(_Pushing.GetMovementDirection()).eulerAngles.y;

			if (_PushReady)
			{
				return;
			}

			_Pushing.OnPikminReady(this);
			_PushReady = true;
		}
		else
		{
			MoveTowards(destination, false);
		}
	}

	void HandleSuckNectar()
	{
		if (_CurrentMaturity == PikminMaturity.Flower)
		{
			ChangeState(PikminStates.Idle);
			return;
		}

		_SuckNectarTimer += Time.deltaTime;

		if (_SuckNectarTimer >= Nectar.NECTAR_DRINK_TIME)
		{
			SetMaturity(_CurrentMaturity + 1);
			ChangeState(PikminStates.Idle);
			return;
		}

		if (_NectarTransform != null)
		{
			_FaceDirectionAngle = Quaternion
			                      .LookRotation(MathUtil.DirectionFromTo(_Transform.position, _NectarTransform.position))
			                      .eulerAngles.y;
			_DirectionVector = Vector3.zero;
			_AddedVelocity = Vector3.zero;
		}
	}

	void HandleSquish()
	{
		RemoveFromSquad(PikminStates.Squish);

		if (_PressedTimer > 0.0f)
		{
			_PressedTimer -= Time.deltaTime;
		}
		else
		{
			Die();
		}
	}

	void HandleDeath()
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

		if (_Data._DeathNoise != null)
		{
			AudioSource.PlayClipAtPoint(_Data._DeathNoise, _Transform.position, _Data._AudioVolume);
		}

		if (_DeathParticle != null)
		{
			ParticleSystem soul = Instantiate(_DeathParticle, _Transform.position + Vector3.up * 1.25f, Quaternion.Euler(-90, 0, 0)).GetComponent<ParticleSystem>();
			ParticleSystem.MainModule soulEffect = soul.main;
			soulEffect.startColor = _Data._DeathSpiritPikminColour;
			Destroy(soul.gameObject, 5f);
		}

		int totalPikmin = PikminStatsManager.GetTotalPikminInAllOnions() + PikminStatsManager.GetTotalPikminOnField();

		if (totalPikmin == 0)
		{
			Player._Instance.PikminExtinction();
		}

		Destroy(gameObject);
	}

	void HandleBeingHeld()
	{
		if (_HeldAudioTimer >= 0.5f)
		{
			_AudioSource.pitch = Random.Range(0.75f, 1.25f);
			PlaySound(_Data._HeldNoise, Random.Range(0, 0.15f));
			_HeldAudioTimer = 0;
		}

		_HeldAudioTimer += Time.deltaTime;
	}

	void HandleThrown()
	{
		const float GROWTH_TIMER = 0.2f;

		if (_ColliderTimer >= GROWTH_TIMER)
		{
			_Collider.radius = _ColliderOriginRadius;
			_Collider.height = _ColliderOriginHeight;
			return;
		}

		_ColliderTimer += Time.deltaTime;
		_Collider.radius = Mathf.Lerp(0.001f, _ColliderOriginRadius, _ColliderTimer / GROWTH_TIMER);
		_Collider.height = Mathf.Lerp(0.001f, _ColliderOriginHeight, _ColliderTimer / GROWTH_TIMER);
	}

	void HandleOnFire()
	{
		_FireTimer += Time.fixedDeltaTime;

		if (_FireTimer >= _Data._FireDeathTimer)
		{
			Die(0.5f);
			return;
		}

		_FaceDirectionAngle = Mathf.LerpAngle(_FaceDirectionAngle, _WanderAngle, 3.0f * Time.fixedDeltaTime);

		if (Mathf.Abs(Mathf.DeltaAngle(_FaceDirectionAngle, _WanderAngle)) < 1.0f)
		{
			_WanderAngle = Random.Range(-360.0f, 360.0f);
		}

		MoveTowards(_Transform.position + _Transform.forward, false, false);
	}

	#endregion

	#region Misc

	void MoveTowards(Vector3 position, bool stopEarly = true, bool rotateTowards = true)
	{
		// Rotate to look at the object we're moving towards
		Vector3 delta = MathUtil.DirectionFromTo(_Transform.position, position);

		if (delta != Vector3.zero && rotateTowards)
		{
			// Look at the player
			_FaceDirectionAngle = _InSquad
				? Quaternion.LookRotation(MathUtil.DirectionFromTo(_Transform.position, _PlayerTransform.position)).eulerAngles
				            .y
				: Quaternion.LookRotation(delta).eulerAngles.y;
		}

		if (stopEarly && MathUtil.DistanceTo(_Transform.position, position, false) < _StoppingDistance)
		{
			_CurrentMoveSpeed = Mathf.Lerp(_CurrentMoveSpeed, 0, 15 * Time.deltaTime);
		}
		else
		{
			// To prevent instant, janky movement we step towards the resultant max speed according to _Acceleration
			_CurrentMoveSpeed = Mathf.Lerp(
				_CurrentMoveSpeed,
				_Data.GetMaxSpeed(_CurrentMaturity),
				_Data.GetAcceleration(_CurrentMaturity) * Time.deltaTime
			);
		}

		Vector3 newVelocity = delta * _CurrentMoveSpeed;
		newVelocity.y = _Rigidbody.velocity.y;
		_DirectionVector = newVelocity;
	}

	void MoveTowardsTarget()
	{
		Vector3 destination;

		switch (_Intention)
		{
			case PikminIntention.Push when _Pushing != null:
				destination = _Pushing.GetPushPosition(this);

				if (_CurrentState == PikminStates.Idle)
				{
					return;
				}

				break;
			default:
				destination = ClosestPointOnTarget(_TargetObject, _TargetObjectCollider, _Data._SearchRadius);
				break;
		}

		MoveTowards(destination, false);
	}

	void RepelPikminAndPlayers()
	{
		Collider[] objects = Physics.OverlapSphere(_Transform.position, _AvoidSphereSize, _PlayerAndPikminLayer);

		foreach (Collider col in objects)
		{
			if (col.CompareTag("Pikmin"))
			{
				Vector3 direction = MathUtil.DirectionFromTo(col.transform.position, _Transform.position);

				if (direction.sqrMagnitude <= 0.01f)
				{
					direction.x += Random.Range(-0.025f, 0.025f);
					direction.z += Random.Range(-0.025f, 0.025f);
				}

				_AddedVelocity += _PikminPushScale * Time.fixedDeltaTime * direction;
			}
			else if (col.CompareTag("Player"))
			{
				Vector3 direction = MathUtil.DirectionFromTo(col.transform.position, _Transform.position);
				_AddedVelocity += _PlayerPushScale * Time.fixedDeltaTime * direction;

				AddToSquad();
			}
		}
	}

	void OnCollisionHandle(Collision collision)
	{
		if (_InSquad)
		{
			return;
		}

		Collider col = collision.collider;

		switch (_CurrentState)
		{
			// Just landed from a throw, check if we're on something we interact with
			case PikminStates.Thrown when col.CompareTag("PikminInteract"):
			{
				// Handle squishing
				IPikminSquish squish = col.GetComponentInParent<IPikminSquish>();

				if (squish != null)
				{
					Vector3 closestPoint = col.ClosestPoint(_Transform.position);

					float spot = Vector3.Dot(Vector3.up, MathUtil.DirectionFromTo(_Transform.position, closestPoint, true));

					if (spot <= -0.65f)
					{
						squish.OnSquish(this);
					}

					ChangeState(PikminStates.Idle);
				}
				else
				{
					_TargetObject = collision.transform;
					_TargetObjectCollider = col;
					_Intention = col.GetComponentInParent<IPikminInteractable>().IntentionType;
					CarryoutIntention();
				}

				break;
			}
			case PikminStates.Thrown:
			{
				if (!col.CompareTag("Pikmin") && !col.CompareTag("Player"))
				{
					ChangeState(PikminStates.Idle);
				}

				break;
			}
			// If we've been running towards something, we've touched it and now we
			// can carryout our intention
			case PikminStates.RunTowards when _TargetObjectCollider == null || _TargetObjectCollider != col
			                                                                || _TargetObjectCollider.gameObject.layer == _RunTowardsMask:
				return;
			case PikminStates.RunTowards:
				_Intention = _TargetObjectCollider.GetComponentInParent<IPikminInteractable>().IntentionType;
				CarryoutIntention();
				break;
			default:
			{
				if (col.CompareTag("Player")
				    && _CurrentState is PikminStates.Idle or PikminStates.RunTowards)
				{
					AddToSquad();
				}

				break;
			}
		}
	}

	Vector3 ClosestPointOnTarget(Transform target, Collider col = null, float maxDistance = float.PositiveInfinity)
	{
		// Check if there is a collider for the target object we're running to
		if (col == null)
		{
			return target.position;
		}

		Vector3 closestPoint = col.ClosestPoint(_Transform.position);
		Vector3 direction = MathUtil.DirectionFromTo(_Transform.position, closestPoint);

		// If we can hit the target and it's in our straight-on eye line
		if (Physics.Raycast(
			    _Transform.position,
			    direction,
			    out RaycastHit hit,
			    maxDistance,
			    _AllMask,
			    QueryTriggerInteraction.Ignore
		    )
		    && hit.collider == col)
		{
			return hit.point;
		}

		float yOffset = Mathf.Abs(closestPoint.y - _Transform.position.y);

		if (yOffset < 3.5f)
		{
			return closestPoint;
		}

		return target.position;
	}

	/*		if (_TargetObjectCollider != null)
		{
			Vector3 thisPos = _Transform.position;
			Vector3 nextPos = _TargetObjectCollider.ClosestPoint(pos);
			Vector3 direct = MathUtil.DirectionFromTo(thisPos, nextPos, !useY);

			if (Physics.Raycast(thisPos, direct, out RaycastHit info, 1.5f + _LatchNormalOffset)
				&& info.transform == _LatchedTransform)
			{
				Vector3 resultPos = nextPos + (info.normal * _LatchNormalOffset);
				if (Physics.OverlapSphere(resultPos, 0.5f, _MapMask).Length == 0)
				{
					pos = Vector3.Lerp(thisPos, resultPos, 35 * Time.deltaTime);
				}
			}
		}
		else
		{
			_TargetObjectCollider = _LatchedTransform.GetComponent<Collider>();
		}*/
	void MaintainLatch()
	{
		if (!Latch_IsLatchedOntoObject())
		{
			return;
		}

		Vector3 pos = _LatchedTransform.position + _LatchedOffset;
		bool useY = _CurrentState == PikminStates.Carry;

		Vector3 directionFromPosToObj = MathUtil.DirectionFromTo(pos, _LatchedTransform.position, !useY);
		_Transform.SetPositionAndRotation(pos, Quaternion.LookRotation(directionFromPosToObj));
	}

	#endregion

	#region Public Function

	public PikminColour GetColour()
	{
		return _Data._PikminColour;
	}

	public int CompareTo(object obj)
	{
		PikminAI other = obj as PikminAI;

		// Sort by selected Pikmin colour first.
		PikminColour selectedColour = Player._Instance._PikminController._SelectedThrowPikmin;
		Debug.Assert(other != null, nameof(other) + " != null");

		if (GetColour() == selectedColour && other.GetColour() != selectedColour)
		{
			return 1;
		}

		if (GetColour() != selectedColour && other.GetColour() == selectedColour)
		{
			return -1;
		}

		// Sort by Pikmin colour if they are not the selected colour.
		return GetColour().CompareTo(other.GetColour());
	}


	public void SetMaturity(PikminMaturity newMaturity)
	{
		PikminStatsManager.Remove(_Data._PikminColour, _CurrentMaturity, _CurrentStatSpecifier);
		_CurrentMaturity = newMaturity;
		PikminStatsManager.Add(_Data._PikminColour, _CurrentMaturity, _CurrentStatSpecifier);

		// Activate appropriate head model based on new maturity.
		for (int i = 0; i < _HeadModels.Length; i++)
		{
			_HeadModels[i].SetActive(i == (int)_CurrentMaturity);
		}
	}

	public void ChangeState(PikminStates newState)
	{
		// There's no saving pikmin from death
		if (_CurrentState == PikminStates.Dead)
		{
			return;
		}

		// cleaning up from OLD
		switch (_CurrentState)
		{
			case PikminStates.RunTowards:
				_Animator.SetBool(Animations.Walking, false);
				_TargetObject = null;
				_TargetObjectCollider = null;
				break;
			case PikminStates.Idle when _TargetObject != null:
				_TargetObject = null;
				_TargetObjectCollider = null;
				break;
			case PikminStates.SuckNectar:
				_SuckNectarTimer = 0.0f;
				_NectarTransform = null;
				break;
			case PikminStates.OnFire:
				_FireTimer = 0.0f;
				_WanderAngle = Random.Range(0.0f, 360.0f);
				_FireVFX.Stop();
				break;
			case PikminStates.Attack:
				LatchOnto(null);

				_DirectionVector = Vector3.down * 2;
				_Animator.SetBool(Animations.Attacking, false);

				_Attacking?.OnAttackEnd(this);

				_AttackingTransform = null;
				_Attacking = null;

				_FaceDirectionAngle = _EulerAngles.y;
				_Transform.eulerAngles = new(0, _EulerAngles.y, 0);
				break;
			case PikminStates.Carry:
				LatchOnto(null);
				_Carrying?.OnCarryLeave(this);
				_Carrying = null;

				_FaceDirectionAngle = _EulerAngles.y;
				break;
			case PikminStates.Push:
				_Pushing?.OnPikminLeave(this);
				_Pushing = null;
				_PushReady = false;
				break;
			case PikminStates.Thrown:
				_ThrowTrailRenderer.enabled = false;
				_ColliderTimer = 0.0f;
				_Collider.radius = _ColliderOriginRadius;
				_Collider.height = _ColliderOriginHeight;
				break;
			case PikminStates.BeingHeld:
				_AudioSource.pitch = 1.0f;
				_TargetObject = null;
				_Animator.SetBool(Animations.Holding, false);
				break;
		}

		_CurrentState = newState;

		// initialising NEW
		switch (newState)
		{
			case PikminStates.Thrown:
				_ThrowTrailRenderer.enabled = true;
				break;
			case PikminStates.Carry:
				PlaySoundForced(_Data._CarryAddNoise);
				break;
			case PikminStates.Squish:
				_Rigidbody.isKinematic = true;

				_PressedTimer = 1.5f;

				_Transform.localScale = new(2, 0.01f, 2);
				_DirectionVector = Vector3.zero;

				if (Physics.Raycast(
					    _Transform.position,
					    Vector3.down,
					    out RaycastHit info,
					    float.PositiveInfinity,
					    _MapMask,
					    QueryTriggerInteraction.Ignore
				    ))
				{
					_Transform.position = info.point;
				}

				break;
			case PikminStates.OnFire:
				_FireTimer = 0.0f;
				_WanderAngle = Random.Range(0.0f, 360.0f);
				RemoveFromSquad(PikminStates.OnFire);
				_FireVFX.Play();
				break;
			case PikminStates.Idle:
				LatchOnto(null);

				_FaceDirectionAngle = _EulerAngles.y;
				_Transform.eulerAngles = new(0, _EulerAngles.y, 0);

				_AudioSource.volume = 0.01f;
				_AudioSource.clip = _Data._IdleNoise;
				_AudioSource.Play();
				break;
		}
	}

	public void StartThrowHold()
	{
		_Rigidbody.isKinematic = true;
		_Rigidbody.useGravity = false;
		_Collider.isTrigger = true;

		if (_Animator.GetBool(Animations.Walking))
		{
			_Animator.SetBool(Animations.Walking, false);
		}

		_Animator.SetBool(Animations.Holding, true);

		_ColliderTimer = 0.0f;

		ChangeState(PikminStates.BeingHeld);
		_FaceDirectionAngle = _PlayerTransform.eulerAngles.y;
	}

	// We've been thrown!
	public void EndThrowHold()
	{
		_Rigidbody.isKinematic = false;
		_Rigidbody.useGravity = true;
		_Collider.isTrigger = false;
		_Collider.radius = 0.001f;

		_Animator.SetBool(Animations.Holding, false);

		_InSquad = false;
		_CurrentStatSpecifier = PikminStatSpecifier.OnField;
		_TargetObject = null;
		ChangeState(PikminStates.Thrown);

		_FaceDirectionAngle = Quaternion.LookRotation(MathUtil.DirectionFromTo(_PlayerTransform.position, _Transform.position)).eulerAngles.y;
		PlaySoundForced(_Data._ThrowNoise);

		PikminStatsManager.RemoveFromSquad(this, _Data._PikminColour, _CurrentMaturity);
		PikminStatsManager.ReassignFormation();
	}

	/// <summary>
	///   Latches the Pikmin onto an object.
	/// </summary>
	/// <param name="obj">Object to latch onto.</param>
	public void LatchOnto(Transform obj)
	{
		// Already latched onto the same object
		if (obj == _LatchedTransform)
		{
			return;
		}

		_LatchedTransform = obj;
		_TargetObject = obj;

		if (obj != null)
		{
			// Disable physics simulation for the Pikmin.
			_Rigidbody.isKinematic = true;
			_Collider.isTrigger = true;

			// Get the collider of the target object.
			_TargetObjectCollider = obj.GetComponent<Collider>();

			// If the Pikmin is not grounded, adjust its position and rotation.
			if (!IsGrounded())
			{
				// Find the closest point on the target object and calculate the direction.
				Vector3 closestPosition = ClosestPointOnTarget(_TargetObject, _TargetObjectCollider);
				Vector3 dirToClosestPos = MathUtil.DirectionFromTo(_Transform.position, closestPosition, true);

				// Raycast to check if the Pikmin is obstructed by the target object.
				if (Physics.Raycast(
					    _Transform.position,
					    dirToClosestPos,
					    out RaycastHit info,
					    1.5f,
					    _InteractableMask,
					    QueryTriggerInteraction.Collide
				    ) &&
				    info.collider == _TargetObjectCollider)
				{
					// Adjust the Pikmin's position and rotation to latch onto the target object.
					Vector3 point = info.point;
					_Transform.LookAt(point);
					_Transform.position = point + info.normal * _LatchNormalOffset;
				}
			}

			// Set the latched object as the parent and calculate the offset.
			_Transform.parent = _LatchedTransform;
			_LatchedOffset = _Transform.position - _LatchedTransform.position;
		}
		else
		{
			// Enable physics simulation for the Pikmin.
			_Rigidbody.isKinematic = false;
			_Collider.isTrigger = false;
			_Transform.parent = null;
			_TargetObjectCollider = null;

			// Reset the Pikmin's rotation, keeping the Y and Z angles intact.
			_Transform.eulerAngles = new(0, _EulerAngles.y, _EulerAngles.z);
		}
	}

	public void AddToSquad()
	{
		if (_InSquad)
		{
			return;
		}

		switch (_CurrentState)
		{
			case PikminStates.Dead:
			case PikminStates.Squish:
			case PikminStates.SuckNectar:
			case PikminStates.Thrown:
			case PikminStates.Push when !_Pushing.IsPikminSpotAvailable():
				return;
		}

		_InSquad = true;
		_CurrentStatSpecifier = PikminStatSpecifier.InSquad;

		ChangeState(PikminStates.RunTowards);

		PikminStatsManager.AddToSquad(this, _Data._PikminColour, _CurrentMaturity);
		PikminStatsManager.ReassignFormation();
	}

	public void RemoveFromSquad(PikminStates to = PikminStates.Idle)
	{
		if (!_InSquad)
		{
			return;
		}

		_InSquad = false;
		_CurrentStatSpecifier = PikminStatSpecifier.OnField;

		_TargetObject = null;
		ChangeState(to);

		PikminStatsManager.RemoveFromSquad(this, _Data._PikminColour, _CurrentMaturity);
		PikminStatsManager.ReassignFormation();
	}

	public void Die(float ragdollTimer = 0)
	{
		if (_CurrentState == PikminStates.Dead)
		{
			return;
		}

		_RagdollTime = ragdollTimer;
		ChangeState(PikminStates.Dead);
	}

	public bool IsGrounded(float distTo = 0.2f)
	{
		return Physics.Raycast(_Transform.position, Vector3.down, distTo, _MapMask, QueryTriggerInteraction.Ignore);
	}

	// CALLED BY PIKMIN ANIMATION - ATTACK
	public void ANIM_Attack()
	{
		if (_Attacking == null)
		{
			return;
		}

		_AttackVFXInstance.transform.position = _HeadBoneTransform.position - _HeadBoneTransform.forward / 5;
		_AttackVFXInstance.Play();

		_Attacking.OnAttackRecieve(_Data._AttackDamage, _AttackingTransform);
		PlaySoundForced(_Data._AttackHitNoise);
	}

	public void PlaySound(AudioClip clip, float delay = 0)
	{
		if (_AudioSource.isPlaying && _AudioSource.clip == clip)
		{
			return;
		}

		_AudioSource.clip = clip;
		_AudioSource.volume = _Data._AudioVolume;

		if (delay > 0)
		{
			_AudioSource.PlayDelayed(delay);
		}
		else
		{
			_AudioSource.Play();
		}
	}

	public void PlaySoundForced(AudioClip clip)
	{
		_AudioSource.Stop();
		_AudioSource.clip = clip;
		_AudioSource.volume = _Data._AudioVolume;
		_AudioSource.Play();
	}

	public void Latch_SetOffset(Vector3 latchedOffset)
	{
		_LatchedOffset = latchedOffset;
	}

	public bool Latch_IsLatchedOntoObject()
	{
		return _LatchedTransform != null;
	}

	public bool InteractNectar(Transform nectarTransform)
	{
		if (_CurrentMaturity == PikminMaturity.Size - 1
		    && _CurrentState != PikminStates.OnFire
		    && _CurrentState != PikminStates.Carry
		    && _CurrentState != PikminStates.BeingHeld
		    && _CurrentState != PikminStates.Attack)
		{
			return false;
		}

		RemoveFromSquad();
		ChangeState(PikminStates.SuckNectar);
		_NectarTransform = nectarTransform;
		return true;
	}

	public void ActSquish()
	{
		if (_CurrentState == PikminStates.Dead || _CurrentState == PikminStates.Squish
		                                       || Latch_IsLatchedOntoObject())
		{
			return;
		}

		ChangeState(PikminStates.Squish);
	}

	public void ActFire()
	{
		if (!_Data._IsAffectedByFire && _CurrentState != PikminStates.OnFire)
		{
			return;
		}

		ChangeState(PikminStates.OnFire);
	}

	public void ActWater()
	{
		if (_Data._PikminColour != PikminColour.Blue)
		{
			Die(0.5f);
			return;
		}

		if (_CurrentState == PikminStates.OnFire)
		{
			ChangeState(PikminStates.Idle);
		}
	}

	#endregion
}
