/*
 * Water.cs
 * Created by: Ambrosia
 * Created on: 14/3/2020 (dd/mm/yy)
 * Created for: needing Pikmin to start drowning when entering Water
 */

using UnityEngine;

public class Water : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		// Handle (TODO) Water splash / audio for object entering water
		Rigidbody rbComponent = other.GetComponent<Rigidbody>();
		if (rbComponent != null)
		{
			//Debug.Log("Object came into water at velocity of " + rbComponent.velocity.ToString());
		}

		// Handle Pikmin entering the water
		if (other.CompareTag("Pikmin"))
		{
			other.GetComponent<PikminAI>().WaterEnter();
			return;
		}

		if (other.CompareTag("Player"))
		{
			// TODO - handle Playe entering water
			return;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Pikmin"))
		{
			other.GetComponent<PikminAI>().WaterLeave();
		}
	}
}
