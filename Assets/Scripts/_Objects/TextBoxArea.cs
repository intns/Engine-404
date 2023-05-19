using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextBoxArea : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] Canvas _Canvas;
	[SerializeField] Animation _PanelAnimationComponent;
	[SerializeField] TextMeshProUGUI _Text;

	[Header("Settings")]
	[SerializeField] float _CharacterPressTime = 0.2f;
	[SerializeField] TextBoxEntry _Entry;
	CanvasGroup _CanvasGroup;
	bool _Enabled;

	bool _FinishedPageWrite;

	bool _IsFadingOut;
	int _PageIndex;

	#region Unity Functions

	void Awake()
	{
		_CanvasGroup = _Canvas.GetComponent<CanvasGroup>();
	}

	void OnEnable()
	{
		_Canvas.gameObject.SetActive(false);
		Debug.Assert(_Entry != null);
	}

	public void AButton()
	{
		if (!_Enabled || !_FinishedPageWrite)
		{
			return;
		}

		if (_PageIndex == _Entry._Pages.Count - 1)
		{
			_Enabled = false;
			Player._Instance.Pause(PauseType.Unpaused);
			Player._Instance._UIController.FadeInUI(true);
			StartCoroutine(FadeOutCanvas());
		}
		else
		{
			_PageIndex++;
			_FinishedPageWrite = false;
			StartCoroutine(WriteText(_Entry._Pages[_PageIndex]._Text));
		}
	}

	public void BButton()
	{
		if (!_Enabled)
		{
			return;
		}

		_FinishedPageWrite = true;
	}

	void OnTriggerStay(Collider other)
	{
		if (!other.CompareTag("Player") || GameManager.IsPaused || _IsFadingOut)
		{
			return;
		}

		// Pause the game and the Player
		Player._Instance.Pause(PauseType.Paused);

		// Fade out with old and in with new!
		Player._Instance._UIController.FadeOutUI();
		StartCoroutine(FadeInCanvas());

		_Enabled = true;
		StartCoroutine(WriteText(_Entry._Pages[0]._Text));
	}

	#endregion

	#region IEnumerators

	IEnumerator FadeInCanvas()
	{
		const float fadeInTime = 0.5f;

		_Canvas.gameObject.SetActive(true);
		_PanelAnimationComponent.Play();

		for (float elapsedTime = 0f; elapsedTime < fadeInTime; elapsedTime += Time.deltaTime)
		{
			_CanvasGroup.alpha = MathUtil.EaseOut3(elapsedTime / fadeInTime);
			yield return null;
		}

		_CanvasGroup.alpha = 1f;
	}

	IEnumerator FadeOutCanvas()
	{
		const float fadeInTime = 0.5f;
		_IsFadingOut = true;

		for (float elapsedTime = 0f; elapsedTime < fadeInTime; elapsedTime += Time.deltaTime)
		{
			_CanvasGroup.alpha = 1.0f - MathUtil.EaseOut3(elapsedTime / fadeInTime);
			yield return null;
		}

		_CanvasGroup.alpha = 1.0f;
		_Canvas.gameObject.SetActive(false);

		Destroy(gameObject);
	}

	IEnumerator WriteText(string toWrite)
	{
		_Text.text = string.Empty;
		WaitForSeconds seconds = new(_CharacterPressTime);

		for (int i = 0; i < toWrite.Length; i++)
		{
			if (_FinishedPageWrite)
			{
				break;
			}

			string wholeToken = toWrite[i].ToString();

			if (toWrite[i] == '<' && toWrite.Contains("</"))
			{
				int endIdx = toWrite.IndexOf("</", i, StringComparison.Ordinal);
				int endEndIdx = toWrite.IndexOf(">", endIdx, StringComparison.Ordinal);

				if (endIdx == -1 || endEndIdx == -1)
				{
					throw new KeyNotFoundException("Text Box Area string doesn't have a closing tag after beginning tag! A < was found with no </ to finish.");
				}

				// Close the tag
				wholeToken = toWrite[i..(endEndIdx + 1)];
				i = endEndIdx;
				yield return new WaitForSeconds(_CharacterPressTime * 20);
			}

			_Text.text += wholeToken;
			yield return seconds;
		}

		_Text.text = toWrite;
		_FinishedPageWrite = true;
		yield return null;
	}

	#endregion
}
