using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;
using TMPro;

public class DemoController : MonoBehaviour
{
	[SerializeField] private Volume _MainVP;
	[SerializeField] private DayTimeManager _DayTimeManager;
	[SerializeField] private string _MapName;

	[Header("Fade-in sequence")]
	[SerializeField] private CanvasGroup _CanvasGroup;
	[SerializeField] private Image _BlackImage = null;
	[SerializeField] private TextMeshProUGUI _Text = null;

	private void Awake()
	{
		_MainVP.profile.TryGet(out Fog fog);

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

		GameManager._IsPaused = true;
		_DayTimeManager.enabled = false;
		_Text.text = "";
	}

	private void Start()
	{
		StartCoroutine(IE_StartScene());
	}

	IEnumerator IE_StartScene()
	{
		_BlackImage.enabled = true;
		yield return null;

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

		_BlackImage.enabled = false;
		_Text.enabled = false;
		GameManager._IsPaused = false;
	}
}
