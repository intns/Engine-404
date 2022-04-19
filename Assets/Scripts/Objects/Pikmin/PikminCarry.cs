/*
 * PikminCarry.cs
 * Created by: Ambrosia, Kman
 * Created on: 11/4/2020 (dd/mm/yy)
 * Created for: testing how carrying would work
 */

using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Collections;

[RequireComponent(typeof(AILerp), typeof(Seeker))]
public class PikminCarry : MonoBehaviour, IPikminCarry
{
	private Transform _EndPoint = null;
	private Onion _DebugOnion = null;
	private AILerp _Agent = null;
	private Seeker _Seeker = null;
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

	private bool _LeapOfFaith = false;
	private Path _CurrentPath = null;

	public PikminIntention IntentionType => PikminIntention.Carry;

	private void Awake()
	{
		_Agent = GetComponent<AILerp>();
		_Agent.speed = _Speed;
		_Agent.enabled = false;

		_Seeker = GetComponent<Seeker>();

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
		if (!_IsBeingCarried)
		{
			return;
		}

		// If we need to jump at some point...
		if (_LeapOfFaith)
		{
			// Check if we need to now!
			if (MathUtil.DistanceTo(transform.position, _CurrentPath.vectorPath[^1]) <= 0.25f)
			{
				StartCoroutine(IE_PauseThenEnableLeap());
			}
		}

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
				_DebugOnion.EnterOnion(_MatchPikminToProduce, _MatchColour);
			}
			else
			{
				_DebugOnion.EnterOnion(_NonMatchPikminToProduce, _MatchColour);
			}

			Destroy(_Text.gameObject);
			Destroy(gameObject);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (_LeapOfFaith && collision.gameObject.layer == LayerMask.NameToLayer("Map"))
		{
			_LeapOfFaith = false;

			_Rigidbody.isKinematic = true;
			_Agent.enabled = true;
			_Seeker.StartPath(transform.position, _EndPoint.position, FinishGeneratingPath);
		}
	}

	private IEnumerator IE_PauseThenEnableLeap()
	{
		_LeapOfFaith = false;

		_Rigidbody.isKinematic = false;
		_Agent.enabled = false;

		Vector3 arrow = _CurrentPath.vectorPath[^1] - _CurrentPath.vectorPath[^2];
		_Rigidbody.velocity = arrow.normalized * 3;

		yield return new WaitForSeconds(0.25f);

		_LeapOfFaith = true;
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
			_Agent.speed = 0;
			_Agent.enabled = false;
			_Rigidbody.isKinematic = false;
			_IsBeingCarried = false;
		}

		_Text.SetText(_CarryingPikmin.Count, _MinAmountRequired);
		if (_CarryingPikmin.Count == 0)
		{
			_Text.FadeOut();
		}
	}

	private void FinishGeneratingPath(Path p)
	{
		_CurrentPath = p;

		if (MathUtil.DistanceTo(p.vectorPath[^1], _EndPoint.position, true) <= 0.5f)
		{
			_LeapOfFaith = false;
			return;
		}

		// We can't make it! We need to do something drastic.
		_LeapOfFaith = true;
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
				_Agent.speed = _Speed;
			}

			_Seeker.StartPath(transform.position, _EndPoint.position, FinishGeneratingPath);
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
