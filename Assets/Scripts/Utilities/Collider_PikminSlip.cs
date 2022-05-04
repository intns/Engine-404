using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Collider_PikminSlip : MonoBehaviour
{
	[SerializeField] UnityEvent _OnTouch = null;
	[SerializeField] Transform _Parent = null;
	private const float k_PushForce = 300;

	private void Update()
	{
		if (_Parent != null)
		{
			transform.position = _Parent.position;
		}
	}

	void OnTouch(Transform obj)
	{
		PikminAI ai = obj.GetComponent<PikminAI>();

		Vector3 direction = MathUtil.DirectionFromTo(transform.position, obj.position);
		ai._AddedVelocity += k_PushForce * Time.deltaTime * direction;

		if (_OnTouch != null)
		{
			_OnTouch.Invoke();
		}
	}

	private void OnCollisionStay(Collision collision)
	{
		if (collision.gameObject.CompareTag("Pikmin"))
		{
			OnTouch(collision.transform);
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.gameObject.CompareTag("Pikmin"))
		{
			OnTouch(other.transform);
		}
	}
}
