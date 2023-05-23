/*
 * PlayerPikminController.cs
 * Created by: Ambrosia, SenkaSkriena
 * Created on: 12/2/2020 (dd/mm/yy)
 * Created for: Managing the Players Pikmin and the associated data
 */

using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPikminController : MonoBehaviour
{
	[Header("Components")]
	public Transform _FormationCenter;
	[SerializeField] LayerMask _PikminLayer;
	[SerializeField] LineRenderer _LineRenderer;

	[Header("Plucking")]
	[SerializeField] float _PikminPluckDistance = 3.5f;
	[SerializeField] LayerMask _PikminPluckLayer;

	[Header("Throwing")]
	[SerializeField] float _TimeBetweenThrow = 0.07f;
	[SerializeField] float _PikminThrowRadius = 17.5f;
	[SerializeField] LayerMask _AllMask;

	[Header("Grabbing")]
	[SerializeField] float _PikminGrabRadius = 5;
	[SerializeField] float _VerticalMaxGrabRadius = 1.5f;
	[SerializeField] Transform _WhistleTransform;

	[Header("Formation")]
	[SerializeField] LayerMask _MapMask;
	[Space]
	[SerializeField] float _IterationScale = 1;
	[SerializeField] float _StartingDistance = 2;
	[SerializeField] float _DistancePerPikmin = 0.05f; // How much is added to the offset for each pikmin
	[Space]
	[SerializeField] float _CrowdControlLength = 2.5f;

	[SerializeField] Vector2 _DirectionToMouse = Vector2.zero;
	[SerializeField] bool _UsingCrowdControl;
	[HideInInspector] public bool _CanThrowPikmin = true;
	[HideInInspector] public bool _CanPlayerAttack;
	[HideInInspector] public PikminColour _SelectedThrowPikmin = PikminColour.Red;

	[HideInInspector] public PikminAI _PikminInHand;
	PikminSprout _ClosestSprout;
	bool _ControlFormation;
	bool _HoldingPikmin;
	Vector3[] _LinePositions = new Vector3[50];
	bool _Plucking;
	Vector3 _ThrownVelocity = Vector3.zero;

	float _ThrowTimer;

	void Awake()
	{
		_LineRenderer.positionCount = _LinePositions.Length;
		_ThrownVelocity = transform.forward;

		// Player can't spawn with any Pikmin in squad
		_SelectedThrowPikmin = PikminColour.Size;
	}

	void Update()
	{
		if (Player._Instance._MovementController._Paralysed || GameManager.IsPaused)
		{
			if (_PikminInHand != null)
			{
				EndThrow();
			}

			_ControlFormation = false;
			PikminStatsManager.ReassignFormation();
			return;
		}

		_ThrowTimer = Mathf.Max(_ThrowTimer - Time.deltaTime, 0.0f);

		if (PikminStatsManager.GetTotalPikminInSquad() > 0)
		{
			if (_SelectedThrowPikmin == PikminColour.Size)
			{
				_SelectedThrowPikmin = GameUtil.GetMajorityColour(PikminStatsManager._InSquad);
			}
			else if (PikminStatsManager.GetTotalPikminInSquad(_SelectedThrowPikmin) == 0)
			{
				_SelectedThrowPikmin = GameUtil.GetMajorityColour(PikminStatsManager._InSquad);
			}
		}
		else
		{
			_SelectedThrowPikmin = PikminColour.Size;
			_CanPlayerAttack = true;
		}

		Collider[] colls = Physics.OverlapSphere(
			transform.position,
			_PikminPluckDistance,
			_PikminPluckLayer,
			QueryTriggerInteraction.Collide
		);
		bool canPluck = colls.Length != 0;

		if (canPluck)
		{
			if (_Plucking)
			{
				PikminAI pikmin = _ClosestSprout.OnPluck();
				pikmin.AddToSquad();

				Destroy(_ClosestSprout.gameObject);
				_Plucking = false;
			}
		}

		if (_CanThrowPikmin && _HoldingPikmin)
		{
			// Move the Pikmin's model to in front of the player
			_PikminInHand.transform.position = transform.position
			                                   + transform.right / 1.75f
			                                   + transform.forward / 4f
			                                   + Vector3.up / 3.5f;

			transform.LookAt(new Vector3(_WhistleTransform.position.x, transform.position.y, _WhistleTransform.position.z));

			_PikminInHand.transform.LookAt(
				new Vector3(
					_WhistleTransform.position.x,
					_PikminInHand.transform.position.y,
					_WhistleTransform.position.z
				)
			);

			Vector3 whistleTransform = _WhistleTransform.position;

			float maxHeight = transform.position.y + _PikminInHand._Data._ThrowingHeight;

			if (whistleTransform.y > maxHeight)
			{
				whistleTransform.y = maxHeight;
			}

			Vector2 whistleDistance = new(whistleTransform.x - transform.position.x, whistleTransform.z - transform.position.z);

			if (whistleDistance.magnitude >= _PikminThrowRadius)
			{
				whistleDistance = whistleDistance.normalized * _PikminThrowRadius;

				whistleTransform.x = transform.position.x + whistleDistance.x;
				whistleTransform.z = transform.position.z + whistleDistance.y;

				whistleTransform.y = transform.position.y;
			}

			_ThrownVelocity = CalculateVelocity(whistleTransform);

			SetLineRenderer();

			_CanPlayerAttack = false;
		}
		else if (_PikminInHand != null)
		{
			EndThrow();

			_CanPlayerAttack = true;
		}

		HandleFormation();
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(_FormationCenter.position, 1);

		for (int i = 0; i < PikminStatsManager._InSquad.Count; i++)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(GetPositionAt(i), 0.1f);
		}

		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, _PikminPluckDistance);
	}

	public Vector3 GetPositionAt(int index)
	{
		int currentOnLevel = index;
		int maxOnLevel = 4;
		int currentIteration = 1;

		while (currentOnLevel >= maxOnLevel)
		{
			currentOnLevel -= maxOnLevel;
			maxOnLevel += 4;
			currentIteration++;
		}

		Vector3 rawCirclePos
			= MathUtil.XZToXYZ(_IterationScale * currentIteration * MathUtil.PositionInUnit(maxOnLevel, currentOnLevel));

		if (_UsingCrowdControl)
		{
			rawCirclePos *= 1 / (_CrowdControlLength - 0.5f);
			rawCirclePos.x *= _DirectionToMouse.x * 1.15f * _CrowdControlLength;
			rawCirclePos.z *= _DirectionToMouse.y * 1.15f * _CrowdControlLength;
		}

		return _FormationCenter.position + rawCirclePos;
	}

	public void OnControlFormation(InputAction.CallbackContext context)
	{
		if (Player._Instance._MovementController._Paralysed || GameManager.IsPaused)
		{
			_ControlFormation = false;
			return;
		}

		if (context.started)
		{
			_ControlFormation = true;
		}
		else if (context.canceled)
		{
			_ControlFormation = false;
			_UsingCrowdControl = false;

			PikminStatsManager.ReassignFormation();
		}
	}

	public void OnDisbandPikmins(InputAction.CallbackContext context)
	{
		if (!context.started || Player._Instance._MovementController._Paralysed || GameManager.IsPaused)
		{
			return;
		}

		PikminStatsManager._IsDisbanding = true;

		// Remove each Pikmin from the squad
		while (PikminStatsManager._InSquad.Count > 0)
		{
			PikminStatsManager._InSquad[0].RemoveFromSquad();
		}

		PikminStatsManager._IsDisbanding = false;
		_CanPlayerAttack = true;
	}

	public void OnPrimaryAction(InputAction.CallbackContext context)
	{
		if (Player._Instance._MovementController._Paralysed || GameManager.IsPaused)
		{
			return;
		}

		Collider[] colls = Physics.OverlapSphere(
			transform.position,
			_PikminPluckDistance,
			_PikminPluckLayer,
			QueryTriggerInteraction.Collide
		);
		bool canPluck = colls.Length != 0;

		// Determine if button is being pressed or released
		if (context.started)
		{
			if (canPluck)
			{
				_ClosestSprout = null;
				float closestDist = float.PositiveInfinity;

				foreach (Collider coll in colls)
				{
					PikminSprout sprout = coll.GetComponent<PikminSprout>();

					if (!sprout.CanPluck())
					{
						continue;
					}

					float dist = MathUtil.DistanceTo(transform.position, coll.transform.position);

					if (dist >= closestDist)
					{
						continue;
					}

					closestDist = dist;
					_ClosestSprout = sprout;
				}

				if (_ClosestSprout != null)
				{
					_Plucking = true;
				}
			}

			else if (_CanThrowPikmin && PikminStatsManager.GetTotalPikminOnField() > 0 && _PikminInHand == null)
			{
				GameObject closestPikmin = GetClosestPikmin();

				if (closestPikmin != null && _ThrowTimer <= 0.0f)
				{
					_PikminInHand = closestPikmin.GetComponent<PikminAI>();

					_PikminInHand.StartThrowHold();
					_LineRenderer.enabled = true;

					_HoldingPikmin = true;
					_CanPlayerAttack = false;
				}
				else
				{
					_CanPlayerAttack = true;
				}
			}
		}
		else if (context.canceled)
		{
			// The rest of the throw depends if we even got a Pikmin
			if (_PikminInHand != null)
			{
				Vector3 whistlePos = new(_WhistleTransform.position.x, 0, _WhistleTransform.position.z);
				transform.LookAt(new Vector3(whistlePos.x, transform.position.y, whistlePos.z));
				_PikminInHand.transform.LookAt(new Vector3(whistlePos.x, _PikminInHand.transform.position.y, whistlePos.z));

				Rigidbody rigidbody = _PikminInHand.GetComponent<Rigidbody>();

				EndThrow();

				if (_ThrownVelocity != Vector3.zero)
				{
					rigidbody.velocity = _ThrownVelocity;
				}

				_CanPlayerAttack = true;
			}
		}
	}

	public void OnPrintPikminStatManager(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			PikminStatsManager.Print();
		}
	}

	public void OnSelectMajorityPikminType(InputAction.CallbackContext context)
	{
		if (
			!context.started ||
			Player._Instance._MovementController._Paralysed ||
			GameManager.IsPaused ||
			PikminStatsManager.GetTotalPikminInSquad() <= 0
		)
		{
			return;
		}

		// Else, select the majority of pikmin type
		_SelectedThrowPikmin = GameUtil.GetMajorityColour(PikminStatsManager._InSquad);
	}

	public void OnSelectNextPikminType(InputAction.CallbackContext context)
	{
		if (
			!context.started ||
			Player._Instance._MovementController._Paralysed ||
			GameManager.IsPaused ||
			PikminStatsManager.GetTotalPikminInSquad() <= 0
		)
		{
			return;
		}

		// Else, select first next pikmin type
		_SelectedThrowPikmin = SelectValidPikminType(true);
	}

	public void OnSelectPreviousPikminType(InputAction.CallbackContext context)
	{
		if (
			!context.started ||
			Player._Instance._MovementController._Paralysed ||
			GameManager.IsPaused ||
			PikminStatsManager.GetTotalPikminInSquad() <= 0
		)
		{
			return;
		}

		// Else, select first previous pikmin type
		_SelectedThrowPikmin = SelectValidPikminType(false);
	}

	Vector3 CalculateVelocity(Vector3 destination, float heightDiff)
	{
		float gravity = Physics.gravity.y - 9.81f;

		Vector3 displacementXZ = MathUtil.XZToXYZ(
			new(
				destination.x - _PikminInHand.transform.position.x,
				destination.z - _PikminInHand.transform.position.z
			)
		);

		float throwHeight = _PikminInHand._Data._ThrowingHeight;
		heightDiff = Mathf.Clamp(heightDiff, 0.0f, throwHeight - 0.01f);

		float time = Mathf.Sqrt(-2 * (_PikminInHand.transform.position.y + throwHeight) / gravity)
		             + Mathf.Sqrt(2 * (heightDiff - throwHeight) / gravity);

		Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * throwHeight);
		Vector3 velocityXZ = displacementXZ / time * 1.01f;

		return velocityXZ + velocityY * -Mathf.Sign(gravity);
	}

	// Gives the direction/unit vector in the direction of vector v's projection on the xz plane.
	Vector3 NormalizeXZ(Vector3 v)
	{
		float magnitude = Mathf.Sqrt(v.x * v.x + v.z * v.z);
		Vector3 result;

		result.x = v.x / magnitude;
		result.y = 0.0f;
		result.z = v.z / magnitude;

		return result;
	}

	Vector3 CalculateVelocity(Vector3 target)
	{
		Vector3 dirTowardTarget = target - _PikminInHand.transform.position;
		Vector3 velDirection = NormalizeXZ(dirTowardTarget) * 1.025f;

		float gravity = Physics.gravity.y;
		float throwHeight = _PikminInHand._Data._ThrowingHeight;

		float velY = Mathf.Sqrt(-2 * gravity * throwHeight);

		float t = (-velY - Mathf.Sqrt(velY * velY + 2 * gravity * dirTowardTarget.y)) / gravity;

		// math to get initial horizontal velocity
		float velX = Mathf.Sqrt(dirTowardTarget.x * dirTowardTarget.x + dirTowardTarget.z * dirTowardTarget.z) / t;

		return new() { x = velDirection.x * velX, y = velY, z = velDirection.z * velX };
	}

	void EndThrow()
	{
		PikminColour colour = _PikminInHand.GetColour();
		bool reassign = PikminStatsManager.GetTotalPikminInSquad(colour) - 1 <= 0;

		if (reassign)
		{
			foreach (PikminColour unused in from kvp in PikminStatsManager._TypeStats
			                                let currentColor = kvp.Key
			                                let currentStats = kvp.Value
			                                where PikminStatsManager.GetTotalPikminInSquad(currentColor) > 0
			                                select currentColor)
			{
				// Handle the case when at least one Pikmin of the current color is in the squad
				break;
			}

			_SelectedThrowPikmin = PikminStatsManager.GetTotalPikminInSquad(colour) <= 0 ? PikminColour.Size : colour;
		}

		_ThrowTimer = _TimeBetweenThrow;
		_HoldingPikmin = false;
		_PikminInHand.EndThrowHold();
		_PikminInHand = null;
		_LineRenderer.enabled = false;
	}

	/// <summary>
	///   Searches for the closest Pikmin in proximity to the Player and returns it
	/// </summary>
	/// <returns>The closest Pikmin gameobject in the Player's squad</returns>
	GameObject GetClosestPikmin()
	{
		GameObject closestPikmin = null;
		float closestPikminDistance = _PikminGrabRadius;

		// Grab all colliders within a given radius from our current position
		Collider[] hitColliders = Physics.OverlapSphere(transform.position, _PikminGrabRadius, _PikminLayer);

		foreach (Collider collider in hitColliders)
		{
			PikminAI pikminComponent = collider.GetComponent<PikminAI>();

			// Check if they're in the squad
			if (!pikminComponent || !pikminComponent._InSquad)
			{
				continue;
			}

			if (pikminComponent.GetColour() != _SelectedThrowPikmin)
			{
				continue;
			}

			// Vertical check, make sure Pikmin don't get thrown if too far up
			// or downwards from the position of the Player
			float verticalDistance = Mathf.Abs(transform.position.y - collider.transform.position.y);

			if (verticalDistance > _VerticalMaxGrabRadius)
			{
				continue;
			}

			// Assign it on our first run
			if (closestPikmin == null)
			{
				closestPikmin = collider.gameObject;
				continue;
			}

			// Checks the distance between the previously checked Pikmin
			// and the Pikmin we're doing now
			float distanceToPlayer = MathUtil.DistanceTo(collider.transform.position, transform.position);

			if (distanceToPlayer < closestPikminDistance)
			{
				closestPikmin = collider.gameObject;
				closestPikminDistance = distanceToPlayer;
			}
		}

		return closestPikmin;
	}

	/// <summary>
	///   Prevents the Pikmin formation center from moving every frame
	///   by clamping it to a set distance away from the player
	/// </summary>
	void HandleFormation()
	{
		Vector3 targetPos = _FormationCenter.transform.position - transform.position;

		if (_ControlFormation)
		{
			try
			{
				Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

				if (Physics.Raycast(ray, out RaycastHit hit, float.PositiveInfinity, _MapMask, QueryTriggerInteraction.Ignore))
				{
					Vector3 direction = MathUtil.DirectionFromTo(_FormationCenter.transform.position, hit.point);

					_DirectionToMouse.x = direction.x;
					_DirectionToMouse.y = direction.z;

					targetPos = Vector3.Lerp(targetPos, targetPos + direction, 10 * Time.deltaTime);
					_UsingCrowdControl = true;
				}
			}
			catch
			{
				// Do nothing
			}
		}
		else
		{
			_UsingCrowdControl = false;
		}

		if (_ControlFormation)
		{
			PikminStatsManager.ReassignFormation();
		}

		_FormationCenter.transform.position =
			transform.position + Vector3.ClampMagnitude(
				targetPos,
				_StartingDistance + _DistancePerPikmin * PikminStatsManager.GetTotalPikminInSquad()
			);
	}

	// Actual modulo that works with negative numbers
	static int Modulo(int x, int m)
	{
		return (x % m + m) % m;
	}

	PikminColour SelectValidPikminType(bool forward)
	{
		int step = forward ? 1 : -1;
		PikminColour colour = (PikminColour)Modulo((int)_SelectedThrowPikmin + step, (int)PikminColour.Size);

		while (true)
		{
			if (PikminStatsManager.GetTotalPikminInSquad(colour) > 0)
			{
				return colour;
			}

			colour = (PikminColour)Modulo((int)colour + step, (int)PikminColour.Size);
		}
	}

	void SetLineRenderer()
	{
		Vector3 velocity = _ThrownVelocity;
		Vector3 pos = _PikminInHand.transform.position;
		Vector3 oldPos = pos;
		int i = 0;

		for (; i < _LinePositions.Length; i++)
		{
			velocity += Physics.gravity * Time.fixedDeltaTime;
			pos += velocity * Time.fixedDeltaTime;

			Vector3 heading = pos - oldPos;

			if (Physics.Raycast(oldPos, heading.normalized, heading.magnitude, _AllMask, QueryTriggerInteraction.Ignore))
			{
				break;
			}

			oldPos = pos;
			_LinePositions[i] = pos;
		}

		_LineRenderer.positionCount = i;
		_LineRenderer.SetPositions(_LinePositions);
	}
}
