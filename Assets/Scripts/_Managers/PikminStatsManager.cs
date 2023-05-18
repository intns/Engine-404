using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public enum PikminStatSpecifier
{
	InSquad = 0,
	OnField,
	InOnion,
}

// Specific information about a maturity of Pikmin
[Serializable]
public class PikminMaturityStats
{
	public PikminMaturity _Maturity;

	public int _InSquad;
	public int _OnField;
	public int _InOnion;

	public PikminMaturityStats(PikminMaturity maturity)
	{
		_Maturity = maturity;
	}

	public void AddTo(PikminStatSpecifier specifier)
	{
		switch (specifier)
		{
			case PikminStatSpecifier.InSquad:
				_InSquad++;
				break;
			case PikminStatSpecifier.OnField:
				_OnField++;
				_OnField = Mathf.Min(PikminStatsManager._MaxPikminOnField, _OnField);
				break;
			case PikminStatSpecifier.InOnion:
				_InOnion++;
				break;
		}
	}

	// Prints out the information relevant to the stats of the Pikmin
	public void Print()
	{
		Debug.Log($"{_Maturity}\tInSquad: {_InSquad}, OnField: {_OnField}, InOnion: {_InOnion}");
	}

	public void RemoveFrom(PikminStatSpecifier specifier)
	{
		switch (specifier)
		{
			case PikminStatSpecifier.InSquad:
				_InSquad--;
				_InSquad = Mathf.Max(0, _InSquad);
				break;
			case PikminStatSpecifier.OnField:
				_OnField--;
				_OnField = Mathf.Max(0, _OnField);
				break;
			case PikminStatSpecifier.InOnion:
				_InOnion--;
				_InOnion = Mathf.Max(0, _InOnion);
				break;
			default: throw new ArgumentOutOfRangeException(nameof(specifier), specifier, null);
		}
	}

	public override string ToString()
	{
		return $"{_Maturity}\tInSquad: {_InSquad}, OnField: {_OnField}, InOnion: {_InOnion}\n";
	}
}

// Specific information about the type of Pikmin (Colour, and maturity)
[Serializable]
public class PikminTypeStats
{
	public PikminColour _Colour;

	public PikminMaturityStats _Leaf = new(PikminMaturity.Leaf);
	public PikminMaturityStats _Bud = new(PikminMaturity.Bud);
	public PikminMaturityStats _Flower = new(PikminMaturity.Flower);


	public PikminTypeStats(PikminColour colour)
	{
		_Colour = colour;
	}

	// Adds a Pikmin to their specified matury level stats
	public void AddTo(PikminMaturity maturity, PikminStatSpecifier specifier)
	{
		switch (maturity)
		{
			case PikminMaturity.Leaf:
				_Leaf.AddTo(specifier);
				break;
			case PikminMaturity.Bud:
				_Bud.AddTo(specifier);
				break;
			case PikminMaturity.Flower:
				_Flower.AddTo(specifier);
				break;
		}
	}


	public int GetTotalInOnion()
	{
		return _Leaf._InOnion + _Bud._InOnion + _Flower._InOnion;
	}


	public int GetTotalInSquad()
	{
		return _Leaf._InSquad + _Bud._InSquad + _Flower._InSquad;
	}


	public int GetTotalOnField()
	{
		return _Leaf._OnField + _Bud._OnField + _Flower._OnField;
	}

	// Prints out the information relevant to the stats of the Pikmin
	public void Print()
	{
		Debug.Log($"\tCOLOUR\t{_Colour}");
		_Leaf.Print();
		_Bud.Print();
		_Flower.Print();
	}

	// Removes a Pikmin from their specified maturity level stats
	public void RemoveFrom(PikminMaturity maturity, PikminStatSpecifier specifier)
	{
		switch (maturity)
		{
			case PikminMaturity.Leaf:
				_Leaf.RemoveFrom(specifier);
				break;
			case PikminMaturity.Bud:
				_Bud.RemoveFrom(specifier);
				break;
			case PikminMaturity.Flower:
				_Flower.RemoveFrom(specifier);
				break;
		}
	}

	public override string ToString()
	{
		string str = $"\tCOLOUR\t{_Colour}\n";
		str += _Leaf.ToString();
		str += _Bud.ToString();
		str += _Flower.ToString();
		return str;
	}
}

public static class PikminStatsManager
{
	// Stores specific stats of each colour
	public const int _MaxPikminOnField = 100;

	public static Dictionary<PikminColour, PikminTypeStats> _TypeStats = new();

	public static List<PikminAI> _InSquad = new();
	public static bool _IsDisbanding = false;

	static PikminStatsManager()
	{
		foreach (PikminColour colour in Enum.GetValues(typeof(PikminColour)))
		{
			_TypeStats[colour] = new(colour);
		}
	}

	/// <summary>
	///   Adds a Pikmin to the corresponding PikminTypeStats based on the specified color, maturity, and specifier.
	/// </summary>
	/// <param name="colour">The color of the Pikmin to add.</param>
	/// <param name="maturity">The maturity level of the Pikmin to add.</param>
	/// <param name="specifier">The stat specifier indicating the context of the addition.</param>
	public static void Add(PikminColour colour, PikminMaturity maturity, PikminStatSpecifier specifier)
	{
		GetPikminStats(colour).AddTo(maturity, specifier);
	}

