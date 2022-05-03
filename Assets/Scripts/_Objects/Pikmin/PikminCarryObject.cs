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

	[SerializeField, Tooltip("How close we have to be to the position to move onto the next position")]
	private float _DistanceToNextPosition = 0.5f;

	[SerializeField] private float _CarryCircleRadius = 1;
	[SerializeField] private Vector3 _CarryCircleOffset = Vector3.zero;

	[Space()]
	[SerializeField] private float _AccelerationSpeed = 2;
	[SerializeField] private float _BaseSpeed = 2;
	[SerializeField] private float _SpeedAddedPerPikmin = 0.5f;
	[SerializeField] private float _MaxSpeed = 3;

	[Space()]
	[SerializeField] private PikminColour _ColourToGenerateFor = PikminColour.Size;
	[SerializeField] private int _PikminToProduceMatchColour = 2;
	[SerializeField] private int _PikminToProduceNonMatchColour = 1;

	[Header("Debugging")]
	[SerializeField] private float _CurrentMoveSpeed = 0;
	[SerializeField] private Vector3 _MoveVector = Vector3.zero;
	[SerializeField] private Vector3 _NextDestination = Vector3.zero;

	private CarryText _CarryText = null;
	private Onion _MainOnion = null;
	private Rigidbody _Rigidbody = null;
	private Seeker _Pathfinder = null;

	private float _CurrentSpeedTarget = 0;

	private Path _CurrentPath = null;
	private int _CurrentPathPosIdx = 0;

	private List<PikminAI> _CarryingPikmin = new List<PikminAI>();
	private bool _IsBeingCarried = false;
	private bool _ShutdownInProgress = false;

	private void Awake()
	{
		_MainOnion = GameObject.FindGameObjectWithTag("DebugOnion").GetComponent<Onion>();

		_Rigidbody = GetComponent<Rigidbody>();
		_Pathfinder = GetComponent<Seeker>();

		_CurrentSpeedTarget = _BaseSpeed;

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

		MoveTowards(_NextDestination);

		if (MathUtil.DistanceTo(transform.position, _MainOnion._CarryEndpoint.position, false) < _DistanceToNextPosition)
		{
			_ShutdownInProgress = true;
			PikminColour targetColour = GameUtil.GetMajorityColour(_CarryingPikmin);
			while (_CarryingPikmin.Count > 0)
			{
				_CarryingPikmin[0].ChangeState(PikminStates.Idle);
			}

			if (targetColour == _ColourToGenerateFor
				|| _ColourToGenerateFor == PikminColour.Size)
			{
				_MainOnion.AddSproutsToSpawn(_PikminToProduceMatchColour, targetColour);
			}
			else
			{
				_MainOnion.AddSproutsToSpawn(_PikminToProduceNonMatchColour, targetColour);
			}

			Destroy(_CarryText.gameObject);
			Destroy(gameObject);
		}
		else if (_CurrentPath != null && _CurrentPath.vectorPath.Count != 0)
		{
			if (MathUtil.DistanceTo(transform.position, _CurrentPath.vectorPath[_CurrentPathPosIdx], false) < _DistanceToNextPosition)
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
		else
		{
			_NextDestination = transform.position - _MainOnion._CarryEndpoint.position;
		}
	}

	private void FixedUpdate()
	{
		if (GameManager._IsPaused)
		{
			return;
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

		_CurrentMoveSpeed = Mathf.SmoothStep(_CurrentMoveSpeed, _CurrentSpeedTarget, _AccelerationSpeed * Time.deltaTime);

		Vector3 newVelocity = delta * _CurrentMoveSpeed;
		newVelocity.y = _Rigidbody.velocity.y;
		_MoveVector = newVelocity;
	}

	private void OnPathCalculated(Path p)
	{
		// If we're about to delete the object, no point in updating it
		if (_ShutdownInProgress) { return; }

		_CurrentPath = p;
		_CurrentPathPosIdx = 0;
		if (_CurrentPath.error)
		{
			Debug.Log(_CurrentPath.errorLog);
		}
	}

	private void UpdateText()
	{
		// If we're about to delete the object, no point in updating it
		if (_ShutdownInProgress) { return; }

		_CarryText.UpdateColor(_CarryingPikmin);
		_CarryText.SetText(_CarryingPikmin.Count, _CarryMinMax.x);
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
			_IsBeingCarried = true;

			_CurrentSpeedTarget += _SpeedAddedPerPikmin;
			if (_CurrentSpeedTarget > _MaxSpeed)
			{
				_CurrentSpeedTarget = _MaxSpeed;
			}

			// Set the position and rotations of the Pikmin
			for (int i = 0; i < _CarryingPikmin.Count; i++)
			{
				PikminAI pikminAi = _CarryingPikmin[i];
				pikminAi._LatchedOffset = _CarryCircleOffset + MathUtil.XZToXYZ(MathUtil.PositionInUnit(_CarryingPikmin.Count, i)) * _CarryCircleRadius;
			}
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

			_CurrentSpeedTarget -= _SpeedAddedPerPikmin;
			if (_CurrentSpeedTarget < _BaseSpeed)
			{
				// SHOULDN'T be needed, but just in case
				_CurrentSpeedTarget = _BaseSpeed;
			}

			// Set the position and rotations of the Pikmin
			for (int i = 0; i < _CarryingPikmin.Count; i++)
			{
				PikminAI pikminAi = _CarryingPikmin[i];
				pikminAi._LatchedOffset = transform.position - GetPikminPosition(_CarryingPikmin.Count, i);
			}
		}

		UpdateText();
	}

	public bool PikminSpotAvailable()
	{
		return _CarryingPikmin.Count < _CarryMinMax.y;
	}
	#endregion
}
