public enum PauseType
{
	Paused,           // A 'full' pause
	OnlyPikminActive, // Not fully paused because Pikmin AI can still work
	Unpaused,         // Active game, no pause applied
}

public static class GameManager
{
	static PauseType _pauseType = PauseType.Unpaused;

	public static bool IsPaused => PauseType != PauseType.Unpaused;

	public static PauseType PauseType
	{
		get => _pauseType;
		set
		{
			OnPauseEvent?.Invoke(value);

			_pauseType = value;
		}
	}

	#region Subscriber Events

	/// Callbacks
	public delegate void PauseEvent(PauseType t);

	public static event PauseEvent OnPauseEvent;

	#endregion
}
