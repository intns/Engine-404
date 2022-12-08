using System.Collections;
using UnityEngine;

public class ANIM_ImpactSiteBoxPush : MonoBehaviour
{
	[Header("Components")]
	Transform _Transform = null;
	[SerializeField] CardboardBox _Box = null;

	void Awake()
	{
		_Transform = transform;
		_Box._OnPush += () =>
		{
			StartCoroutine(ANIM_Overlook());
		};
	}

	#region IEnumerators
	IEnumerator ANIM_Overlook()
	{
		yield return null;

		Camera _Camera = Camera.main;
		Player _Player = Player._Instance;

		// Pause the game
		_Player.Pause(PauseType.OnlyPikminActive);
		_Camera.GetComponent<CameraFollow>().enabled = false;

		float maxFov = 55;
		float minFov = 30;

		Color fadedColor = new Color(0.4078f, 0.4078f, 0.4078f);

		Vector3 originCameraPos = _Camera.transform.position;

		_Camera.transform.position = _Player.transform.position - (-_Box.transform.forward * 12.5f) + (Vector3.up * 15);
		_Camera.transform.LookAt(_Box.transform.position);
		_Camera.fieldOfView = minFov;

		float t = 0;
		float length = 15f;

		float startingOffs = Mathf.Atan2(_Transform.position.z, _Transform.position.x) - Mathf.Atan2(_Box.transform.position.z, _Box.transform.position.x);
		while (t <= length)
		{

			Vector3 pos = _Box.transform.position;
			if (t <= 11.5f)
			{
				pos += MathUtil.XZToXYZ(MathUtil.PositionInUnit(startingOffs + t / MathUtil.M_TAU, 20), 15);
				_Camera.fieldOfView = Mathf.Lerp(minFov, maxFov, t / length);
				_Camera.transform.SetPositionAndRotation(
					// Position
					Vector3.Lerp(_Camera.transform.position, pos, 3 * Time.deltaTime), Quaternion.Lerp(_Camera.transform.rotation,
					// Rotation
					Quaternion.LookRotation(MathUtil.DirectionFromTo(_Camera.transform.position, _Box.transform.position, true)),
					3 * Time.deltaTime));
			}
			else
			{
				_Camera.fieldOfView = Mathf.Lerp(_Camera.fieldOfView, 72.5f, 0.1f * Time.deltaTime);
				pos -= _Box.transform.right * 20;
				pos -= _Box.transform.forward * 3.5f;
				pos.y += 20;
				_Camera.transform.SetPositionAndRotation(
					// Position
					Vector3.Lerp(_Camera.transform.position, pos, Time.deltaTime),
					// Rotation
					Quaternion.Lerp(
						_Camera.transform.rotation,
						Quaternion.LookRotation(MathUtil.DirectionFromTo(_Camera.transform.position, _Box.transform.position + Vector3.up * 4.5f, true)),
						0.25f * Time.deltaTime));
			}

			t += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}

		FadeManager._Instance.FadeInOut(0.75f, 0.75f, () =>
		{
			_Camera.transform.position = originCameraPos;
			_Camera.transform.LookAt(_Player.transform.position);
			_Player.Pause(PauseType.Unpaused);
			_Camera.GetComponent<CameraFollow>().enabled = true;
		});
	}
	#endregion
}
