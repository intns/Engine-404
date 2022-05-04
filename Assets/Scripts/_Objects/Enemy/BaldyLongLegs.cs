using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BLL_Values
{
	public float _FootHeightFloorOffset;

	[Space]
	public AnimationCurve _LegLiftCurve;
	public Vector2 _LegLiftTimeRange; // X is min bound Y is max bound

	[Space]
	public float _StepHeight;
	public float _DistanceForStep;

	public bool _FlipToes;

	[Space]
	public LayerMask _MapMask;
	public LayerMask _FootStompInteractMask;
	public AudioClip _FootStep;
}

[System.Serializable]
public class BaldyLongLegsFoot
{
	GameObject _RaycastObj = null;
	BaldyLongLegs _Parent = null;
	Transform _Target = null;

	Collider _DeathCollider = null;
	Collider_PikminDie _DieCollider = null;
	Collider_PikminSlip _SlipCollider = null;

	int _LegIdx = 0;

	BLL_Values _Values;

	float _LiftTimer = 0;
	float _RNGTime = 0;

	float _DeathCollTimer = 0;

	Vector3 _NewPosition = Vector3.zero;
	Vector3 _TargetPosition = Vector3.zero;

	List<BaldyLongLegsFoot> _OtherFeet = new List<BaldyLongLegsFoot>();

	public BaldyLongLegsFoot(BaldyLongLegs parentLegs, Transform target, Collider dColl, int legIdx)
	{
		_RaycastObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		_RaycastObj.name = $"name={target.name} idx={legIdx}";
		_Parent = parentLegs;

		_DeathCollider = dColl;
		_DeathCollider.enabled = true;
		_DieCollider = _DeathCollider.GetComponent<Collider_PikminDie>();
		_SlipCollider = _DeathCollider.GetComponent<Collider_PikminSlip>();

		_Target = target;
		_LegIdx = legIdx;

		_DeathCollTimer = 0.0f;
	}

	public bool IsMoving() { return _LiftTimer < _RNGTime; }

	public void Set(List<BaldyLongLegsFoot> otherFeet, BLL_Values values)
	{
		_Values = values;
		_RNGTime = Random.Range(_Values._LegLiftTimeRange.x, _Values._LegLiftTimeRange.y);

		_OtherFeet = otherFeet;

		_RaycastObj.transform.localPosition = _Parent.GetRaycastObjPosition(_LegIdx);
		if (!Physics.SphereCast(_RaycastObj.transform.position, 5, Vector3.down, out RaycastHit hit, 200, _Values._MapMask))
		{
			return;
		}

		_NewPosition = hit.point + (Vector3.up * _Values._FootHeightFloorOffset);
		_Target.position = _NewPosition;
	}

	public void Update()
	{
		_RaycastObj.transform.localPosition = _Parent.GetRaycastObjPosition(_LegIdx);

		Quaternion nextRotation = _Target.rotation;
		if (Physics.Raycast(_RaycastObj.transform.position, Vector3.down, out RaycastHit hit, float.PositiveInfinity, _Values._MapMask))
		{
			if (_Values._FlipToes)
			{
				nextRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
			}
			else
			{
				nextRotation = Quaternion.FromToRotation(Vector3.down, hit.normal);
			}
		}

		_Target.SetPositionAndRotation(_TargetPosition, nextRotation);

		if (_DeathCollTimer <= 0.25f)
		{
			_DieCollider._Enabled = false;
			_DeathCollTimer += Time.deltaTime;
		}
		else
		{
			_DieCollider._Enabled = true;
			_SlipCollider.enabled = true;
		}

		if (_LiftTimer >= _RNGTime)
		{
			for (int i = 0; i < _OtherFeet.Count; i++)
			{
				if (_OtherFeet[i].IsMoving())
				{
					return;
				}
			}

			Vector3 dirToObj = MathUtil.DirectionFromTo(_RaycastObj.transform.position, _Parent._Target.position);

			// If we couldn't hit the map, and the distance isn't big enough
			if (!Physics.SphereCast(_RaycastObj.transform.position + (dirToObj * (_Values._DistanceForStep / 2)), 5, Vector3.down, out hit, float.PositiveInfinity, _Values._MapMask)
				|| Vector3.Distance(_NewPosition, hit.point) <= _Values._DistanceForStep)
			{
				return;
			}

			_LiftTimer = 0;
			_NewPosition = hit.point + (Vector3.up * _Values._FootHeightFloorOffset);

			AudioSource.PlayClipAtPoint(_Values._FootStep, Camera.main.transform.position);

			_RNGTime = Random.Range(_Values._LegLiftTimeRange.x, _Values._LegLiftTimeRange.y);
		}
		else // We're lifting the leg
		{
			float t = _Values._LegLiftCurve.Evaluate(_LiftTimer / _RNGTime);

			_TargetPosition = Vector3.Lerp(_Target.position, _NewPosition, t);
			_TargetPosition.y += Mathf.Abs(Mathf.Sin(t * Mathf.PI)) * _Values._StepHeight;

			_LiftTimer += Time.deltaTime;

			if (_LiftTimer < _RNGTime - 0.25f)
			{
				_DieCollider._Enabled = false;
				_SlipCollider.enabled = true;

				_DeathCollTimer = 0;
			}
		}
	}
}


