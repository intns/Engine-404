using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public class FlintBeetle : Entity, IPikminSquish, IInteraction
{
	[Header("Settings")]
	[SerializeField] [Tooltip("Set to -1 for infinite")] int _AmountUntilBurrow = 3;
	[SerializeField] GameObject _ThrownItem;
	[SerializeField] float _LifetimeLength = 45.0f;
	[Space]
	[SerializeField] float _ActivationRadius = 5.0f;
	[SerializeField] LayerMask _ActivationMask;
	[SerializeField] LayerMask _MapMask;
	[Space]
	[SerializeField] SkinnedMeshRenderer _Renderer;
	[SerializeField] Material _NormalMaterial;
	[SerializeField] Material _FireMaterial;
	[SerializeField] VisualEffect _FireDistortionVFX;

	[SerializeField] bool _IsFire;
	[SerializeField] float _FireTimer;
	Animator _Animator;
	float _CurrentScale = 0.001f;

	BasicFSM<Entity> _FSM;

	float _LifetimeTimer;

	Vector3 _MoveDirection = Vector3.zero;
	Rigidbody _Rigidbody;

	Vector3 _StartingScale = Vector3.zero;
	float _TargetScale = 0.001f;

	public new void Awake()
	{
		_Rigidbody = GetComponent<Rigidbody>();
		_Animator = GetComponent<Animator>();

		_FireDistortionVFX.Stop();

		_FSM = new();
		_FSM.AddState(new StateInvisible((int)FSMStates.Invisible));
		_FSM.AddState(new StateMove((int)FSMStates.Move));
		_FSM.AddState(new StateWait((int)FSMStates.Wait));
		_FSM.AddState(new StateSquished((int)FSMStates.Squished));
		_FSM.AddState(new StateBurrow((int)FSMStates.Burrow));
		_FSM.SetState((int)FSMStates.Invisible, this);

		_Transform = transform;
		_StartingScale = _Transform.localScale;
		_FaceDirection = Random.Range(0.0f, 360.0f);
		_FireTimer = 0.0f;
	}

	public new void Update()
	{
		_FSM.ExecuteState(this);

		if (_LifetimeTimer > _LifetimeLength)
		{
			_FSM.SetState((int)FSMStates.Burrow, this);
		}

		if (_FireTimer < 1.0f)
		{
			Material source = _IsFire ? _NormalMaterial : _FireMaterial;
			Material target = !_IsFire ? _NormalMaterial : _FireMaterial;
			_Renderer.materials[2].Lerp(source, target, _FireTimer / 1.0f);

			_FireTimer += Time.deltaTime;

			if (_FireTimer >= 1.0f)
			{
				if (_IsFire)
				{
					_FireDistortionVFX.Play();
				}
				else
				{
					_FireDistortionVFX.Stop();
				}
			}
		}

		ApplyScaling();
	}

	public new void FixedUpdate()
	{
		base.FixedUpdate();

		if (_MoveDirection != Vector3.zero)
		{
			_Rigidbody.velocity = _MoveDirection + Vector3.down;
			_MoveDirection = Vector3.Lerp(_MoveDirection, Vector3.zero, 12.5f * Time.fixedDeltaTime);
		}
	}

	void OnCollisionEnter(Collision collision)
	{
		OnCollisionHandle(collision);
	}

	void OnCollisionStay(Collision collision)
	{
		OnCollisionHandle(collision);
	}

#if UNITY_EDITOR
	public new void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();

		Gizmos.DrawWireSphere(transform.position, _ActivationRadius);

		if (_Transform != null)
		{
			if (_FSM != null && _FSM.IsCurrentStateValid())
			{
				Handles.Label(_Transform.position + Vector3.up * 3.0f, _FSM.GetCurrentState()._Name);
			}

			Handles.Label(_Transform.position + Vector3.up * 2.0f, _Flags.ToString());
		}
	}
