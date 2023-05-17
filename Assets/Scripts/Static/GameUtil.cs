using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GameUtil
{
	static readonly Dictionary<PikminColour, Color> _PikminColorMap = new() { { PikminColour.Red, Color.red }, { PikminColour.Yellow, Color.yellow }, { PikminColour.Blue, Color.blue } };

	/// <summary>
	///   Computes the majority colour in a list of Pikmin, if they all match, defaults to first pikmin added
	/// </summary>
	/// <param name="pikmin">List of Pikmin to check</param>
	/// <returns>The majority colour of the Pikmin in the list</returns>
	public static PikminColour GetMajorityColour(List<PikminAI> pikmin)
	{
		/*
		 * TODO: See if this is slower than the implementation below, even though it's completely future-proof
		 * Dictionary<PikminColour, int> colorCounts = Enum.GetValues(typeof(PikminColour)).Cast<PikminColour>().ToDictionary(color => color, color => 0);
		 */
		var colorCounts = new Dictionary<PikminColour, int> { { PikminColour.Red, 0 }, { PikminColour.Yellow, 0 }, { PikminColour.Blue, 0 } };

		foreach (PikminAI p in pikmin)
		{
			if (p == null)
			{
				continue;
			}

			colorCounts[p._Data._PikminColour]++;
		}

		return colorCounts.OrderByDescending(kvp => kvp.Value)
		                  .Select(kvp => kvp.Key)
		                  .DefaultIfEmpty(pikmin[0]._Data._PikminColour)
		                  .First();
	}


	public static Color PikminColorToColor(PikminColour col)
	{
		if (_PikminColorMap.TryGetValue(col, out Color color))
		{
			return color;
		}

		return Color.white;
	}
}
