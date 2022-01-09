/*
 * PikminStatsManager.cs
 * Created by: Ambrosia
 * Created on: 30/4/2020 (dd/mm/yy)
 */

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public enum PikminStatSpecifier {
  InSquad = 0,
  OnField,
  InOnion
}

// Specific information about a maturity of Pikmin
public class PikminMaturityStats {
  public PikminMaturity _Maturity;

  public int _InSquad = 0;
  public int _OnField = 0;
  public int _InOnion = 0;

  public int Total =>
    // OnField contains InSquad as well, so we don't need to add it here
    _OnField + _InOnion;

  public PikminMaturityStats (PikminMaturity maturity) {
    _Maturity = maturity;
  }

  // Prints out the information relevant to the stats of the Pikmin
  public void Print () {
    Debug.Log ($"{_Maturity.ToString()}\tInSquad: {_InSquad}, OnField: {_OnField}, InOnion: {_InOnion}");
  }

  public override string ToString () {
    return $"{_Maturity.ToString()}\tInSquad: {_InSquad}, OnField: {_OnField}, InOnion: {_InOnion}\n";
  }

  public void AddTo (PikminStatSpecifier specifier) {
    switch (specifier) {
      case PikminStatSpecifier.InSquad:
        _InSquad++;
        break;
      case PikminStatSpecifier.OnField:
        _OnField++;
        break;
      case PikminStatSpecifier.InOnion:
        _InOnion++;
        break;
      default:
        break;
    }
  }

  public void RemoveFrom (PikminStatSpecifier specifier) {
    switch (specifier) {
      case PikminStatSpecifier.InSquad:
        _InSquad--;
        break;
      case PikminStatSpecifier.OnField:
        _OnField--;
        break;
      case PikminStatSpecifier.InOnion:
        _InOnion--;
        break;
      default:
        break;
    }
  }
}

// Specific information about the type of Pikmin (Colour, and maturity)
public class PikminTypeStats {
  public PikminColour _Colour;

  public PikminMaturityStats _Leaf = new PikminMaturityStats (PikminMaturity.Leaf);
  public PikminMaturityStats _Bud = new PikminMaturityStats (PikminMaturity.Bud);
  public PikminMaturityStats _Flower = new PikminMaturityStats (PikminMaturity.Flower);

  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public PikminTypeStats (PikminColour colour) {
    _Colour = colour;
  }

  // Prints out the information relevant to the stats of the Pikmin
  public void Print () {
    Debug.Log ($"\tCOLOUR\t{_Colour.ToString()}");
    _Leaf.Print ();
    _Bud.Print ();
    _Flower.Print ();
  }

  public override string ToString () {
    string str = $"\tCOLOUR\t{ _Colour.ToString()}\n";
    str += _Leaf.ToString ();
    str += _Bud.ToString ();
    str += _Flower.ToString ();
    return str;
  }

  // Adds a Pikmin to their specified matury level stats
  public void AddTo (PikminMaturity maturity, PikminStatSpecifier specifier) {
    switch (maturity) {
      case PikminMaturity.Leaf:
        _Leaf.AddTo (specifier);
        break;
      case PikminMaturity.Bud:
        _Bud.AddTo (specifier);
        break;
      case PikminMaturity.Flower:
        _Flower.AddTo (specifier);
        break;
      default:
        break;
    }
  }

  // Removes a Pikmin from their specified maturity level stats
  public void RemoveFrom (PikminMaturity maturity, PikminStatSpecifier specifier) {
    switch (maturity) {
      case PikminMaturity.Leaf:
        _Leaf.RemoveFrom (specifier);
        break;
      case PikminMaturity.Bud:
        _Bud.RemoveFrom (specifier);
        break;
      case PikminMaturity.Flower:
        _Flower.RemoveFrom (specifier);
        break;
      default:
        break;
    }
  }

  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public int GetTotalInSquad () {
    return _Leaf._InSquad + _Bud._InSquad + _Flower._InSquad;
  }

  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public int GetTotalOnField () {
    return _Leaf._OnField + _Bud._OnField + _Flower._OnField;
  }

  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public int GetTotalInOnion () {
    return _Leaf._InOnion + _Bud._InOnion + _Flower._InOnion;
  }
}

