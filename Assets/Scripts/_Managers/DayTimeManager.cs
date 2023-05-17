using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

[Serializable]
public class DTM_Audio_Event
{
	public int _SecondsTillExecution;
	public AudioClip _ToPlay;

	public bool Check(double time)
	{
		if (time >= _SecondsTillExecution)
		{
			return true;
		}

		return false;
	}
}

/// <summary>
///   TODO: This isn't a manager, this is a day-time controller
///   TODO: Implement days as savedata in the real daytime manager
/// </summary>
public class DayTimeManager : MonoBehaviour
{
	public static DayTimeManager _Instance;
	[Header("Components")]
	[SerializeField] Transform _SunLight;

	[Header("Settings")]
	[Tooltip("Time it takes from day start to day end (in seconds)")]
	[SerializeField] float _TotalDayTime = 1000;

	[SerializeField] AnimationCurve _ShipLiftCurve;

	[Header("Sun Rotation")]
	[SerializeField] Vector3 _FromRotVec = Vector3.zero;
	[SerializeField] Vector3 _ToRotVec = Vector3.zero;

	[Header("Events")]
	[SerializeField] List<DTM_Audio_Event> _AudioEvents = new();
	Quaternion _FromRot = Quaternion.identity;
	AudioSource _Source;
	bool _StartEndDay;
	Stopwatch _TimeElapsed = new();
	Quaternion _ToRot = Quaternion.identity;

	void Awake()
	{
		_Source = GetComponent<AudioSource>();

		_FromRot = Quaternion.Euler(_FromRotVec);
		_ToRot = Quaternion.Euler(_ToRotVec);
		transform.rotation = _FromRot;
	}

	void Start()
	{
		_TimeElapsed.Start();
	}

	void Update()
	{
		if (_StartEndDay)
		{
			return;
		}

		double seconds = _TimeElapsed.Elapsed.TotalSeconds;

		for (int i = 0; i < _AudioEvents.Count; i++)
		{
			if (_AudioEvents[i].Check(seconds))
			{
				_Source.PlayOneShot(_AudioEvents[i]._ToPlay);
				_AudioEvents.RemoveAt(i);
			}
		}

		_SunLight.rotation = Quaternion.Slerp(_FromRot, _ToRot, (float)(seconds / _TotalDayTime));

		if (seconds >= _TotalDayTime && !_StartEndDay)
		{
			_StartEndDay = true;
			FinishDay();
		}
	}

	void OnEnable()
	{
		_Instance = this;
	}

	public void FinishDay()
	{
		StartCoroutine(IE_EODSequence());
	}

	IEnumerator IE_EODSequence()
	{
		yield return null;

		Player._Instance.Pause(PauseType.Paused);

		Ship._Instance?.SetEngineFlamesVFX(true);
		Transform shipTransform = Ship._Instance._Transform;

		// So the animator can't set the transform of the object
		shipTransform.GetComponent<Animator>().enabled = false;

		Vector3 fixedPosOffset = Vector3.forward * 30 + Vector3.up * 17.5f;

		Vector3 shipStartPosition = shipTransform.position;
		Vector3 shipEndPosition = shipTransform.position + Vector3.up * 150;

		Transform mcTransform = Camera.main.transform;

		FadeManager._Instance.FadeInOut(
			1.5f,
			1,
			() =>
			{
				mcTransform.GetComponent<CameraFollow>().enabled = false;
				mcTransform.position = shipTransform.position + fixedPosOffset;
				mcTransform.LookAt(shipTransform);
			}
		);

		yield return new WaitForSeconds(2.5f);

		float timer = 10;
		float t = 0;
		bool started = false;

		while (t < timer)
		{
			shipTransform.position = Vector3.Lerp(
				shipStartPosition,
				shipEndPosition,
				_ShipLiftCurve.Evaluate(t / timer) * 35.0f * Time.fixedDeltaTime
			);

			mcTransform.position = Vector3.Lerp(
				mcTransform.position,
				shipTransform.position + fixedPosOffset,
				2.5f * Time.fixedDeltaTime
			);

			if (started == false && t > 7.5f)
			{
				FadeManager._Instance.FadeOut(
					2.5f,
					() =>
					{
						PikminStatsManager.ClearSquad();
						PikminStatsManager.ClearStats();

						SceneManager.LoadScene(0);
						Debug.Log("End of Day, fadeout done");
					}
				);
				started = true;
			}

			t += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
	}
}
