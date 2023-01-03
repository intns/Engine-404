using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.VersionControl.Asset;

[System.Serializable]
public class Entity : MonoBehaviour, IPikminAttack, IHealth
{
	[Header("Components")]
	[SerializeField, Range(0.0f, 360.0f)] float _RotationSpeed = 10.0f;
	[SerializeField, Range(0.0f, 1.0f)] float _RotationAcceleration = 0.1f;

	[Header("Health Wheel")]
	[SerializeField] GameObject _HWObject = null;
	[SerializeField] Vector3 _HWOffset = Vector3.up;
	[SerializeField] float _HWScale = 1;


	[SerializeField] float _MaxHealth = 3500;
	[SerializeField] Vector3 _DeadOffset = Vector3.zero;
	[SerializeField] GameObject _DeadObject = null;

	float _CurrentHealth = 0.0f;
	float _DamageAnimationTimer = 0.0f;

	bool _IsTakingDamage = false;

	List<PikminAI> _AttachedPikmin = new List<PikminAI>();
	HealthWheel _HWScript = null;
	Transform _Transform = null;

	public virtual void Awake()
	{
		_Transform = transform;

		_HWScript = Instantiate(_HWObject, transform.position + _HWOffset, Quaternion.identity).GetComponentInChildren<HealthWheel>();
		_HWScript._Parent = transform;
		_HWScript._Offset = _HWOffset;
		_HWScript._InUse = true;
		_HWScript._MaxHealth = _MaxHealth;
		_HWScript._CurrentHealth = _MaxHealth;
		_HWScript.transform.localScale = Vector3.one * _HWScale;
	}

	public virtual void Update()
	{
		ScaleDamageAnimation();
	}

	#region Private Methods
	private void ScaleDamageAnimation()
	{
		if (_DamageAnimationTimer == 0.0f)
		{
			if (_IsTakingDamage)
			{
				_DamageAnimationTimer += Time.deltaTime;
			}

			return;
		}

		float horizontalMod = 0.0f;
		float scaleDuration = 0.35f;
		float factor = 1.0f;

		_DamageAnimationTimer += Time.deltaTime;

		if (_DamageAnimationTimer > scaleDuration)
		{
			_DamageAnimationTimer = 0.0f;
		}
		else
		{
			float s = Mathf.Sin((_DamageAnimationTimer / scaleDuration) * MathUtil.M_TAU);
			float t = 1.0f - (_DamageAnimationTimer / scaleDuration);
			horizontalMod = t * s;
		}

		float xzScale = horizontalMod * (factor * 0.2f);

		_Transform.localScale = new(1.0f - xzScale, horizontalMod * 0.25f + 1.0f, 1.0f - xzScale);
	}
	#endregion

	#region Public Methods
	public float GetFaceDirection()
	{
		return _Transform.eulerAngles.y * Mathf.Deg2Rad;
	}

	public float ChangeFaceDirection(Transform target)
	{
		float rotSpeed = _RotationSpeed;
		float rotAccel = _RotationAcceleration;

		Vector3 targetPos = target.position;
		Vector3 pos = _Transform.position;

		float angleDist = MathUtil.AngleDistance(MathUtil.AngleXZ(targetPos, pos), GetFaceDirection());

		float limit = (Mathf.Deg2Rad * rotSpeed) * Mathf.PI;
		float approxSpeed = angleDist * rotAccel;
		if (Mathf.Abs(approxSpeed) > limit)
		{
			approxSpeed = (approxSpeed > 0.0f) ? limit : -limit;
		}

		float faceDir = MathUtil.RoundAngle(approxSpeed + GetFaceDirection());
		_Transform.eulerAngles = new Vector3(0, faceDir * Mathf.Rad2Deg, 0);
		return angleDist;
	}

	public void SetupRadarObject()
	{
		RadarController._Instance._RadarObjects.Add(this);
	}

	#region Attacking
	public PikminIntention IntentionType => PikminIntention.Attack;

	public void OnAttackEnd(PikminAI pikmin)
	{
		_AttachedPikmin.Remove(pikmin);
		if (_AttachedPikmin.Count == 0)
		{
			_IsTakingDamage = false;
		}
	}

	public void OnAttackStart(PikminAI pikmin)
	{
		_AttachedPikmin.Add(pikmin);
	}

	public void OnAttackRecieve(float damage)
	{
		if (this == null)
		{
			return;
		}

		_IsTakingDamage = true;

		// Should be called last in case the 
		SubtractHealth(damage);
		_HWScript._CurrentHealth = GetCurrentHealth();
	}
	#endregion
	#region IHealth
	// 'Getter' functions
	public float GetCurrentHealth()
	{
		return _CurrentHealth;
	}

	public float GetMaxHealth()
	{
		return _MaxHealth;
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
	#endregion
}
