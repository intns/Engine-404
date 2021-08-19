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
	[SerializeField] private bool _Showcase = false;

	[Header("Settings")]
	[SerializeField] private float _MaxHealth = 10;
	[SerializeField] private GameObject _DeadObject = null;

	[Header("Health Wheel")]
	[SerializeField] private GameObject _HWObject = null;
	[SerializeField] private Vector3 _HWOffset = Vector3.up;
	[SerializeField] private float _HWScale = 1;

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
			_HWScript._InUse = true;
			_HWScript._MaxHealth = _MaxHealth;
			_HWScript._CurrentHealth = _MaxHealth;
			_HWScript.transform.SetParent(transform);
			_HWScript.transform.localScale = Vector3.one * _HWScale;
		}
	}

	private void Update()
	{
		if (_CurrentHealth <= 0)
		{
			while (_AttachedPikmin.Count > 0)
			{
				_AttachedPikmin[0].ChangeState(PikminStates.Idle);
			}

			Instantiate(_DeadObject, transform.position, Quaternion.identity);
			Destroy(gameObject);
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position + _HWOffset, _HWScale);
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
}
