using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PikminCarryObject : MonoBehaviour, IPikminCarry
{
	[Header("References")]
	[SerializeField] GameObject _CarryTextPrefab;
	[SerializeField] AudioClip _CarryNoiseClip;

	[Header("Settings")]
	[SerializeField, Tooltip("Minimum / Maximum carrying capacity")]
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

	[Space()]

	[SerializeField] LayerMask _MapMask;

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

		_MapMask.value |= 1 << gameObject.layer;
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

		RotateUpwards();

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

	void RotateUpwards()
	{
		if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 2.5f, _MapMask, QueryTriggerInteraction.Ignore))
		{
			return;
		}

		// Determine the desired up direction based on the surface normal.
		Vector3 desiredUpDirection = hit.normal;

		// Calculate the dot product of the current up direction and the desired up direction.
		float dotProduct = Vector3.Dot(transform.up, desiredUpDirection);

		// If the dot product is close to 1, the object is already upright.
		if (dotProduct > 0.99f)
		{
			return;
		}

		// If the dot product is close to -1, the object is upside down and needs to be rotated.
		if (dotProduct < -0.9f)
		{
			// Invert the desired up direction to mirror according to the polygon normal.
			desiredUpDirection = Vector3.ProjectOnPlane(-transform.up, hit.normal).normalized;
		}

		Vector3 torqueDirection = Vector3.Cross(transform.up, desiredUpDirection);
		float torqueMagnitude = Mathf.Asin(torqueDirection.magnitude) * _Rigidbody.mass * Physics.gravity.magnitude;

		if (torqueMagnitude < 0.005f)
		{
			return;
		}

		_Rigidbody.AddTorque(torqueMagnitude * torqueDirection.normalized);
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

	void UpdateUI()
	{
		// If we're about to delete the object, no point in updating it
		if (_ShutdownInProgress) { return; }

		_CarryText.UpdateColor(_CarryingPikmin);
		_CarryText.SetText(_CarryingPikmin.Count, _CarryMinMax.x);
	}

	#region Pikmin Carry Implementation
	public PikminIntention IntentionType => PikminIntention.Carry;

	/// <summary>
	/// Searches for the onion and sets target position if enough Pikmin are carrying, increases speed based on number of carriers.
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

		for (int i = 0; i < _CarryingPikmin.Count; i++)
		{
			// Calculate the offset for the Pikmin based on its position in the carrying circle.
			Vector3 offset = _CarryCircleOffset + MathUtil.XZToXYZ(MathUtil.PositionInUnit(_CarryingPikmin.Count, i)) * _CarryCircleRadius;
			_CarryingPikmin[i].Latch_SetOffset(offset);
		}

		if (_CarryingPikmin.Count >= _CarryMinMax.x && !_IsBeingCarried)
		{
			// Set the target onion based on the colour of the Pikmin carrying.
			if (OnionManager.IsAnyOnionActiveInScene)
			{
				PikminColour colour = GameUtil.GetMajorityColour(_CarryingPikmin);
				_TargetOnion = OnionManager.GetOnionOfColour(colour);

				// Assert that the target onion exists and is active.
				Debug.Assert(_TargetOnion != null, $"Target Onion ({colour}) not found!");
				Debug.Assert(_TargetOnion.OnionActive == true, $"Target Onion ({colour}) not active!");
			}

			// Set the current waypoint and target position for this object, then update the speed.
			_CurrentWaypoint = WayPointManager._Instance.GetClosestWaypoint(transform.position);
			_TargetPosition = _CurrentWaypoint.transform.position;

			_IsBeingCarried = true;

			_CurrentSpeedTarget = Mathf.Min(_CurrentSpeedTarget + _SpeedAddedPerPikmin, _MaxSpeed);
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