	/// <summary>
	///   Adds a Pikmin to the squad and updates the corresponding stats.
	/// </summary>
	/// <param name="pikmin">The Pikmin to add to the squad.</param>
	/// <param name="colour">The color of the Pikmin to add.</param>
	/// <param name="maturity">The maturity level of the Pikmin to add.</param>
	public static void AddToSquad(PikminAI pikmin, PikminColour colour, PikminMaturity maturity)
	{
		_InSquad.Add(pikmin);
		Add(colour, maturity, PikminStatSpecifier.InSquad);
	}

	/// <summary>
	///   Clears the squad by removing all Pikmin from it.
	/// </summary>
	public static void ClearSquad()
	{
		while (_InSquad.Count > 0)
		{
			_InSquad[0].RemoveFromSquad();
		}
	}

	/// <summary>
	///   Clears all the squad and on-field stats for each color and maturity level.
	/// </summary>
	public static void ClearStats()
	{
		for (int i = 0; i < _MaxPikminOnField; i++)
		{
			foreach (PikminColour colour in Enum.GetValues(typeof(PikminColour)))
			{
				foreach (PikminMaturity maturity in Enum.GetValues(typeof(PikminMaturity)))
				{
					Remove(colour, maturity, PikminStatSpecifier.InSquad);
					Remove(colour, maturity, PikminStatSpecifier.OnField);
				}
			}
		}
	}

	/// <summary>
	///   Prints the relevant information for the Pikmin stats, including the number of Pikmin in the squad.
	/// </summary>
	public static void Print()
	{
		Debug.Log($"Number of Pikmin in squad: {_InSquad.Count}");

		foreach (KeyValuePair<PikminColour, PikminTypeStats> kvp in _TypeStats)
		{
			kvp.Value.Print();
		}
	}

	/// <summary>
	///   Sets up formations for the Pikmin to use based on the current squad.
	/// </summary>
	public static void ReassignFormation()
	{
		if (_IsDisbanding)
		{
			return;
		}

		for (int i = 0; i < _InSquad.Count; i++)
		{
			_InSquad[i]._FormationPosition.position = Player._Instance._PikminController.GetPositionAt(i);
		}
	}

	/// <summary>
	///   Removes a Pikmin from the corresponding PikminTypeStats based on the specified color, maturity, and specifier.
	/// </summary>
	/// <param name="colour">The color of the Pikmin to remove.</param>
	/// <param name="maturity">The maturity level of the Pikmin to remove.</param>
	/// <param name="specifier">The stat specifier indicating the context of the removal.</param>
	public static void Remove(PikminColour colour, PikminMaturity maturity, PikminStatSpecifier specifier)
	{
		GetPikminStats(colour).RemoveFrom(maturity, specifier);
	}

	// Removes a Pikmin from the squad, and handles decrementing the stats

	public static void RemoveFromSquad(PikminAI pikmin, PikminColour colour, PikminMaturity maturity)
	{
		_InSquad.Remove(pikmin);
		Remove(colour, maturity, PikminStatSpecifier.InSquad);
	}

	#region Getters

	/// <summary>
	///   Retrieves the PikminTypeStats for the specified color.
	/// </summary>
	/// <param name="colour">The color of the Pikmin to retrieve stats for.</param>
	/// <returns>The PikminTypeStats for the specified color.</returns>
	public static PikminTypeStats GetPikminStats(PikminColour colour)
	{
		if (_TypeStats.TryGetValue(colour, out PikminTypeStats stats))
		{
			return stats;
		}

		// Handle invalid Pikmin color
		throw new ArgumentException("Invalid Pikmin color");
	}

	/// <summary>
	///   Gets the total number of Pikmin of the specified color currently on the field.
	/// </summary>
	/// <param name="colour">The color of the Pikmin.</param>
	/// <returns>The total number of Pikmin of the specified color on the field.</returns>
	public static int GetTotalPikminOnField(PikminColour colour)
	{
		return GetPikminStats(colour).GetTotalOnField();
	}

	/// <summary>
	///   Gets the total number of Pikmin of the specified color currently in the squad.
	/// </summary>
	/// <param name="colour">The color of the Pikmin.</param>
	/// <returns>The total number of Pikmin of the specified color in the squad.</returns>
	public static int GetTotalPikminInSquad(PikminColour colour)
	{
		return GetPikminStats(colour).GetTotalInSquad();
	}

	/// <summary>
	///   Gets the total number of Pikmin of the specified color currently in the Onion.
	/// </summary>
	/// <param name="colour">The color of the Pikmin.</param>
	/// <returns>The total number of Pikmin of the specified color in the Onion.</returns>
	public static int GetTotalPikminInOnion(PikminColour colour)
	{
		return GetPikminStats(colour).GetTotalInOnion();
	}

	/// <summary>
	///   Gets the total number of Pikmin currently in the squad across all colors.
	/// </summary>
	/// <returns>The total number of Pikmin in the squad.</returns>
	public static int GetTotalPikminInSquad()
	{
		return _TypeStats.Values.Sum(stats => stats.GetTotalInSquad());
	}

	/// <summary>
	///   Gets the total number of Pikmin currently on the field across all colors.
	/// </summary>
	/// <returns>The total number of Pikmin on the field.</returns>
	public static int GetTotalPikminOnField()
	{
		return _TypeStats.Values.Sum(stats => stats.GetTotalOnField());
	}

	/// <summary>
	///   Gets the total number of Pikmin currently in all Onions across all colors.
	/// </summary>
	/// <returns>The total number of Pikmin in all Onions.</returns>
	public static int GetTotalPikminInAllOnions()
	{
		return _TypeStats.Values.Sum(stats => stats.GetTotalInOnion());
	}

	#endregion
}
