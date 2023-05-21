using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace Demo
{
	public class DemoController : MonoBehaviour
	{
		[SerializeField] bool _ClearSave;
		[Space]
		[SerializeField] Volume _MainVP;
		[SerializeField] DayTimeManager _DayTimeManager;
		[SerializeField] string _MapName;

		[Header("Fade-in sequence")]
		[SerializeField] CanvasGroup _CanvasGroup;
		[SerializeField] Image _BlackImage;
		[SerializeField] TextMeshProUGUI _Text;

		[Header("Intro Sequence")]
		[SerializeField] bool _DoSequence;
		[SerializeField] Animator _CeresAnimator;

		bool _IsTutorialDay;

		void Awake()
		{
			_ClearSave = false;

			SaveData.LoadData();

			_Text.text = "";

			if (_MainVP.profile.TryGet(out Fog fog))
			{
				bool editFog = Random.Range(0, 5) < 2.5f;

				// We're going to randomise the VP a bit
				if (editFog)
				{
					fog.meanFreePath.value = Random.Range(50.0f, 250.0f);
					fog.baseHeight.value = Random.Range(-1.0f, 13.0f);
					fog.albedo.value = Color.Lerp(new(1, 0.95f, 0.815f), new(1, 0.578f, 0.306f), Random.Range(0.2f, 0.8f));
				}
				else
				{
					fog.enabled.value = false;
				}
			}

			if (SaveData._CurrentData._Day == 1)
			{
				_IsTutorialDay = true;
			}
			else
			{
				_DoSequence = true;
			}
		}

		void Start()
		{
			if (_DoSequence)
			{
				Player._Instance.Pause(PauseType.Paused, false);
				Player._Instance._UIController.FadeOutUI(0.0f);
				Player._Instance.SetModelVisibility(false);

				StartCoroutine(IE_StartScene());
			}
			else if (_IsTutorialDay)
			{
				Player._Instance.Pause(PauseType.Paused, false);
				Player._Instance._UIController.FadeOutUI(0.0f);

				foreach (Transform t in GenerationManager._Instance.transform)
				{
					ANIM_ImpactSiteOlimarDazed a = t.GetComponentInChildren<ANIM_ImpactSiteOlimarDazed>();

					if (a == null)
					{
						continue;
					}

					a.StartAnimation();
					break;
				}
			}
			else
			{
				Player._Instance.Pause(PauseType.Unpaused);
			}
		}

		void OnGUI()
		{
			if (!SaveData._CurrentData._IsDebug)
			{
				return;
			}

			GUIStyle style = new(GUI.skin.label);
			style.normal.textColor = Color.white;
			style.fontSize = 20;
			style.border = new(2, 2, 2, 2);
			style.padding = new(5, 5, 5, 5);

			Vector2 position = new(25, 25);

			StringBuilder debugTextBuilder = new();

			debugTextBuilder.AppendLine($"Save File:\t{SaveData._SaveFile}");
			debugTextBuilder.AppendLine($"Day: {SaveData._CurrentData._Day}");

			debugTextBuilder.AppendLine();
			debugTextBuilder.AppendLine("Ship Part Data:");

			foreach (KeyValuePair<ShipPartType, ShipPartData> kvp in SaveData._CurrentData._ShipPartData)
			{
				debugTextBuilder.AppendLine($"- {kvp.Key}, isCollected({kvp.Value._Collected}), isDiscovered({kvp.Value._Discovered})");
			}

			debugTextBuilder.AppendLine();
			debugTextBuilder.AppendLine("In Onion Pikmin:");

			foreach (KeyValuePair<PikminColour, PikminTypeStats> kvp in SaveData._CurrentData._InOnionPikmin)
			{
				debugTextBuilder.AppendLine(kvp.Value.ToString());
			}

			debugTextBuilder.AppendLine();
			debugTextBuilder.AppendLine("Discovered Onions:");

			foreach (KeyValuePair<PikminColour, bool> kvp in SaveData._CurrentData._DiscoveredOnions)
			{
				debugTextBuilder.AppendLine($"- Onion Colour: {kvp.Key}, Discovered: {kvp.Value}");
			}

			GUIContent guiContent = new(debugTextBuilder.ToString());

			GUI.Label(new(position.x, position.y, Screen.width, Screen.height), guiContent, style);
		}

		void OnDrawGizmosSelected()
		{
			if (_ClearSave)
			{
				SaveData.ResetData();
				// _ClearSave = false;
			}
		}

		IEnumerator IE_DoAnimation()
		{
			Ship._Instance.SetEngineFlamesVFX(true);
			_CeresAnimator.SetTrigger("DEMO_INTRO");
			Transform main = Camera.main.transform;

			CameraFollow cameraFollow = main.GetComponent<CameraFollow>();
			cameraFollow.enabled = false;
			Transform shipTransform = _CeresAnimator.transform;

			main.position = shipTransform.position + Vector3.up * 15 + Vector3.back * 20;

			Vector3 position = Vector3.Lerp(
				main.position,
				shipTransform.position + Vector3.up * 20 + Vector3.forward * 35,
				5 * Time.deltaTime
			);

			float t = 0;
			const float length = 10;

			while (t <= length)
			{
				if (t >= 8.0f)
				{
					Ship._Instance.SetEngineFlamesVFX(false);
				}

				// Rotate the camera to look at the Player
				position = Vector3.Lerp(
					position,
					shipTransform.position + Vector3.up * 20 + Vector3.forward * 45,
					5 * Time.deltaTime
				);

				Quaternion rotation = Quaternion.Lerp(
					main.rotation,
					Quaternion.LookRotation(MathUtil.DirectionFromTo(main.position, shipTransform.position + Vector3.up * 5, true)),
					10 * Time.deltaTime
				);

				main.SetPositionAndRotation(position, rotation);

				t += Time.deltaTime;
				yield return null;
			}

			cameraFollow.enabled = true;
		}

		IEnumerator IE_StartScene()
		{
			_BlackImage.enabled = true;

			yield return new WaitForSeconds(1.5f);

			_Text.color = Color.clear;
			_Text.text = _MapName;

			float t = 0;

			while (t <= 2)
			{
				t += Time.deltaTime;
				_Text.color = Color.Lerp(Color.clear, Color.white, MathUtil.EaseIn3(t / 2.0f));
				yield return null;
			}

			yield return new WaitForSeconds(1.25f);

			StartCoroutine(IE_DoAnimation());

			t = 0;

			while (t <= 2)
			{
				t += Time.deltaTime;
				_Text.color = Color.Lerp(Color.white, Color.clear, MathUtil.EaseIn2(t / 2.0f));
				yield return null;
			}

			t = 0;

			while (t <= 1.25f)
			{
				t += Time.deltaTime;
				_CanvasGroup.alpha = 1 - MathUtil.EaseIn3(t / 1.25f);
				yield return null;
			}

			_Text.enabled = false;
			_BlackImage.enabled = false;
			_CanvasGroup.alpha = 1;

			yield return new WaitForSecondsRealtime(4.5f);

			// Update UI before the fade in occurs, due to animation
			Player._Instance._UIController.UpdateFullUI();

			FadeManager._Instance.FadeInOut(
				2.0f,
				2.0f,
				() =>
				{
					Player._Instance.SetModelVisibility(true);
					Player._Instance.Pause(PauseType.Unpaused);
					_DayTimeManager.enabled = true;
				}
			);
		}
	}
}
