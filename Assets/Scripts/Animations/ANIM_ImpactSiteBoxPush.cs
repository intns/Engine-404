using System.Collections;
using UnityEngine;

public class ANIM_ImpactSiteBoxPush : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] CardboardBox _Box;

	void Awake()
	{
		_Box._OnPush += () => { StartCoroutine(ANIM_Overlook()); };
	}

	#region IEnumerators

	IEnumerator ANIM_Overlook()
	{
		yield return null;

		Camera camera = Camera.main;
		Player player = Player._Instance;
		CameraFollow cameraController = camera.GetComponent<CameraFollow>();

		player.Pause(PauseType.OnlyPikminActive);
		cameraController.enabled = false;

		const float MAX_FOV = 65;
		const float MIN_FOV = 45;

		Vector3 startPosition = _Box.transform.position
		                        + _Box.transform.forward * 17.5f
		                        + Vector3.up * 11.5f;

		bool startAnimation = false;

		FadeManager._Instance.FadeInOut(
			0.75f,
			0.75f,
			() =>
			{
				camera.transform.position = startPosition;
				camera.transform.LookAt(_Box.transform.position);

				camera.fieldOfView = MIN_FOV;
				startAnimation = true;
			}
		);

		while (startAnimation == false)
		{
			yield return null;
		}

		for (float t = 0; t < 15; t += Time.deltaTime)
		{
			camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, MAX_FOV, 0.1f * Time.deltaTime);

			if (t <= 11.5f)
			{
				camera.transform.RotateAround(_Box.transform.position, Vector3.up, -7.0f * Time.deltaTime);
			}
			else
			{
				camera.transform.position = Vector3.Lerp(camera.transform.position, camera.transform.position + Vector3.down * 2.0f, 0.25f * Time.deltaTime);

				camera.transform.rotation = Quaternion.Lerp(
					camera.transform.rotation,
					Quaternion.LookRotation(MathUtil.DirectionFromTo(camera.transform.position, _Box.transform.position + Vector3.up * 4.5f, true)),
					0.6f * Time.deltaTime
				);
			}

			yield return null;
		}

		FadeManager._Instance.FadeInOut(
			0.75f,
			0.75f,
			() =>
			{
				player.Pause(PauseType.Unpaused);

				camera.transform.LookAt(player.transform.position);
				cameraController.enabled = true;
			}
		);
	}

	#endregion
}
