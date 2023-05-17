using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextBoxArea : MonoBehaviour
{
	[SerializeField] Canvas _Canvas = null;
	[SerializeField] Animation _PanelAnimationComponent = null;
	[SerializeField] TextMeshProUGUI _Text = null;

	[Header("Settings")]
	[SerializeField] float _CharacterPressTime = 0.2f;
	[SerializeField] TextBoxEntry _Entry;
	int _PageIndex = 0;

	string _GlobalName = "";
	bool _Enabled = false;

	bool _FinishedPageWrite = false;

	#region Unity Functions
	void OnEnable()
	{
		_Canvas.gameObject.SetActive(false);
		Debug.Assert(_Entry != null);

		_GlobalName = gameObject.name + "_" + gameObject.GetInstanceID();
		if (PlayerPrefs.GetInt(_GlobalName) == 1)
		{
			return;
		}

		PlayerPrefs.SetInt(_GlobalName, 0);
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
			PlayerPrefs.SetInt(_GlobalName, 1);
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
		if (!other.CompareTag("Player") || PlayerPrefs.GetInt(_GlobalName) == 1
				|| GameManager.IsPaused)
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
		float t = 0;
		float time = 0.5f;

		CanvasGroup _CanvasGroup = _Canvas.GetComponent<CanvasGroup>();

		_Canvas.gameObject.SetActive(true);
		_PanelAnimationComponent.Play();

		while (t <= time)
		{
			t += Time.deltaTime;
			_CanvasGroup.alpha = MathUtil.EaseOut3(t / time);
			yield return null;
		}
	}

	IEnumerator FadeOutCanvas()
	{
		float t = 0;
		float time = 0.5f;

		CanvasGroup _CanvasGroup = _Canvas.GetComponent<CanvasGroup>();

		while (t <= time)
		{
			t += Time.deltaTime;

			_CanvasGroup.alpha = 1 - MathUtil.EaseOut3(t / time);
			yield return null;
		}

		_Canvas.gameObject.SetActive(false);
	}

	IEnumerator WriteText(string toWrite)
	{
		_Text.text = string.Empty;

		WaitForSeconds seconds = new WaitForSeconds(_CharacterPressTime);

		for (int i = 0; i < toWrite.Length; i++)
		{
			if (_FinishedPageWrite)
			{
				break;
			}

			string wholeToken = toWrite[i].ToString();
			if (toWrite[i] == '<' && toWrite.Contains("</"))
			{
				int endIdx = toWrite.IndexOf("</", i);
				int endEndIdx = toWrite.IndexOf(">", endIdx);
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
