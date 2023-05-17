/*
 * FadeManager.cs
 * Created by: Ambrosia
 * Created on: 26/4/2020 (dd/mm/yy)
 * Created for: Needing an object that can smoothly transition between events with a PikminColoured backdrop overlaying the screen
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
	[SerializeField] Image _FadeImage;

	FadeManager()
	{
		_Instance = this;
	}

	public static FadeManager _Instance { get; private set; }

	void Awake()
	{
		_Instance = this;
	}

	// Note: onFadeEnd must have no parameters
	public void FadeIn(float time, Action onFadeEnd)
	{
		StartCoroutine(FadeIn_Coroutine(time, onFadeEnd));
	}

	// Note: midFade must have no parameters
	public void FadeInOut(float fadeIn, float fadeOut, Action midFade)
	{
		StartCoroutine(FadeInOut_Coroutine(fadeIn, fadeOut, midFade));
	}

	// Note: onFadeEnd must have no parameters
	public void FadeOut(float time, Action onFadeEnd)
	{
		StartCoroutine(FadeOut_Coroutine(time, onFadeEnd));
	}

	IEnumerator FadeIn_Coroutine(float time, Action onFadeEnd)
	{
		_FadeImage.enabled = true;

		float t = 0;

		while (t <= time)
		{
			t += Time.deltaTime;
			_FadeImage.color = new(0, 0, 0, 1 - MathUtil.EaseOut3(t / time));

			yield return null;
		}

		_FadeImage.color = new(0, 0, 0, 0);
		_FadeImage.enabled = false;

		yield return new WaitForSecondsRealtime(0.2f);

		onFadeEnd?.Invoke();
	}

	IEnumerator FadeInOut_Coroutine(float fadeIn, float fadeOut, Action midFade)
	{
		// Fade in
		_FadeImage.enabled = true;

		float t = 0;

		while (t <= fadeIn)
		{
			t += Time.deltaTime;
			_FadeImage.color = new(0, 0, 0, MathUtil.EaseIn3(t / fadeIn));

			yield return null;
		}

		_FadeImage.color = new(0, 0, 0, 255);

		yield return new WaitForSecondsRealtime(0.1f);

		// Black screen at point of invoke
		midFade?.Invoke();

		yield return new WaitForSecondsRealtime(0.1f);

		t = 0;

		while (t <= fadeOut)
		{
			t += Time.deltaTime;
			_FadeImage.color = new(0, 0, 0, 1 - MathUtil.EaseIn3(t / fadeOut));
			yield return null;
		}

		_FadeImage.color = new(0, 0, 0, 0);
		_FadeImage.enabled = false;

		yield return new WaitForSecondsRealtime(0.1f);
	}

	IEnumerator FadeOut_Coroutine(float time, Action onFadeEnd)
	{
		_FadeImage.enabled = true;

		float t = 0;

		while (t <= time)
		{
			t += Time.deltaTime;
			_FadeImage.color = new(0, 0, 0, MathUtil.EaseIn3(t / time));

			yield return null;
		}

		_FadeImage.color = new(0, 0, 0, 255);
		_FadeImage.enabled = true;

		yield return new WaitForSecondsRealtime(0.2f);

		onFadeEnd?.Invoke();
	}
}
