/*
 * ReplayAudioDelay.cs
 * Created by: Ambrosia
 * Created on: 14/3/2020 (dd/mm/yy)
 * Created for: needing to replay audio after a set amount of time
 */

using UnityEngine;

public class ReplayAudioDelay : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] private AudioSource _ToReplay = null;

	[Header("Settings")]
	[SerializeField] private float _Delay = 0;
	[SerializeField] private bool _UseRandomDelay = false;
	[SerializeField] private Vector2 _RandomDelayBounds = Vector2.one * 2;
	private float _CurrentDelay;
	private float _CurrentTime = 0;

	private void Awake()
	{
		// Set the current delay based on if we're using the random delay or not
		_CurrentDelay = _UseRandomDelay ? Random.Range(_RandomDelayBounds.x, _RandomDelayBounds.y) : _Delay;
	}

	private void Update()
	{
		// Increment the timer
		_CurrentTime += Time.deltaTime;
		if (_CurrentTime >= _CurrentDelay)
		{
			// Play the audio, and reset the timer
			_ToReplay.Play();
			_CurrentTime = 0;

			// Change the Delay based on if we're using the random delay
			if (_UseRandomDelay)
			{
				_CurrentDelay = Random.Range(_RandomDelayBounds.x, _RandomDelayBounds.y);
			}
		}
	}
}
