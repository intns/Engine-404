/*
 * EnemyDamageScript.cs
 * Created by: Neo, Ambrosia
 * Created on: 15/2/2020 (dd/mm/yy)
 * Created for: Generic enemy health manager script
 */

using System.Collections.Generic;
using UnityEngine;

public class EnemyDamageScript : MonoBehaviour, IHealth
{
	[Header("ENABLE WHEN NOT USED FOR GAMEPLAY")]
	[SerializeField] bool _Showcase = false;

	[Header("Settings")]
	[SerializeField] float _MaxHealth = 10;
	[SerializeField] public Vector3 _DeadObjectOffset = Vector3.zero;
	[SerializeField] public GameObject[] _DeadObject;

	[Header("Health Wheel")]
	[SerializeField] GameObject _HWObject = null;
	[SerializeField] Vector3 _HWOffset = Vector3.up;
	[SerializeField] float _HWScale = 1;

	[HideInInspector] public List<PikminAI> _AttachedPikmin = new List<PikminAI>();
	[HideInInspector] public bool _Dead = false;
	[HideInInspector] public HealthWheel _HWScript = null;
	private float _CurrentHealth = 0;

	public PikminIntention IntentionType => PikminIntention.Attack;

	private void Awake()
	{
		_CurrentHealth = _MaxHealth;
	}

	private void Start()
	{
		if (_Showcase == false)
		{
			// Find a health wheel that hasn't been claimed already
			_HWScript = Instantiate(_HWObject, transform.position + _HWOffset, Quaternion.identity).GetComponentInChildren<HealthWheel>();
			// Apply all of the required variables 
			_HWScript._Parent = transform;
			_HWScript._Offset = _HWOffset;
			_HWScript._InUse = true;
			_HWScript._MaxHealth = _MaxHealth;
			_HWScript._CurrentHealth = _MaxHealth;
			_HWScript.transform.localScale = Vector3.one * _HWScale;
		}
	}

	private void Update()
	{
		if (GameManager.IsPaused)
		{
			return;
		}

		if (_CurrentHealth <= 0)
		{
			while (_AttachedPikmin.Count > 0)
			{
				_AttachedPikmin[0].ChangeState(PikminStates.Idle);
			}

			for (int i = 0; i < _DeadObject.Length; i++)
			{
				Vector3 newPosition = transform.position + _DeadObjectOffset;

				// Spawn object in a circle around each other
				newPosition += MathUtil.XZToXYZ(MathUtil.PositionInUnit(_DeadObject.Length, i)) * 1.5f;

				Instantiate(_DeadObject[i], newPosition, Quaternion.identity);
			}

			Destroy(gameObject);
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position + _HWOffset, _HWScale);

		Gizmos.color = Color.red;

		foreach (GameObject obj in _DeadObject)
		{
			MeshFilter filter = obj.GetComponentInChildren<MeshFilter>();
			Mesh mesh;
			if (filter != null && (mesh = filter.sharedMesh) != null)
			{
				Gizmos.DrawWireMesh(mesh, transform.position + _DeadObjectOffset);
			}
			else
			{
				Gizmos.DrawWireSphere(transform.position + _DeadObjectOffset, 1);
			}

		}
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
		_CurrentHealth -= take;
		_HWScript._CurrentHealth = GetCurrentHealth();
		return _CurrentHealth;
	}

	public void SetHealth(float set)
	{
		_CurrentHealth = set;
	}

	#endregion
}
