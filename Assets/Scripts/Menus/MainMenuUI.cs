/*
 * MainMenuUI.cs
 * Created by: Ambrosia
 * Created on: 25/4/2020 (dd/mm/yy)
 * Created for: needing a UI for the main menu
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] private Transform _Canvas = null;
	[SerializeField] private CanvasGroup _CanvasGroup = null;
	[SerializeField] private Transform _TemplateButton = null;

	private IEnumerator FadeInCanvas()
	{
		float t = 0;
		float time = 0.5f;
		while (t <= time)
		{
			t += Time.deltaTime;
			_CanvasGroup.alpha = Mathf.Lerp(0, 1, t / time);
			yield return null;
		}
	}

	private void Awake()
	{
		_CanvasGroup.alpha = 0;

		FadeManager._Instance.FadeIn(3.25f, new Action(() =>
		{
			if (Application.isEditor || Debug.isDebugBuild)
			{
				bool skippedCurrent = false;
				List<Transform> objects = new List<Transform>();
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
					obj.GetComponentInChildren<Text>().text = sceneName;
					obj.GetComponent<Button>().onClick.AddListener(() => FadeManager._Instance.FadeInOut(1, 1, () => SceneManager.LoadScene(sceneName)));
					obj.GetComponent<RectTransform>().localPosition = new Vector3(-450, 300 - ((skippedCurrent ? i - 1 : i) * 100));
					obj.gameObject.SetActive(true);
					objects.Add(obj);
				}

				StartCoroutine(FadeInDebugControls(objects));
			}

			StartCoroutine(FadeInCanvas());
		}));
	}

	public void PressPlay()
	{
		FadeManager._Instance.FadeInOut(1, 1, () => SceneManager.LoadScene("scn_demo_level"));
	}

	public void PressExit()
	{
		Application.Quit();
		Debug.Break();
	}

	public IEnumerator FadeInDebugControls(List<Transform> objects)
	{
		List<Image> imageComponents = new List<Image>();
		List<Text> textComponents = new List<Text>();
		for (int i = 0; i < objects.Count; i++)
		{
			imageComponents.Add(objects[i].GetComponent<Image>());
			textComponents.Add(objects[i].GetComponentInChildren<Text>());
		}

		float t = 0;
		float time = 0.5f;
		while (t <= time)
		{
			t += Time.deltaTime;

			foreach (Image item in imageComponents)
			{
				item.color = Color.Lerp(Color.clear, Color.white, t / time);
			}

			foreach (Text item in textComponents)
			{
				item.color = Color.Lerp(Color.clear, Color.white, t / time);
			}

			yield return null;
		}

		// Reparent because we want it to overlay everything
		yield return null;
	}
}
