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

	private void OnCollisionStay(Collision collision)
	{
		if (collision.gameObject.CompareTag("Pikmin"))
		{
			Transform pikminObj = collision.transform;
			PikminAI ai = pikminObj.GetComponent<PikminAI>();

			Vector3 direction = MathUtil.DirectionFromTo(transform.position, pikminObj.position);
			ai._AddedVelocity += k_PushForce * Time.deltaTime * direction;

			if (_OnTouch != null)
			{
				_OnTouch.Invoke();
			}
		}
	}
}
