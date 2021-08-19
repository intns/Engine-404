using UnityEngine;

public class ShadowProjectorWobble : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] private float _RotationSpeed = 5;
	[SerializeField] private Vector3 _StartingRotation = new Vector3(90, 0, 60);
	[SerializeField] private Vector3 _MaxOffset = new Vector3(5, 0, 5);
	private Vector3 _Next = Vector3.zero;
	private Quaternion _NextQuat = Quaternion.identity;

	private void Awake()
	{
		_Next = _StartingRotation;
		_NextQuat = Quaternion.Euler(_Next);
		transform.rotation = _NextQuat;

		InvokeRepeating(nameof(SetNewRotation), 0, 2);
	}

	private void SetNewRotation()
	{
		_Next = new Vector3(_StartingRotation.x + Random.Range(-_MaxOffset.x, _MaxOffset.x), 0, _StartingRotation.z
																																													+ Random.Range(-_MaxOffset.z, _MaxOffset.z));
		_NextQuat = Quaternion.Euler(_Next);
	}

	private void Update()
	{
		if (transform.rotation == _NextQuat)
		{
			SetNewRotation();
		}

		transform.rotation = Quaternion.Lerp(transform.rotation, _NextQuat, _RotationSpeed * Time.deltaTime);
	}
}