[RequireComponent(typeof(EnemyDamageScript))]
public class BaldyLongLegs : MonoBehaviour, IPikminAttack
{
	[Header("Components")]
	[SerializeField] Transform[] _LegTargets;
	[SerializeField] Collider[] _LegColliders;
	public Transform _Target;

	[Header("Settings")]
	[SerializeField] BLL_Values _Values;
	[SerializeField] LayerMask _MapMask;

	[SerializeField] float _LegRotation;
	[SerializeField] float _LegDistance;
	[SerializeField] float _Speed;

	List<BaldyLongLegsFoot> _Feet = new List<BaldyLongLegsFoot>();
	private EnemyDamageScript _DamageScript = null;

	public Vector3 GetRaycastObjPosition(int legIdx)
	{
		return transform.position + MathUtil.XZToXYZ(MathUtil.PositionInUnit(_LegTargets.Length, legIdx, _LegRotation) * _LegDistance, transform.position.y + 50);
	}

	private void Awake()
	{
		_DamageScript = GetComponent<EnemyDamageScript>();

		for (int i = 0; i < _LegTargets.Length; i++)
		{
			Vector3 currentPos = GetRaycastObjPosition(i);

			int currentIdx = 0;
			float minDist = float.PositiveInfinity;
			for (int j = 0; j < _LegTargets.Length; j++)
			{
				float currentDist = MathUtil.DistanceTo(_LegTargets[j].position, currentPos, false);
				if (currentDist < minDist)
				{
					minDist = currentDist;
					currentIdx = i;
				}
			}

			_Feet.Add(new BaldyLongLegsFoot(this, _LegTargets[currentIdx], _LegColliders[currentIdx], currentIdx));
		}

		for (int i = 0; i < _Feet.Count; i++)
		{
			List<BaldyLongLegsFoot> feet = new();

			for (int j = 0; j < _Feet.Count; j++)
			{
				if (j == i)
				{
					continue;
				}

				feet.Add(_Feet[j]);
			}

			_Feet[i].Set(feet, _Values);
		}
	}

	private void Update()
	{
		_LegRotation += Time.deltaTime / 20;

		for (int i = 0; i < _Feet.Count; i++)
		{
			_Feet[i].Update();
		}

		Vector3 target = _Target.position;
		target.y = transform.position.y;

		Vector3 velocity = Vector3.zero;
		transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, Time.smoothDeltaTime, _Speed);
	}


	#region Pikmin Attacking Implementation
	public PikminIntention IntentionType => PikminIntention.Attack;

	public void OnAttackEnd(PikminAI pikmin)
	{
		_DamageScript._AttachedPikmin.Remove(pikmin);
	}

	public void OnAttackStart(PikminAI pikmin)
	{
		_DamageScript._AttachedPikmin.Add(pikmin);
	}

	public void OnAttackRecieve(float damage)
	{
		if (this == null || _DamageScript == null)
		{
			return;
		}

		_DamageScript.SubtractHealth(damage);
		_DamageScript._HWScript._CurrentHealth = _DamageScript.GetCurrentHealth();
	}
	#endregion
}
