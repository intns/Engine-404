/*
 * WhistleController.cs
 * Created by: Ambrosia & Kaelan
 * Created on: 9/2/2020, overhaul on 31/3/2020 (dd/mm/yy)
 * Created for: Controlling the whistle
 */

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WhistleController : MonoBehaviour
{
	/* We use the localScale to keep track of how scaled the Whistle
     actually is, as opposed to just changing the decals localScale */

	[Header("Components")]
	[SerializeField] private GameObject _WhistleParticle = null;
	[SerializeField] private AudioClip _BlowSound = null;
	[SerializeField] private Transform _Reticle = null;

	[Header("Particles")]
	[SerializeField] private int _ParticleDensity = 15;
	[SerializeField] private float _ParticleRotationSpeed = 1;
	[SerializeField] private float _ParticleRaycastAddedHeight = 100;
	[SerializeField] private float _HeightOffset = 0.5f;

	[Header("Settings")]
	[SerializeField] private float _StartingRadius = 1;
	[SerializeField] private float _ExpandedRadius = 10;
	[SerializeField] private float _PikminCallHeight = 0;

	[SerializeField] private float _MaxBlowTime = 3;
	[SerializeField] private float _OffsetFromSurface = 0.5f;

	[SerializeField] private LayerMask _PikminMask = 0;

	[Header("Controller Settings")]
	[SerializeField] private float _MaxDistFromPlayer = 5;
	[SerializeField] private float _MoveSpeed = 5;
	private Vector2 _OffsetFromPlayer = Vector2.zero;

	[Header("Raycast Settings")]
	[SerializeField] private float _MaxDistance = Mathf.Infinity;
	[SerializeField] private LayerMask _MapMask = 0;

	[Header("Debugging")]
	[SerializeField] private uint _WhistleCircleSegments = 20;
	private AudioSource _Source;
	private GameObject[] _Particles;
	private Camera _MainCamera;
	private Transform _PlayerTransform;
	private bool _Blowing = false;
	private float _TimeBlowing = 0;

	private void Awake()
	{
		// Generate particles for blowing later on
		_Particles = new GameObject[_ParticleDensity + 1];
		for (int i = 0; i < _Particles.Length; i++)
		{
			_Particles[i] = Instantiate(_WhistleParticle);
		}
		AssignParticlePositions();
		SetParticlesActive(false);

		// Reset the local scale
		transform.localScale = Vector3.one * _StartingRadius;

		// Get local components
		_Source = GetComponent<AudioSource>();
		_Source.clip = _BlowSound;

		_MainCamera = Camera.main;
	}

	private void Start()
	{
		_PlayerTransform = Globals._Player.transform;
	}

	private void Update()
	{
		// Check if there is a controller attached
		string[] names = Input.GetJoystickNames();
		if (names.Length > 0 && names[0].Length > 0)
		{
			Vector3 directionVector = new Vector3(Input.GetAxis("Horizontal") * _MoveSpeed, 0, Input.GetAxis("Vertical") * _MoveSpeed);
			//Rotate the input vector into camera space so up is camera's up and right is camera's right
			directionVector = _MainCamera.transform.rotation * directionVector;

			_OffsetFromPlayer.x += directionVector.x;
			_OffsetFromPlayer.y += directionVector.z;

			float totalDistanceSquared = (_OffsetFromPlayer.x * _OffsetFromPlayer.x) +
				(_OffsetFromPlayer.y * _OffsetFromPlayer.y);

			if (totalDistanceSquared > _MaxDistFromPlayer * _MaxDistFromPlayer)
			{
				float totalDistance = _MaxDistFromPlayer / Mathf.Sqrt(totalDistanceSquared);
				_OffsetFromPlayer.x *= totalDistance;
				_OffsetFromPlayer.y *= totalDistance;
			}

			Vector3 currentPosition = new Vector3(_PlayerTransform.position.x + _OffsetFromPlayer.x,
				transform.position.y,
				_PlayerTransform.position.z + _OffsetFromPlayer.y);

			// Assign our position to the reticles position to the new position!
			transform.position = _Reticle.position = currentPosition;

			currentPosition.y += _ParticleRaycastAddedHeight;
			if (Physics.Raycast(currentPosition, Vector3.down, out RaycastHit hit, _MaxDistance, _MapMask, QueryTriggerInteraction.Ignore))
			{
				Vector3 target = hit.point + hit.normal * _OffsetFromSurface;
				transform.position = _Reticle.position = target;
			}
		}
		else
		{
			// Reliant on the mouse, so cannot be used with controllers
			Ray ray = _MainCamera.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit hit, _MaxDistance, _MapMask, QueryTriggerInteraction.Ignore))
			{
				Vector3 target = hit.point + hit.normal * _OffsetFromSurface;
				transform.position = _Reticle.position = target;
			}
		}

		// Detecting Player input
		if (Input.GetButtonDown("B Button"))
		{
			transform.localScale = Vector3.one * _StartingRadius;
			_Blowing = true;

			// Start the particles
			SetParticlesActive(true);

			// Play the blow sound
			_Source.Play();
		}
		if (Input.GetButtonUp("B Button"))
		{
			EndBlow();
		}

		if (_Blowing)
		{
			// Handle Particle movement
			AssignParticlePositions();

			// Used to keep track of how long we've been blowing for
			_TimeBlowing += Time.deltaTime;
			if (_TimeBlowing >= _MaxBlowTime)
			{
				EndBlow();
			}

			// Grow the scale of the whistle to the radius we want it to become
			float timeFrac = _TimeBlowing / _MaxBlowTime;
			transform.localScale = Vector3.Lerp(transform.localScale, MathUtil.XZToXYZ(Vector2.one * _ExpandedRadius, 1), timeFrac);

			// Handle collisions with Pikmin
			Collider[] collisions = Physics.OverlapCapsule(transform.position + (Vector3.down * _PikminCallHeight),
				transform.position + (Vector3.up * _PikminCallHeight),
				transform.localScale.x,
				_PikminMask);
			foreach (Collider pikmin in collisions)
			{
				pikmin.GetComponent<PikminAI>().AddToSquad();
			}
		}
	}

	/// <summary>
	/// Displays debug information about the Whistle when selected in the editor
	/// </summary>
	private void OnDrawGizmosSelected()
	{
		if (Application.isPlaying)
		{
			// Draw particles
			AssignParticlePositions();
			foreach (GameObject particle in _Particles)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(particle.transform.position, 0.05f * transform.localScale.x);
			}
		}

		// Draw default whistle radius
		for (int i = 0; i < _WhistleCircleSegments + 1; i++)
		{
			Vector3 pos = transform.position + MathUtil.XZToXYZ(MathUtil.PositionInUnit((int)_WhistleCircleSegments, i)) * _StartingRadius;
			Gizmos.color = Color.yellow;
			Gizmos.DrawSphere(pos, 0.05f * _StartingRadius);
		}
		// Draw expanded whistle radius
		for (int i = 0; i < _WhistleCircleSegments + 1; i++)
		{
			Vector3 pos = transform.position + MathUtil.XZToXYZ(MathUtil.PositionInUnit((int)_WhistleCircleSegments, i)) * _ExpandedRadius;
			Gizmos.color = Color.green;
			Gizmos.DrawSphere(pos, 0.05f * _ExpandedRadius);
		}
	}

	private void EndBlow()
	{
		transform.localScale = Vector3.one * _StartingRadius;
		_TimeBlowing = 0;
		_Blowing = false;
		SetParticlesActive(false);
		_Source.Stop();
	}

	private void SetParticlesActive(bool isActive)
	{
		for (int i = 0; i < _Particles.Length; i++)
		{
			_Particles[i].SetActive(isActive);
		}
	}

	/// <summary>
	/// Assigns the positions of the blow particles
	/// </summary>
	private void AssignParticlePositions()
	{
		Transform cacheTransform = transform;
		for (int i = 0; i < _ParticleDensity + 1; i++)
		{
			Vector3 localPos = MathUtil.XZToXYZ(MathUtil.PositionInUnit(_ParticleDensity, i, _TimeBlowing * _ParticleRotationSpeed)) * cacheTransform.localScale.x;
			// Offset the local position to be global
			localPos += cacheTransform.position;
			//Cache the Y for later
			float originalY = localPos.y;

			// Put the Y of the particle waay above everything else, so it can raycast downwards onto surfaces that may be above it 
			localPos.y += _ParticleRaycastAddedHeight;

			// Check if there is a surface beneath the particle
			if (Physics.Raycast(localPos, Vector3.down, out RaycastHit hitInfo, _MaxDistance, _MapMask, QueryTriggerInteraction.Ignore))
			{
				localPos.y = hitInfo.point.y + _HeightOffset;
			}
			else
			{
				// We couldn't find anything, so reset back to the original Y
				localPos.y = originalY;
			}

			_Particles[i].transform.position = localPos;
		}
	}
}
