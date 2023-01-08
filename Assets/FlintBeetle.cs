using Cinemachine.Utility;
using TMPro;
using Unity.Burst;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public class FlintBeetle : Entity
{
	public class StateAppear : BasicFSMState<Entity>
	{
		public StateAppear(int stateIndex) : base(stateIndex, "Appear State")
		{
		}

		public override void Start(Entity ent, StateArg arg)
		{
			FlintBeetle obj = (FlintBeetle)ent;
			obj.SetAttackable(false);
			obj.SetVelocity(Vector3.zero);
		}

		public override void Execute(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
			if (obj.ShouldAppear())
			{
				obj._FSM.SetState((int)FSMStates.Move, ent);
			}
		}

		public override void Cleanup(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
			obj.SetAttackable(true);

			// TODO: Create effect
		}
	}

	public class StateDisappear : BasicFSMState<Entity>
	{
		public StateDisappear(int stateIndex) : base(stateIndex, "Disappear State")
		{
		}

		public override void Start(Entity ent, StateArg arg)
		{
			FlintBeetle obj = (FlintBeetle)ent;
		}

		public override void Execute(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
			if (obj.Shrink())
			{
				Debug.Log("Done shrinking");
			}
		}

		public override void Cleanup(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
		}
	}

	public class StateMove : BasicFSMState<Entity>
	{
		public StateMove(int stateIndex) : base(stateIndex, "Move State")
		{
		}

		public override void Start(Entity ent, StateArg arg)
		{
			FlintBeetle obj = (FlintBeetle)ent;
			obj.ResetMoveTimer(0.5f, 2.0f);
			obj.SetTargetPosition(Vector3.zero);
		}

		public override void Execute(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			obj.Grow();
			obj.MoveTowards();

			if (obj._LifetimeTimer > 100.0f || obj._MoveTimer > 2.5f)
			{
				obj.MoveStop();
			}

			obj._LifetimeTimer += Time.deltaTime;
			obj._MoveTimer += Time.deltaTime;

			if (obj._FauxAnimationTimer > 5.0f)
			{
				if (obj._LifetimeTimer > 100.0f)
				{
					obj._FSM.SetState((int)FSMStates.Disappear, ent);
				}
				else
				{
					obj._FSM.SetState((int)FSMStates.Wait, ent);
				}
			}
		}

		public override void Cleanup(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
		}
	}

	public class StateWait : BasicFSMState<Entity>
	{
		public StateWait(int stateIndex) : base(stateIndex, "Waiting State")
		{
		}

		public override void Start(Entity ent, StateArg arg)
		{
			FlintBeetle obj = (FlintBeetle)ent;
			obj.ResetMoveTimer(0.5f, 2.0f);
			obj.SetVelocity(Vector3.zero);
		}

		public override void Execute(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			obj.Grow();
			if (obj._MoveTimer > 2.0f)
			{
				obj.MoveStop();
			}

			obj._LifetimeTimer += Time.deltaTime;
			obj._MoveTimer += Time.deltaTime;

			if (obj._FauxAnimationTimer > 5.5f)
			{
				obj._FSM.SetState((int)FSMStates.Move, ent);
			}
		}

		public override void Cleanup(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
		}
	}

	public class StatePress : BasicFSMState<Entity>
	{
		public StatePress(int stateIndex) : base(stateIndex, "Press State")
		{
		}

		public override void Start(Entity ent, StateArg arg)
		{
			FlintBeetle obj = (FlintBeetle)ent;
		}

		public override void Execute(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;

			obj.Grow();
			obj._MoveTimer += Time.deltaTime;

			obj.ThrowItem();

			if (obj._LifetimeTimer > 12800.0f)
			{
				obj._FSM.SetState((int)FSMStates.Move, ent);
			}
		}

		public override void Cleanup(Entity ent)
		{
			FlintBeetle obj = (FlintBeetle)ent;
		}
	}

	enum FSMStates
	{
		Appear = 0,
		Disappear,
		Move,
		Wait,
		Press
	}

	[Header("Settings")]
	[SerializeField] float _DetectionRadius = 5.0f;
	[SerializeField] LayerMask _PikminPlayerMask;
	[Space()]
	[SerializeField] GameObject _ThrownItem;

	[Header("Debugging")]
	[SerializeField] Vector3 _Velocity = Vector3.zero;
	[SerializeField] float _ScaleTimer = 0.0f;
	[SerializeField] float _MoveTimer = 0.0f;
	[SerializeField] float _LifetimeTimer = 0.0f;
	[SerializeField] Vector3 _TargetPosition = Vector3.zero;
	Vector3 _BaseScale = Vector3.zero;

	float _FauxAnimationTimer = 0.0f;

	BasicFSM<Entity> _FSM;
	Rigidbody _Rigidbody;

	public override void Awake()
	{
		base.Awake();

		_Rigidbody = GetComponent<Rigidbody>();

		_BaseScale = _Transform.localScale;

		_FSM = new();
		_FSM.AddState(new StateAppear((int)FSMStates.Appear));
		_FSM.AddState(new StateDisappear((int)FSMStates.Disappear));
		_FSM.AddState(new StateMove((int)FSMStates.Move));
		_FSM.AddState(new StateWait((int)FSMStates.Wait));
		_FSM.AddState(new StatePress((int)FSMStates.Press));

		_FSM.SetState((int)FSMStates.Appear, this);
	}

	public override void Update()
	{
		base.Update();

		_FSM.ExecuteState(this);
		_FauxAnimationTimer += Time.deltaTime;
		if (_FauxAnimationTimer > 6.0f)
		{
			_FauxAnimationTimer = 0.0f;
		}
	}

	public void FixedUpdate()
	{
		if (_Velocity != Vector3.zero)
		{
			_Rigidbody.MovePosition(_Rigidbody.position + _Velocity);
		}
	}

	public override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();

		if (_Transform != null)
		{
			if (_FSM != null && _FSM.IsCurrentStateValid())
			{
				Handles.Label(_Transform.position + Vector3.up * 3.0f, _FSM.GetCurrentState()._Name);
			}

			Handles.Label(_Transform.position + Vector3.up * 2.0f, _Flags.ToString());
		}
	}

	#region Public Functions
	public void SetAttackable(bool toAttack)
	{
		if (toAttack)
		{
			FlagsHelper.Set(ref _Flags, EntityFlags.IsAttackAvailable);
		}
		else
		{
			FlagsHelper.Unset(ref _Flags, EntityFlags.IsAttackAvailable);
		}
	}

	public void SetVelocity(Vector3 vel)
	{
		_Velocity = vel;
	}

	public void ResetMoveTimer(float min, float max)
	{
		_MoveTimer = Random.Range(min, max);
	}

	public void SetTargetPosition(Vector3 pos)
	{
		if (pos != Vector3.zero)
		{
			_TargetPosition.x = pos.x * 10.0f + _Transform.position.x;
			_TargetPosition.y = _Transform.position.y;
			_TargetPosition.z = pos.z * 10.0f + _Transform.position.z;
			return;
		}
		
		Vector3 dir = MathUtil.XZToXYZ(Random.insideUnitCircle) * 15.0f;
		dir.y = _Transform.position.y;
		_TargetPosition = dir;
	}

	public bool Shrink()
	{
		bool isFinished = false;

		if (_ScaleTimer > 0.0001f)
		{
			_ScaleTimer -= Time.deltaTime;

			if (_ScaleTimer <= 0.0001f)
			{
				_ScaleTimer = 0.0001f;
				isFinished = true;
			}

			_Transform.localScale = _BaseScale * _ScaleTimer;
		}

		return isFinished;
	}

	public bool Grow()
	{
		bool isFinished = false;

		if (_ScaleTimer < 1.0f)
		{
			_ScaleTimer += Time.deltaTime;
			if (_ScaleTimer < 1.0f)
			{
				_ScaleTimer = 1.0f;
				isFinished = true;
			}

			_Transform.localScale = _BaseScale * _ScaleTimer;
		}

		return isFinished;
	}

	public void MoveTowards()
	{
		ChangeFaceDirection(_TargetPosition);
		_Velocity = MathUtil.DirectionFromTo(_Transform.position, _TargetPosition) * 0.05f;
	}

	public void MoveStop()
	{
		_Velocity = Vector3.zero;
	}

	public void ThrowItem()
	{
		GameObject obj = Instantiate(_ThrownItem, _Transform.position, Quaternion.identity);
		obj.GetComponent<Rigidbody>().velocity = Vector3.up * 25.0f;
	}

	public bool ShouldAppear()
	{
		Collider[] colls = Physics.OverlapSphere(_Transform.position, _DetectionRadius, _PikminPlayerMask, QueryTriggerInteraction.Ignore);
		return colls.Length != 0;
	}
	#endregion
}
