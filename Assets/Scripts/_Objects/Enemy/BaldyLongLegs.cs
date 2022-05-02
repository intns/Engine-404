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
	public LayerMask _FootStompInteractMask;
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
		_Target.SetPositionAndRotation(_TargetPosition, _TargetRotation);

		if (_LiftTimer >= _RNGTime)
		{
			if (_OtherFoot.IsMoving())
			{
				return;
			}

			Vector3 dirToObj = MathUtil.DirectionFromTo(_RaycastObj.transform.position, _Parent._Target.position);

			// If we couldn't hit the map, and the distance isn't big enough
			if (!Physics.SphereCast(_RaycastObj.transform.position + (dirToObj * 8), _Values._FootOverlapSphereSize, Vector3.down, out RaycastHit hit, 200, _Values._MapMask)
				|| Vector3.Distance(_NewPosition, hit.point) <= _Values._DistanceForStep)
			{
				return;
			}

			_LiftTimer = 0;
			_NewPosition = hit.point + (Vector3.up * _Values._FootHeightFloorOffset);
			_NewRotation = Quaternion.FromToRotation(Vector3.down, hit.normal);

			AudioSource.PlayClipAtPoint(_Values._FootStep, _Target.position, 2);

			_RNGTime = Random.Range(_Values._LegLiftTimeRange.x, _Values._LegLiftTimeRange.y);
		}
		else // We're lifting the leg
		{
			float t = _Values._LegLiftCurve.Evaluate(_LiftTimer / _RNGTime);

			_TargetPosition = Vector3.Lerp(_Target.position, _NewPosition, t);
			_TargetPosition.y += Mathf.Abs(Mathf.Sin(t * Mathf.PI)) * _Values._StepHeight;

			_LiftTimer += Time.deltaTime * _Values._StepSpeed;

			if (_LiftTimer < _RNGTime)
			{
				return;
			}

			// We've just touched the floor!
			_TargetRotation = _NewRotation;

			Collider[] colls = Physics.OverlapSphere(_Target.position, _Values._FootOverlapSphereSize, _Values._FootStompInteractMask);
			foreach (var coll in colls)
			{
				if (coll.CompareTag("Pikmin"))
				{
					coll.GetComponent<PikminAI>().Die(0.5f);
				}
				else if (coll.CompareTag("Player"))
				{
					var player = coll.GetComponent<Player>();
					player.SubtractHealth(player.GetMaxHealth() / 3);
				}
			}
		}
	}
}


[RequireComponent(typeof(EnemyDamageScript))]
public class BaldyLongLegs : MonoBehaviour, IPikminAttack
{
	[Header("Components")]
	[SerializeField] Transform[] _LegTargets;
	public Transform _Target;

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
		return transform.position + MathUtil.XZToXYZ(MathUtil.PositionInUnit(_LegTargets.Length, legIdx + 1, _LegRotation) * _LegDistance, transform.position.y + 50);
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

		Vector3 target = _Target.position;
		target.y = transform.position.y;
		transform.position = Vector3.Lerp(transform.position, target, _Speed * Time.deltaTime);
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