#endif

	void ApplyScaling()
	{
		_CurrentScale = Mathf.Lerp(_CurrentScale, _TargetScale, 6.5f * Time.deltaTime);
		_Transform.localScale = _StartingScale * _CurrentScale;
	}

	void OnCollisionHandle(Collision collision)
	{
		if (!_IsFire)
		{
			return;
		}

		if (collision.transform.TryGetComponent(out IInteraction i))
		{
			i.ActFire();
		}
	}

	enum FSMStates
	{
		Invisible = 0,
		Move,
		Wait,
		Squished,
		Burrow,
	}

	public class StateBurrow : BasicFSMState<Entity>
	{
		public StateBurrow(int stateIndex) : base(stateIndex, "Burrowing")
		{
		}

		public override void Cleanup(Entity ent)
		{
		}

		public override void Execute(Entity ent)
		{
		}

		public override void Start(Entity ent, StateArg arg)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			obj._Animator.SetBool("IsBurrow", true);
			CameraFollow._Instance.Shake(2);
		}
	}

	public class StateInvisible : BasicFSMState<Entity>
	{
		public StateInvisible(int stateIndex) : base(stateIndex, "Waiting for interaction")
		{
		}

		public override void Cleanup(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
			FlagsHelper.Set(ref obj._Flags, EntityFlags.IsAttackAvailable);
		}

		public override void Execute(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			if (obj.ShouldActivate())
			{
				obj._Animator.SetBool("IsPopup", true);
				obj._TargetScale = 1.0f;
				obj._FSM.SetState((int)FSMStates.Move, ent);
			}
		}

		public override void Start(Entity ent, StateArg arg)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			FlagsHelper.Unset(ref obj._Flags, EntityFlags.IsAttackAvailable);
			obj._TargetScale = 0.0f;
		}
	}

	public class StateMove : BasicFSMState<Entity>
	{
		float _RandomLength;
		float _MoveTimer;

		public StateMove(int stateIndex) : base(stateIndex, "Moving around")
		{
		}

		public override void Cleanup(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
			obj._Animator.SetBool("IsRunning", false);
		}

		public override void Execute(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			obj._LifetimeTimer += Time.deltaTime;
			obj.MoveForwards();
			obj.CheckForBurrow();

			if (Physics.Raycast(
				    obj._Transform.position + Vector3.up,
				    obj._Transform.right,
				    out RaycastHit hit,
				    1.5f,
				    obj._MapMask,
				    QueryTriggerInteraction.Ignore
			    ))
			{
				// Check angle of the wall
				float wallAngle = Vector3.Angle(Vector3.up, hit.normal);

				if (wallAngle > 65.0f)
				{
					obj._FaceDirection = Random.Range(-1000.0f, 1000.0f);
					obj._FSM.SetState((int)FSMStates.Wait, ent);
					return;
				}
			}

			_MoveTimer += Time.deltaTime;

			if (_MoveTimer >= _RandomLength)
			{
				obj._FSM.SetState((int)FSMStates.Wait, ent);
			}
		}

		public override void Start(Entity ent, StateArg arg)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			_RandomLength = Random.Range(1.0f, 2.5f);

			_MoveTimer = 0.0f;

			obj._Animator.SetBool("IsPopup", false);
			obj._Animator.SetBool("IsRunning", true);
		}
	}

	public class StateSquished : BasicFSMState<Entity>
	{
		public StateSquished(int stateIndex) : base(stateIndex, "Squished State")
		{
		}

		public override void Cleanup(Entity ent)
		{
		}

		public override void Execute(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			obj._FSM.SetState((int)FSMStates.Move, ent);
		}

		public override void Start(Entity ent, StateArg arg)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			obj._Animator.Play(
				Random.Range(0.0f, 1.0f) < 0.5f
					? "Armature_FlipForwardsAnimation"
					: "Armature_FlipBackwardsAnimation"
			);
		}
	}

	public class StateWait : BasicFSMState<Entity>
	{
		float _WaitTimer;
		float _WaitLength;

		bool _HasRotated;

		public StateWait(int stateIndex) : base(stateIndex, "Wait State")
		{
		}

		public override void Cleanup(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
			obj._Animator.SetBool("IsWaiting", false);
			_HasRotated = false;
		}

		public override void Execute(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			obj.CheckForBurrow();
			obj._LifetimeTimer += Time.deltaTime;

			_WaitTimer += Time.deltaTime;

			if (_WaitTimer >= _WaitLength)
			{
				obj._FSM.SetState((int)FSMStates.Move, ent);
			}
			else if (_WaitTimer >= _WaitLength / 2.0f && !_HasRotated)
			{
				_HasRotated = true;
				obj._FaceDirection = Random.Range(-1000.0f, 1000.0f);
			}
		}

		public override void Start(Entity ent, StateArg arg)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			_WaitTimer = 0.0f;
			_WaitLength = Random.Range(0.5f, 2.0f);

			obj._Animator.SetBool("IsPopup", false);
			obj._Animator.SetBool("IsWaiting", true);
		}
	}

	#region Public Functions

	public bool ShouldActivate()
	{
		Collider[] colls = Physics.OverlapSphere(_Transform.position, _ActivationRadius, _ActivationMask);
		return colls.Length != 0;
	}

	public void ReleaseItem()
	{
		if (_AmountUntilBurrow != -1)
		{
			_AmountUntilBurrow--;
		}

		CameraFollow._Instance.Shake(1);

		GameObject go = Instantiate(
			_ThrownItem,
			_Transform.position + Vector3.up * 2.5f,
			Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0.0f)
		);
		go.GetComponent<Rigidbody>().velocity = Vector3.up * 35.0f + MathUtil.XZToXYZ(Random.insideUnitCircle) * 50.0f;
	}

	public void ANIM_RunLoop()
	{
		if (_FSM.GetCurrentState()._Index == (int)FSMStates.Move)
		{
			_Animator.Play("Armature_RunAnimation", 0, 0.4f);
		}
	}

	public void CheckForBurrow()
	{
		if (_AmountUntilBurrow != 0)
		{
			return;
		}

		if (_FSM.IsCurrentStateID((int)FSMStates.Burrow))
		{
			return;
		}

		_FSM.SetState((int)FSMStates.Burrow, this);
	}

	public void ANIM_BurrowFinish()
	{
		while (_AttachedPikmin.Count != 0)
		{
			PikminAI ai = _AttachedPikmin[0];

			if (Physics.Raycast(ai.transform.position, Vector3.down, out RaycastHit info, float.PositiveInfinity, _MapMask))
			{
				ai.transform.position = info.point + Vector3.up;
			}

			_AttachedPikmin[0].ChangeState(PikminStates.Idle);
		}

		Die();
	}

	public void MoveForwards()
	{
		_MoveDirection = _Transform.right * 10.0f;
	}

	public new void OnAttackEnd(PikminAI pikmin)
	{
		_AttachedPikmin.Remove(pikmin);
	}

	public new void OnAttackStart(PikminAI pikmin)
	{
		_AttachedPikmin.Add(pikmin);
	}

	public void OnAttackRecieve(float damage)
	{
	}

	public void OnSquish(PikminAI ai)
	{
		if (!_FSM.IsCurrentStateID((int)FSMStates.Squished))
		{
			AnimatorStateInfo info = _Animator.GetCurrentAnimatorStateInfo(0);
			bool isFlipAnim = info.IsName("Armature_FlipForwardsAnimation") || info.IsName("Armature_FlipBackwardsAnimation");

			if (!isFlipAnim)
			{
				_FSM.SetState((int)FSMStates.Squished, this);
			}
		}
	}

	public void ActFire()
	{
		if (!_IsFire)
		{
			_IsFire = true;
			_FireTimer = 0.0f;
		}
	}

	public void ActSquish()
	{
	}

	public void ActWater()
	{
		if (_IsFire)
		{
			_IsFire = false;
			_FireTimer = 0.0f;
		}
	}

	#endregion
}
