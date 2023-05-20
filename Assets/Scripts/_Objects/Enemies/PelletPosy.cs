using UnityEngine;

public class PelletPosy : Entity, IPikminAttack
{
	public new void OnAttackRecieve(float damage, Transform hitCollider)
	{
		if (hitCollider.name == "pellet_base_collider")
		{
			base.OnAttackRecieve(_CurrentHealth, hitCollider);
			return;
		}

		base.OnAttackRecieve(damage, hitCollider);
	}
}
