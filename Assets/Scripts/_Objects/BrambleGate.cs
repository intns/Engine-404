using UnityEngine;

/*[RequireComponent(typeof(EnemyDamageScript))]
public class BrambleGate : MonoBehaviour, IPikminAttack
{
	[Header("Components")]
	Transform _Transform = null;
	EnemyDamageScript _DamageScript = null;

	// [Header("Settings")]

	#region Unity Functions
	void Awake()
	{
		_Transform = transform;
		_DamageScript = GetComponent<EnemyDamageScript>();
	}
	#endregion

	#region IEnumerators
	#endregion

	#region Utility Functions
	#endregion

	#region Pikmin Attacking Implementation
	public PikminIntention IntentionType => PikminIntention.Attack;
	bool IPikminAttack.IsAttackAvailable() => true;

	public void OnAttackEnd(PikminAI pikmin)
	{
		_DamageScript._AttachedPikmin.Remove(pikmin);
	}

	public void OnAttackStart(PikminAI pikmin)
	{
		_DamageScript._AttachedPikmin.Add(pikmin);
	}

	public void OnAttackRecieve(float damage, Transform hitPart = default)
	{
		if (this == null || _DamageScript == null)
		{
			return;
		}

		// Should be called last in case the 
		_DamageScript.SubtractHealth(damage);
	}

	#endregion

	#region Public Functions
	#endregion
}
*/