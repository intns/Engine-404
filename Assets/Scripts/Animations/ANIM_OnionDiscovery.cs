using System.Collections;
using Demo;
using UnityEngine;

public class ANIM_OnionDiscovery : MonoBehaviour
{
	[SerializeField] SkinnedMeshRenderer _BodyRenderer;

	[SerializeField] Transform _LookAtTarget;
	[SerializeField] Animator _Animator;
	[SerializeField] Onion _Onion;
	[SerializeField] AnimationClip _DiscoverClip;
	Camera _Camera;
	Player _Player;

	bool _Playing;

	void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			StartCoroutine(IE_Play());
		}
	}

	IEnumerator IE_Play()
	{
		if (_Playing)
		{
			yield break;
		}

		_Playing = true;

		yield return null;

		_Camera = Camera.main;
		_Player = Player._Instance;


		// Pause the game
		Player._Instance.Pause(PauseType.Paused);
		_Camera.GetComponent<CameraFollow>().enabled = false;

		float maxFov = 55;
		float minFov = 30;

		Color fadedColor = new(0.4078f, 0.4078f, 0.4078f);

		_BodyRenderer.material.color = fadedColor;

		FadeManager._Instance.FadeInOut(
			0.75f,
			0.75f,
			() =>
			{
				Vector3 fromPlayerToTarget = MathUtil.DirectionFromTo(_Player.transform.position, _LookAtTarget.position);
				_Camera.transform.position = _Player.transform.position - fromPlayerToTarget * 12.5f + Vector3.up * 15;
				_Camera.transform.LookAt(_LookAtTarget.position + Vector3.down * 4);
				_Camera.fieldOfView = minFov;
			}
		);

		yield return new WaitForSeconds(2);

		_Animator.SetTrigger("Discover");
		_Onion.GetComponent<MeshRenderer>().enabled = false;

		float t = 0;
		float length = _DiscoverClip.length + 0.5f;

		while (t <= length)
		{
			// Rotate the camera to look at the Player
			_Camera.transform.rotation = Quaternion.Lerp(
				_Camera.transform.rotation,
				Quaternion.LookRotation(
					MathUtil.DirectionFromTo(
						_Camera.transform.position,
						_LookAtTarget.position + Vector3.down * 4,
						true
					)
				),
				MathUtil.EaseOut3(10 * Time.deltaTime)
			);

			_Camera.fieldOfView = Mathf.Lerp(minFov, maxFov, MathUtil.EaseOut4(t / length));
			_BodyRenderer.material.color = Color.Lerp(fadedColor, Color.white, MathUtil.EaseOut4(t / length));
			t += Time.deltaTime;
			yield return null;
		}

		_Onion.AddSproutsToSpit(1, _Onion.Colour);

		t = 0;
		length = 5;

		while (t <= length)
		{
			_Camera.transform.rotation = Quaternion.Lerp(
				_Camera.transform.rotation,
				Quaternion.LookRotation(
					MathUtil.DirectionFromTo(
						_Camera.transform.position,
						_LookAtTarget.position + Vector3.down * 7.5f,
						true
					)
				),
				MathUtil.EaseOut3(t / length)
			);

			t += Time.deltaTime;
			yield return null;
		}

		FadeManager._Instance.FadeInOut(
			0.75f,
			0.75f,
			() =>
			{
				_Player.Pause(PauseType.Unpaused);
				_Camera.GetComponent<CameraFollow>().enabled = true;
			}
		);

		SaveData._CurrentData._DiscoveredOnions[_Onion.Colour] = true;

		_Onion.ANIM_EndDiscovery();

		gameObject.SetActive(false);
	}
}
