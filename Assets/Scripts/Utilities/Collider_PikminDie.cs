using UnityEngine;

public class Collider_PikminDie : MonoBehaviour
{
	public bool _Enabled = false;

	void OnTouch(GameObject collGo)
	{
		if (collGo.CompareTag("Pikmin"))
		{
			collGo.GetComponent<PikminAI>().Die(0.5f);
		}
		else if (collGo.CompareTag("Player"))
		{
			Player player = collGo.GetComponent<Player>();
			player.SubtractHealth(player.GetMaxHealth() / 3);
		}
	}

	void OnCollisionEnter(Collision collision)
	{
		if (!_Enabled)
		{
			return;
		}

		if (collision.gameObject.CompareTag("Pikmin"))
		{
			OnTouch(collision.gameObject);
		}
	}

	void OnTriggerEnter(Collider other)
	{
		if (!_Enabled)
		{
			return;
		}

		if (other.gameObject.CompareTag("Pikmin"))
		{
			OnTouch(other.gameObject);
		}
	}
}
