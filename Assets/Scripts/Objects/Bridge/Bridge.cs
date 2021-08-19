/*
 * Bridge.cs
 * Created by: Ambrosia
 * Created on: 25/4/2020 (dd/mm/yy)
 * Created for: needing an object to cross an area that takes a certain amount of time to complete
 */

using System.Collections.Generic;
using UnityEngine;

public class Bridge : MonoBehaviour, IPikminAttack, IHealth
{
	[Header("Components")]
	[SerializeField] private Transform _StartPoint = null;
	[SerializeField] private Transform _EndPoint = null;

	[Header("Bridge Parts")]
	// Will be used to create the inital ramp, midpoints and ending ramp
	[SerializeField] private GameObject _Piece = null;
	// Will be used for the Pikmin to attack
	[SerializeField] private GameObject _AttackablePiece = null;

	[Header("Settings")]
	// The size of the steps we're going to take when iterating
	[SerializeField] private float _StepSize = 1;
	// The angle (in degrees) that the starting and ending ramp will be instantiated at
	[SerializeField] [Range(1, 89)] private float _AngleOfRamp = 25;

	[SerializeField] private float _HealthUntilStep = 10;
	private float _CurrentHealth = 0;

	// An added height that the midpoint will be instantiated at
	private float _RampHeightOffset = 0;

	// Distance between the start point and the end point (X & Z)
	private float _DistanceBetween = 0;

	// How many times we'll have to iterate before reaching the destination
	private int _StepsToFinish = 0;

	// Stores all of the bridge pieces ([0] will be start ramp, [1] will be end ramp)
	private readonly List<GameObject> _BridgePieces = new List<GameObject>();

	// Stores the current position of the step and the step index
	private Vector3 _CurrentStepPos = Vector3.zero;
	private int _StepIndex = 0;
	private readonly List<PikminAI> _AttackingPikmin = new List<PikminAI>();

	public PikminIntention IntentionType => PikminIntention.Attack;

	private void Awake()
	{
		_DistanceBetween = Mathf.Sqrt(MathUtil.DistanceTo(_StartPoint.position, _EndPoint.position));
		// Calculate the amount of steps needed to stop building (- 2 because we build a start ramp and and end ramp)
		_StepsToFinish = Mathf.CeilToInt(_DistanceBetween / _StepSize) - 1;

		// Height offset for the angle of the ramp to not affect how the cube is planted in the ground (bottom corners aren't visible because they're in the floor)
		_RampHeightOffset = (Mathf.Sin(_AngleOfRamp * Mathf.Deg2Rad) / 2) - (_Piece.transform.localScale.y / 2);

		// look at the end from the start, and the start from the end
		Quaternion lookingAtEnd = Quaternion.LookRotation((_EndPoint.position - _StartPoint.position).normalized);
		Quaternion lookingAtStart = Quaternion.LookRotation((_StartPoint.position - _EndPoint.position).normalized);

		// Spawn both ramps, make them look at each end of the bride and add them to the _BridgePieces list
		GameObject startingRamp = Instantiate(_Piece);
		startingRamp.transform.SetPositionAndRotation(_StartPoint.position + Vector3.up * _RampHeightOffset,
			Quaternion.Euler(lookingAtEnd.eulerAngles.x - _AngleOfRamp, lookingAtEnd.eulerAngles.y, lookingAtEnd.eulerAngles.z));
		_BridgePieces.Add(startingRamp);

		GameObject endingRamp = Instantiate(_Piece);
		endingRamp.transform.SetPositionAndRotation(_EndPoint.position + Vector3.up * _RampHeightOffset,
			 Quaternion.Euler(lookingAtStart.eulerAngles.x - _AngleOfRamp, lookingAtStart.eulerAngles.y, lookingAtStart.eulerAngles.z));
		endingRamp.SetActive(false);
		_BridgePieces.Add(endingRamp);

		_CurrentStepPos = _StartPoint.position;
	}

	private void Update()
	{
		if (_CurrentHealth <= 0)
		{
			_CurrentHealth = _HealthUntilStep;
			Step();
		}
	}

