using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BLL_Values
{
	public float _FootHeightFloorOffset = 0.8f;

	[Space]
	public AnimationCurve _LegLiftCurve;
	public Vector2 _LegLiftTimeRange = new Vector2(1.2f, 1.6f); // X is min bound Y is max bound

	[Space]
	public float _StepHeight = 3;
	public float _DistanceForStep = 2;

	public bool _FlipToes = false;

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

	int _LegIdx = 0;

	BLL_Values _Values;

	float _LiftTimer = 0;
	float _RNGTime = 0;

	float _DeathCollTimer = 0;

	Vector3 _NewPosition = Vector3.zero;
	Vector3 _TargetPosition = Vector3.zero;

	ParticleSystem _StompFX = null;

	List<BaldyLongLegsFoot> _OtherFeet = new List<BaldyLongLegsFoot>();

	public BaldyLongLegsFoot(BaldyLongLegs parentLegs, Transform target, Collider dColl, int legIdx)
	{
		_RaycastObj = new();
		_RaycastObj.name = $"name={target.name} idx={legIdx}";
		_Parent = parentLegs;

		_DeathCollider = dColl;
		_DeathCollider.enabled = true;
		_DieCollider = _DeathCollider.GetComponent<Collider_PikminDie>();

		_Target = target;
		_StompFX = _Target.GetComponentInChildren<ParticleSystem>();

		_LegIdx = legIdx;

		_DeathCollTimer = 0.0f;
	}

	public float GetDistanceFromParent()
	{
		float x = _Target.position.x - _Parent.transform.position.x;
		float z = _Target.position.z - _Parent.transform.position.z;
		return x * x + z * z;
	}

	public bool IsMoving() { return _LiftTimer < _RNGTime; }

	public void Set(List<BaldyLongLegsFoot> otherFeet, BLL_Values values)
	{
		_Values = values;
		_RNGTime = Random.Range(_Values._LegLiftTimeRange.x, _Values._LegLiftTimeRange.y);

		_OtherFeet = otherFeet;

		_RaycastObj.transform.localPosition = _Parent.GetRaycastObjPosition(_LegIdx);

		Vector3 dirToObj = MathUtil.DirectionFromTo(_RaycastObj.transform.position, _Parent._Target.position);
		if (!Physics.SphereCast(_RaycastObj.transform.position + (dirToObj * (_Values._DistanceForStep / 2)), 5, Vector3.down, out RaycastHit hit, float.PositiveInfinity, _Values._MapMask))
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
		if (Physics.Raycast(_Target.transform.position, Vector3.down, out RaycastHit hit, float.PositiveInfinity, _Values._MapMask))
		{
			nextRotation = Quaternion.FromToRotation(_Values._FlipToes ? Vector3.up : Vector3.down, hit.normal);
		}

		_Target.SetPositionAndRotation(_TargetPosition, nextRotation);

		if (_LiftTimer < _RNGTime)
		{
			// Lift the leg
			float t = _Values._LegLiftCurve.Evaluate(_LiftTimer / _RNGTime);

			_TargetPosition = Vector3.Lerp(_Target.position, _NewPosition, t);
			_TargetPosition.y += Mathf.Abs(Mathf.Sin(t * Mathf.PI)) * _Values._StepHeight;

			_LiftTimer += Time.deltaTime;

			if (_LiftTimer >= _RNGTime - 0.125f)
			{
				_StompFX.Play();
				_DieCollider._Enabled = true;
				_DeathCollTimer = 0;

				var colls = Physics.OverlapSphere(_Target.position, 4, _Values._FootStompInteractMask);
				for (int i = 0; i < colls.Length; i++)
				{
					colls[i].GetComponent<PikminAI>().Die(0.5f);
				}
			}
		}
		else
		{
			_DeathCollTimer += Time.deltaTime;

			if (_DeathCollTimer <= 0.15f)
			{
				_DieCollider._Enabled = false;
			}

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
			_DeathCollTimer = 0;
			_RNGTime = Random.Range(_Values._LegLiftTimeRange.x, _Values._LegLiftTimeRange.y);

			_NewPosition = hit.point + (Vector3.up * _Values._FootHeightFloorOffset);
			AudioSource.PlayClipAtPoint(_Values._FootStep, Camera.main.transform.position);
		}
	}
}


[RequireComponent(typeof(EnemyDamageScript))]
public class BaldyLongLegs : MonoBehaviour, IPikminAttack
{
	enum States
	{
		Spawning,
		Idle,
		Shake
	}

	[Header("Components")]
	[SerializeField] Transform[] _LegTargets;
	[SerializeField] Collider[] _LegColliders;
	public Transform _Target;

	[Header("Settings")]
	[SerializeField] BLL_Values _Values;
	[SerializeField] LayerMask _MapMask;
	[Space]
	[SerializeField] float _LegDistance = 9;
	[SerializeField] float _DistanceForSlow = 175;
	[SerializeField] float _Height = 25;
	[SerializeField] float _MaxSpeed = 2.5f;
	[SerializeField] float _Offset = 0;

	float _MoveSpeed = 0.0f;
	float _CircleTimer = 0;
	List<BaldyLongLegsFoot> _Feet = new List<BaldyLongLegsFoot>();
	private EnemyDamageScript _DamageScript = null;

	public Vector3 GetRaycastObjPosition(int legIdx)
	{
		return transform.position + MathUtil.XZToXYZ(MathUtil.PositionInUnit(_LegTargets.Length, legIdx, _Offset) * _LegDistance, transform.position.y + 50);
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
		for (int i = 0; i < _Feet.Count; i++)
		{
			_Feet[i].Update();
		}

		Vector3 velocity = Vector3.zero;

		float maxDist = 0;
		for (int i = 0; i < _Feet.Count; i++)
		{
			float curDist = _Feet[i].GetDistanceFromParent();
			maxDist = Mathf.Max(maxDist, curDist);
		}

		_MoveSpeed = Mathf.Lerp(_MoveSpeed, maxDist <= _DistanceForSlow ? _MaxSpeed : 0, 1.5f * Time.deltaTime);

		Vector3 target = _Target.position + MathUtil.XZToXYZ(MathUtil.PositionInUnit(_CircleTimer / 8) * 12);
		if (Physics.SphereCast(transform.position + Vector3.up * 50, 4, Vector3.down, out RaycastHit hit, float.PositiveInfinity, _MapMask, QueryTriggerInteraction.Ignore))
		{
			target.y = hit.point.y + _Height;
		}

		transform.SetPositionAndRotation(
			Vector3.SmoothDamp(transform.position, target, ref velocity, Time.smoothDeltaTime, _MoveSpeed),
			Quaternion.AngleAxis(-_Offset * 120, Vector3.up));

		_Offset += Time.deltaTime / 7;
		if (_Offset >= 360)
		{
			_Offset -= 360;
		}
		_CircleTimer += Time.deltaTime;
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
