using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class FlintBeetle : Entity, IPikminSquish
{
	public class StateInvisible : BasicFSMState<Entity>
	{
		public StateInvisible(int stateIndex) : base(stateIndex, "Waiting for interaction")
		{
		}

		public override void Start(Entity ent, StateArg arg)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			FlagsHelper.Unset(ref obj._Flags, EntityFlags.IsAttackAvailable);
			obj._TargetScale = 0.0f;
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

		public override void Cleanup(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
			FlagsHelper.Set(ref obj._Flags, EntityFlags.IsAttackAvailable);
		}
	}

	public class StateMove : BasicFSMState<Entity>
	{
		float _RandomAngle = 0.0f;

		float _RandomLength = 0.0f;
		float _MoveTimer = 0.0f;

		public StateMove(int stateIndex) : base(stateIndex, "Moving around")
		{
		}

		public override void Start(Entity ent, StateArg arg)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			_RandomAngle = Random.Range(-360.0f, 360.0f);
			_RandomLength = Random.Range(3.0f, 6.0f);

			_MoveTimer = 0.0f;

			obj._Animator.SetBool("IsPopup", false);
			obj._Animator.SetBool("IsRunning", true);
		}

		public override void Execute(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
			obj.MoveTowards(_RandomAngle);

			if (Physics.Raycast(obj._Transform.position, obj._Transform.forward, 4.5f, obj._MapMask))
			{
				obj._FaceDirection = Random.Range(-360.0f, 360.0f);
				obj._FSM.SetState((int)FSMStates.Wait, ent);
				return;
			}

			_MoveTimer += Time.deltaTime;
			obj._LifetimeTimer += Time.deltaTime;

			if (_MoveTimer >= _RandomLength)
			{
				obj._FSM.SetState((int)FSMStates.Wait, ent);
			}
		}

		public override void Cleanup(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
			obj._Animator.SetBool("IsRunning", false);
		}
	}

	public class StateWait : BasicFSMState<Entity>
	{
		float _WaitTimer = 0.0f;
		float _WaitLength = 0.0f;

		public StateWait(int stateIndex) : base(stateIndex, "Wait State")
		{
		}

		public override void Start(Entity ent, StateArg arg)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			_WaitTimer = 0.0f;
			_WaitLength = Random.Range(0.5f, 2.0f);

			obj._Animator.SetBool("IsPopup", false);
			obj._Animator.SetBool("IsWaiting", true);
		}

		public override void Execute(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			_WaitTimer += Time.deltaTime;
			obj._LifetimeTimer += Time.deltaTime;

			if (_WaitTimer >= _WaitLength)
			{
				obj._FSM.SetState((int)FSMStates.Move, ent);
			}
		}

		public override void Cleanup(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
			obj._Animator.SetBool("IsWaiting", false);
		}
	}

	public class StateSquished : BasicFSMState<Entity>
	{
		public StateSquished(int stateIndex) : base(stateIndex, "Squished State")
		{
		}

		public override void Start(Entity ent, StateArg arg)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			if (Random.Range(0.0f, 1.0f) < 0.5f)
			{
				obj._Animator.Play("Armature_FlipForwardsAnimation");
			}
			else
			{
				obj._Animator.Play("Armature_FlipBackwardsAnimation");
			}
		}

		public override void Execute(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			obj.ReleaseItem();
			obj._FSM.SetState((int)FSMStates.Move, ent);
		}

		public override void Cleanup(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
		}
	}

	public class StateBurrow : BasicFSMState<Entity>
	{
		public StateBurrow(int stateIndex) : base(stateIndex, "Burrowing")
		{
		}

		public override void Start(Entity ent, StateArg arg)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			obj._Animator.SetBool("IsBurrow", true);
		}

		public override void Execute(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
		}

		public override void Cleanup(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
		}
	}

	enum FSMStates : int
	{
		Invisible = 0,
		Move,
		Wait,
		Squished,
		Burrow,
	}

	[Header("Settings")]
	[SerializeField, Tooltip("Set to -1 for infinite")] int _AmountUntilBurrow = 3;
	[SerializeField] GameObject _ThrownItem = null;
	[SerializeField] float _LifetimeLength = 45.0f;
	[Space]
	[SerializeField] float _ActivationRadius = 5.0f;
	[SerializeField] LayerMask _ActivationMask = default;
	[SerializeField] LayerMask _MapMask = default;

	BasicFSM<Entity> _FSM;
	Animator _Animator = null;
	Rigidbody _Rigidbody;

	float _LifetimeTimer = 0.0f;

	Vector3 _MoveDirection = Vector3.zero;

	Vector3 _StartingScale = Vector3.zero;
	float _TargetScale = 0.001f;
	float _CurrentScale = 0.001f;

	public new void Awake()
	{
		_Rigidbody = GetComponent<Rigidbody>();
		_Animator = GetComponent<Animator>();

		_FSM = new();
		_FSM.AddState(new StateInvisible((int)FSMStates.Invisible));
		_FSM.AddState(new StateMove((int)FSMStates.Move));
		_FSM.AddState(new StateWait((int)FSMStates.Wait));
		_FSM.AddState(new StateSquished((int)FSMStates.Squished));
		_FSM.AddState(new StateBurrow((int)FSMStates.Burrow));
		_FSM.SetState((int)FSMStates.Invisible, this);

		_Transform = transform;
		_StartingScale = _Transform.localScale;
		_FaceDirection = Random.Range(-360.0f, 360.0f);
	}

	public new void Update()
	{
		_FSM.ExecuteState(this);

		if (_LifetimeTimer > _LifetimeLength)
		{
			_FSM.SetState((int)FSMStates.Burrow, this);
		}

		ApplyScaling();
	}

	public new void FixedUpdate()
	{
		base.FixedUpdate();

		if (_MoveDirection != Vector3.zero)
		{
			_Rigidbody.velocity = _MoveDirection;
			_MoveDirection = Vector3.Lerp(_MoveDirection, Vector3.zero, 12.5f * Time.fixedDeltaTime);
		}
	}

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

	void ApplyScaling()
	{
		_CurrentScale = Mathf.Lerp(_CurrentScale, _TargetScale, 6.5f * Time.deltaTime);
		_Transform.localScale = _StartingScale * _CurrentScale;
	}

	#region Public Functions
	public bool ShouldActivate()
	{
		Collider[] colls = Physics.OverlapSphere(_Transform.position, _ActivationRadius, _ActivationMask);
		return colls.Length != 0;
	}

	public void ReleaseItem()
	{
		if (_AmountUntilBurrow == 0)
		{
			if (_FSM.GetCurrentState()._Index == (int)FSMStates.Burrow)
			{
				return;
			}

			_FSM.SetState((int)FSMStates.Burrow, this);
		}

		if (_AmountUntilBurrow != -1)
		{
			_AmountUntilBurrow--;
		}


		GameObject go = Instantiate(_ThrownItem, _Transform.position + (Vector3.up * 2.5f), Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0.0f));
		go.GetComponent<Rigidbody>().velocity = (Vector3.up * 35.0f) + (MathUtil.XZToXYZ(Random.insideUnitCircle) * 50.0f);
	}

	public void ANIM_RunLoop()
	{
		if (_FSM.GetCurrentState()._Index == (int)FSMStates.Move)
		{
			_Animator.Play("Armature_RunAnimation", 0, 0.4f);
		}
	}

	public void ANIM_BurrowFinish()
	{
		Die();
	}

	public void MoveTowards(float angle)
	{
		LookTowards(angle);
		_MoveDirection = _Transform.right * 7.0f;
	}

	public new void OnAttackEnd(PikminAI pikmin)
	{
		_AttachedPikmin.Remove(pikmin);
	}

	public new void OnAttackStart(PikminAI pikmin)
	{
		_AttachedPikmin.Add(pikmin);
	}

	public new void OnAttackRecieve(float damage)
	{
	}

	public void OnSquish(PikminAI ai)
	{
		_FSM.SetState((int)FSMStates.Squished, this);
	}
	#endregion
}
