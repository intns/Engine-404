using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PikminCarryObject : MonoBehaviour, IPikminCarry
{
	[Header("References")]
	[SerializeField] GameObject _CarryTextPrefab;
	[SerializeField] AudioClip _CarryNoiseClip;

	[Header("Settings")]
	[SerializeField, Tooltip("Number on the left signifies the minimum amount to carry, number on the right is the maximum.")]
	Vector2Int _CarryMinMax = new Vector2Int(1, 2);

	[SerializeField, Tooltip("How close we have to be to the position to move onto the next position")]
	float _DistanceToNextPosition = 0.5f;

	[SerializeField, Tooltip("How long does it take for the object to be carried after spawn?")]
	float _InvulnTimeAfterSpawn = 1.0f;

	[SerializeField] float _CarryCircleRadius = 1;
	[SerializeField] Vector3 _CarryCircleOffset = Vector3.zero;

	[Space()]
	[SerializeField] float _AccelerationSpeed = 2;
	[SerializeField] float _BaseSpeed = 2;
	[SerializeField] float _SpeedAddedPerPikmin = 0.5f;
	[SerializeField] float _MaxSpeed = 3;

	[Space()]
	[SerializeField] PikminColour _ColourToGenerateFor = PikminColour.Size;
	[SerializeField] int _PikminToProduceMatchColour = 2;
	[SerializeField] int _PikminToProduceNonMatchColour = 1;

	[Header("Debugging")]
	[SerializeField] float _CurrentMoveSpeed = 0;
	[SerializeField] Vector3 _MoveVector = Vector3.zero;
	[SerializeField] Vector3 _TargetPosition;

	CarryText _CarryText = null;
	Onion _TargetOnion = null;
	Rigidbody _Rigidbody = null;
	TEST_Waypoint _CurrentWaypoint = null;
	AudioSource _Source;

	float _CurrentSpeedTarget = 0;

	List<PikminAI> _CarryingPikmin = new List<PikminAI>();
	bool _IsBeingCarried = false;
	bool _ShutdownInProgress = false;

	float _SpawnInvulnTimer = 0.0f;

	public bool IsMoving() => _IsBeingCarried;

	void Awake()
	{
		_Rigidbody = GetComponent<Rigidbody>();
		_Source = GetComponent<AudioSource>();
		_Source.clip = _CarryNoiseClip;

		_CurrentSpeedTarget = _BaseSpeed;

		GameObject carryText = Instantiate(_CarryTextPrefab, transform.position, Quaternion.identity);
		_CarryText = carryText.GetComponent<CarryText>();
		_CarryText._FollowTarget = transform;
	}

	void Update()
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
		else if (!OnionManager.IsAnyOnionActiveInScene)
		{
			return;
		}

		if (!_Source.isPlaying)
		{
			_Source.Play();
		}

		MoveTowards(_TargetPosition);

		if (_CurrentWaypoint._Next == null)
		{
			if (MathUtil.DistanceTo(transform.position, _CurrentWaypoint.transform.position, false) < _DistanceToNextPosition)
			{
				OnionSuckProcedure();
			}
		}
		else
		{
			float distToWp = MathUtil.DistanceTo(transform.position, _CurrentWaypoint.transform.position, false);

			const float K_MIN_SMOOTH_DIST = 10.0f;
			if (distToWp <= K_MIN_SMOOTH_DIST)
			{
				_CurrentWaypoint = _CurrentWaypoint._Next;
				_TargetPosition = _CurrentWaypoint.transform.position;
			}

			/*
			const float K_MAX_SMOOTH_DIST = 25;
			else if (distToWp <= K_MAX_SMOOTH_DIST)
			{
				float t = Mathf.InverseLerp(K_MIN_SMOOTH_DIST, K_MAX_SMOOTH_DIST, distToWp);
				Debug.Log(t);

				_TargetPosition = Vector3.Lerp(_CurrentWaypoint.transform.position, _CurrentWaypoint._Next.transform.position, t);
			}*/
		}
	}

	private void OnionSuckProcedure()
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
			_TargetOnion.StartSuction(gameObject, _PikminToProduceMatchColour);
		}
		else
		{
			_TargetOnion.StartSuction(gameObject, _PikminToProduceNonMatchColour);
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
	}

	void FixedUpdate()
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

	void OnDrawGizmosSelected()
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

	Vector3 GetPikminPosition(int maxPikmin, int pikminIdx)
	{
		return transform.position + _CarryCircleOffset + (MathUtil.XZToXYZ(MathUtil.PositionInUnit(maxPikmin, pikminIdx)) * _CarryCircleRadius);
	}

	void MoveTowards(Vector3 position)
	{
		_CurrentMoveSpeed = Mathf.SmoothStep(_CurrentMoveSpeed, _CurrentSpeedTarget, _AccelerationSpeed * Time.deltaTime);

		Vector3 newVelocity = MathUtil.DirectionFromTo(transform.position, position) * _CurrentMoveSpeed;
		newVelocity.y = _Rigidbody.velocity.y;
		_MoveVector = newVelocity;
	}

	void UpdateText()
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
		p.ChangeState(PikminStates.Carry);
		_CarryingPikmin.Add(p);

		// Circle offset + ((circle pos with qualtiy 'carry max' at the index 'pikmin count') * circle size)
		_CarryingPikmin[^1].Latch_SetOffset(
			_CarryCircleOffset
			+ MathUtil.XZToXYZ(MathUtil.PositionInUnit(_CarryMinMax.y, _CarryingPikmin.IndexOf(p)))
			* _CarryCircleRadius);

		if (_CarryingPikmin.Count >= _CarryMinMax.x)
		{
			if (OnionManager.IsAnyOnionActiveInScene)
			{
				PikminColour colour = GameUtil.GetMajorityColour(_CarryingPikmin);
				_TargetOnion = OnionManager.GetOnionOfColour(colour);

				Debug.Assert(_TargetOnion != null, $"Target Onion ({colour}) not found!");
				Debug.Assert(_TargetOnion.OnionActive == true, $"Target Onion ({colour}) not active!");
			}

			_CurrentWaypoint = WayPointManager._Instance.GetWaypointTowards(transform.position);
			_TargetPosition = _CurrentWaypoint.transform.position;
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

	public bool IsPikminSpotAvailable()
	{
		return _SpawnInvulnTimer >= _InvulnTimeAfterSpawn && _CarryingPikmin.Count < _CarryMinMax.y;
	}
	#endregion
}
