using System.Collections.Generic;
using UnityEngine;

public static class Perlin
{
	#region Noise functions

	public static float Noise(float x)
	{
		var X = Mathf.FloorToInt(x) & 0xff;
		x -= Mathf.Floor(x);
		var u = Fade(x);
		return Lerp(u, Grad(perm[X], x), Grad(perm[X + 1], x - 1)) * 2;
	}

	public static float Noise(float x, float y)
	{
		var X = Mathf.FloorToInt(x) & 0xff;
		var Y = Mathf.FloorToInt(y) & 0xff;
		x -= Mathf.Floor(x);
		y -= Mathf.Floor(y);
		var u = Fade(x);
		var v = Fade(y);
		var A = (perm[X] + Y) & 0xff;
		var B = (perm[X + 1] + Y) & 0xff;
		return Lerp(v, Lerp(u, Grad(perm[A], x, y), Grad(perm[B], x - 1, y)),
									 Lerp(u, Grad(perm[A + 1], x, y - 1), Grad(perm[B + 1], x - 1, y - 1)));
	}

	public static float Noise(Vector2 coord)
	{
		return Noise(coord.x, coord.y);
	}

	public static float Noise(float x, float y, float z)
	{
		var X = Mathf.FloorToInt(x) & 0xff;
		var Y = Mathf.FloorToInt(y) & 0xff;
		var Z = Mathf.FloorToInt(z) & 0xff;
		x -= Mathf.Floor(x);
		y -= Mathf.Floor(y);
		z -= Mathf.Floor(z);
		var u = Fade(x);
		var v = Fade(y);
		var w = Fade(z);
		var A = (perm[X] + Y) & 0xff;
		var B = (perm[X + 1] + Y) & 0xff;
		var AA = (perm[A] + Z) & 0xff;
		var BA = (perm[B] + Z) & 0xff;
		var AB = (perm[A + 1] + Z) & 0xff;
		var BB = (perm[B + 1] + Z) & 0xff;
		return Lerp(w, Lerp(v, Lerp(u, Grad(perm[AA], x, y, z), Grad(perm[BA], x - 1, y, z)),
													 Lerp(u, Grad(perm[AB], x, y - 1, z), Grad(perm[BB], x - 1, y - 1, z))),
									 Lerp(v, Lerp(u, Grad(perm[AA + 1], x, y, z - 1), Grad(perm[BA + 1], x - 1, y, z - 1)),
													 Lerp(u, Grad(perm[AB + 1], x, y - 1, z - 1), Grad(perm[BB + 1], x - 1, y - 1, z - 1))));
	}

	public static float Noise(Vector3 coord)
	{
		return Noise(coord.x, coord.y, coord.z);
	}

	#endregion

	#region fBm functions

	public static float Fbm(float x, int octave)
	{
		var f = 0.0f;
		var w = 0.5f;
		for (var i = 0; i < octave; i++)
		{
			f += w * Noise(x);
			x *= 2.0f;
			w *= 0.5f;
		}
		return f;
	}

	public static float Fbm(Vector2 coord, int octave)
	{
		var f = 0.0f;
		var w = 0.5f;
		for (var i = 0; i < octave; i++)
		{
			f += w * Noise(coord);
			coord *= 2.0f;
			w *= 0.5f;
		}
		return f;
	}

	public static float Fbm(float x, float y, int octave)
	{
		return Fbm(new Vector2(x, y), octave);
	}

	public static float Fbm(Vector3 coord, int octave)
	{
		var f = 0.0f;
		var w = 0.5f;
		for (var i = 0; i < octave; i++)
		{
			f += w * Noise(coord);
			coord *= 2.0f;
			w *= 0.5f;
		}
		return f;
	}

	public static float Fbm(float x, float y, float z, int octave)
	{
		return Fbm(new Vector3(x, y, z), octave);
	}

	#endregion

	#region functions

	static float Fade(float t)
	{
		return t * t * t * (t * (t * 6 - 15) + 10);
	}

	static float Lerp(float t, float a, float b)
	{
		return a + t * (b - a);
	}

	static float Grad(int hash, float x)
	{
		return (hash & 1) == 0 ? x : -x;
	}

	static float Grad(int hash, float x, float y)
	{
		return ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
	}

	static float Grad(int hash, float x, float y, float z)
	{
		var h = hash & 15;
		var u = h < 8 ? x : y;
		var v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
		return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
	}

	static int[] perm = {
				151,160,137,91,90,15,
				131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
				190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
				88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
				77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
				102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
				135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
				5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
				223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
				129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
				251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
				49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
				138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,
				151
		};