	private void Step()
	{
		if (_StepIndex >= _StepsToFinish)
		{
			_AttackablePiece.SetActive(false);
			_BridgePieces[1].SetActive(true);
			return;
		}

		_CurrentStepPos = Vector3.MoveTowards(_CurrentStepPos, _EndPoint.position, _StepSize);
		Quaternion lookRotation = Quaternion.LookRotation((_CurrentStepPos - _EndPoint.position).normalized);
		Vector3 nextPosition = _CurrentStepPos + Vector3.up * (Mathf.Sin(_AngleOfRamp * Mathf.Deg2Rad) - (_Piece.transform.localScale.y / 2));
		GameObject bridgePiece = Instantiate(_Piece, nextPosition, lookRotation);
		_AttackablePiece.transform.SetPositionAndRotation(nextPosition + Vector3.up / 2, lookRotation);
		_BridgePieces.Add(bridgePiece);

		_StepIndex++;
	}

	private void OnDrawGizmos()
	{
		// Draw the outline of the bridge
		if (_Piece == null || _StartPoint == null || _EndPoint == null || _StepSize < 0)
		{
			return;
		}

		// Grab the Mesh as an optimisation
		Mesh pieceMesh = _Piece.GetComponent<MeshFilter>().sharedMesh;

		float distBetween = Mathf.Sqrt(MathUtil.DistanceTo(_StartPoint.position, _EndPoint.position));
		// Calculate the amount of steps needed to stop building (- 2 because we build a start ramp and and end ramp)
		int stepsToFinish = Mathf.CeilToInt(distBetween / _StepSize) - 1;

		// Calculate the height offset for the ramps
		float rampHeightOffset = (Mathf.Sin(_AngleOfRamp * Mathf.Deg2Rad) / 2) - (_Piece.transform.localScale.y / 2);

		Gizmos.DrawWireSphere(_StartPoint.position + Vector3.up * rampHeightOffset, 1);
		Gizmos.DrawWireSphere(_EndPoint.position + Vector3.up * rampHeightOffset, 1);

		// Draw starting ramp
		Quaternion lookAtEnd = Quaternion.LookRotation((_EndPoint.position - _StartPoint.position).normalized);
		Gizmos.DrawMesh(pieceMesh, _StartPoint.position + (Vector3.up * rampHeightOffset),
									Quaternion.Euler(lookAtEnd.eulerAngles.x - _AngleOfRamp, lookAtEnd.eulerAngles.y, lookAtEnd.eulerAngles.z),
									_Piece.transform.localScale);
		// Draw ending ramp
		Quaternion lookAtStart = Quaternion.LookRotation((_StartPoint.position - _EndPoint.position).normalized);
		Gizmos.DrawMesh(pieceMesh, _EndPoint.position + (Vector3.up * rampHeightOffset),
			Quaternion.Euler(lookAtStart.eulerAngles.x - _AngleOfRamp, lookAtStart.eulerAngles.y, lookAtStart.eulerAngles.z),
			_Piece.transform.localScale);

		Vector3 point = _StartPoint.position;
		for (int i = 0; i < stepsToFinish; i++)
		{
			point = Vector3.MoveTowards(point, _EndPoint.position, _StepSize);
			Quaternion lookRotation = Quaternion.LookRotation((point - _EndPoint.position).normalized);
			Gizmos.DrawMesh(pieceMesh,
				point + (Vector3.up * (Mathf.Sin(_AngleOfRamp * Mathf.Deg2Rad) - (_Piece.transform.localScale.y / 2))), lookRotation,
				_Piece.transform.localScale);
		}
	}

	#region Pikmin Attacking Implementation

	public void OnAttackRecieve(float damage)
	{
		SubtractHealth(damage);
	}

	public void OnAttackStart(PikminAI attachedPikmin)
	{
		_AttackingPikmin.Add(attachedPikmin);
	}

	public void OnAttackEnd(PikminAI detachedPikmin)
	{
		_AttackingPikmin.Remove(detachedPikmin);
	}
	#endregion

	#region Health Implementation

	// 'Getter' functions
	public float GetCurrentHealth()
	{
		return _CurrentHealth;
	}

	public float GetMaxHealth()
	{
		return _HealthUntilStep;
	}

	// 'Setter' functions
	public float AddHealth(float give)
	{
		return _CurrentHealth += give;
	}

	public float SubtractHealth(float take)
	{
		return _CurrentHealth -= take;
	}

	public void SetHealth(float set)
	{
		_CurrentHealth = set;
	}

	#endregion
}
