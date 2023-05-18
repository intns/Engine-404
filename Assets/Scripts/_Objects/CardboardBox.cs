using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardboardBox : MonoBehaviour, IPikminPush
{
	[Header("Components")]
	[SerializeField] Transform _EndPosition;
	[SerializeField] GameObject _CarryTextPrefab;

	[Header("Settings")]
	[SerializeField] int _PikminToPush = 10;
	[SerializeField] float _TimePerPush = 4;
	[SerializeField] int _StepCount = 4;
	[SerializeField] AnimationCurve _PushMoveCurve;
	[Space]
	[SerializeField] Transform _PushStartPosition;
	[SerializeField] float _DistancePerPikmin = 0.5f;
	List<PikminAI> _AttachedPiki = new(); // All the Pikmin currently pushing at the push point

	CarryText _CarryText;

	List<PushPoint> _PushPoints = new();
	Transform _Transform;

	public Action _OnPush { get; set; }

	class PushPoint
	{
		public Transform _Transform;
		public PikminAI _Pusher;

		public PushPoint(Transform t, PikminAI p)
		{
			_Transform = t;
			_Pusher = p;
		}
	}

	#region Unity Functions

	void Awake()
	{
		_Transform = transform;

		GameObject carryText = Instantiate(_CarryTextPrefab, transform.position, Quaternion.identity);
		_CarryText = carryText.GetComponent<CarryText>();
		_CarryText._FollowTarget = _Transform;
		_CarryText._HeightOffset = 7.5f;

		for (int i = 0; i < _PikminToPush; i++)
		{
			GameObject obj = new("push_point_" + i) { transform = { parent = _PushStartPosition, position = _PushStartPosition.position + _DistancePerPikmin * i * _PushStartPosition.right } };
			_PushPoints.Add(new(obj.transform, null));
		}
	}

	void OnDrawGizmos()
	{
		Gizmos.DrawSphere(transform.position, 2.5f);

		for (int i = 1; i < _StepCount; i++)
		{
			Vector3 target = Vector3.Lerp(transform.position, _EndPosition.position, (float)i / _StepCount);
			Gizmos.DrawSphere(target, 2.5f);
		}

		Gizmos.DrawSphere(_EndPosition.position, 2.5f);

		for (int i = 0; i < _PikminToPush; i++)
		{
			Gizmos.DrawCube(_PushStartPosition.position + _PushStartPosition.right * i * _DistancePerPikmin, Vector3.one * 0.5f);
		}
	}

	#endregion

	#region IEnumerators

	bool _Pushed;

	IEnumerator IE_PushingSequence()
	{
		yield return null;

		_CarryText.Destroy();
		_Pushed = true;

		Vector3 firstOrigin = _Transform.position;
		Vector3 endPosition = _EndPosition.position;
		float t = 0;

		for (int i = 1; i < _StepCount; i++)
		{
			Vector3 target = Vector3.Lerp(firstOrigin, endPosition, (float)i / _StepCount);

			Vector3 originPosition = _Transform.position;

			while (t <= _TimePerPush)
			{
				_Transform.position = Vector3.Lerp(originPosition, target, _PushMoveCurve.Evaluate(t / _TimePerPush));
				t += Time.deltaTime;
				yield return new WaitForEndOfFrame();
			}

			t = 0;
		}

		firstOrigin = _Transform.position;
		t = 0;

		while (t <= _TimePerPush)
		{
			_Transform.position = Vector3.Lerp(firstOrigin, endPosition, _PushMoveCurve.Evaluate(t / _TimePerPush));
			t += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}

		while (_AttachedPiki.Count != 0)
		{
			_AttachedPiki[0].ChangeState(PikminStates.Idle);
		}
	}

	#endregion

	#region Public Functions

	public PikminIntention IntentionType => PikminIntention.Push;

	public bool OnPikminAdded(PikminAI p)
	{
		if (_AttachedPiki.Count >= _PikminToPush || _Pushed)
		{
			return false;
		}

		p.ChangeState(PikminStates.Push);
		return true;
	}

	public void OnPikminLeave(PikminAI p)
	{
		// Clear out the appropriate push point
		foreach (PushPoint point in _PushPoints.Where(point => point._Pusher == p))
		{
			point._Pusher = null;
			break;
		}

		if (_AttachedPiki.Contains(p))
		{
			_AttachedPiki.Remove(p);
		}

		_CarryText.UpdateColor(_AttachedPiki);
		_CarryText.SetText(_AttachedPiki.Count, _PikminToPush);
	}

	public void OnPikminReady(PikminAI p)
	{
		if (_Pushed)
		{
			p.ChangeState(PikminStates.Idle);
			return;
		}

		_AttachedPiki.Add(p);

		_CarryText.UpdateColor(_AttachedPiki);
		_CarryText.SetText(_AttachedPiki.Count, _PikminToPush);

		if (_AttachedPiki.Count < _PikminToPush)
		{
			return;
		}

		StartCoroutine(IE_PushingSequence());
		_OnPush?.Invoke();
	}

	public Vector3 GetPushPosition(PikminAI ai)
	{
		// Check if the AI is already in the pushing list
		PushPoint usedPushPos = _PushPoints.FirstOrDefault(v => v._Pusher == ai);

		if (usedPushPos != null)
		{
			return usedPushPos._Transform.position;
		}

		// Find the closest available point
		PushPoint closest = _PushPoints
		                    .Where(p => p._Pusher == null)
		                    .OrderBy(p => MathUtil.DistanceTo(ai.transform.position, p._Transform.position))
		                    .FirstOrDefault();

		if (closest == null)
		{
			closest = _PushPoints
			          .Where(p => p._Pusher != null && p._Pusher._CurrentState != PikminStates.Push)
			          .OrderBy(p => MathUtil.DistanceTo(ai.transform.position, p._Transform.position))
			          .FirstOrDefault();

			if (closest == null)
			{
				ai.ChangeState(PikminStates.Idle);
				return Vector3.zero;
			}
		}

		closest._Pusher = ai;
		return closest._Transform.position;
	}

	public bool IsPikminSpotAvailable()
	{
		return _AttachedPiki.Count < _PikminToPush && !_Pushed;
	}

	public Vector3 GetMovementDirection()
	{
		return MathUtil.DirectionFromTo(_Transform.position, _EndPosition.position);
	}

	#endregion
}
