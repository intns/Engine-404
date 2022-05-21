using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementEngine : MonoBehaviour
{
	[Header("Components")]
	Transform _Transform = null;
	Rigidbody _Rigidbody = null;

	public Vector3 SmoothVelocity
	{
		get { return _SmoothVelocity; }
		set
		{
			_SmoothVelocityTimer = 0;
			_CurrentSmoothVelocity = value;
			_SmoothVelocity = value;
		}
	}

	public Vector3 RealVelocity
	{
		get => _Rigidbody.velocity;
	}

	[SerializeField] AnimationCurve _DeaccelerateCurve;
	[SerializeField] float _DeaccelerateTime = 0.25f;

	Vector3 _SmoothVelocity;
	[SerializeField] Vector3 _CurrentSmoothVelocity;
	float _SmoothVelocityTimer = 0;

	#region Unity Functions
	private void Awake()
	{
		_Transform = transform;
		_Rigidbody = GetComponent<Rigidbody>();
	}

	private void Update()
	{
		if (_SmoothVelocityTimer >= _DeaccelerateTime)
		{
			return;
		}

		_CurrentSmoothVelocity = Vector3.Lerp(_SmoothVelocity, Vector3.up * _Rigidbody.velocity.y,
			_DeaccelerateCurve.Evaluate(_SmoothVelocityTimer / _DeaccelerateTime));

		_SmoothVelocityTimer += Time.deltaTime;
		if (_SmoothVelocityTimer >= _DeaccelerateTime)
		{
			_CurrentSmoothVelocity = Vector3.zero;
		}
	}

	private void FixedUpdate()
	{
		float storedY = _Rigidbody.velocity.y;
		if (_CurrentSmoothVelocity != Vector3.zero)
		{
			SetVelocity(_CurrentSmoothVelocity);
		}
		_CurrentSmoothVelocity.y = storedY;
	}

	public void SetVelocity(Vector3 velocity)
	{
		_Rigidbody.velocity = velocity;
	}
	#endregion
}
