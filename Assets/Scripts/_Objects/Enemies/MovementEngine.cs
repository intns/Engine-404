using UnityEngine;

public class MovementEngine : MonoBehaviour
{
	[SerializeField] AnimationCurve _DeaccelerateCurve;
	[SerializeField] float _DeaccelerateTime = 0.25f;
	[SerializeField] Vector3 _CurrentSmoothVelocity;

	Rigidbody _Rigidbody;

	Vector3 _SmoothVelocity;
	float _SmoothVelocityTimer;

	public Vector3 SmoothVelocity
	{
		get => _SmoothVelocity;
		set
		{
			_SmoothVelocityTimer = 0;
			_CurrentSmoothVelocity = value;
			_SmoothVelocity = value;
		}
	}

	#region Unity Functions

	void Awake()
	{
		_Rigidbody = GetComponent<Rigidbody>();
	}

	void Update()
	{
		if (GameManager.IsPaused)
		{
			return;
		}

		if (_SmoothVelocityTimer >= _DeaccelerateTime)
		{
			return;
		}

		_CurrentSmoothVelocity = Vector3.Lerp(
			_SmoothVelocity,
			Vector3.up * _Rigidbody.velocity.y,
			_DeaccelerateCurve.Evaluate(_SmoothVelocityTimer / _DeaccelerateTime)
		);

		_SmoothVelocityTimer += Time.deltaTime;

		if (_SmoothVelocityTimer >= _DeaccelerateTime)
		{
			_CurrentSmoothVelocity = Vector3.zero;
		}
	}

	void FixedUpdate()
	{
		if (GameManager.IsPaused)
		{
			return;
		}

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
