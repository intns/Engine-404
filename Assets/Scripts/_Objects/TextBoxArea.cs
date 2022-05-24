using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextBoxArea : MonoBehaviour
{
	[Header("Components")]
	Transform _Transform = null;
	[SerializeField] Canvas _Canvas = null;
	[SerializeField] Animation _PanelAnimationComponent = null;
	[SerializeField] TextMeshProUGUI _Text = null;

	[Header("Settings")]
	[SerializeField] float _CharacterPressTime = 0.2f;
	[SerializeField] TextBoxEntry _Entry;
	int _PageIndex = 0;

	string _GlobalName = "";
	bool _Enabled = false;

	bool _FinishEarly = false;
	bool _FinishedPageWrite = false;

	#region Unity Functions
	private void OnEnable()
	{
		_Canvas.gameObject.SetActive(false);
		Debug.Assert(_Entry != null);
	}

	private void Awake()
	{
		_Transform = transform;
		_GlobalName = gameObject.name + "_" + gameObject.GetInstanceID();
		PlayerPrefs.SetInt(_GlobalName, 0);
	}

	private void Update()
	{
		if (!_Enabled)
		{
			return;
		}

		if (Input.GetButtonDown("A Button") && _FinishedPageWrite)
		{
			if (_PageIndex == _Entry._Pages.Count - 1)
			{
				_Enabled = false;

				GameManager._IsPaused = false;
				Player._Instance._MovementController._Paralysed = false;

				Player._Instance._UIController.FadeInUI();
				StartCoroutine(FadeOutCanvas());
			}
			else
			{
				_PageIndex++;
				_FinishedPageWrite = false;
				StartCoroutine(WriteText(_Entry._Pages[_PageIndex]._Text));
			}
		}

		if (Input.GetButtonDown("B Button"))
		{
			_FinishedPageWrite = true;
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!other.CompareTag("Player") || PlayerPrefs.GetInt(_GlobalName) == 1)
		{
			return;
		}

		PlayerPrefs.SetInt(_GlobalName, 1);

		// Pause the game and the Player
		GameManager._IsPaused = true;
		Player._Instance._MovementController._Paralysed = true;

		// Fade out with old and in with new!
		Player._Instance._UIController.FadeOutUI();
		StartCoroutine(FadeInCanvas());

		_Enabled = true;
		StartCoroutine(WriteText(_Entry._Pages[0]._Text));
	}
	#endregion

	#region IEnumerators
	private IEnumerator FadeInCanvas()
	{
		float t = 0;
		float time = 0.5f;

		CanvasGroup _CanvasGroup = _Canvas.GetComponent<CanvasGroup>();

		_Canvas.gameObject.SetActive(true);
		_PanelAnimationComponent.Play();

		while (t <= time)
		{
			t += Time.deltaTime;
			_CanvasGroup.alpha = Mathf.Lerp(0, 1, MathUtil.EaseOut2(t / time));
			yield return null;
		}
	}

	private IEnumerator FadeOutCanvas()
	{
		float t = 0;
		float time = 0.5f;

		CanvasGroup _CanvasGroup = _Canvas.GetComponent<CanvasGroup>();

		while (t <= time)
		{
			t += Time.deltaTime;

			_CanvasGroup.alpha = Mathf.Lerp(1, 0, MathUtil.EaseOut2(t / time));
			yield return null;
		}

		_Canvas.gameObject.SetActive(false);
	}

	private IEnumerator WriteText(string toWrite)
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
				endEndIdx++;
				wholeToken = toWrite[i..endEndIdx];
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
