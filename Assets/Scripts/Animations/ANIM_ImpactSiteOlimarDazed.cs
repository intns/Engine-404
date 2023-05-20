using System.Collections;
using UnityEngine;

public class ANIM_ImpactSiteOlimarDazed : MonoBehaviour
{
	[Header("Animation Curves")]
	[SerializeField] AnimationCurve _TwistCurve;
	[SerializeField] AnimationCurve _LookupCurve;

	public void StartAnimation()
	{
		StartCoroutine(ANIM_Twist());
	}

	IEnumerator ANIM_Twist()
	{
		yield return null;

		Camera _Camera = Camera.main;
		_Camera.fieldOfView = 45.0f;

		CameraFollow cameraFollow = _Camera.GetComponent<CameraFollow>();
		cameraFollow.enabled = false;

		Player _Player = Player._Instance;

		_Camera.transform.position = _Player.transform.right * 5.0f + Vector3.up * 10.0f;
		_Camera.transform.LookAt(_Player.transform.position);

		Vector3 playerToCamera = _Camera.transform.position - _Player.transform.position;
		float rotationAngle = -Mathf.Atan2(playerToCamera.z, playerToCamera.x);

		float t = 0;
		float length = 17.0f;

		while (t <= length)
		{
			float tPercentage = t / length;

			if (tPercentage < 0.7f)
			{
				float normalizedT = tPercentage / 0.7f;

				float twistRange = Mathf.Lerp(1.5f, 0.6f, _TwistCurve.Evaluate(normalizedT));
				float heightRange = Mathf.Lerp(12.0f, 7.0f, _TwistCurve.Evaluate(normalizedT));

				_Camera.transform.position = _Player.transform.position + MathUtil.XZToXYZ(MathUtil.PositionInUnit(rotationAngle + twistRange, 10.5f), heightRange);
				_Camera.transform.LookAt(_Player.transform);
			}
			else if (tPercentage < 0.88f)
			{
				float amount = Mathf.Lerp(0.0f, 2.0f, _LookupCurve.Evaluate((tPercentage - 0.7f) / 0.18f));

				Vector3 dest = _Player.transform.position + _Player.transform.forward * 7.0f;

				if (Physics.Raycast(dest + Vector3.up * 5.0f, Vector3.down, out RaycastHit hit))
				{
					dest = hit.point + Vector3.up * 1.5f;
				}

				_Camera.transform.position = dest;
				_Camera.transform.LookAt(_Player.transform.position + Vector3.up * amount);
			}

			t += Time.deltaTime;
			yield return null;
		}

		_Player.transform.eulerAngles += Vector3.up * 90;
		cameraFollow.enabled = true;
		_Player.Pause(PauseType.Unpaused);
	}
}
