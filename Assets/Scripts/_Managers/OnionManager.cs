using System.Collections.Generic;
using System.Linq;

public static class OnionManager
{
	public static List<Onion> _OnionsInScene = new();

	public static bool IsAnyOnionActiveInScene()
	{
		return _OnionsInScene.Any(onion => onion.OnionActive);
	}

	public static Onion GetOnionOfColour(PikminColour colour)
	{
		return _OnionsInScene.FirstOrDefault(onion => onion.OnionColour == colour);
	}
}