	#endregion
}

public class PuffStool : MonoBehaviour, IPikminAttack, IHealth
{
	enum States
	{
		Idle,
		Walking,

		StunStart,
		Stunned,
		StunEnd,

		Attack,
		Death
	}

	[Header("Components")]
	Transform _Transform = null;
	Animator _Animator = null;
	MovementEngine _MovementEngine = null;

	[Header("Settings")]
	[SerializeField] float _Speed = 15;
	[SerializeField] float _MaxHealth = 3500;
	[SerializeField] Vector3 _DeadOffset = Vector3.zero;
	[SerializeField] GameObject _DeadObject = null;
	[Space]
	[SerializeField] float _TimePerAttack = 5;
	[Space]
	[SerializeField] float _TimeForStun = 3.5f;
	[SerializeField] float _HealthForStun = 100;
	[Space]
	[SerializeField] float _DetectionSphere = 5;
	[SerializeField] float _DeathSphere = 2.5f;
	[SerializeField] LayerMask _PlayerAndPikminMask;
	[SerializeField] ParticleSystem _ToxicPS;

	[Header("Health Wheel")]
	[SerializeField] GameObject _HWObject = null;
	[SerializeField] Vector3 _HWOffset = Vector3.up;
	[SerializeField] float _HWScale = 1;

	[Header("Debugging")]
	[SerializeField] States _CurrentState = States.Idle;
	[SerializeField] Transform _TargetObject;
	[SerializeField] float _AttackTimer = 0;
	[SerializeField] float _StunTimer = 0;
	[SerializeField] float _CurrentHealthForStun = 0;
	[SerializeField] Vector3 _WanderDirection = Vector3.zero;
	Vector2 _RngStartDir = Vector2.zero;

	List<PikminAI> _AttachedPikmin = new List<PikminAI>();

	float _CurrentHealth = 0;
	HealthWheel _HWScript = null;

	[Header("Animations")]
	[SerializeField] AnimationClip _IdleAnim;
	[SerializeField] AnimationClip _WalkAnim;
	[SerializeField] AnimationClip _AttackAnim;
	[SerializeField] AnimationClip _StunStartAnim;
	[SerializeField] AnimationClip _StunEndAnim;
	[SerializeField] AnimationClip _StunAnim;
	[SerializeField] AnimationClip _DeathStandAnim;
	[SerializeField] AnimationClip _DeathStunnedAnim;
	AnimationController _AnimController = new AnimationController();

	public static class AnimationState
	{
		public const int Idle = 0;
		public const int Walk = 1;
		public const int Attack = 2;
		public const int StunStart = 3;
		public const int StunEnd = 4;
		public const int Stun = 5;
		public const int DeathStand = 6;
		public const int DeathStunned = 7;
	}

	#region Unity Functions
	void Awake()
	{
		_Transform = transform;
		_Animator = GetComponent<Animator>();
		_MovementEngine = GetComponent<MovementEngine>();
		_CurrentHealthForStun = _HealthForStun;

		float randomVal = Random.value * Mathf.PI * 2;
		_RngStartDir.x = randomVal;
		_RngStartDir.y = randomVal + Random.value;
		_WanderDirection = new Vector3(Mathf.Sin(randomVal), 0, Mathf.Cos(randomVal));

		_CurrentHealth = _MaxHealth;

		_HWScript = Instantiate(_HWObject, transform.position + _HWOffset, Quaternion.identity).GetComponentInChildren<HealthWheel>();
		_HWScript._Parent = transform;
		_HWScript._Offset = _HWOffset;
		_HWScript._InUse = true;
		_HWScript._MaxHealth = _MaxHealth;
		_HWScript._CurrentHealth = _MaxHealth;
		_HWScript.transform.localScale = Vector3.one * _HWScale;

		_AnimController._ParentAnimator = _Animator;
		Debug.Assert(AnimationState.Idle == _AnimController.AddState(_IdleAnim));
		Debug.Assert(AnimationState.Walk == _AnimController.AddState(_WalkAnim));
		Debug.Assert(AnimationState.Attack == _AnimController.AddState(_AttackAnim));
		Debug.Assert(AnimationState.StunStart == _AnimController.AddState(_StunStartAnim));
		Debug.Assert(AnimationState.StunEnd == _AnimController.AddState(_StunEndAnim));
		Debug.Assert(AnimationState.Stun == _AnimController.AddState(_StunAnim));
		Debug.Assert(AnimationState.DeathStand == _AnimController.AddState(_DeathStandAnim));
		Debug.Assert(AnimationState.DeathStunned == _AnimController.AddState(_DeathStunnedAnim));
	}

