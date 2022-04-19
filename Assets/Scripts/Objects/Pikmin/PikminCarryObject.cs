using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(Seeker), typeof(Rigidbody))]
public class PikminCarryObject : MonoBehaviour, IPikminCarry
{
	[Header("References")]
	[SerializeField] private GameObject _CarryTextPrefab;

	[Header("Settings")]
	[SerializeField, Tooltip("Number on the left signifies the minimum amount to carry, number on the right is the maximum.")]
	private Vector2Int _CarryMinMax = new Vector2Int(1, 2);

	[SerializeField] private float _CarryCircleRadius = 1;
	[SerializeField] private Vector3 _CarryCircleOffset = Vector3.zero;

	[Space()]
	[SerializeField] private float _AccelerationSpeed = 2;
	[SerializeField] private float _MaxSpeed = 2;

	[Space()]
	[SerializeField] private int _AmountToCreate = 1;

	[Header("Debugging")]
	[SerializeField] private float _CurrentMoveSpeed = 0;
	[SerializeField] private Vector3 _MoveVector = Vector3.zero;
	private Vector3 _NextDestination = Vector3.zero;

	private CarryText _CarryText = null;
	private Onion _MainOnion = null;
	private Rigidbody _Rigidbody = null;
	private Seeker _Pathfinder = null;

	private Path _CurrentPath = null;
	private int _CurrentPathPosIdx = 0;

	private List<PikminAI> _CarryingPikmin = new List<PikminAI>();
	private bool _IsBeingCarried = false;

	private void Awake()
	{
		_MainOnion = GameObject.FindGameObjectWithTag("DebugOnion").GetComponent<Onion>();

		_Rigidbody = GetComponent<Rigidbody>();
		_Pathfinder = GetComponent<Seeker>();

		GameObject carryText = Instantiate(_CarryTextPrefab, transform.position, Quaternion.identity);
		_CarryText = carryText.GetComponent<CarryText>();
		_CarryText._FollowTarget = transform;

		InvokeRepeating(nameof(CheckPath), 0, 1f);
	}

	private void CheckPath()
	{
		if (!_IsBeingCarried || GameManager._IsPaused)
		{
			return;
		}

		_Pathfinder.StartPath(transform.position, _MainOnion._CarryEndpoint.position, OnPathCalculated);
	}

	private void Update()
	{
		if (!_IsBeingCarried || GameManager._IsPaused)
		{
			return;
		}

		if (MathUtil.DistanceTo(transform.position, _MainOnion._CarryEndpoint.position, false) < 0.25f)
		{
			while (_CarryingPikmin.Count > 0)
			{
				_CarryingPikmin[0].ChangeState(PikminStates.Idle);
			}

			_MainOnion.EnterOnion(_AmountToCreate, PikminColour.Red);

			Destroy(_CarryText.gameObject);
			Destroy(gameObject);
		}
		else if (MathUtil.DistanceTo(transform.position, _CurrentPath.vectorPath[_CurrentPathPosIdx], false) < 0.24f)
		{
			_CurrentPathPosIdx++;
			if (_CurrentPathPosIdx >= _CurrentPath.vectorPath.Count)
			{
				// we've reached the final point and yet still not at the onion, recalculate!
				_Pathfinder.StartPath(transform.position, _MainOnion._CarryEndpoint.position, OnPathCalculated);
			}
			else
			{
				_NextDestination = _CurrentPath.vectorPath[_CurrentPathPosIdx];
			}
		}
	}

	private void FixedUpdate()
	{
		if (GameManager._IsPaused)
		{
			return;
		}

		// Set the position and rotations of the Pikmin
		for (int i = 0; i < _CarryingPikmin.Count; i++)
		{
			PikminAI pikminAi = _CarryingPikmin[i];
			Vector3 delta = transform.position - pikminAi.transform.position;
			delta.y = 0;
			delta.Normalize();

			pikminAi.transform.SetPositionAndRotation(
				GetPikminPosition(_CarryingPikmin.Count, i),
				Quaternion.LookRotation(delta));
		}

		if (_IsBeingCarried)
		{
			MoveTowards(_NextDestination);
		}

		float storedY = _Rigidbody.velocity.y;
		_Rigidbody.velocity = _MoveVector;
		_MoveVector = new Vector3(0, storedY, 0);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		for (int i = 0; i < _CarryMinMax.x; i++)
		{
			Gizmos.DrawWireSphere(GetPikminPosition(_CarryMinMax.x, i), 0.1f);
		}

		Gizmos.color = Color.blue;
		for (int i = 0; i < _CarryMinMax.y; i++)
		{
			Gizmos.DrawWireSphere(GetPikminPosition(_CarryMinMax.y, i), 0.15f);
		}
	}

	private Vector3 GetPikminPosition(int maxPikmin, int pikminIdx)
	{
		return transform.position + _CarryCircleOffset + (MathUtil.XZToXYZ(MathUtil.PositionInUnit(maxPikmin, pikminIdx)) * _CarryCircleRadius);
	}

	private void MoveTowards(Vector3 position)
	{
		Vector3 delta = position - transform.position;
		delta.y = 0;
		delta.Normalize();

		_CurrentMoveSpeed = Mathf.SmoothStep(_CurrentMoveSpeed, _MaxSpeed, _AccelerationSpeed * Time.fixedDeltaTime);

		Vector3 newVelocity = delta * _CurrentMoveSpeed;
		newVelocity.y = _Rigidbody.velocity.y;
		_MoveVector = newVelocity;
	}

	private void OnPathCalculated(Path p)
	{
		_CurrentPath = p;
		_CurrentPathPosIdx = 0;
		if (_CurrentPath.error)
		{
			Debug.Log(_CurrentPath.errorLog);
		}
	}

	private void UpdateText()
	{
		_CarryText.HandleColor(_CarryingPikmin);
		_CarryText.SetText(_CarryingPikmin.Count, _CarryMinMax.x);
		if (_CarryingPikmin.Count == 0)
		{
			_CarryText.FadeOut();
		}
		else if (_CarryingPikmin.Count == 1)
		{
			_CarryText.FadeIn();
		}
	}

	#region Pikmin Carry Implementation
	public PikminIntention IntentionType => PikminIntention.Carry;

	public void OnCarryStart(PikminAI p)
	{
		// If we have too many Pikmin
		if (_CarryingPikmin.Count >= _CarryMinMax.y)
		{
			p.ChangeState(PikminStates.Idle);
			return;
		}

		p.LatchOnto(transform);
		p.ChangeState(PikminStates.Carrying);
		_CarryingPikmin.Add(p);

		if (_CarryingPikmin.Count >= _CarryMinMax.x)
		{
			// Enable AI
			Path path = _Pathfinder.StartPath(transform.position, _MainOnion._CarryEndpoint.position, OnPathCalculated);
			path.BlockUntilCalculated();
			_IsBeingCarried = true;
		}

		UpdateText();
	}

	public void OnCarryLeave(PikminAI p)
	{
		_CarryingPikmin.Remove(p);
		p.LatchOnto(null);

		// If we don't have enough Pikmin
		if (_CarryingPikmin.Count < _CarryMinMax.x && _IsBeingCarried)
		{
			// Disable AI
			_IsBeingCarried = false;
		}

		UpdateText();
	}

	public bool PikminSpotAvailable()
	{
		return _CarryingPikmin.Count < _CarryMinMax.y;
	}
	#endregion
}
