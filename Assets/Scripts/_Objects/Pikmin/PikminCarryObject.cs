using System.Collections.Generic;
using UnityEngine;

public enum CarryObjectType
{
	Normal,
	Treasure,
}

[RequireComponent(typeof(Rigidbody))]
public class PikminCarryObject : MonoBehaviour, IPikminCarry
{
	[Header("References")]
	[SerializeField] GameObject _CarryTextPrefab;
	[SerializeField] AudioClip _CarryNoiseClip;

	[Header("Settings")]
	[SerializeField]
	[Tooltip("Minimum / Maximum carrying capacity")]
	Vector2Int _CarryMinMax = new(1, 2);

	[SerializeField]
	[Tooltip("How close we have to be to the position to move onto the next position")]
	float _DistanceToNextPosition = 0.5f;

	[SerializeField]
	[Tooltip("How long does it take for the object to be carried after spawn?")]
	float _InvulnTimeAfterSpawn = 1.0f;

	[SerializeField]
	[Tooltip("What type of object is this? (Normal -> Onion | Treasure -> Ship)")]
	CarryObjectType _ObjectType = CarryObjectType.Normal;

	[SerializeField] float _CarryCircleRadius = 1;
	[SerializeField] Vector3 _CarryCircleOffset = Vector3.zero;

	[Space]
	[SerializeField] float _AccelerationSpeed = 2;
	[SerializeField] float _BaseSpeed = 2;
	[SerializeField] float _MaxSpeed = 3;

	[Space]
	[SerializeField] PikminColour _ColourToGenerateFor = PikminColour.Size;
	[SerializeField] int _PikminToProduceMatchColour = 2;
	[SerializeField] int _PikminToProduceNonMatchColour = 1;

	[Space]
	[SerializeField] LayerMask _MapMask;

	[Header("Debugging")]
	[SerializeField] float _CurrentMoveSpeed;
	[SerializeField] Vector3 _MoveVector = Vector3.zero;
	[SerializeField] Vector3 _TargetPosition;

	List<PikminAI> _CarryingPikmin = new();
	ICarryObjectAbsorb _CarryTarget;

	CarryText _CarryText;

	float _CurrentSpeedTarget;
	bool _IsBeingCarried;
	bool _IsDestroyReady;
	Queue<Waypoint> _JourneyWaypoints = new();
	Rigidbody _Rigidbody;
	AudioSource _Source;

	float _SpawnInvulnTimer;
	float _SpeedAddedPerPikmin;

	void Awake()
	{
		_Rigidbody = GetComponent<Rigidbody>();
		_Source = GetComponent<AudioSource>();
		_Source.clip = _CarryNoiseClip;

		_CurrentSpeedTarget = _BaseSpeed;

		GameObject carryText = Instantiate(_CarryTextPrefab, transform.position, Quaternion.identity);
		_CarryText = carryText.GetComponent<CarryText>();
		_CarryText._FollowTarget = transform;

		_SpeedAddedPerPikmin = (_MaxSpeed - _BaseSpeed) / (_CarryMinMax.y - _CarryMinMax.x);
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

		for (int i = 0; i < _CarryingPikmin.Count; i++)
		{
			_CarryingPikmin[i].Latch_SetOffset(GetPikminPosition(_CarryMinMax.y, i));
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

		MoveTowards(_TargetPosition);

		if (_MoveVector != Vector3.zero)
		{
			Quaternion targetRotation = Quaternion.LookRotation(_MoveVector.normalized, Vector3.up);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1.0f * Time.deltaTime);
		}

		if (_JourneyWaypoints.Count == 0)
		{
			Debug.LogError("No pathfinding journey found!");
			return;
		}

		Waypoint waypoint = _JourneyWaypoints.Peek();

		if (waypoint._Next == null)
		{
			if (MathUtil.DistanceTo(transform.position, waypoint.transform.position, false) >= _DistanceToNextPosition)
			{
				return;
			}

			AbsorbCarryObject();
		}
		else
		{
			float distToWp = MathUtil.DistanceTo(transform.position, waypoint.transform.position, false);

			if (distToWp >= 25.0f)
			{
				return;
			}

			MoveToNextWaypoint();
		}
	}

	void FixedUpdate()
	{
		if (GameManager.IsPaused)
		{
			_MoveVector = Vector3.zero;
			_Rigidbody.velocity = Vector3.zero;
			return;
		}

		RotateUpwards();

		float storedY = _Rigidbody.velocity.y;
		_Rigidbody.velocity = _MoveVector;
		_MoveVector = new(0, storedY, 0);
	}

	void OnCollisionStay(Collision other)
	{
		if (other.transform.CompareTag("Player"))
		{
			Vector3 toOther = other.transform.position - transform.position;
			Vector3 perpendicularOffset = new(-toOther.z, 0f, toOther.x);

			Player._Instance._MovementController._ImpulseVelocity = (toOther + perpendicularOffset).normalized * 0.1f;
		}
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;

		for (int i = 0; i < _CarryMinMax.x; i++)
		{
			Gizmos.DrawWireSphere(transform.position + GetPikminPosition(_CarryMinMax.x, i), 0.1f);
		}

		Gizmos.color = Color.blue;

		for (int i = 0; i < _CarryMinMax.y; i++)
		{
			Gizmos.DrawWireSphere(transform.position + GetPikminPosition(_CarryMinMax.y, i), 0.15f);
		}
	}

	public bool IsMoving()
	{
		return _IsBeingCarried;
	}

	void MoveToNextWaypoint()
	{
		_JourneyWaypoints.Dequeue();

		if (_JourneyWaypoints.Count > 0)
		{
			_TargetPosition = _JourneyWaypoints.Peek().transform.position;
		}
	}

