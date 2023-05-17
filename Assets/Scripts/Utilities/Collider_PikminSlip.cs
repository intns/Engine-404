using UnityEngine;
using UnityEngine.Events;

public class Collider_PikminSlip : MonoBehaviour
{
	[SerializeField] UnityEvent _OnTouch;
	[SerializeField] Transform _Parent;
	[SerializeField] float _PushForce = 300;
	[SerializeField] bool _UseVerticalVel = true;

	void Update()
	{
		if (_Parent != null)
		{
			transform.position = _Parent.position;
		}
	}

	void OnCollisionStay(Collision collision)
	{
		if (collision.gameObject.CompareTag("Pikmin"))
		{
			OnTouch(collision.transform);
		}
	}

	void OnTriggerStay(Collider other)
	{
		if (other.gameObject.CompareTag("Pikmin"))
		{
			OnTouch(other.transform);
		}
	}

	void OnTouch(Transform obj)
	{
		PikminAI ai = obj.GetComponent<PikminAI>();

		Vector3 direction = MathUtil.DirectionFromTo(transform.position, obj.position, true);

		if (!_UseVerticalVel)
		{
			direction.y = 0;
		}

		ai._AddedVelocity = _PushForce * Time.deltaTime * direction;

		if (_OnTouch != null)
		{
			_OnTouch.Invoke();
		}
	}
}
