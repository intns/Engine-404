using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class GameUtil
{
	/// <summary>
	/// Computes the majority colour in a list of Pikmin, if they all match, defaults to first pikmin added
	/// </summary>
	/// <param name="pikmin">List of Pikmin to check</param>
	/// <returns>The majority colour of the Pikmin in the list</returns>
	public static PikminColour GetMajorityColour(List<PikminAI> pikmin)
	{
		int red = 0;
		int ylw = 0;
		int blu = 0;

		foreach (PikminAI c in pikmin)
		{
			switch (c._Data._PikminColour)
			{
				case PikminColour.Red:
					red++;
					break;
				case PikminColour.Yellow:
					ylw++;
					break;
				case PikminColour.Blue:
					blu++;
					break;

				case PikminColour.Size:
				default:
					break;
			}
		}

		if (red > ylw && red > blu)
		{
			return PikminColour.Red;
		}
		else if (blu > red && blu > ylw)
		{
			return PikminColour.Blue;
		}
		else if (ylw > red && ylw > blu)
		{
			return PikminColour.Yellow;
		} 
		else if (pikmin.Count > 0) 
		{
			return pikmin[0]._Data._PikminColour;
        }

		return PikminColour.Red;
	}


	public static Color PikminColorToColor(PikminColour col)
	{
		switch (col)
		{
			case PikminColour.Red:
				return Color.red;
			case PikminColour.Yellow:
				return Color.yellow;
			case PikminColour.Blue:
				return Color.blue;
			case PikminColour.Size:
			default:
				return Color.white;
		}
	}
}