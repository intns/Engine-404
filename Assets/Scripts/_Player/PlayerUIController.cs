using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerUIController : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI _DayText;
	[SerializeField] private TextMeshProUGUI _SquadText;
	[SerializeField] private TextMeshProUGUI _AreaText;
	[SerializeField] private CanvasGroup _CanvasGroup;

	[SerializeField] bool _DisplayValues = false;
	int _InSquadAmount = 0;
	int _InFieldAmount = 0;

	private IEnumerator FadeInCanvas()
	{
		float t = 0;
		float time = 1;
		_DisplayValues = false;

		while (t <= time)
		{
			t += Time.deltaTime;

			_CanvasGroup.alpha = Mathf.Lerp(0, 1, MathUtil.EaseIn4(t / time));
			yield return null;
		}

		_DisplayValues = true;
		_DayText.GetComponent<Animation>().Play();
		_SquadText.GetComponent<Animation>().Play();
		_AreaText.GetComponent<Animation>().Play();
	}

	private IEnumerator FadeOutCanvas()
	{
		float t = 0;
		float time = 1;
		_DisplayValues = true;

		while (t <= time)
		{
			t += Time.deltaTime;
			_CanvasGroup.alpha = Mathf.Lerp(1, 0, MathUtil.EaseIn4(t / time));
			yield return null;
		}

		_DisplayValues = false;
	}

	public void FadeInUI() => StartCoroutine(FadeInCanvas());
	public void FadeOutUI() => StartCoroutine(FadeOutCanvas());

	private void Awake()
	{
		if (!PlayerPrefs.HasKey("day"))
		{
			PlayerPrefs.SetInt("day", 0);
		}

		_DayText.text = string.Empty;
		_SquadText.text = string.Empty;
		_AreaText.text = string.Empty;
		FadeInUI();
	}

	private void Update()
	{
		if (_DisplayValues)
		{
			int newInSquad = PikminStatsManager.GetTotalInSquad();
			int newOnField = PikminStatsManager.GetTotalOnField();

			if (_InSquadAmount != newInSquad)
			{
				_SquadText.GetComponent<Animation>().Stop();
				_SquadText.GetComponent<Animation>().Play();
			}

			if (_InFieldAmount != newOnField)
			{
				_AreaText.GetComponent<Animation>().Stop();
				_AreaText.GetComponent<Animation>().Play();
			}

			_InSquadAmount = newInSquad;
			_InFieldAmount = newOnField;

			_SquadText.text = _InSquadAmount.ToString();
			_AreaText.text = _InFieldAmount.ToString();
			_DayText.text = PlayerPrefs.GetInt("day").ToString();
		}
	}
}
