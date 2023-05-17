/*
 * MainMenuUI.cs
 * Created by: Ambrosia
 * Created on: 25/4/2020 (dd/mm/yy)
 * Created for: needing a UI for the main menu
 */

using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.InputSystem.InputAction;

public enum MainMenuState
{
	None = 0,

	PressStart,
	Menu,
}

public class MainMenuUI : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] Transform _Canvas;
	[SerializeField] CanvasGroup _EngineLogo;
	[Space]
	[SerializeField] CanvasGroup _PressStartCanvasGroup;
	[SerializeField] TextMeshProUGUI _StartText;
	[Space]
	[SerializeField] CanvasGroup _MainMenuCanvasGroup;
	[Space]
	[SerializeField] Transform _TemplateButton;

	[Header("Settings")]
	[SerializeField] float _FadeinBlackTime = 2;
	[SerializeField] float _FadeinUITime = 2;
	[SerializeField] float _FadeoutTime = 2;

	[Header("Audio")]
	[SerializeField] AudioClip _HoverAudio;
	[SerializeField] AudioClip _SelectAudio;

	[Header("Debugging")]
	[SerializeField] MainMenuState _State = MainMenuState.None;

	float _StartTextTimer;

	void Awake()
	{
		_PressStartCanvasGroup.alpha = 0;
		_MainMenuCanvasGroup.alpha = 0;
		_EngineLogo.alpha = 0;

		_PressStartCanvasGroup.gameObject.SetActive(true);
		_MainMenuCanvasGroup.gameObject.SetActive(false);
	}

	void Start()
	{
		ChangeState(MainMenuState.PressStart);
	}

	void Update()
	{
		switch (_State)
		{
			case MainMenuState.None:
				break;
			case MainMenuState.PressStart:
				_StartText.alpha = Mathf.Lerp(0.0f, 1.0f, MathUtil.EaseIn2(Mathf.Abs(Mathf.Sin(_StartTextTimer * 2))));

				_StartTextTimer += Time.deltaTime;
				break;
			case MainMenuState.Menu:
				break;
		}
	}

	#region Misc Functions

	void ChangeState(MainMenuState newState)
	{
		switch (newState)
		{
			case MainMenuState.None:
				break;
			case MainMenuState.PressStart:
				_StartTextTimer = 0.0f;

				FadeManager._Instance.FadeIn(_FadeinBlackTime, () => { StartCoroutine(FadeInStartText()); });
				break;
			case MainMenuState.Menu:
				StartCoroutine(FadeOutCanvas(_PressStartCanvasGroup));

				if (Application.isEditor || Debug.isDebugBuild)
				{
					StartCoroutine(FadeInDebugControls());
				}

				StartCoroutine(FadeInCanvas(_MainMenuCanvasGroup));
				break;
		}

		_State = newState;
	}

	#endregion

	#region Public Functions

	public void CONTROLS_AnyButtonPress(CallbackContext ctx)
	{
		if (_State != MainMenuState.PressStart || _StartTextTimer < 8.0f)
		{
			return;
		}

		if (ctx.started)
		{
			_StartText.GetComponent<Animation>().Play("anim_gui_shrink");
			ChangeState(MainMenuState.Menu);
		}
	}

	public void PressPlay()
	{
		AudioSource.PlayClipAtPoint(_SelectAudio, Camera.main.transform.position, 0.25f);
		FadeManager._Instance.FadeInOut(_FadeoutTime, _FadeoutTime, () => SceneManager.LoadScene(1));
	}

	public void PressExit()
	{
		AudioSource.PlayClipAtPoint(_SelectAudio, Camera.main.transform.position, 0.25f);

		FadeManager._Instance.FadeOut(
			_FadeoutTime,
			() =>
			{
				Application.Quit();
				Debug.Break();
			}
		);
	}

	public void OnPointerEnter(BaseEventData ev)
	{
		AudioSource.PlayClipAtPoint(_HoverAudio, Camera.main.transform.position, 0.25f);
	}

	#endregion

	#region IEnumerators

	IEnumerator FadeInCanvas(CanvasGroup cg)
	{
		cg.gameObject.SetActive(true);

		float t = 0;
		float time = _FadeinUITime;

		while (t <= time)
		{
			t += Time.deltaTime;
			cg.alpha = MathUtil.EaseIn3(t / time);
			yield return null;
		}
	}

	IEnumerator FadeOutCanvas(CanvasGroup cg)
	{
		float t = 0;
		float time = _FadeinUITime;

		while (t <= time)
		{
			t += Time.deltaTime;
			cg.alpha = MathUtil.EaseIn3(1 - t / time);
			yield return null;
		}

		cg.gameObject.SetActive(false);
	}

	IEnumerator FadeInStartText()
	{
		_PressStartCanvasGroup.gameObject.SetActive(true);

		float t = 0;
		float time = _FadeinUITime;

		while (t <= time)
		{
			t += Time.deltaTime;
			_PressStartCanvasGroup.alpha = MathUtil.EaseIn3(t / time);
			_EngineLogo.alpha = MathUtil.EaseIn3(t / time);
			yield return null;
		}

		_StartText.GetComponent<Animation>().Play("anim_gui_pulse_1sec");
	}

	IEnumerator FadeInDebugControls()
	{
		bool skippedCurrent = false;
		var objects = new List<Transform>();

		// Generate options for every scene
		for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
		{
			// Don't generate an option for the current scene
			if (SceneUtility.GetScenePathByBuildIndex(i) == SceneManager.GetActiveScene().path)
			{
				skippedCurrent = true;
				continue;
			}

			string sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));

			Transform obj = Instantiate(_TemplateButton, _Canvas);
			RectTransform rectTransform = obj.GetComponent<RectTransform>();
			obj.GetComponentInChildren<TextMeshProUGUI>().text = sceneName;

			obj.GetComponent<Button>().onClick.AddListener(
				() =>
					FadeManager._Instance.FadeInOut(1, 1, () => SceneManager.LoadScene(sceneName))
			);

			obj.GetComponent<RectTransform>().localPosition = new(
				rectTransform.localPosition.x,
				rectTransform.localPosition.y - (skippedCurrent ? i - 1 : i) * 100
			);
			obj.gameObject.SetActive(true);
			objects.Add(obj);
		}

		var imageComponents = new List<Image>();
		var textComponents = new List<TextMeshProUGUI>();

		foreach (Transform t1 in objects)
		{
			imageComponents.Add(t1.GetComponent<Image>());
			textComponents.Add(t1.GetComponentInChildren<TextMeshProUGUI>());
		}

		float t = 0;
		float time = _FadeinUITime;

		while (t <= time)
		{
			t += Time.deltaTime;

			foreach (Image item in imageComponents)
			{
				item.color = Color.Lerp(Color.clear, Color.white, t / time);
			}

			foreach (TextMeshProUGUI item in textComponents)
			{
				item.alpha = Mathf.SmoothStep(0, 1, t / time);
			}

			yield return null;
		}

		// Reparent because we want it to overlay everything
		yield return null;
	}

	#endregion
}
