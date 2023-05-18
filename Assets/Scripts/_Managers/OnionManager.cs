using System.Collections.Generic;
using System.Linq;

public static class OnionManager
{
	public static List<Onion> _OnionsInScene = new();

	public static bool IsAnyOnionActiveInScene => _OnionsInScene.Any(onion => onion.OnionActive);

	public static Onion GetOnionOfColour(PikminColour colour)
	{
		Onion o = _OnionsInScene.FirstOrDefault(onion => onion.Colour == colour);

		return o != null ? o : _OnionsInScene.FirstOrDefault(x => x.OnionActive);
	}
}
