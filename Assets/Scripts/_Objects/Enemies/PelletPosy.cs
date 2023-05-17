using UnityEngine;

public class PelletPosy : Entity
{
	[Header("Settings")]
	public float _TimeToSprout = 2.5f;

	public new void Awake()
	{
		base.Awake();
	}

	#region Pikmin Attacking

	public new void OnAttackRecieve(float damage, Transform hitPart)
	{
		Debug.Log(hitPart.name);

		if (hitPart.name == "pellet_base_collider")
		{
			base.OnAttackRecieve(_CurrentHealth, hitPart);
			return;
		}

		base.OnAttackRecieve(damage, hitPart);
	}

	#endregion
}
