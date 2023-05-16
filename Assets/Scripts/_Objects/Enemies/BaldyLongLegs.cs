using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

[System.Serializable]
public class BLL_Values
{
	public float _FootHeightFloorOffset = 0.8f;

	[Space]
	public Vector3 _FootColliderOffset = Vector3.zero;
	public float _FootColliderSize = 2;

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

	int _LegIdx = 0;

	BLL_Values _Values;

	float _LiftTimer = 0;
	float _RNGTime = 0;

	Vector3 _NewPosition = Vector3.zero;
	Vector3 _TargetPosition = Vector3.zero;

	CameraFollow _Camera = null;

	VisualEffect _StompFX = null;
	VisualEffect _TrailFX = null;

	List<BaldyLongLegsFoot> _OtherFeet = new List<BaldyLongLegsFoot>();

	public BaldyLongLegsFoot(BaldyLongLegs parentLegs, Transform target, Collider dColl, int legIdx)
	{
		_RaycastObj = new();
		_RaycastObj.name = $"name={target.name} idx={legIdx}";
		_Parent = parentLegs;

		_Target = target;
		_StompFX = _Target.GetChild(0).GetComponentInChildren<VisualEffect>();
		_TrailFX = _Target.GetChild(1).GetComponentInChildren<VisualEffect>();

		_StompFX.Stop();
		_TrailFX.Stop();

		_LegIdx = legIdx;
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

		_Camera = CameraFollow._Instance;
		_TargetPosition = _Target.position;

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

		_Target.position = _TargetPosition;
		Physics.Raycast(_Target.transform.position, Vector3.down, out RaycastHit hit, float.PositiveInfinity, _Values._MapMask);

		if (_LiftTimer < _RNGTime)
		{
			// Lift the leg
			float realT = _LiftTimer / _RNGTime;
			float t = _Values._LegLiftCurve.Evaluate(realT);

			_TargetPosition = Vector3.Lerp(_Target.position, _NewPosition, t);
			_TargetPosition.y += Mathf.Abs(Mathf.Sin(t * Mathf.PI)) * _Values._StepHeight;

			Vector3 euler = Quaternion.FromToRotation(Vector3.down, hit.normal).eulerAngles;

			const float minDist = 0.01f;
			const float maxDist = 1.0f;
			if (t >= minDist && t <= maxDist)
			{
				float normalised = Mathf.InverseLerp(minDist, maxDist, realT);
				euler.z += Mathf.Abs(Mathf.Sin(normalised * Mathf.PI)) * 20;
			}

			_Target.rotation = Quaternion.Euler(euler);

			_LiftTimer += Time.deltaTime;
			if (_LiftTimer + 0.1f >= _RNGTime)
			{
				_StompFX.Play();
				_TrailFX.Stop();

				if (_LiftTimer + 0.06f >= _RNGTime)
				{
					_Camera.Shake(2);
				}

				Collider[] colls = Physics.OverlapSphere(_Target.position + _Values._FootColliderOffset, _Values._FootColliderSize, _Values._FootStompInteractMask);
				foreach (Collider v in colls)
				{
					if (v.TryGetComponent(out IInteraction interaction))
					{
						interaction.ActSquish();
					}
				}
			}

		}
		else
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
			_RNGTime = Random.Range(_Values._LegLiftTimeRange.x, _Values._LegLiftTimeRange.y);
			_TrailFX.Play();

			_NewPosition = hit.point + (Vector3.up * _Values._FootHeightFloorOffset);
			AudioSource.PlayClipAtPoint(_Values._FootStep, Camera.main.transform.position);
		}
	}

	public void SpawnUpdate()
	{
		_Target.position = _TargetPosition;

		_RaycastObj.transform.localPosition = _Parent.GetRaycastObjPosition(_LegIdx);
		Vector3 dirToObj = MathUtil.DirectionFromTo(_RaycastObj.transform.position, _Parent._Target.position);
		if (Physics.SphereCast(_RaycastObj.transform.position + (dirToObj * (_Values._DistanceForStep / 2)), 5, Vector3.down, out RaycastHit hit, float.PositiveInfinity, _Values._MapMask))
		{
			_TargetPosition = hit.point + (Vector3.up * _Values._FootHeightFloorOffset);
			Vector3 euler = Quaternion.FromToRotation(Vector3.down, hit.normal).eulerAngles;
			_Target.rotation = Quaternion.Euler(euler);
		}

		_LiftTimer = _RNGTime;
	}

	public void PlayStompFX()
	{
		_StompFX.Play();
		_Camera.Shake(15);
	}
}


public class BaldyLongLegs : MonoBehaviour, IPikminAttack
{
	enum States
	{
		Spawning,
		Active,
		Shake,
		DeathStart,
		Death
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

	[Header("Debugging")]
	[SerializeField] States _State = States.Spawning;

	float _MoveSpeed = 0.0f;
	float _CircleTimer = 0;
	List<BaldyLongLegsFoot> _Feet = new List<BaldyLongLegsFoot>();
	/*EnemyDamageScript _DamageScript = null;*/
	Animator _Animator;

