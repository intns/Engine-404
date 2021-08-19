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

	[Header("Throwing")]
	[SerializeField] private float _PikminGrabRadius = 5;
	[SerializeField] private float _VerticalMaxGrabRadius = 1.5f;
	[SerializeField] private float _ThrowingGravity = Physics.gravity.y;
	[SerializeField] private float _LaunchAngle = 50.0f;
	[SerializeField] private Transform _WhistleTransform = null;

	[Header("Formation")]
	[SerializeField] private float _IterationScale = 1;
	[SerializeField] private float _StartingDistance = 2;
	[SerializeField] private float _DistancePerPikmin = 0.05f; // How much is added to the offset for each pikmin

	private GameObject _PikminInHand;

	private void Update()
	{
		HandleThrowing();
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
		// Draw the formation center position
		Gizmos.DrawWireSphere(_FormationCenter.position, 1);
	}

	/// <summary>
	/// Handles throwing the Pikmin including arc calculation
	/// </summary>
	private void HandleThrowing()
	{
		// Check if we've got more than 0 Pikmin in
		// our squad and we press the Throw key (currently Space)

		if (Input.GetButtonDown("A Button") && PikminStatsManager.GetTotalOnField() > 0 && _PikminInHand == null)
		{
			GameObject closestPikmin = GetClosestPikmin();
			// Check if we've even gotten a Pikmin
			if (closestPikmin != null)
			{
				_PikminInHand = closestPikmin;
				_PikminInHand.GetComponent<PikminAI>().StartThrowHold();
			}
		}

		// The rest of the throw depends if we even got a Pikmin
		if (_PikminInHand != null)
		{
			if (Input.GetButton("A Button"))
			{
				// Move the Pikmin's model to in front of the player
				_PikminInHand.transform.position = transform.position + (transform.forward / 2);
			}
			if (Input.GetButtonUp("A Button"))
			{
				/*
         * TODO: convert to Quadratic Bezier curve
         */
				_PikminInHand.GetComponent<PikminAI>().EndThrowHold();

				// Cache the Rigidbody component
				Rigidbody rigidbody = _PikminInHand.GetComponent<Rigidbody>();

				// Use X and Z coordinates to calculate distance between Pikmin and whistle                
				Vector3 whistlePos = new Vector3(_WhistleTransform.position.x, 0, _WhistleTransform.position.z);
				Vector3 pikiPos = new Vector3(_PikminInHand.transform.position.x, 0, _PikminInHand.transform.position.z);

				// Calculate vertical and horizontal distance between Pikmin and whistle
				float vd = _WhistleTransform.position.y - _PikminInHand.transform.position.y;
				float d = Vector3.Distance(pikiPos, whistlePos);

				// Plug the variables into the equation...
				float g = _ThrowingGravity;
				float angle = Mathf.Deg2Rad * _LaunchAngle;

				// Calculate horizontal and vertical velocity
				float velX = Mathf.Sqrt(g * d * d / (2.0f * (vd - (d * Mathf.Tan(angle)))));
				float velY = velX * Mathf.Tan(angle);

				// Face whistle and convert local velocity to global, and apply it
				transform.LookAt(new Vector3(whistlePos.x, transform.position.y, whistlePos.z));
				_PikminInHand.transform.LookAt(new Vector3(whistlePos.x, _PikminInHand.transform.position.y, whistlePos.z));
				Vector3 finalVel = _PikminInHand.transform.TransformDirection(new Vector3(0.0f, velY, velX));
				if (finalVel != Vector3.zero)
				{
					rigidbody.velocity = finalVel;
				}

				// TODO: Adjust targeting to be more accurate to whistle position/avoid having Pikmin
				// be thrown directly in front of Olimar rather than onto the whistle.

				// As the Pikmin has been thrown, remove it from the hand variable
				_PikminInHand = null;
			}
		}

		// (test) Killing the Pikmin
		if (Input.GetKeyDown(KeyCode.B) && PikminStatsManager.GetTotalOnField() > 0)
		{
			GameObject closestPikmin = GetClosestPikmin();
			if (closestPikmin != null)
			{
				PikminAI pikminComponent = closestPikmin.GetComponent<PikminAI>();
				pikminComponent.ChangeState(PikminStates.Dead);
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
		Collider[] hitColliders = Physics.OverlapSphere(transform.position, _PikminGrabRadius);
		foreach (Collider collider in hitColliders)
		{
			// Check if the collider is actually a pikmin
			if (collider.CompareTag("Pikmin"))
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
				float distanceToPlayer = Vector3.Distance(collider.transform.position, transform.position);
				if (distanceToPlayer < closestPikminDistance)
				{
					closestPikmin = collider.gameObject;
					closestPikminDistance = distanceToPlayer;
				}
			}
		}

		return closestPikmin;
	}
}
