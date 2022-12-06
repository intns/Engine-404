/*
 * GameManager.cs
 * Created by: Ambrosia
 * Created on: 27/10/2020 (dd/mm/yy)
 */

using UnityEngine;

public enum Language
{
	English,
	French,
}

public enum PauseType
{
	Paused,            // A 'full' pause
	OnlyPikminActive, // Not fully paused because Pikmin AI can still work
	Unpaused,          // Active game, no pause applied
}

public static class GameManager
{
	public static bool IsPaused { get => PauseType != PauseType.Unpaused; }
	public static PauseType PauseType
	{
		get
		{
			return _pauseType;
		}
		set
		{
			OnPauseEvent?.Invoke(value);

			_pauseType = value;
		}
	}
	private static PauseType _pauseType = PauseType.Unpaused;

	public static bool _DebugGui = Application.isEditor; // Used for debugging
	public static Language _Language = Language.English; // Used for alternate texts

	#region Subscriber Events
	/// Callbacks
	public delegate void PauseEvent(PauseType t);
	public static event PauseEvent OnPauseEvent;
	#endregion
}
