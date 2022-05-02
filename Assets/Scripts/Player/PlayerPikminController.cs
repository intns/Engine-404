/*
 * PlayerPikminController.cs
 * Created by: Ambrosia
 * Created on: 12/2/2020 (dd/mm/yy)
 * Created for: managing the Players Pikmin and the associated data
 */

using UnityEngine;

public class PlayerPikminController : MonoBehaviour
{
	[Header("Components")]
	public Transform _FormationCenter;
	[SerializeField] private LayerMask _PikminLayer;
	[SerializeField] LineRenderer _LineRenderer;

	[Header("Plucking")]
	[SerializeField] private float _PikminPluckDistance = 3.5f;
	[SerializeField] private LayerMask _PikminPluckLayer;

	[Header("Throwing")]
	[SerializeField] private float _PikminThrowRadius = 17.5f;

	[Header("Grabbing")]
	[SerializeField] private float _PikminGrabRadius = 5;
	[SerializeField] private float _VerticalMaxGrabRadius = 1.5f;
	[SerializeField] private Transform _WhistleTransform = null;

	[Header("Formation")]
	[SerializeField] private float _IterationScale = 1;
	[SerializeField] private float _StartingDistance = 2;
	[SerializeField] private float _DistancePerPikmin = 0.05f; // How much is added to the offset for each pikmin

	[HideInInspector] public bool _CanThrowPikmin = true;
	private PikminAI _PikminInHand;

	Vector3[] _LinePositions = new Vector3[50];
	Vector3 _ThrownVelocity = Vector3.zero;

	private void Awake()
	{
		_LineRenderer.positionCount = _LinePositions.Length;
		_ThrownVelocity = transform.forward;
	}

	private void Update()
	{
		if (Player._Instance._MovementController._Paralysed || GameManager._IsPaused)
		{
			return;
		}

		Collider[] colls = Physics.OverlapSphere(transform.position, _PikminPluckDistance, _PikminPluckLayer, QueryTriggerInteraction.Collide);
		bool canPluck = colls.Length != 0;
		if (colls.Length != 0)
		{
			PikminSprout closest = null;
			float closestDist = float.PositiveInfinity;
			foreach (Collider coll in colls)
			{
				PikminSprout sprout = coll.GetComponent<PikminSprout>();
				if (sprout.CanPluck())
				{
					float dist = MathUtil.DistanceTo(transform.position, coll.transform.position);
					if (dist < closestDist)
					{
						closestDist = dist;
						closest = sprout;
					}
				}
			}

			if (closest != null && Input.GetButtonDown("A Button"))
			{
				PikminAI pikmin = closest.OnPluck();
				pikmin.AddToSquad();
				Destroy(closest.gameObject);
			}
		}

		if (_CanThrowPikmin)
		{
			HandleThrowing(canPluck);
		}
		else
		{
			if (_PikminInHand != null)
			{
				_PikminInHand.EndThrowHold();
				_PikminInHand = null;
				_LineRenderer.enabled = false;
			}
		}

		HandleFormation();

		// Disbanding
		if (Input.GetButtonDown("X Button"))
		{
			// Remove each Pikmin from the squad
			while (PikminStatsManager._InSquad.Count > 0)
			{
				PikminStatsManager._InSquad[0].RemoveFromSquad();
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(_FormationCenter.position, 1);

	  for (int i = 0; i < PikminStatsManager._InSquad.Count; i++)
		{
			Gizmos.DrawSphere(GetPositionAt(i), 0.1f);
		}

		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, _PikminPluckDistance);
	}

	private Vector3 CalculateVelocity(Vector3 destination, float vd)
	{
		Vector3 displacementXZ = MathUtil.XZToXYZ(new Vector2(destination.x - _PikminInHand.transform.position.x,
			destination.z - _PikminInHand.transform.position.z));

		float throwHeight = _PikminInHand._Data._ThrowingHeight;

		float time = Mathf.Sqrt(-2 * (_PikminInHand.transform.position.y + throwHeight) / Physics.gravity.y)
			+ Mathf.Sqrt(2 * (vd - throwHeight) / Physics.gravity.y);

		Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * Physics.gravity.y * throwHeight);
		Vector3 velocityXZ = (displacementXZ / time) * 1.01f;

		return velocityXZ + (velocityY * -Mathf.Sign(Physics.gravity.y));
	}

