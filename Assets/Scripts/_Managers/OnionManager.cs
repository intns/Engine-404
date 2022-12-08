using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class OnionManager
{
	public static List<Onion> _OnionsInScene = new();

	public static bool IsAnyOnionActiveInScene => _OnionsInScene.Any(onion => onion.OnionActive);
	public static Onion GetOnionOfColour(PikminColour colour) => _OnionsInScene.FirstOrDefault(onion => onion.Colour == colour);

	public static class SaveData
	{
		public static bool IsOnionDiscovered(PikminColour colour) => PlayerPrefs.GetInt($"ONION_{colour}_DISCOVERED") != 0;
		public static void SetOnionDiscovered(PikminColour colour, bool isDiscovered) => PlayerPrefs.SetInt($"ONION_{colour}_DISCOVERED", isDiscovered ? 1 : 0);

		public static int AmountInsideOnion(PikminColour colour) => PikminStatsManager.GetTotalInOnion(colour);
	}
}