	void Update()
	{
		if (GameManager.IsPaused)
		{
			return;
		}

		switch (_CurrentState)
		{
			case States.Idle:
				{
					HandleIdle();
					break;
				}
			case States.Walking:
				{
					HandleWalking();
					break;
				}
			case States.Stunned:
				{
					HandleStunned();
					break;
				}
			case States.Attack:
				{
					_MovementEngine.SetVelocity(Vector3.zero);
					break;
				}
			case States.Death:
				{
					while (_AttachedPikmin.Count > 0)
					{
						PikminAI pik = _AttachedPikmin[0];
						if (pik == null)
						{
							break;
						}

						pik.ChangeState(PikminStates.Idle);
						pik._AddedVelocity = MathUtil.DirectionFromTo(_Transform.position, pik.transform.position) * 5;
					}


					_MovementEngine.SetVelocity(Vector3.zero);
					break;
				}
			case States.StunStart:
				{
					while (_AttachedPikmin.Count > 10)
					{
						PikminAI pik = _AttachedPikmin[^1];
						if (pik == null)
						{
							break;
						}

						pik.ChangeState(PikminStates.Idle);
						pik._AddedVelocity = MathUtil.DirectionFromTo(_Transform.position, pik.transform.position) * 5;
					}

					Collider[] pikmin = Physics.OverlapSphere(_Transform.position, _DeathSphere, _PlayerAndPikminMask);
					foreach (Collider pik in pikmin)
					{
						PikminAI ai = pik.GetComponent<PikminAI>();
						if (ai != null)
						{
							ai._AddedVelocity = MathUtil.DirectionFromTo(_Transform.position, pik.transform.position) * 15;
						}
					}

					_MovementEngine.SetVelocity(Vector3.zero);
					break;
				}
			case States.StunEnd:
				_MovementEngine.SetVelocity(Vector3.zero);
				break;
			default:
				break;
		}
	}


