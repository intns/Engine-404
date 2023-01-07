﻿using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class StateArg
{

}

public abstract class BasicFSMState<T>
{
	public int _Index;
	public string _Name;

	public BasicFSMState(int stateIndex, string name)
	{
		_Index = stateIndex;
		_Name = name;
	}

	public abstract void Start(T obj, StateArg arg);
	public abstract void Execute(T obj);
	public abstract void Cleanup(T obj);
}

public class BasicFSM<T>
{
	private List<BasicFSMState<T>> _States;
	private int _CurrentState;

	public BasicFSM()
	{
		_States = new();
		_CurrentState = 0;
	}

	public void AddState(BasicFSMState<T> state)
	{
		_States.Add(state);
	}

	public void ExecuteState(T obj)
	{
		_States[_CurrentState].Execute(obj);
	}

	public void SetState(int idx, T obj, StateArg arg = null)
	{
		if (idx > _States.Count || idx < 0)
		{
			return;
		}

		_States[_CurrentState].Cleanup(obj);
		_CurrentState = idx;
		_States[_CurrentState].Start(obj, arg);
	}

	public bool IsCurrentStateValid()
	{
		return _States != null && _States.Count > 0 && _CurrentState <= _States.Count;
	}

	public BasicFSMState<T> GetCurrentState()
	{
		return _States[_CurrentState];
	}
}