using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class DemoController : MonoBehaviour
{
	[SerializeField] private Volume _MainVP;
	[SerializeField] private DayTimeManager _DayTimeManager;

	private void Awake()
	{
		_MainVP.profile.TryGet(out Fog fog);

		bool editFog = Random.Range(0, 5) < 2.5f;
		// We're going to randomise the VP a bit
		if (editFog)
		{
			fog.meanFreePath.value = Random.Range(50.0f, 250.0f);
			fog.baseHeight.value = Random.Range(-1.0f, 13.0f);
			fog.albedo.value = Color.Lerp(new Color(1, 0.95f, 0.815f), new Color(1, 0.578f, 0.306f), Random.Range(0.2f, 0.8f));
		}
		else
		{
			fog.enabled.value = false;
		}

		GameManager._IsPaused = true;
		_DayTimeManager.enabled = false;
	}

	private void Start()
	{
		FadeManager._Instance.FadeIn(1.5f, () =>
		{
			GameManager._IsPaused = false;
			_DayTimeManager.enabled = true;
		});
	}
}
