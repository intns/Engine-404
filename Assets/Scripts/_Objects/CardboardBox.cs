using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardboardBox : MonoBehaviour, IPikminPush
{
	[Header("Components")]
	[SerializeField] Transform _EndPosition = null;
	[SerializeField] GameObject _CarryTextPrefab = null;
	Transform _Transform = null;

	[Header("Settings")]
	[SerializeField] int _PikminToPush = 10;
	[SerializeField] float _TimePerPush = 4;
	[SerializeField] int _StepCount = 4;

	CarryText _CarryText = null;
	List<PikminAI> _Attached = new List<PikminAI>();

	#region Unity Functions
	private void Awake()
	{
		_Transform = transform;

		GameObject carryText = Instantiate(_CarryTextPrefab, transform.position, Quaternion.identity);
		_CarryText = carryText.GetComponent<CarryText>();
		_CarryText._FollowTarget = _Transform;
		_CarryText._HeightOffset = 7.5f;
	}

	private void OnDrawGizmos()
	{
		Gizmos.DrawSphere(transform.position, 2.5f);

		for (int i = 1; i < _StepCount; i++)
		{
			Vector3 target = Vector3.Lerp(transform.position, _EndPosition.position, (float)i / _StepCount);
			Gizmos.DrawSphere(target, 2.5f);
		}

		Gizmos.DrawSphere(_EndPosition.position, 2.5f);
	}
	#endregion

	#region IEnumerators
	bool _Pushed = false;
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
				_Transform.position = Vector3.Lerp(originPosition, target, MathUtil.EaseOut3(t / _TimePerPush));
				t += Time.deltaTime;
				yield return new WaitForEndOfFrame();
			}

			t = 0;
		}

		firstOrigin = _Transform.position;
		t = 0;
		while (t <= _TimePerPush)
		{
			_Transform.position = Vector3.Lerp(firstOrigin, endPosition, MathUtil.EaseOut3(t / _TimePerPush));
			t += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}

		while (_Attached.Count != 0)
		{
			_Attached[0].ChangeState(PikminStates.Idle);
		}
	}
	#endregion

	#region Utility Functions
	#endregion

	#region Public Functions

	public PikminIntention IntentionType => PikminIntention.Push;

	public void OnPikminAdded(PikminAI p)
	{
		p.LatchOnto(transform);
		p.ChangeState(PikminStates.Push);
		_Attached.Add(p);

		_CarryText.UpdateColor(_Attached);
		_CarryText.SetText(_Attached.Count, _PikminToPush);

		if (_Attached.Count >= _PikminToPush)
		{
			StartCoroutine(IE_PushingSequence());
		}
	}

	public void OnPikminLeave(PikminAI p)
	{
		p.LatchOnto(null);
		_Attached.Remove(p);

		_CarryText.UpdateColor(_Attached);
		_CarryText.SetText(_Attached.Count, _PikminToPush);
	}

	public bool PikminSpotAvailable()
	{
		return _Attached.Count < _PikminToPush && !_Pushed;
	}
	#endregion
}
