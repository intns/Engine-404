using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ANIM_OnionDiscovery : MonoBehaviour
{
	[SerializeField] private SkinnedMeshRenderer _BodyRenderer = null;

	[SerializeField] Transform _LookAtTarget = null;
	[SerializeField] Animator _Animator = null;
	[SerializeField] Onion _Onion = null;
	[SerializeField] AnimationClip _DiscoverClip;
	Camera _Camera;
	Player _Player;

	IEnumerator IE_Play()
	{
		yield return null;

		_Camera = Camera.main;
		_Player = Player._Instance;


		// Pause the game
		_Player.Pause(true);
		_Camera.GetComponent<CameraFollow>().enabled = false;

		float maxFov = _Camera.fieldOfView;
		float minFov = 30;

		Color fadedColor = new Color(0.4078f, 0.4078f, 0.4078f);

		_BodyRenderer.material.color = fadedColor;

		FadeManager._Instance.FadeInOut(0.3f, 0.3f, () =>
		{
			Vector3 fromPlayerToTarget = MathUtil.DirectionFromTo(_Player.transform.position, _LookAtTarget.position);
			_Camera.transform.position = _Player.transform.position - (fromPlayerToTarget * 12.5f) + (Vector3.up * 15);
			_Camera.transform.LookAt(_LookAtTarget.position + (Vector3.down * 4));
			_Camera.fieldOfView = minFov;
		});

		yield return new WaitForSeconds(1.7f);

		_Animator.SetTrigger("Discover");

		float t = 0;
		float length = _DiscoverClip.length + 0.5f;
		while (t <= length)
		{
			// Rotate the camera to look at the Player
			_Camera.transform.rotation = Quaternion.Lerp(_Camera.transform.rotation,
				Quaternion.LookRotation(MathUtil.DirectionFromTo(_Camera.transform.position, _LookAtTarget.position + (Vector3.down * 4), true)),
				MathUtil.EaseOut3(10 * Time.deltaTime));

			_Camera.fieldOfView = Mathf.Lerp(minFov, maxFov, MathUtil.EaseOut4(t / length));
			_BodyRenderer.material.color = Color.Lerp(fadedColor, Color.white, MathUtil.EaseOut4(t / length));
			t += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}

		t = 0;
		length = 1.5f;
		while (t <= length)
		{
			_Camera.transform.rotation = Quaternion.Lerp(_Camera.transform.rotation,
				Quaternion.LookRotation(MathUtil.DirectionFromTo(_Camera.transform.position, _LookAtTarget.position + (Vector3.down * 6), true)),
				MathUtil.EaseOut3(2.5f * Time.deltaTime));

			t += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}

		_Onion.AddSproutsToSpawn(1, PikminColour.Red);

		yield return new WaitForSeconds(1.25f);

		_Player.Pause(false);
		_Camera.GetComponent<CameraFollow>().enabled = true;

		PlayerPrefs.SetInt("ONION_Discovered", 1);

		gameObject.SetActive(false);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			StartCoroutine(IE_Play());
		}
	}
}