	private void SetLineRenderer()
	{
		transform.LookAt(new Vector3(_WhistleTransform.position.x, transform.position.y, _WhistleTransform.position.z));
		_PikminInHand.transform.LookAt(new Vector3(_WhistleTransform.position.x, _PikminInHand.transform.position.y, _WhistleTransform.position.z));

		Vector3 whistleTransform = _WhistleTransform.position;
		if (whistleTransform.y > _PikminInHand.transform.position.y + _PikminInHand._Data._ThrowingHeight)
		{
			whistleTransform.y = (_PikminInHand.transform.position.y + _PikminInHand._Data._ThrowingHeight) - 0.05f;
		}

		Vector3 offs = (whistleTransform - _PikminInHand.transform.position) * 1.025f;
		// TODO: Fix clamp magnitude not working as intended!
		Vector3 destination = _PikminInHand.transform.position + Vector3.ClampMagnitude(offs, _PikminThrowRadius);
		float vd = destination.y - _PikminInHand.transform.position.y;
		_ThrownVelocity = CalculateVelocity(destination, vd);

		if (float.IsNaN(_ThrownVelocity.x))
		{
			_ThrownVelocity = Vector3.forward;
			return;
		}

		Vector3 velocity = _ThrownVelocity * 1.015f;
		Vector3 pos = _PikminInHand.transform.position;
		Vector3 oldPos = pos;
		int i = 0;
		for (; i < _LinePositions.Length; i++)
		{
			velocity += Physics.gravity * Time.fixedDeltaTime;
			pos += velocity * Time.fixedDeltaTime;

			Vector3 heading = pos - oldPos;
			if (Physics.Raycast(oldPos, heading.normalized, heading.magnitude))
			{
				break;
			}

			oldPos = pos;
			_LinePositions[i] = pos;
		}

		_LineRenderer.positionCount = i;
		_LineRenderer.SetPositions(_LinePositions);
	}

	/// <summary>
	/// Handles throwing the Pikmin including arc calculation
	/// </summary>
	private void HandleThrowing(bool canPluck)
	{
		// Check if we've got more than 0 Pikmin in
		// our squad and we press the Throw key (currently Space)

		if (!canPluck && Input.GetButtonDown("A Button") && PikminStatsManager.GetTotalOnField() > 0 && _PikminInHand == null)
		{
			GameObject closestPikmin = GetClosestPikmin();
			// Check if we've even gotten a Pikmin
			if (closestPikmin != null)
			{
				_PikminInHand = closestPikmin.GetComponent<PikminAI>();

				_PikminInHand.StartThrowHold();
				_LineRenderer.enabled = true;
			}
		}

		// The rest of the throw depends if we even got a Pikmin
		if (_PikminInHand != null)
		{
			bool aButtonAction = false;
			if (Input.GetButton("A Button"))
			{
				// Move the Pikmin's model to in front of the player
				_PikminInHand.transform.position = transform.position + (transform.forward / 2);
				SetLineRenderer();
				aButtonAction = true;
			}

			if (Input.GetButtonUp("A Button"))
			{
				_PikminInHand.EndThrowHold();

				Vector3 whistlePos = new Vector3(_WhistleTransform.position.x, 0, _WhistleTransform.position.z);
				transform.LookAt(new Vector3(whistlePos.x, transform.position.y, whistlePos.z));
				_PikminInHand.transform.LookAt(new Vector3(whistlePos.x, _PikminInHand.transform.position.y, whistlePos.z));

				Rigidbody rigidbody = _PikminInHand.GetComponent<Rigidbody>();
				if (_ThrownVelocity != Vector3.zero)
				{
					rigidbody.velocity = _ThrownVelocity;
				}

				_LineRenderer.enabled = false;

				// As the Pikmin has been thrown, remove it from the hand variable
				_PikminInHand = null;
				aButtonAction = true;
			}

			if (!aButtonAction)
			{
				_PikminInHand.EndThrowHold();
				_PikminInHand = null;
				_LineRenderer.enabled = false;
			}
		}
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

		return _FormationCenter.position + MathUtil.XZToXYZ(_IterationScale * currentIteration * MathUtil.PositionInUnit(maxOnLevel, currentOnLevel));
	}

	/// <summary>
	/// Prevents the Pikmin formation center from moving every frame
	/// by clamping it to a set distance away from the player
	/// </summary>
	private void HandleFormation()
	{
		Vector3 targetPosition = _FormationCenter.transform.position - transform.position;
		_FormationCenter.transform.position =
			transform.position + Vector3.ClampMagnitude(targetPosition, _StartingDistance + _DistancePerPikmin * PikminStatsManager.GetTotalInSquad());
	}

	/// <summary>
	/// Searches for the closest Pikmin in proximity to the Player and returns it
	/// </summary>
	/// <returns>The closest Pikmin gameobject in the Player's squad</returns>
	private GameObject GetClosestPikmin()
	{
		GameObject closestPikmin = null;
		float closestPikminDistance = _PikminGrabRadius;

		// Grab all colliders within a given radius from our current position
		Collider[] hitColliders = Physics.OverlapSphere(transform.position, _PikminGrabRadius, _PikminLayer);
		foreach (Collider collider in hitColliders)
		{
			PikminAI pikminComponent = collider.GetComponent<PikminAI>();
			// Check if they're in the squad
			if (!pikminComponent._InSquad)
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
