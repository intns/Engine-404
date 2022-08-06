using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;
using TMPro;

public class DemoController : MonoBehaviour
{
	[SerializeField] bool _ResetPrefs = false;
	[Space]
	[SerializeField] private Volume _MainVP;
	[SerializeField] private DayTimeManager _DayTimeManager;
	[SerializeField] private string _MapName;

	[Header("Fade-in sequence")]
	[SerializeField] private CanvasGroup _CanvasGroup;
	[SerializeField] private Image _BlackImage = null;
	[SerializeField] private TextMeshProUGUI _Text = null;

	[Header("Intro Sequence")]
	[SerializeField] bool _DoSequence = false;
	[SerializeField] Animator _CeresAnimator = null;

	private void Awake()
	{
		_Text.text = "";

		if (!_MainVP.profile.TryGet(out Fog fog))
		{
			return;
		}

		bool editFog = Random.Range(0, 5) < 2.5f;
		// We're going to randomise the VP a bit
		if (editFog)
		{
			fog.meanFreePath.value = Random.Range(50.0f, 250.0f);
			fog.baseHeight.value = Random.Range(-1.0f, 13.0f);
			fog.albedo.value = Color.Lerp(new Color(1, 0.95f, 0.815f), new Color(1, 0.578f, 0.306f), Random.Range(0.2f, 0.8f));
		}
		else
		{
			fog.enabled.value = false;
		}
	}

	private void Start()
	{
		if (_DoSequence)
		{
			Player._Instance.Pause(true);
			StartCoroutine(IE_StartScene());
		}
	}

	void Update()
	{
		if (_ResetPrefs)
		{
			PlayerPrefs.DeleteAll();
			_ResetPrefs = false;
		}
	}

	private void OnDrawGizmos()
	{
		if (_ResetPrefs)
		{
			PlayerPrefs.DeleteAll();
			_ResetPrefs = false;
		}
	}

	IEnumerator IE_StartScene()
	{
		_BlackImage.enabled = true;

		yield return new WaitForSeconds(1.5f);

		_Text.color = Color.clear;
		_Text.text = _MapName;

		float t = 0;
		while (t <= 2)
		{
			t += Time.deltaTime;
			_Text.color = Color.Lerp(Color.clear, Color.white, t / 2.0f);
			yield return new WaitForEndOfFrame();
		}

		yield return new WaitForSeconds(1.25f);

		_DayTimeManager.enabled = true;
		StartCoroutine(IE_DoAnimation());

		t = 0;
		while (t <= 2)
		{
			t += Time.deltaTime;
			_Text.color = Color.Lerp(Color.white, Color.clear, t / 2.0f);
			yield return new WaitForEndOfFrame();
		}

		t = 0;
		while (t <= 1.25f)
		{
			t += Time.deltaTime;
			_CanvasGroup.alpha = Mathf.Lerp(1, 0, t / 1.25f);
			yield return new WaitForEndOfFrame();
		}

		_Text.enabled = false;
		_BlackImage.enabled = false;
		_CanvasGroup.alpha = 1;

		yield return new WaitForSecondsRealtime(4);
		Player._Instance.Pause(false);
	}

	IEnumerator IE_DoAnimation()
	{
		Ship._Instance.SetEngineFlamesVFX(true);
		_CeresAnimator.SetTrigger("DEMO_INTRO");
		Transform main = Camera.main.transform;

		CameraFollow cameraFollow = main.GetComponent<CameraFollow>();
		cameraFollow.enabled = false;
		Transform shipTransform = _CeresAnimator.transform;

		main.position = shipTransform.position + Vector3.up * 15 + Vector3.back * 20;
		Vector3 position = Vector3.Lerp(main.position, shipTransform.position + Vector3.up * 20 + Vector3.forward * 35, 5 * Time.deltaTime);

		float t = 0;
		float length = 7.5f;
		while (t <= length)
		{
			// Rotate the camera to look at the Player
			position = Vector3.Lerp(position, shipTransform.position + Vector3.up * 20 + Vector3.forward * 35, 5 * Time.deltaTime);

			Quaternion rotation = Quaternion.Lerp(main.rotation,
							Quaternion.LookRotation(MathUtil.DirectionFromTo(main.position, shipTransform.position + Vector3.up * 5, true)),
							10 * Time.deltaTime);

			main.SetPositionAndRotation(position, rotation);

			t += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}

		cameraFollow.enabled = true;
		Ship._Instance.SetEngineFlamesVFX(false);
	}
}
