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
	public static FadeManager _Instance { get; private set; }

	[SerializeField] Image _FadeImage;

	FadeManager()
	{
		_Instance = this;
	}

	void Awake()
	{
		_Instance = this;
	}

	// Note: onFadeEnd must have no parameters
	public void FadeOut(float time, Action onFadeEnd)
	{
		StartCoroutine(FadeOut_Coroutine(time, onFadeEnd));
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

	private IEnumerator FadeOut_Coroutine(float time, Action onFadeEnd)
	{
		_FadeImage.enabled = true;

		float t = 0;
		while (t <= time)
		{
			t += Time.deltaTime;
			_FadeImage.color = new Color(0, 0, 0, t / time);

			yield return null;
		}

		_FadeImage.color = new Color(0, 0, 0, 255);
		_FadeImage.enabled = true;

		yield return null;

		onFadeEnd.Invoke();
	}

	private IEnumerator FadeIn_Coroutine(float time, Action onFadeEnd)
	{
		_FadeImage.enabled = true;

		float t = 0;
		while (t <= time)
		{
			t += Time.deltaTime;
			_FadeImage.color = new Color(0, 0, 0, Mathf.Lerp(1, 0, MathUtil.EaseOut3(t / time)));

			yield return null;
		}
		_FadeImage.color = new Color(0, 0, 0, 0);
		_FadeImage.enabled = false;

		yield return null;

		onFadeEnd.Invoke();
	}

	private IEnumerator FadeInOut_Coroutine(float fadeIn, float fadeOut, Action midFade)
	{
		// Fade in
		_FadeImage.enabled = true;

		float t = 0;
		while (t <= fadeIn)
		{
			t += Time.deltaTime;
			_FadeImage.color = new Color(0, 0, 0, t / fadeIn);

			yield return null;
		}
		_FadeImage.color = new Color(0, 0, 0, 255);

		yield return null;

		// Black screen at point of invoke
		midFade.Invoke();

		yield return null;

		t = 0;
		while (t <= fadeOut)
		{
			t += Time.deltaTime;
			_FadeImage.color = new Color(0, 0, 0, Mathf.Lerp(1, 0, t / fadeOut));
			yield return null;
		}

		_FadeImage.color = new Color(0, 0, 0, 0);
		_FadeImage.enabled = false;

		yield return null;
	}
}
