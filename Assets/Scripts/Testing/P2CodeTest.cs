using UnityEditor;
using UnityEngine;

public class P2CodeTest : Entity
{
	BasicFSM<Entity> _FSM;

	public override void Awake()
	{
		base.Awake();

		_FSM = new();
		_FSM.AddState(new IdleState((int)FSMStates.Idle));
	}

	public override void Update()
	{
		base.Update();

		_FSM.ExecuteState(this);
		LookTowards(Player._Instance.transform.position);
	}

	#if UNITY_EDITOR
	public override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();

		if (_FSM != null && _FSM.IsCurrentStateValid())
		{
			Handles.Label(_Transform.position + Vector3.up * 5.0f, _FSM.GetCurrentState()._Name);
		}
	}
	#endif

	enum FSMStates
	{
		Idle = 0,
	}

	public class IdleState : BasicFSMState<Entity>
	{
		float _Timer;

		public IdleState(int stateIndex) : base(stateIndex, "Idle State")
		{
		}

		public override void Cleanup(Entity ent)
		{
			Debug.Log("Idle Cleanup");
		}

		public override void Execute(Entity ent)
		{
			P2CodeTest obj = (P2CodeTest)ent;

			_Timer += Time.deltaTime;

			if (_Timer > 5.0f)
			{
				_Timer = 0.0f;
				PikminAI ai;

				for (; obj._AttachedPikmin.Count > 0; obj._AttachedPikmin.Remove(ai))
				{
					ai = obj._AttachedPikmin[0];
					ai.Die();
				}
			}
		}

		public override void Start(Entity ent, StateArg arg)
		{
			Debug.Log("Idle Start");
		}
	}
}
