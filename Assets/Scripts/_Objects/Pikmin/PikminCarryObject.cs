using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(Seeker), typeof(Rigidbody))]
public class PikminCarryObject : MonoBehaviour, IPikminCarry
{
	[Header("References")]
	[SerializeField] private GameObject _CarryTextPrefab;
	[SerializeField] private AudioClip _CarryNoiseClip;

	[Header("Settings")]
	[SerializeField, Tooltip("Number on the left signifies the minimum amount to carry, number on the right is the maximum.")]
	private Vector2Int _CarryMinMax = new Vector2Int(1, 2);

	[SerializeField, Tooltip("How close we have to be to the position to move onto the next position")]
	private float _DistanceToNextPosition = 0.5f;

	[SerializeField, Tooltip("How long does it take for the object to be carried after spawn?")]
	private float _InvulnTimeAfterSpawn = 1.0f;

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
	[SerializeField] Vector3 _TargetPosition;

	private CarryText _CarryText = null;
	private Onion _TargetOnion = null;
	private Rigidbody _Rigidbody = null;
	private Seeker _Pathfinder = null;
	AudioSource _Source;

	private float _CurrentSpeedTarget = 0;

	private Path _CurrentPath = null;
	private int _CurrentPathPosIdx = 0;

	private List<PikminAI> _CarryingPikmin = new List<PikminAI>();
	private bool _IsBeingCarried = false;
	private bool _ShutdownInProgress = false;
	Vector3 _SpawnPosition = Vector3.zero;

	private float _SpawnInvulnTimer = 0.0f;

	public bool IsMoving() => _IsBeingCarried;

	private void Awake()
	{
		_Rigidbody = GetComponent<Rigidbody>();
		_Pathfinder = GetComponent<Seeker>();
		_Source = GetComponent<AudioSource>();
		_Source.clip = _CarryNoiseClip;

		_CurrentSpeedTarget = _BaseSpeed;

		GameObject carryText = Instantiate(_CarryTextPrefab, transform.position, Quaternion.identity);
		_CarryText = carryText.GetComponent<CarryText>();
		_CarryText._FollowTarget = transform;

		_SpawnPosition = transform.position;

		InvokeRepeating(nameof(CheckPath), 0, 1f);
	}

	private void CheckPath()
	{
		if (!_IsBeingCarried || GameManager.IsPaused)
		{
			return;
		}

		_Pathfinder.StartPath(transform.position, _TargetPosition, OnPathCalculated);
	}

	private void Update()
	{
		if (GameManager.IsPaused)
		{
			if (_Source.isPlaying)
			{
				_Source.Stop();
			}

			return;
		}

		_SpawnInvulnTimer += Time.deltaTime;
		if (_SpawnInvulnTimer < _InvulnTimeAfterSpawn)
		{
			return;
		}

		if (!_IsBeingCarried)
		{
			if (_Source.isPlaying)
			{
				_Source.Stop();
			}

			return;
		}

		if (!_Source.isPlaying)
		{
			_Source.Play();
		}

		MoveTowards(_NextDestination);

		if (MathUtil.DistanceTo(transform.position, _TargetPosition, false) < _DistanceToNextPosition)
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
				_TargetOnion.StartSuction(gameObject, _PikminToProduceMatchColour, targetColour);
			}
			else
			{
				_TargetOnion.StartSuction(gameObject, _PikminToProduceNonMatchColour, targetColour);
			}

			if (_CarryText != null && _CarryText.gameObject != null)
			{
				Destroy(_CarryText.gameObject);
			}

			_Rigidbody.isKinematic = true;

			Collider[] colliders = gameObject.GetComponentsInChildren<Collider>(false);
			foreach (Collider coll in colliders)
			{
				coll.enabled = false;
			}

			enabled = false;
			return;
		}
		else if (_CurrentPath != null && _CurrentPath.vectorPath.Count != 0)
		{
			if (MathUtil.DistanceTo(transform.position, _CurrentPath.vectorPath[_CurrentPathPosIdx], false) < _DistanceToNextPosition)
			{
				_CurrentPathPosIdx++;
				if (_CurrentPathPosIdx >= _CurrentPath.vectorPath.Count)
				{
					// we've reached the final point and yet still not at the onion, recalculate!
					_Pathfinder.StartPath(transform.position, _TargetPosition, OnPathCalculated);
				}
				else
				{
					_NextDestination = _CurrentPath.vectorPath[_CurrentPathPosIdx];
				}
			}
		}
		else
		{
			_NextDestination = (transform.position + _TargetPosition) / 2;
		}
	}

	private void FixedUpdate()
	{
		if (GameManager.IsPaused)
		{
			_MoveVector = Vector3.zero;
			_Rigidbody.velocity = Vector3.zero;
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
		_CurrentMoveSpeed = Mathf.SmoothStep(_CurrentMoveSpeed, _CurrentSpeedTarget, _AccelerationSpeed * Time.deltaTime);

		Vector3 directionToPos = MathUtil.DirectionFromTo(transform.position, position);
		Vector3 newVelocity = directionToPos * _CurrentMoveSpeed;
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
		_CarryingPikmin[^1]._LatchedOffset = _CarryCircleOffset + MathUtil.XZToXYZ(MathUtil.PositionInUnit(_CarryMinMax.y, _CarryingPikmin.Count)) * _CarryCircleRadius;

		if (_CarryingPikmin.Count >= _CarryMinMax.x)
		{
			PikminColour colour = GameUtil.GetMajorityColour(_CarryingPikmin);

			foreach (var onion in Onion._ActiveOnions)
			{
				if (onion.OnionColour == colour)
				{
					_TargetOnion = onion;
					break;
				}
			}

			if (_TargetOnion.OnionActive)
			{
				_TargetPosition = _TargetOnion._CarryEndpoint.position;
			}

			// Enable AI
			Path path = _Pathfinder.StartPath(transform.position, _TargetPosition, OnPathCalculated);
			_IsBeingCarried = true;

			_CurrentSpeedTarget += _SpeedAddedPerPikmin;
			if (_CurrentSpeedTarget > _MaxSpeed)
			{
				_CurrentSpeedTarget = _MaxSpeed;
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
		}

		UpdateText();
	}

	public bool PikminSpotAvailable()
	{
		return _SpawnInvulnTimer >= _InvulnTimeAfterSpawn && _CarryingPikmin.Count < _CarryMinMax.y;
	}
	#endregion
}