	void AbsorbCarryObject()
	{
		_IsDestroyReady = true;

		_CarryTarget.StartAbsorb(this);

		while (_CarryingPikmin.Count > 0)
		{
			_CarryingPikmin[0].ChangeState(PikminStates.Idle);
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

	public int GetPikminSpawnAmount()
	{
		PikminColour targetColour = GameUtil.GetMajorityColour(_CarryingPikmin);

		return targetColour == _ColourToGenerateFor && _ColourToGenerateFor != PikminColour.Size
			? _PikminToProduceMatchColour
			: _PikminToProduceNonMatchColour;
	}

	Vector3 GetPikminPosition(int maxPikmin, int pikminIdx)
	{
		return _CarryCircleOffset
		       + MathUtil.XZToXYZ(MathUtil.PositionInUnit(maxPikmin, pikminIdx, -transform.eulerAngles.y * Mathf.Deg2Rad)) * _CarryCircleRadius;
	}

	void MoveTowards(Vector3 position)
	{
		_CurrentMoveSpeed = Mathf.SmoothStep(_CurrentMoveSpeed, _CurrentSpeedTarget, _AccelerationSpeed * Time.deltaTime);

		Vector3 newVelocity = MathUtil.DirectionFromTo(transform.position, position) * _CurrentMoveSpeed;
		newVelocity.y = _Rigidbody.velocity.y;
		_MoveVector = newVelocity;
	}

	void RotateUpwards()
	{
		if (!Physics.Raycast(
			    transform.position,
			    Vector3.down,
			    out RaycastHit hit,
			    2.5f,
			    _MapMask,
			    QueryTriggerInteraction.Ignore
		    ))
		{
			return;
		}

		Vector3 desiredUpDirection = hit.normal;
		float dotProduct = Vector3.Dot(transform.up, desiredUpDirection);

		switch (dotProduct)
		{
			// If the dot product is close to 1, the object is already upright.
			case > 0.99f: return;

			// If the dot product is close to -1, the object is upside down and needs to be rotated.
			case < -0.9f:
				// Invert the desired up direction to mirror according to the polygon normal.
				desiredUpDirection = Vector3.ProjectOnPlane(-transform.up, hit.normal).normalized;
				break;
		}

		Vector3 torqueDirection = Vector3.Cross(transform.up, desiredUpDirection);
		float torqueMagnitude = Mathf.Asin(torqueDirection.magnitude) * _Rigidbody.mass * Physics.gravity.magnitude;

		if (torqueMagnitude < 0.005f)
		{
			return;
		}

		Quaternion targetRotation = Quaternion.FromToRotation(transform.up, desiredUpDirection) * transform.rotation;
		_Rigidbody.MoveRotation(Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 6.5f));
	}

	void UpdateUI()
	{
		// If we're about to delete the object, no point in updating it
		if (_IsDestroyReady)
		{
			return;
		}

		_CarryText.UpdateColor(_CarryingPikmin);
		_CarryText.SetText(_CarryingPikmin.Count, _CarryMinMax.x);
	}

	#region Pikmin Carry Implementation

	public PikminIntention IntentionType => PikminIntention.Carry;

	/// <summary>
	///   Searches for the onion and sets target position if enough Pikmin are carrying, increases speed based on number of
	///   carriers.
	/// </summary>
	/// <param name="p">The PikminAI object being carried.</param>
	public void OnCarryStart(PikminAI p)
	{
		if (_CarryingPikmin.Count >= _CarryMinMax.y)
		{
			p.ChangeState(PikminStates.Idle);
			return;
		}

		p.LatchOnto(transform);
		p.ChangeState(PikminStates.Carry);
		_CarryingPikmin.Add(p);

		if (_CarryingPikmin.Count >= _CarryMinMax.x && !_IsBeingCarried)
		{
			CarryObjectType carryObjectType = _ObjectType;
			WaypointType destinationType;
			Vector3 destinationPosition;

			switch (carryObjectType)
			{
				case CarryObjectType.Normal when OnionManager.IsAnyOnionActiveInScene:
				{
					Onion onion = OnionManager.GetOnionOfColour(GameUtil.GetMajorityColour(_CarryingPikmin));
					_CarryTarget = onion;

					destinationType = WaypointType.Onion;
					destinationPosition = onion.transform.position;
					break;
				}

				case CarryObjectType.Treasure:
					_CarryTarget = Ship._Instance;

					destinationType = WaypointType.Ship;
					destinationPosition = Ship._Instance.GetSuctionPosition();
					break;

				case CarryObjectType.Normal:
					// TODO: No destination is found! Find nearest waypoint and circle it until path becomes available
					return;
				default:
					Debug.LogError("No destination found!");
					return;
			}

			Waypoint start = WaypointManager._Instance.GetClosestWaypoint(transform.position);
			Waypoint destination = WaypointManager._Instance.GetClosestWaypoint(destinationPosition);

			_JourneyWaypoints = WaypointManager._Instance.FindBestDestination(start, destination, destinationType);

			if (_JourneyWaypoints.Count > 0)
			{
				_TargetPosition = _JourneyWaypoints.Peek().transform.position;
				_IsBeingCarried = true;
				_CurrentSpeedTarget = Mathf.Min(_CurrentSpeedTarget + _SpeedAddedPerPikmin, _MaxSpeed);
			}
			else
			{
				Debug.LogError("No waypoints found for the journey!");
				return;
			}
		}

		UpdateUI();
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
			_CurrentSpeedTarget = Mathf.Max(_CurrentSpeedTarget, _BaseSpeed);
		}

		UpdateUI();
	}

	public bool IsPikminSpotAvailable()
	{
		return _SpawnInvulnTimer >= _InvulnTimeAfterSpawn && _CarryingPikmin.Count < _CarryMinMax.y;
	}

	#endregion
}
