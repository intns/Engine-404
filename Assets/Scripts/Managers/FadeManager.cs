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
	private FadeManager()
	{
		Globals._FadeManager = this;
	}

	private Image GetBlackImage()
	{
		if (Globals._Player == null)
		{
			// We're in a UI scene
			BaseUIController uiController = FindObjectOfType<BaseUIController>();
			Debug.Assert(uiController != null);
			return uiController._FadePanel;
		}
		else
		{
			// We're in a level
			Image fadePanel = Globals._Player._UIController._FadePanel;
			Debug.Assert(fadePanel != null);
			return fadePanel;
		}
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
	public void FadeInOut(float time, Action midFade)
	{
		StartCoroutine(FadeInOut_Coroutine(time, time, midFade));
	}

	public void FadeInOut(float fadeIn, float fadeOut, Action midFade)
	{
		StartCoroutine(FadeInOut_Coroutine(fadeIn, fadeOut, midFade));
	}

	private IEnumerator FadeOut_Coroutine(float time, Action onFadeEnd)
	{
		Image blackImg = GetBlackImage();
		blackImg.enabled = true;

		float t = 0;
		while (t <= time)
		{
			t += Time.deltaTime;
			if (blackImg != null)
			{
				blackImg.color = new Color(0, 0, 0, t / time);
			}

			yield return null;
		}
		if (blackImg != null)
		{
			blackImg.color = new Color(0, 0, 0, 255);
		}

		yield return null;

		onFadeEnd.Invoke();
	}

	private IEnumerator FadeIn_Coroutine(float time, Action onFadeEnd)
	{
		Image blackImg = GetBlackImage();
		blackImg.enabled = true;

		float t = 0;
		while (t <= time)
		{
			t += Time.deltaTime;
			if (blackImg != null)
			{
				blackImg.color = new Color(0, 0, 0, Mathf.Lerp(1, 0, t / time));
			}

			yield return null;
		}
		if (blackImg != null)
		{
			blackImg.color = new Color(0, 0, 0, 0);
		}

		yield return null;

		onFadeEnd.Invoke();
	}

	private IEnumerator FadeInOut_Coroutine(float fadeIn, float fadeOut, Action midFade)
	{
		Image blackImg = GetBlackImage();
		blackImg.enabled = true;

		float t = 0;
		while (t <= fadeIn)
		{
			t += Time.deltaTime;
			if (blackImg != null)
			{
				blackImg.color = new Color(0, 0, 0, t / fadeIn);
			}

			yield return null;
		}
		if (blackImg != null)
		{
			blackImg.color = new Color(0, 0, 0, 255);
		}

		yield return null;

		midFade.Invoke();

		yield return null;

		blackImg = GetBlackImage();
		blackImg.enabled = true;

		t = 0;
		while (t <= fadeOut)
		{
			t += Time.deltaTime;
			if (blackImg != null)
			{
				blackImg.color = new Color(0, 0, 0, Mathf.Lerp(1, 0, t / fadeOut));
			}

			yield return null;
		}
		if (blackImg != null)
		{
			blackImg.color = new Color(0, 0, 0, 0);
		}

		yield return null;
	}
}
