using System;
using System.Linq;

namespace Demo
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

		// Debug Settings
		public bool _IsDebug;

		// Day Settings
		public int _Day = 1;

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
				if (colour == PikminColour.Size)
				{
					continue;
				}

				PikminTypeStats currentStats = PikminStatsManager.GetPikminStats(colour);

				if (_CurrentData._InOnionPikmin.TryGetValue(colour, out PikminTypeStats loadedStats))
				{
					currentStats._Leaf._InOnion = loadedStats._Leaf._InOnion;
					currentStats._Bud._InOnion = loadedStats._Bud._InOnion;
					currentStats._Flower._InOnion = loadedStats._Flower._InOnion;
				}
				else
				{
					currentStats._Leaf._InOnion = 0;
					currentStats._Bud._InOnion = 0;
					currentStats._Flower._InOnion = 0;
				}
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
			// Move all Pikmin in squad into onions
			foreach (PikminAI pikmin in PikminStatsManager._InSquad)
			{
				PikminStatsManager.Add(pikmin.GetColour(), pikmin._CurrentMaturity, PikminStatSpecifier.InOnion);
			}

			// Save the onion data
			foreach (PikminColour colour in Enum.GetValues(typeof(PikminColour)))
			{
				PikminTypeStats typeStats = PikminStatsManager._TypeStats[colour];
				PikminTypeStats inOnionStats = new(colour) { _Leaf = { _InOnion = typeStats._Leaf._InOnion }, _Bud = { _InOnion = typeStats._Bud._InOnion }, _Flower = { _InOnion = typeStats._Flower._InOnion } };

				_CurrentData._InOnionPikmin[colour] = inOnionStats;
			}

			// Set the discovered onions
			foreach (Onion onion in OnionManager._OnionsInScene.Where(onion => !_CurrentData._DiscoveredOnions.TryAdd(onion.Colour, onion.OnionActive)))
			{
				_CurrentData._DiscoveredOnions[onion.Colour] = onion.OnionActive;
			}
		}

		public static void EndOfDaySave()
		{
			_CurrentData._Day++;
			ShipManager._Instance.UpdateSaveData();

			UpdateData();
			WriteData();
		}
	}
}