public static class PikminStatsManager {
  // Stores specific stats of each colour
  public static PikminTypeStats _RedStats = new PikminTypeStats (PikminColour.Red);
  public static PikminTypeStats _BlueStats = new PikminTypeStats (PikminColour.Blue);
  public static PikminTypeStats _YellowStats = new PikminTypeStats (PikminColour.Yellow);

  public static List<PikminAI> _InSquad = new List<PikminAI> ();

  // Clears the Squad
  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public static void ClearSquad () {
    while (_InSquad.Count > 0) {
      _InSquad[0].RemoveFromSquad ();
    }
  }

  // Adds a Pikmin to the squad, and handles adding to the stats
  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public static void AddToSquad (PikminAI pikmin, PikminColour colour, PikminMaturity maturity) {
    _InSquad.Add (pikmin);
    Add (colour, maturity, PikminStatSpecifier.InSquad);
  }

  // Removes a Pikmin from the squad, and handles decrementing the stats
  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public static void RemoveFromSquad (PikminAI pikmin, PikminColour colour, PikminMaturity maturity) {
    _InSquad.Remove (pikmin);
    Remove (colour, maturity, PikminStatSpecifier.InSquad);
  }

  // Adds a Pikmin to the stats
  public static void Add (PikminColour colour, PikminMaturity maturity, PikminStatSpecifier specifier) {
    switch (colour) {
      case PikminColour.Red:
        _RedStats.AddTo (maturity, specifier);
        break;
      case PikminColour.Yellow:
        _YellowStats.AddTo (maturity, specifier);
        break;
      case PikminColour.Blue:
        _BlueStats.AddTo (maturity, specifier);
        break;
      default:
        break;
    }
  }

  // Removes a Pikmin from the stats
  public static void Remove (PikminColour colour, PikminMaturity maturity, PikminStatSpecifier specifier) {
    switch (colour) {
      case PikminColour.Red:
        _RedStats.RemoveFrom (maturity, specifier);
        break;
      case PikminColour.Yellow:
        _YellowStats.RemoveFrom (maturity, specifier);
        break;
      case PikminColour.Blue:
        _BlueStats.RemoveFrom (maturity, specifier);
        break;
      default:
        break;
    }
  }

  // Prints out the information relevant for the stats of the Pikmin
  public static void Print () {
    Debug.Log ($"Length of the 'InSquad' list: {_InSquad.Count}");
    _RedStats.Print ();
    _BlueStats.Print ();
    _YellowStats.Print ();
  }

  //Sets up formations for the pikmin to use
  public static void ReassignFormation () {
    for (int i = 0; i < _InSquad.Count; i++) {
      _InSquad[i]._FormationPosition.position = Globals._Player._PikminController.GetPositionAt (i);
    }
  }

  #region Getters
  public static int GetOnField (PikminColour colour) {
    switch (colour) {
      case PikminColour.Red:
        return _RedStats.GetTotalOnField ();
      case PikminColour.Yellow:
        return _YellowStats.GetTotalOnField ();
      case PikminColour.Blue:
        return _BlueStats.GetTotalOnField ();
      default:
        return 0;
    }
  }
  public static int GetInSquad (PikminColour colour) {
    switch (colour) {
      case PikminColour.Red:
        return _RedStats.GetTotalInSquad ();
      case PikminColour.Yellow:
        return _YellowStats.GetTotalInSquad ();
      case PikminColour.Blue:
        return _BlueStats.GetTotalInSquad ();
      default:
        return 0;
    }
  }

  public static int GetInOnion (PikminColour colour) {
    switch (colour) {
      case PikminColour.Red:
        return _RedStats.GetTotalInOnion ();
      case PikminColour.Yellow:
        return _YellowStats.GetTotalInOnion ();
      case PikminColour.Blue:
        return _BlueStats.GetTotalInOnion ();
      default:
        return 0;
    }
  }

  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public static int GetTotalInSquad () {
    return _RedStats.GetTotalInSquad () + _YellowStats.GetTotalInSquad () + _BlueStats.GetTotalInSquad ();
  }

  [MethodImpl (MethodImplOptions.AggressiveInlining)]
  public static int GetTotalOnField () {
    return _RedStats.GetTotalOnField () + _YellowStats.GetTotalOnField () + _BlueStats.GetTotalOnField ();
  }
  #endregion
}
