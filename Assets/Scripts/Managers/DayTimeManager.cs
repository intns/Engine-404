using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

[Serializable]
public class DTM_Audio_Event
{
	public int _SecondsTillExecution = 0;
	public AudioClip _ToPlay = null;

	public bool Check(double time)
	{
		if (time >= _SecondsTillExecution)
		{
			return true;
		}

		return false;
	}
}

public class DayTimeManager : MonoBehaviour
{
	[Header("Components")]
	public Transform _SunLight = null;

	[Header("Settings")]
	[Tooltip("Time it takes from day start to day end (in seconds)")]
	[SerializeField] private float _TotalDayTime = 1000;

	[Header("Sun Rotation")]
	[SerializeField] private Vector3 _FromRotVec = Vector3.zero;
	[SerializeField] private Vector3 _ToRotVec = Vector3.zero;
	private Quaternion _FromRot = Quaternion.identity;
	private Quaternion _ToRot = Quaternion.identity;

	[Header("Events")]
	[SerializeField] private List<DTM_Audio_Event> _AudioEvents = new List<DTM_Audio_Event>();
	private readonly Stopwatch _TimeElapsed = new Stopwatch();
	private AudioSource _Source = null;

	private void Awake()
	{
		_Source = GetComponent<AudioSource>();

		_FromRot = Quaternion.Euler(_FromRotVec);
		_ToRot = Quaternion.Euler(_ToRotVec);
		transform.rotation = _FromRot;
	}

	private void Start()
	{
		_TimeElapsed.Start();
	}

	private void Update()
	{
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

		if (seconds >= _TotalDayTime)
		{
			// End of Day
			Globals._FadeManager.FadeOut(2.5f, new Action(EndOfDayFadeoutAction));
		}
	}

	private static void EndOfDayFadeoutAction()
	{
		SceneManager.LoadScene(0);
		Debug.Log("End of Day, fadeout done");
	}
}
