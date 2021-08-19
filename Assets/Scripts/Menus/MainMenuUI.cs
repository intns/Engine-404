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

public class MainMenuUI : BaseUIController
{
	[Header("Components")]
	[SerializeField] private Transform _Canvas = null;
	[SerializeField] private Transform _TemplateButton = null;

	private void Awake()
	{
		Globals._FadeManager.FadeIn(1, new Action(() =>
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
					obj.GetComponent<Button>().onClick.AddListener(() => Globals._FadeManager.FadeInOut(1, 1, () => SceneManager.LoadScene(sceneName)));
					obj.GetComponent<RectTransform>().localPosition = new Vector3(-450, 300 - ((skippedCurrent ? i - 1 : i) * 100));
					obj.gameObject.SetActive(true);
					objects.Add(obj);
				}

				StartCoroutine(FadeInDebugControls(objects));
			}
		}));
	}

	public void PressPlay()
	{
		Globals._FadeManager.FadeInOut(1, 1, () => SceneManager.LoadScene("scn_demo_level"));
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
		_FadePanel.transform.SetParent(null, false);
		_FadePanel.transform.SetParent(_Canvas, false);
		_FadePanel.enabled = false;
		yield return null;
	}
}