	public Vector3 GetRaycastObjPosition(int legIdx)
	{
		return transform.position + MathUtil.XZToXYZ(MathUtil.PositionInUnit(_LegTargets.Length, legIdx, _Offset) * _LegDistance, transform.position.y + 50);
	}

	void Awake()
	{
		_Animator = GetComponent<Animator>();
/*		_DamageScript = GetComponent<EnemyDamageScript>();*/

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

	void OnDrawGizmosSelected()
	{
		if (_LegTargets.Length == 0)
		{
			return;
		}


		foreach (Transform t in _LegTargets)
		{
			Gizmos.DrawWireSphere(t.position + _Values._FootColliderOffset, _Values._FootColliderSize);
		}
	}

	void Update()
	{
		if (GameManager.IsPaused)
		{
			return;
		}

		switch (_State)
		{
			case States.Spawning:
				HandleSpawning();
				break;
			case States.Active:
				HandleActive();
				break;
			case States.Shake:
				break;
			case States.DeathStart:
				bool finishing = false;
				for (int i = 0; i < _Feet.Count; i++)
				{
					if (_Feet[i].IsMoving())
					{
						_Feet[i].Update();
						finishing = true;
					}
				}

				if (!finishing)
				{
					_State = States.Death;
					_Animator.Play("Death");

				/*	while (_DamageScript._AttachedPikmin.Count > 0)
					{
						PikminAI pik = _DamageScript._AttachedPikmin[0];
						if (pik == null)
						{
							break;
						}

						pik.ChangeState(PikminStates.Idle);
						pik._AddedVelocity = MathUtil.DirectionFromTo(transform.position, pik.transform.position) * 5;
					}*/
				}

				break;

			case States.Death:
				/*while (_DamageScript._AttachedPikmin.Count > 0)
				{
					PikminAI pik = _DamageScript._AttachedPikmin[0];
					if (pik == null)
					{
						break;
					}

					pik.ChangeState(PikminStates.Idle);
					pik._AddedVelocity = MathUtil.DirectionFromTo(transform.position, pik.transform.position) * 5;
				}*/
				break;
			default:
				break;
		}
	}

	#region States

	bool _Spawning = false;
	Vector3 _StartPosition;
	Vector3 _EndPosition;
	float _SpawnTimer = 0;
	[SerializeField] AnimationCurve _SpawnCurve;
	void HandleSpawning()
	{
		if (!_Spawning)
		{
			if (Physics.SphereCast(transform.position + Vector3.up * 50, 4, Vector3.down, out RaycastHit hit, float.PositiveInfinity, _MapMask, QueryTriggerInteraction.Ignore))
			{
				_StartPosition = transform.position + Vector3.up * 50;
				_EndPosition = hit.point;
			}

			_Spawning = true;
		}
		else
		{
			for (int i = 0; i < _Feet.Count; i++)
			{
				_Feet[i].SpawnUpdate();
			}

			float spawnTime = 1;

			Vector3 target = Vector3.Lerp(_StartPosition, _EndPosition, _SpawnCurve.Evaluate(_SpawnTimer / spawnTime));
			if (Physics.SphereCast(transform.position + Vector3.up * 50, 4, Vector3.down, out RaycastHit hit, float.PositiveInfinity, _MapMask, QueryTriggerInteraction.Ignore))
			{
				target.y += hit.point.y + _Height;
			}
			transform.position = target;

			_SpawnTimer += Time.deltaTime;

			if (_SpawnTimer + 0.05f >= spawnTime)
			{
				for (int i = 0; i < _Feet.Count; i++)
				{
					_Feet[i].PlayStompFX();
				}

				if (_SpawnTimer >= spawnTime)
				{
					_State = States.Active;
				}
			}
		}
	}

	void HandleActive()
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
			Quaternion.AngleAxis(-_Offset * 50, Vector3.up));

		_Offset += Time.deltaTime / 12;
		if (_Offset >= 360)
		{
			_Offset -= 360;
		}
		_CircleTimer += Time.deltaTime;
	}

	public void ANIM_Death()
	{
		/*Instantiate(_DamageScript._DeadObject[0], transform.position + _DamageScript._DeadObjectOffset, Quaternion.identity);*/
	}

	public void ANIM_DeathEnd()
	{
		Destroy(gameObject);
	}

	#endregion


	#region Pikmin Attacking Implementation
	public PikminIntention IntentionType => PikminIntention.Attack;
	bool IPikminAttack.IsAttackAvailable() => true;

	public void OnAttackEnd(PikminAI pikmin)
	{
/*		_DamageScript._AttachedPikmin.Remove(pikmin);
*/	}

	public void OnAttackStart(PikminAI pikmin)
	{
/*		_DamageScript._AttachedPikmin.Add(pikmin);
*/	}

	public void OnAttackRecieve(float damage, Transform hitPart = default)
	{
/*		if (this == null || _DamageScript == null)
		{
			return;
		}
*/
/*		_DamageScript._HWScript._CurrentHealth -= damage;

		float health = _DamageScript._HWScript._CurrentHealth;*/
		/*if (health <= 0)
		{
			_State = States.DeathStart;
		}*/
	}
	#endregion
}
