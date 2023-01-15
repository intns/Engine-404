/*
 * PikminStatsManager.cs
 * Created by: Ambrosia
 * Created on: 30/4/2020 (dd/mm/yy)
 */

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public enum PikminStatSpecifier
{
	InSquad = 0,
	OnField,
	InOnion
}

// Specific information about a maturity of Pikmin
public class PikminMaturityStats
{
	public PikminMaturity _Maturity;

	public int _InSquad = 0;
	public int _OnField = 0;
	public int _InOnion = 0;

	public int Total =>
		// OnField contains InSquad as well, so we don't need to add it here
		_OnField + _InOnion;

	public PikminMaturityStats(PikminMaturity maturity)
	{
		_Maturity = maturity;
	}

	// Prints out the information relevant to the stats of the Pikmin
	public void Print()
	{
		Debug.Log($"{_Maturity}\tInSquad: {_InSquad}, OnField: {_OnField}, InOnion: {_InOnion}");
	}

	public override string ToString()
	{
		return $"{_Maturity}\tInSquad: {_InSquad}, OnField: {_OnField}, InOnion: {_InOnion}\n";
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
				_OnField = Mathf.Min(PikminStatsManager._MaxOnField, _OnField);
				break;
			case PikminStatSpecifier.InOnion:
				_InOnion++;
				break;
			default:
				break;
		}
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
			default:
				break;
		}
	}
}

// Specific information about the type of Pikmin (Colour, and maturity)
public class PikminTypeStats
{
	public PikminColour _Colour;

	public PikminMaturityStats _Leaf = new(PikminMaturity.Leaf);
	public PikminMaturityStats _Bud = new(PikminMaturity.Bud);
	public PikminMaturityStats _Flower = new(PikminMaturity.Flower);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public PikminTypeStats(PikminColour colour)
	{
		_Colour = colour;
	}

	// Prints out the information relevant to the stats of the Pikmin
	public void Print()
	{
		Debug.Log($"\tCOLOUR\t{_Colour}");
		_Leaf.Print();
		_Bud.Print();
		_Flower.Print();
	}

	public override string ToString()
	{
		string str = $"\tCOLOUR\t{_Colour}\n";
		str += _Leaf.ToString();
		str += _Bud.ToString();
		str += _Flower.ToString();
		return str;
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
			default:
				break;
		}
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
			default:
				break;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetTotalInSquad()
	{
		return _Leaf._InSquad + _Bud._InSquad + _Flower._InSquad;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetTotalOnField()
	{
		return _Leaf._OnField + _Bud._OnField + _Flower._OnField;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetTotalInOnion()
	{
		return _Leaf._InOnion + _Bud._InOnion + _Flower._InOnion;
	}
}

public static class PikminStatsManager
{
	// Stores specific stats of each colour
	public const int _MaxOnField = 100;

	public static PikminTypeStats _RedStats = new(PikminColour.Red);
	public static PikminTypeStats _BlueStats = new(PikminColour.Blue);
	public static PikminTypeStats _YellowStats = new(PikminColour.Yellow);

	public static List<PikminAI> _InSquad = new();
	public static bool _IsDisbanding = false;

	// Clears the Squad
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ClearSquad()
	{
		while (_InSquad.Count > 0)
		{
			_InSquad[0].RemoveFromSquad();
		}
	}

	/// <summary>
	/// Clears every squad & on-field stat
	/// </summary>
	public static void ClearStats()
	{
		for (int i = 0; i < _MaxOnField; i++)
		{
			for (int j = 0; j < (int)PikminColour.Size; j++)
			{
				for (int k = 0; k < (int)PikminMaturity.Size; k++)
				{
					Remove((PikminColour)j, (PikminMaturity)k, PikminStatSpecifier.InSquad);
					Remove((PikminColour)j, (PikminMaturity)k, PikminStatSpecifier.OnField);
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static PikminTypeStats GetStats(PikminColour colour)
	{
		return colour switch
		{
			PikminColour.Red => _RedStats,
			PikminColour.Yellow => _YellowStats,
			PikminColour.Blue => _BlueStats,
			_ => default,
		};
	}

	// Adds a Pikmin to the squad, and handles adding to the stats
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AddToSquad(PikminAI pikmin, PikminColour colour, PikminMaturity maturity)
	{
		_InSquad.Add(pikmin);
		Add(colour, maturity, PikminStatSpecifier.InSquad);
	}

	// Removes a Pikmin from the squad, and handles decrementing the stats
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void RemoveFromSquad(PikminAI pikmin, PikminColour colour, PikminMaturity maturity)
	{
		_InSquad.Remove(pikmin);
		Remove(colour, maturity, PikminStatSpecifier.InSquad);
	}

	// Adds a Pikmin to the stats
	public static void Add(PikminColour colour, PikminMaturity maturity, PikminStatSpecifier specifier)
	{
		switch (colour)
		{
			case PikminColour.Red:
				_RedStats.AddTo(maturity, specifier);
				break;
			case PikminColour.Yellow:
				_YellowStats.AddTo(maturity, specifier);
				break;
			case PikminColour.Blue:
				_BlueStats.AddTo(maturity, specifier);
				break;
			default:
				break;
		}
	}

	// Removes a Pikmin from the stats
	public static void Remove(PikminColour colour, PikminMaturity maturity, PikminStatSpecifier specifier)
	{
		switch (colour)
		{
			case PikminColour.Red:
				_RedStats.RemoveFrom(maturity, specifier);
				break;
			case PikminColour.Yellow:
				_YellowStats.RemoveFrom(maturity, specifier);
				break;
			case PikminColour.Blue:
				_BlueStats.RemoveFrom(maturity, specifier);
				break;
			default:
				break;
		}
	}

	// Prints out the information relevant for the stats of the Pikmin
	public static void Print()
	{
		Debug.Log($"Length of the 'InSquad' list: {_InSquad.Count}");
		_RedStats.Print();
		_BlueStats.Print();
		_YellowStats.Print();
	}

	//Sets up formations for the pikmin to use
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

	#region Getters
	public static int GetTotalOnField(PikminColour colour)
	{
		return colour switch
		{
			PikminColour.Red => _RedStats.GetTotalOnField(),
			PikminColour.Yellow => _YellowStats.GetTotalOnField(),
			PikminColour.Blue => _BlueStats.GetTotalOnField(),
			_ => 0,
		};
	}
	public static int GetTotalInSquad(PikminColour colour)
	{
		return colour switch
		{
			PikminColour.Red => _RedStats.GetTotalInSquad(),
			PikminColour.Yellow => _YellowStats.GetTotalInSquad(),
			PikminColour.Blue => _BlueStats.GetTotalInSquad(),
			_ => 0,
		};
	}

	public static int GetTotalInOnion(PikminColour colour)
	{
		return colour switch
		{
			PikminColour.Red => _RedStats.GetTotalInOnion(),
			PikminColour.Yellow => _YellowStats.GetTotalInOnion(),
			PikminColour.Blue => _BlueStats.GetTotalInOnion(),
			_ => 0,
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetTotalInSquad()
	{
		return _RedStats.GetTotalInSquad() + _YellowStats.GetTotalInSquad() + _BlueStats.GetTotalInSquad();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetTotalOnField()
	{
		return _RedStats.GetTotalOnField() + _YellowStats.GetTotalOnField() + _BlueStats.GetTotalOnField();
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetTotalInAllOnions()
	{
		return _RedStats.GetTotalInOnion() + _YellowStats.GetTotalInOnion() + _BlueStats.GetTotalInOnion();
	}
	#endregion
}
