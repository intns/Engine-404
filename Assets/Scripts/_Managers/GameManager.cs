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
	Full,
	OnlyPikminActive,
}

public static class GameManager
{
	public static bool _IsPaused = false; // Used in checks to see if the game is paused
	public static PauseType _PauseType = PauseType.Full;

	public static bool _DebugGui = Application.isEditor; // Used for debugging

	public static Language _Language = Language.English; // Used for alternate texts
}