	void OnDrawGizmos()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(transform.position, _DetectionSphere);

		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position, _DeathSphere);

		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position + _HWOffset, _HWScale);

		Gizmos.color = Color.red;

		Mesh mesh = _DeadObject.GetComponentInChildren<MeshFilter>().sharedMesh;
		if (mesh != null)
		{
			Gizmos.DrawWireMesh(mesh, transform.position + _DeadOffset);
		}
		else
		{
			Gizmos.DrawWireSphere(transform.position + _DeadOffset, 1);
		}
	}
	#endregion

	#region IEnumerators
	#endregion

	#region Utility Functions
	void HandleIdle()
	{
		Collider closestObj = MathUtil.GetClosestCollider(_Transform.position, new(Physics.OverlapSphere(_Transform.position, _DetectionSphere, _PlayerAndPikminMask)));
		if (closestObj != null)
		{
			_TargetObject = closestObj.transform;
			ChangeState(States.Walking);
		}

		_AnimController.ChangeState(AnimationState.Idle);
	}

	void HandleWalking()
	{
		Vector3 direction = new Vector3(Perlin.Noise(Time.time + _RngStartDir.x), 0, Perlin.Noise(Time.time / 2 + _RngStartDir.y)).normalized;
		_WanderDirection = Vector3.Lerp(_WanderDirection, direction, 0.3f * Time.deltaTime);

		_MovementEngine.SmoothVelocity = _Speed * _WanderDirection;
		_Transform.rotation = Quaternion.Slerp(_Transform.rotation, Quaternion.LookRotation(_WanderDirection), 4 * Time.deltaTime);

		_AnimController.ChangeState(AnimationState.Walk);

		if (_TargetObject == null)
		{
			return;
		}

		Vector3 towards = MathUtil.DirectionFromTo(_Transform.position, _TargetObject.position);
		_WanderDirection = Vector3.Lerp(_WanderDirection, towards, 0.25f * Time.deltaTime);

		float distanceToTarget = float.PositiveInfinity;

		Collider[] objects = Physics.OverlapSphere(_Transform.position, _DetectionSphere, _PlayerAndPikminMask);
		Collider closestObj = MathUtil.GetClosestCollider(_Transform.position, new(objects));
		if (closestObj != null)
		{
			float curDist = MathUtil.DistanceTo(_Transform.position, _TargetObject.position);
			float closestDist = MathUtil.DistanceTo(_Transform.position, closestObj.transform.position);
			if (closestDist < curDist)
			{
				_TargetObject = closestObj.transform;
				distanceToTarget = closestDist;
			}
			else
			{
				distanceToTarget = curDist;
			}
		}
		else
		{
			_TargetObject = null;
		}

		// Attack!
		if (distanceToTarget < 4)
		{
			_AttackTimer += Time.deltaTime;

			if (_AttackTimer >= _TimePerAttack)
			{
				_AttackTimer = 0;
				ChangeState(States.Attack);
			}
		}
	}

	void HandleStunned()
	{
		_AnimController.ChangeState(AnimationState.Stun);
		_MovementEngine.SetVelocity(Vector3.zero);

		_StunTimer += Time.deltaTime;
		if (_StunTimer < _TimeForStun)
		{
			return;
		}

		while (_AttachedPikmin.Count > 0)
		{
			PikminAI pik = _AttachedPikmin[0];
			if (pik == null)
			{
				break;
			}

			pik.ChangeState(PikminStates.Idle);
			pik._AddedVelocity = MathUtil.DirectionFromTo(_Transform.position, pik.transform.position) * 40;
		}

		ChangeState(States.StunEnd);
	}

	void ChangeState(States newState)
	{
		States oldState = _CurrentState;

		switch (oldState)
		{
			case States.Walking when _TargetObject != null:
				_TargetObject = null;
				break;
			case States.Stunned:
				_StunTimer = 0;
				_CurrentHealthForStun = _HealthForStun;
				break;
		}

		_CurrentState = newState;

		switch (newState)
		{
			case States.StunStart:
				_AnimController.ChangeState(AnimationState.StunStart);
				break;
			case States.StunEnd:
				_AnimController.ChangeState(AnimationState.StunEnd);
				break;
			case States.Idle:
				_AnimController.ChangeState(AnimationState.Idle);
				break;
			case States.Walking:
				_AnimController.ChangeState(AnimationState.Walk);
				break;
			case States.Attack:
				_AnimController.ChangeState(AnimationState.Attack);
				break;
			case States.Death:
				if (oldState == States.Stunned
				|| oldState == States.StunStart
				|| oldState == States.StunEnd)
				{
					_AnimController.ChangeState(AnimationState.DeathStunned);
				}
				else
				{
					_AnimController.ChangeState(AnimationState.DeathStand);
				}
				break;
		}
	}
	#endregion

	#region Public Functions
	public PikminIntention IntentionType => PikminIntention.Attack;

	public void OnAttackEnd(PikminAI pikmin)
	{
		_AttachedPikmin.Remove(pikmin);
	}

	public void OnAttackStart(PikminAI pikmin)
	{
		_AttachedPikmin.Add(pikmin);
	}

	public void OnAttackRecieve(float damage)
	{
		if (this == null || _Animator == null)
		{
			return;
		}

		_CurrentHealthForStun -= damage;
		if (_CurrentHealthForStun <= 0
			&& (_CurrentState == States.Idle || _CurrentState == States.Walking))
		{
			ChangeState(States.StunStart);
			_CurrentHealthForStun = _HealthForStun;
		}

		// Should be called last in case the 
		SubtractHealth(damage);
		_HWScript._CurrentHealth = GetCurrentHealth();

		if (GetCurrentHealth() <= 0)
		{
			ChangeState(States.Death);
		}
	}

	public void ANIM_OnStunStart_End()
	{
		ChangeState(States.Stunned);
	}

	public void ANIM_OnStunEnd_End()
	{
		ChangeState(States.Attack);
	}

	public void ANIM_OnAttack_End()
	{
		ChangeState(States.Idle);
	}

	public void ANIM_OnAttack_Do()
	{
		_ToxicPS.Play();

		Collider[] objects = Physics.OverlapSphere(_Transform.position, _DeathSphere, _PlayerAndPikminMask);

		foreach (var coll in objects)
		{
			PikminAI ai = coll.GetComponent<PikminAI>();
			if (ai != null)
			{
				ai.Die(0);
			}

			if (coll.transform == _TargetObject)
			{
				_TargetObject = null;
			}
		}
	}

	public void ANIM_OnDeath_End()
	{
		while (_AttachedPikmin.Count > 0)
		{
			_AttachedPikmin[0].ChangeState(PikminStates.Idle);
		}

		Instantiate(_DeadObject, _Transform.position + _DeadOffset, Quaternion.identity);
		Destroy(gameObject);
	}


	#region Health Implementation
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
