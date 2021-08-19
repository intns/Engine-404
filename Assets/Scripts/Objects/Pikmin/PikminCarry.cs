/*
 * PikminCarry.cs
 * Created by: Ambrosia, Kman
 * Created on: 11/4/2020 (dd/mm/yy)
 * Created for: testing how carrying would work
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PikminCarry : MonoBehaviour, IPikminCarry
{
	private Transform _EndPoint = null;
	private Onion _DebugOnion = null;
	private NavMeshAgent _Agent = null;
	private Rigidbody _Rigidbody = null;

	[Header("Settings")]
	[SerializeField] private bool _HasMatchingColour = false;
	[SerializeField] private PikminColour _MatchColour = PikminColour.Red;
	[SerializeField] private int _MatchPikminToProduce = 2;
	[SerializeField] private int _NonMatchPikminToProduce = 1;

	[SerializeField] private int _MinAmountRequired = 1;
	[SerializeField] private int _MaxAmountRequired = 2;
	[SerializeField] private float _Speed = 0;
	[SerializeField] private float _AddedSpeed = 1;

	[SerializeField] private GameObject _CarryTextPrefab = null;
	public float _Radius = 1;
	private bool _IsBeingCarried = false;
	private readonly List<PikminAI> _CarryingPikmin = new List<PikminAI>();
	private CarryText _Text = null;

	public PikminIntention IntentionType => PikminIntention.Carry;

	private void Awake()
	{
		_Agent = GetComponent<NavMeshAgent>();
		_Agent.updateRotation = false;
		_Agent.speed = _Speed;
		_Agent.enabled = false;

		_Rigidbody = GetComponent<Rigidbody>();
		_Rigidbody.isKinematic = false;

		_DebugOnion = GameObject.FindGameObjectWithTag("DebugOnion").GetComponent<Onion>();
		_EndPoint = _DebugOnion._CarryEndpoint;

		GameObject carryText = Instantiate(_CarryTextPrefab, transform.position, Quaternion.identity);
		_Text = carryText.GetComponent<CarryText>();
		_Text._FollowTarget = transform;
	}

	private void Update()
	{
		if (_IsBeingCarried)
		{
			CalculatePikminPositions();

			if (MathUtil.DistanceTo(transform.position, _EndPoint.position) <= 1)
			{
				_Agent.enabled = false;

				// Make every pikmin stop carrying
				while (_CarryingPikmin.Count > 0)
				{
					_CarryingPikmin[0].ChangeState(PikminStates.Idle);
				}

				if (!_HasMatchingColour || _DebugOnion._PikminColour == _MatchColour)
				{
					_DebugOnion.EnterOnion(_MatchPikminToProduce);
				}
				else
				{
					_DebugOnion.EnterOnion(_NonMatchPikminToProduce);
				}

				Destroy(_Text.gameObject);
				Destroy(gameObject);
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		for (int i = 0; i < _MinAmountRequired; i++)
		{
			Gizmos.DrawWireSphere(transform.position + (MathUtil.XZToXYZ(MathUtil.PositionInUnit(_MinAmountRequired, i)) * _Radius), 0.3f);
		}

		Gizmos.color = Color.red;
		for (int i = 0; i < _MaxAmountRequired; i++)
		{
			Gizmos.DrawWireSphere(transform.position + (MathUtil.XZToXYZ(MathUtil.PositionInUnit(_MaxAmountRequired, i)) * _Radius), 0.25f);
		}
	}

	public void OnCarryLeave(PikminAI p)
	{
		_CarryingPikmin.Remove(p);
		p.LatchOnto(null);

		if (_CarryingPikmin.Count < _MinAmountRequired && _IsBeingCarried)
		{
			_Agent.enabled = false;
			_Rigidbody.isKinematic = false;
			_IsBeingCarried = false;
		}

		_Text.SetText(_CarryingPikmin.Count, _MaxAmountRequired);
		if (_CarryingPikmin.Count == 0)
		{
			_Text.FadeOut();
		}
	}

	public void OnCarryStart(PikminAI p)
	{
		if (_CarryingPikmin.Count >= _MaxAmountRequired)
		{
			p.ChangeState(PikminStates.Idle);
			return;
		}

		p.LatchOnto(transform);
		_CarryingPikmin.Add(p);

		if (_CarryingPikmin.Count >= _MinAmountRequired)
		{
			if (_Agent.enabled == false)
			{
				_Agent.enabled = true;
				_Rigidbody.isKinematic = true;
			}

			_Agent.SetDestination(_EndPoint.position);
			_Agent.speed += _AddedSpeed;
			_IsBeingCarried = true;
		}

		_Text.HandleColor(_CarryingPikmin);
		_Text.SetText(_CarryingPikmin.Count, _MinAmountRequired);
		if (_CarryingPikmin.Count == 1)
		{
			_Text.FadeIn();
		}

		p.ChangeState(PikminStates.Carrying);
	}

	public bool PikminSpotAvailable()
	{
		return _CarryingPikmin.Count < _MaxAmountRequired;
	}

	private void CalculatePikminPositions()
	{
		for (int i = 0; i < _CarryingPikmin.Count; i++)
		{
			PikminAI pikminObj = _CarryingPikmin[i];
			pikminObj.transform.SetPositionAndRotation(
				transform.position + (MathUtil.XZToXYZ(MathUtil.PositionInUnit(_CarryingPikmin.Count, i)) * _Radius),
				Quaternion.LookRotation(transform.position - pikminObj.transform.position));
		}
	}
}
