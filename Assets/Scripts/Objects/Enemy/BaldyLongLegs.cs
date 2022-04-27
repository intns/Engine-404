using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BLL_Values
{
	public float _FootRotationSpeed;
	public float _FootOverlapSphereSize;
	public float _FootHeightFloorOffset;

	[Space]
	public AnimationCurve _LegLiftCurve;
	public Vector2 _LegLiftTimeRange; // X is min bound Y is max bound

	[Space]
	public float _StepSpeed;
	public float _StepHeight;
	public float _DistanceForStep;

	[Space]
	public LayerMask _MapMask;
	public AudioClip _FootStep;
}

[System.Serializable]
public class BaldyLongLegsFoot
{
	GameObject _RaycastObj = null;
	BaldyLongLegs _Parent = null;
	Transform _Target = null;
	int _LegIdx = 0;

	BLL_Values _Values;

	float _LiftTimer = 0;
	float _RNGTime = 0;
	Vector3 _NewPosition = Vector3.zero;
	Quaternion _NewRotation = Quaternion.identity;

	Vector3 _TargetPosition = Vector3.zero;
	Quaternion _TargetRotation = Quaternion.identity;

	BaldyLongLegsFoot _OtherFoot = null;

	public BaldyLongLegsFoot(BaldyLongLegs parentLegs, Transform target, int legIdx)
	{
		_RaycastObj = new GameObject();
		_Parent = parentLegs;

		_Target = target;
		_LegIdx = legIdx;
	}

	public bool IsMoving() { return _LiftTimer < _RNGTime; }

	public void Set(BaldyLongLegsFoot otherFoot, BLL_Values values)
	{
		_Values = values;
		_RNGTime = Random.Range(_Values._LegLiftTimeRange.x, _Values._LegLiftTimeRange.y);

		_OtherFoot = otherFoot;
	}

	public void Update()
	{
		_RaycastObj.transform.localPosition = _Parent.GetRaycastObjPosition(_LegIdx);
		_Target.position = _TargetPosition;
		_Target.rotation = _TargetRotation;

		if (_LiftTimer >= _RNGTime && !_OtherFoot.IsMoving())
		{
			if (Physics.SphereCast(_RaycastObj.transform.position, _Values._FootOverlapSphereSize, Vector3.down, out RaycastHit hit, 200, _Values._MapMask)
				&& Vector3.Distance(_NewPosition, hit.point) > _Values._DistanceForStep)
			{
				_LiftTimer = 0;
				_NewPosition = hit.point + Vector3.up * _Values._FootHeightFloorOffset;
				_NewRotation = Quaternion.FromToRotation(Vector3.down, hit.normal);

				AudioSource.PlayClipAtPoint(_Values._FootStep, _Target.position, 2);

				_RNGTime = Random.Range(_Values._LegLiftTimeRange.x, _Values._LegLiftTimeRange.y);
			}
		}

		if (_LiftTimer < _RNGTime)
		{
			float t = _Values._LegLiftCurve.Evaluate(_LiftTimer / _RNGTime);
			_TargetPosition = Vector3.Lerp(_Target.position, _NewPosition, t);
			_TargetPosition.y += Mathf.Sin(t * Mathf.PI) * _Values._StepHeight;
			_TargetRotation = Quaternion.Lerp(_Target.rotation, _NewRotation, _LiftTimer / _RNGTime);

			_LiftTimer += Time.deltaTime * _Values._StepSpeed;
		}
	}
}


[RequireComponent(typeof(EnemyDamageScript))]
public class BaldyLongLegs : MonoBehaviour, IPikminAttack
{
	[Header("Components")]
	[SerializeField] Transform[] _LegTargets;

	[Header("Settings")]
	[SerializeField] BLL_Values _Values;
	[SerializeField] LayerMask _MapMask;

	[SerializeField] float _LegRotation;
	[SerializeField] float _LegDistance;
	[SerializeField] float _Speed;

	[Header("Debug")]
	[SerializeField] bool _TestIK = false;

	List<BaldyLongLegsFoot> _Feet = new List<BaldyLongLegsFoot>();
	private EnemyDamageScript _DamageScript = null;

	public Vector3 GetRaycastObjPosition(int legIdx)
	{
		return transform.position + MathUtil.XZToXYZ(MathUtil.PositionInUnit(4, legIdx + 1, _LegRotation) * _LegDistance, transform.position.y + 50);
	}

	private void Awake()
	{
		_DamageScript = GetComponent<EnemyDamageScript>();

		for (int i = 0; i < _LegTargets.Length; i++)
		{
			_Feet.Add(new BaldyLongLegsFoot(this, _LegTargets[i], i));
		}

		_Feet[0].Set(_Feet[1], _Values);
		_Feet[1].Set(_Feet[2], _Values);
		_Feet[2].Set(_Feet[3], _Values);
		_Feet[3].Set(_Feet[0], _Values);
	}

	private void Update()
	{
		if (_TestIK)
		{
			//StartCoroutine(IE_TestIK());
			_TestIK = false;
		}

		for (int i = 0; i < _LegTargets.Length; i++)
		{
			_Feet[i].Update();
		}

		Vector3 playerPos = Player._Instance.transform.position;
		if (Physics.SphereCast(transform.position, 7, Vector3.down, out RaycastHit info, float.PositiveInfinity, _MapMask))
		{
			playerPos.y += (info.distance / 2);
		}

		transform.position = Vector3.Lerp(transform.position, playerPos, _Speed * Time.deltaTime);
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
