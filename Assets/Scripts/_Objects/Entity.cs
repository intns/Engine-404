using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Entity : MonoBehaviour, IPikminAttack, IHealth
{
	[Flags]
	public enum EntityFlags
	{
		None = 0,
		IsHealthEnabled = 1,      // Should we update the health wheel and 'die'?
		IsDamageAnimEnabled = 2,  // Should we do the wobble animation?
		IsVulnerable = 4,         // Can we take (subtract health) damage?
		ToDestroyOnDeath = 8,     // Should we destroy the object on death?
		IsAttackAvailable = 16,   // Should we allow Pikmin to run towards and latch to attack?
		RegenerateHealth = 32,    // Should we regenerate health over time?
		UseFaceDirection = 64,    // Should we use the face direction to rotate?
	}

	[Header("Components")]
	[SerializeField, Range(0.0f, 1.0f)] float _LifeRegenerationRatio = 0.01f;
	[Space]
	[SerializeField, Range(0.05f, 3.0f)] float _HorizontalDamageAnimFactor = 1.0f;
	[SerializeField, Range(0.05f, 3.0f)] float _VerticalDamageAnimFactor = 1.0f;

	[Header("Health Wheel")]
	[SerializeField] GameObject _HealthWheelPrefab = null;
	[SerializeField] Vector3 _HealthWheelOffset = Vector3.up;
	[SerializeField] float _HealthWheelScale = 1;
	[Space()]
	[SerializeField] float _MaxHealth = 3500;
	[SerializeField] Vector3 _DeathObjectOffset = Vector3.zero;
	[SerializeField] GameObject[] _DeathObjectPrefabs = null;

	[Header("Editor")]
	[SerializeField] bool _PlaceOnFloor = false;

	public EntityFlags _Flags = EntityFlags.IsHealthEnabled | EntityFlags.IsVulnerable
			| EntityFlags.IsDamageAnimEnabled | EntityFlags.ToDestroyOnDeath
			| EntityFlags.IsAttackAvailable;

	protected float _FaceDirection = 0.0f;
	protected Vector3 _EulerAngles = Vector3.zero;
	protected Vector3 _StartSize = Vector3.zero;

	[SerializeField]
	protected float _CurrentHealth = 0.0f;
	protected float _DamageAnimationTimer = 0.0f;
	protected bool _IsTakingDamage = false;


	protected List<PikminAI> _AttachedPikmin = new List<PikminAI>();
	protected HealthWheel _HealthWheelScript = null;
	protected Transform _Transform = null;

	public virtual void Awake()
	{
		_Transform = transform;
		_StartSize = _Transform.localScale;
		_CurrentHealth = _MaxHealth;

		_FaceDirection = _Transform.eulerAngles.y;

		_HealthWheelScript = Instantiate(_HealthWheelPrefab, transform.position + _HealthWheelOffset, Quaternion.identity).GetComponentInChildren<HealthWheel>();
		_HealthWheelScript.Setup(_Transform, _HealthWheelOffset, Vector3.one * _HealthWheelScale, _MaxHealth);
	}

	public virtual void Update()
	{
		_EulerAngles = _Transform.eulerAngles;

		// Hell...
		if (_Transform.position.y < -500)
		{
			Destroy(gameObject);
		}

		if (_Flags.HasFlag(EntityFlags.IsDamageAnimEnabled))
		{
			ScaleDamageAnimation();
		}

		if (_Flags.HasFlag(EntityFlags.IsHealthEnabled))
		{
			HandleHealth();
		}

		if (_Flags.HasFlag(EntityFlags.RegenerateHealth))
		{
			RegenerateHealth();
		}
	}

	public virtual void FixedUpdate()
	{
		if (_Flags.HasFlag(EntityFlags.UseFaceDirection))
		{
			float faceDir = GetFaceDirection();
			if (Mathf.DeltaAngle(faceDir, _FaceDirection) > 0.01f)
			{
				faceDir = Mathf.Lerp(faceDir, _FaceDirection, 2.5f * Time.fixedDeltaTime);
				_Transform.rotation = Quaternion.Euler(0.0f, faceDir, 0.0f);
			}
		}
	}

	public virtual void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position + _HealthWheelOffset, _HealthWheelScale);

		Gizmos.color = Color.red;
		if (_DeathObjectPrefabs != null && _DeathObjectPrefabs.Length > 0)
		{
			int tries = 0;
			for (int i = 0; i < _DeathObjectPrefabs.Length; i++)
			{
				if (tries++ > 1000)
				{
					break;
				}

				GameObject obj = _DeathObjectPrefabs[i];
				MeshFilter filter = obj.GetComponentInChildren<MeshFilter>();
				Mesh mesh;
				if (filter != null && (mesh = filter.sharedMesh) != null)
				{
					Gizmos.DrawWireMesh(mesh, transform.position + _DeathObjectOffset);
				}
				else
				{
					Gizmos.DrawWireSphere(transform.position + _DeathObjectOffset, 1);
				}
			}
		}

		if (_PlaceOnFloor)
		{
			if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit))
			{
				transform.SetPositionAndRotation(hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
			}

			_PlaceOnFloor = false;
		}
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

			_IsTakingDamage = false;
			return;
		}

		float scaleDuration = 0.4f;

		_DamageAnimationTimer += Time.deltaTime;

		float horizontalMod = 0.0f;
		if (_DamageAnimationTimer <= scaleDuration)
		{
			float t = _DamageAnimationTimer / scaleDuration;
			horizontalMod = (1.0f - t) * Mathf.Sin(t * MathUtil.M_TAU) * _VerticalDamageAnimFactor;
		}
		else
		{
			_DamageAnimationTimer = 0.0f;
		}

		float xzScale = horizontalMod * (_HorizontalDamageAnimFactor * 0.2f);
		_Transform.localScale = new(_StartSize.x - xzScale, (horizontalMod * 0.25f) + _StartSize.y, _StartSize.z - xzScale);
	}

	private void HandleHealth()
	{
		if (_CurrentHealth > 0)
		{
			return;
		}

		int tries = 0;
		while (_AttachedPikmin.Count > 0)
		{
			if (tries > 10000)
			{
				Debug.LogError("The Pikmin won't get off me!");
				break;
			}

			_AttachedPikmin[0].ChangeState(PikminStates.Idle);
			tries++;
		}

		for (int i = 0; i < _DeathObjectPrefabs.Length; i++)
		{
			Vector3 newPosition = transform.position + _DeathObjectOffset;

			newPosition += MathUtil.XZToXYZ(MathUtil.PositionInUnit(_DeathObjectPrefabs.Length, i)) * 1.5f;

			Instantiate(_DeathObjectPrefabs[i], newPosition, Quaternion.identity);
		}

		if (_Flags.HasFlag(EntityFlags.ToDestroyOnDeath))
		{
			Destroy(gameObject);
		}
	}

	private void RegenerateHealth()
	{
		AddHealth(_MaxHealth * _LifeRegenerationRatio);
	}
	#endregion

	#region Public Methods
	public float GetFaceDirection()
	{
		return _EulerAngles.y;
	}

	public void Die(bool fadeHealthWheel = true)
	{
		if (!fadeHealthWheel)
		{
			Destroy(_HealthWheelScript.gameObject);
		}

		Destroy(gameObject);
	}

	public void LookTowards(float angle)
	{
		_FaceDirection = angle;
	}

	public void LookTowards(Vector3 position)
	{
		_FaceDirection = MathUtil.AngleXZ(_Transform.position, position) * Mathf.Rad2Deg;
	}

	public void SetupRadarObject()
	{
		RadarController._Instance._RadarObjects.Add(this);
	}

	#region Attacking
	public PikminIntention IntentionType => PikminIntention.Attack;
	public bool IsAttackAvailable() => _Flags.HasFlag(EntityFlags.IsAttackAvailable);

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
		SubtractHealth(damage);
	}
	#endregion
	#region IHealth
	public float GetCurrentHealth()
	{
		return _CurrentHealth;
	}

	public float GetMaxHealth()
	{
		return _MaxHealth;
	}

	public float AddHealth(float give)
	{
		SetHealth(_CurrentHealth + give);

		if (_CurrentHealth > _MaxHealth)
		{
			_CurrentHealth = _MaxHealth;
		}

		return _CurrentHealth;
	}

	public float SubtractHealth(float take)
	{
		if (_Flags.HasFlag(EntityFlags.IsVulnerable))
		{
			SetHealth(_CurrentHealth - take);
		}

		return _CurrentHealth;
	}

	public void SetHealth(float set)
	{
		if (_HealthWheelScript != null)
		{
			_HealthWheelScript.SetCurrentHealth(set);
		}

		_CurrentHealth = set;
	}
	#endregion
	#endregion
}
