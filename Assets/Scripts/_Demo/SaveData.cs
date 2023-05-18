﻿using System;
using System.Linq;
using Demo;

namespace _Demo
{
	public enum Scene
	{
		MainMenu,
		ImpactSite,
		ForestOfHope,
	}

	[Serializable]
	public class SaveData
	{
		public static string _SaveFile = "save_000.json";


		static SaveData _currentData;

		// Day Settings
		public int _Day;

		// Ship Settings
		public SerializableDictionary<ShipPartType, ShipPartData> _ShipPartData = new();

		// Onion Settings
		public SerializableDictionary<PikminColour, PikminTypeStats> _InOnionPikmin = new();
		public SerializableDictionary<PikminColour, bool> _DiscoveredOnions = new();


		SaveData()
		{
		}

		public static SaveData _CurrentData
		{
			get => _currentData ??= new();
			set => _currentData = value ?? throw new ArgumentNullException(nameof(value));
		}

		public static void LoadData()
		{
			_CurrentData = SaveManager.LoadData<SaveData>(_SaveFile);

			foreach (PikminColour colour in Enum.GetValues(typeof(PikminColour)))
			{
				PikminTypeStats loadedStats = _CurrentData._InOnionPikmin[colour];
				PikminTypeStats currentStats = PikminStatsManager.GetPikminStats(colour);

				// Update the stats for each maturity level
				currentStats._Leaf._InOnion = loadedStats._Leaf._InOnion;
				currentStats._Bud._InOnion = loadedStats._Bud._InOnion;
				currentStats._Flower._InOnion = loadedStats._Flower._InOnion;
			}
		}

		public static void WriteData()
		{
			SaveManager.SaveData(_CurrentData, _SaveFile);
		}

		public static void ResetData()
		{
			_CurrentData = new();
			WriteData();
		}

		public static void UpdateData()
		{
			foreach (PikminColour colour in Enum.GetValues(typeof(PikminColour)))
			{
				PikminTypeStats typeStats = PikminStatsManager._TypeStats[colour];
				PikminTypeStats inOnionStats = new(colour) { _Leaf = { _InOnion = typeStats._Leaf._InOnion }, _Bud = { _InOnion = typeStats._Bud._InOnion }, _Flower = { _InOnion = typeStats._Flower._InOnion } };

				_CurrentData._InOnionPikmin[colour] = inOnionStats;
			}

			foreach (Onion onion in OnionManager._OnionsInScene.Where(onion => !_CurrentData._DiscoveredOnions.TryAdd(onion.Colour, onion.OnionActive)))
			{
				_CurrentData._DiscoveredOnions[onion.Colour] = onion.OnionActive;
			}
		}
	}
}