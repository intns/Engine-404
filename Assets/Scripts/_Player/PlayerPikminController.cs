/*
 * PlayerPikminController.cs
 * Created by: Ambrosia, SenkaSkriena
 * Created on: 12/2/2020 (dd/mm/yy)
 * Created for: Managing the Players Pikmin and the associated data
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPikminController : MonoBehaviour
{
	[Header("Components")]
	public Transform _FormationCenter;
	[SerializeField] LayerMask _PikminLayer;
	[SerializeField] LineRenderer _LineRenderer;
	[SerializeField] Animator _Animator;

	[Header("Plucking")]
	[SerializeField] float _PikminPluckDistance = 3.5f;
	[SerializeField] LayerMask _PikminPluckLayer;

	[Header("Throwing")]
	[SerializeField] float _PikminThrowRadius = 17.5f;
	[SerializeField] LayerMask _AllMask;

	[Header("Grabbing")]
	[SerializeField] float _PikminGrabRadius = 5;
	[SerializeField] float _VerticalMaxGrabRadius = 1.5f;
	[SerializeField] Transform _WhistleTransform = null;

	[Header("Formation")]
	[SerializeField] LayerMask _MapMask;
	[Space]
	[SerializeField] float _IterationScale = 1;
	[SerializeField] float _StartingDistance = 2;
	[SerializeField] float _DistancePerPikmin = 0.05f; // How much is added to the offset for each pikmin
	[Space]
	[SerializeField] float _CrowdControlLength = 2.5f;

	[SerializeField] Vector2 _DirectionToMouse = Vector2.zero;
	[SerializeField] bool _UsingCrowdControl = false;
	[HideInInspector] public bool _CanThrowPikmin = true;
	[HideInInspector] public PikminColour _SelectedThrowPikmin = PikminColour.Red;
	
	PikminAI _PikminInHand;
	bool _Plucking = false;
	PikminSprout _ClosestSprout = null;
	bool _HoldingPikmin = false;
	bool _ControlFormation = false;

	Vector3[] _LinePositions = new Vector3[50];
	Vector3 _ThrownVelocity = Vector3.zero;

	void Awake()
	{
		_LineRenderer.positionCount = _LinePositions.Length;
		_ThrownVelocity = transform.forward;
	}

	void Update()
	{
		if (Player._Instance._MovementController._Paralysed || GameManager._IsPaused)
		{
			if (_PikminInHand != null)
			{
				EndThrow();
			}

			return;
		}

		if (PikminStatsManager.GetTotalInSquad() > 0)
		{
			if (_SelectedThrowPikmin == PikminColour.Size)
			{
				_SelectedThrowPikmin = GameUtil.GetMajorityColour(PikminStatsManager._InSquad);
			}
		}
		else
		{
			_SelectedThrowPikmin = PikminColour.Size;
		}

		Collider[] colls = Physics.OverlapSphere(transform.position, _PikminPluckDistance, _PikminPluckLayer, QueryTriggerInteraction.Collide);
		bool canPluck = colls.Length != 0;
		if (canPluck)
		{
			if (_Plucking)
			{
				_Animator.SetTrigger("Pluck");

				PikminAI pikmin = _ClosestSprout.OnPluck();
				pikmin.AddToSquad();
				Destroy(_ClosestSprout.gameObject);
				_Plucking = false;
			}
			else
			{
				_Animator.ResetTrigger("Pluck");
			}
		}
		else
		{
			_Animator.ResetTrigger("Pluck");
		}

		if (_CanThrowPikmin && _HoldingPikmin)
		{
			// Move the Pikmin's model to in front of the player
			_PikminInHand.transform.position = transform.position + (transform.right / 3) + (transform.forward / 3) + (Vector3.down / 4);
			SetLineRenderer();
		}
		else if (_PikminInHand != null)
		{
			EndThrow();
		}

		HandleFormation();
	}

	public void OnDisbandPikmins(InputAction.CallbackContext context)
	{
		if (!context.started || Player._Instance._MovementController._Paralysed || GameManager._IsPaused)
		{
			return;
		}

		PikminStatsManager._Disbanding = true;

		// Remove each Pikmin from the squad
		while (PikminStatsManager._InSquad.Count > 0)
		{
			PikminStatsManager._InSquad[0].RemoveFromSquad();
		}

		PikminStatsManager._Disbanding = false;
	}

	public void OnControlFormation(InputAction.CallbackContext context)
	{
		if (Player._Instance._MovementController._Paralysed || GameManager._IsPaused)
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
			PikminStatsManager.ReassignFormation();
		}
	}

	public void OnPrimaryAction(InputAction.CallbackContext context)
	{
		if (Player._Instance._MovementController._Paralysed || GameManager._IsPaused)
		{
			return;
		}

		Collider[] colls = Physics.OverlapSphere(transform.position, _PikminPluckDistance, _PikminPluckLayer, QueryTriggerInteraction.Collide);
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

			// Check if we've got more than 0 Pikmin in
			// our squad and we press the Throw key (currently Space)

			else if (_CanThrowPikmin && PikminStatsManager.GetTotalOnField() > 0 && _PikminInHand == null)
			{
				GameObject closestPikmin = GetClosestPikmin();
				// Check if we've even gotten a Pikmin
				if (closestPikmin != null)
				{
					_PikminInHand = closestPikmin.GetComponent<PikminAI>();

					_PikminInHand.StartThrowHold();
					_LineRenderer.enabled = true;

					_Animator.SetBool("HoldingThrow", true);
					_HoldingPikmin = true;
				}
			}
		}
		else if (context.canceled)
		{
			// The rest of the throw depends if we even got a Pikmin
			if (_PikminInHand != null)
			{
				Vector3 whistlePos = new Vector3(_WhistleTransform.position.x, 0, _WhistleTransform.position.z);
				transform.LookAt(new Vector3(whistlePos.x, transform.position.y, whistlePos.z));
				_PikminInHand.transform.LookAt(new Vector3(whistlePos.x, _PikminInHand.transform.position.y, whistlePos.z));

				Rigidbody rigidbody = _PikminInHand.GetComponent<Rigidbody>();
				if (_ThrownVelocity != Vector3.zero)
				{
					rigidbody.velocity = _ThrownVelocity;
				}

				EndThrow();
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

	public void OnSelectPreviousPikminType(InputAction.CallbackContext context)
	{
		if (
			!context.started ||
			Player._Instance._MovementController._Paralysed ||
			GameManager._IsPaused ||
			PikminStatsManager.GetTotalInSquad() <= 0
			)
		{
			return;
		}

		// Else, select first previous pikmin type
		_SelectedThrowPikmin = SelectValidPikminType(false);
	}

	public void OnSelectNextPikminType(InputAction.CallbackContext context)
	{
		if (
			!context.started ||
			Player._Instance._MovementController._Paralysed ||
			GameManager._IsPaused ||
			PikminStatsManager.GetTotalInSquad() <= 0
			)
		{
			return;
		}

		// Else, select first next pikmin type
		_SelectedThrowPikmin = SelectValidPikminType(true);
	}

	public void OnSelectMajorityPikminType(InputAction.CallbackContext context)
	{
		if (
			!context.started ||
			Player._Instance._MovementController._Paralysed ||
			GameManager._IsPaused ||
			PikminStatsManager.GetTotalInSquad() <= 0
			)
		{
			return;
		}

		// Else, select the majority of pikmin type
		_SelectedThrowPikmin = GameUtil.GetMajorityColour(PikminStatsManager._InSquad);
	}

	PikminColour SelectValidPikminType(bool forward)
	{
		// Determine the step based on if we want to go forward or backward
		int step = forward ? 1 : -1;

		// Get previous type (modulo the number of types so that it cycles)
		PikminColour colour = (PikminColour)Modulo((int)_SelectedThrowPikmin + step, (int)PikminColour.Size);
		bool invalidColour = true;

		// If there are no pikmin of this type in the squad, we search in the previous/next one
		while (invalidColour)
		{
			switch (colour)
			{
				case PikminColour.Red:
					if (PikminStatsManager._RedStats.GetTotalInSquad() <= 0)
					{
						colour = (PikminColour)Modulo((int)colour + step, (int)PikminColour.Size);
						continue;
					}
					invalidColour = false;
					break;
				case PikminColour.Yellow:
					if (PikminStatsManager._YellowStats.GetTotalInSquad() <= 0)
					{
						colour = (PikminColour)Modulo((int)colour + step, (int)PikminColour.Size);
						continue;
					}
					invalidColour = false;
					break;
				case PikminColour.Blue:
					if (PikminStatsManager._BlueStats.GetTotalInSquad() <= 0)
					{
						colour = (PikminColour)Modulo((int)colour + step, (int)PikminColour.Size);
						continue;
					}
					invalidColour = false;
					break;
				default:
					break;
			}
		}

		return colour;
	}

	// Actual modulo that works with negative numbers
	int Modulo(int x, int m)
	{
		return (x % m + m) % m;
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

	Vector3 CalculateVelocity(Vector3 destination, float vd)
	{
		float gravity = Physics.gravity.y - 9.81f;

		Vector3 displacementXZ = MathUtil.XZToXYZ(new Vector2(destination.x - _PikminInHand.transform.position.x,
			destination.z - _PikminInHand.transform.position.z));

		float throwHeight = _PikminInHand._Data._ThrowingHeight;

		float time = Mathf.Sqrt(-2 * (_PikminInHand.transform.position.y + throwHeight) / gravity)
			+ Mathf.Sqrt(2 * (vd - throwHeight) / gravity);

		Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * throwHeight);
		Vector3 velocityXZ = (displacementXZ / time) * 1.01f;

		return velocityXZ + (velocityY * -Mathf.Sign(gravity));
	}

	void SetLineRenderer()
	{
		transform.LookAt(new Vector3(_WhistleTransform.position.x, transform.position.y, _WhistleTransform.position.z));
		_PikminInHand.transform.LookAt(new Vector3(_WhistleTransform.position.x, _PikminInHand.transform.position.y, _WhistleTransform.position.z));

		Vector3 whistleTransform = _WhistleTransform.position;
		if (whistleTransform.y > _PikminInHand.transform.position.y + _PikminInHand._Data._ThrowingHeight)
		{
			whistleTransform.y = _PikminInHand.transform.position.y + _PikminInHand._Data._ThrowingHeight;
		}

		Vector3 offs = (whistleTransform - transform.position);
		// TODO: Fix clamp magnitude not working as intended!
		Vector2 clamped = Vector2.ClampMagnitude(new Vector2(offs.x, offs.z), _PikminThrowRadius);
		Vector3 destination = transform.position + Vector3.ClampMagnitude(MathUtil.XZToXYZ(clamped), _PikminThrowRadius);
		float vd = destination.y - transform.position.y;
		_ThrownVelocity = CalculateVelocity(destination, vd);

		if (float.IsNaN(_ThrownVelocity.x))
		{
			_ThrownVelocity = Vector3.forward * 10 + Vector3.up * 10;
			return;
		}

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

	void EndThrow()
	{
		PikminColour colour = _PikminInHand.GetColour();
		bool reassign = false;
		switch (colour)
		{
			case PikminColour.Red:
				if (PikminStatsManager._RedStats.GetTotalInSquad() - 1 <= 0)
				{
					reassign = true;
				}
				break;
			case PikminColour.Yellow:
				if (PikminStatsManager._YellowStats.GetTotalInSquad() - 1 <= 0)
				{
					reassign = true;
				}
				break;
			case PikminColour.Blue:
				if (PikminStatsManager._BlueStats.GetTotalInSquad() - 1 <= 0)
				{
					reassign = true;
				}
				break;
			default:
				break;
		}

		if (reassign)
		{
			for (int i = 0; i < (int)PikminColour.Size; i++)
			{
				colour = (PikminColour)i;
				switch (colour)
				{
					case PikminColour.Red:
						if (PikminStatsManager._RedStats.GetTotalInSquad() > 0)
						{
							break;
						}
						break;
					case PikminColour.Yellow:
						if (PikminStatsManager._YellowStats.GetTotalInSquad() > 0)
						{
							break;
						}
						break;
					case PikminColour.Blue:
						if (PikminStatsManager._BlueStats.GetTotalInSquad() > 0)
						{
							break;
						}
						break;
					default:
						break;
				}
			}

			if (PikminStatsManager._BlueStats.GetTotalInSquad() <= 0)
			{
				_SelectedThrowPikmin = PikminColour.Size;
			}
			else
			{
				_SelectedThrowPikmin = colour;
			}
		}

		_HoldingPikmin = false;
		_Animator.SetBool("HoldingThrow", false);
		_PikminInHand.EndThrowHold();
		_PikminInHand = null;
		_LineRenderer.enabled = false;
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

		Vector3 rawCirclePos = MathUtil.XZToXYZ(_IterationScale * currentIteration * MathUtil.PositionInUnit(maxOnLevel, currentOnLevel));

		if (_UsingCrowdControl)
		{
			rawCirclePos *= (1 / (_CrowdControlLength - 0.5f));
			rawCirclePos.x *= _DirectionToMouse.x * 1.15f * _CrowdControlLength;
			rawCirclePos.z *= _DirectionToMouse.y * 1.15f * _CrowdControlLength;
		}

		return _FormationCenter.position + rawCirclePos;
	}

	/// <summary>
	/// Prevents the Pikmin formation center from moving every frame
	/// by clamping it to a set distance away from the player
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
					Vector3 direction = hit.point - _FormationCenter.transform.position;
					direction.y = 0;
					direction.Normalize();

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
			transform.position + Vector3.ClampMagnitude(targetPos, _StartingDistance + _DistancePerPikmin * PikminStatsManager.GetTotalInSquad());
	}

	/// <summary>
	/// Searches for the closest Pikmin in proximity to the Player and returns it
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
}
