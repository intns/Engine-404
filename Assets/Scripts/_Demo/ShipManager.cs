using System;
using System.Collections.Generic;
using _Demo;
using UnityEngine;

namespace Demo
{
	public enum ShipPartType
	{
		None,
		MainEngine,
		PositronGenerator,
		FuelDynamo,
		Bolt,
		Radar,
	}

	[Serializable]
	public class ShipPartData
	{
		public ShipPartType _PartType;
		public Vector3 _Location;
		public bool _Collected;
		public bool _Discovered;
	}

	[Serializable]
	public class SceneShipData
	{
		public ShipPartData _Data;
		public GameObject _Object;
	}

	public class ShipManager : MonoBehaviour
	{
		[Header("Components")]
		[Tooltip("All of the ship parts within the current scene")]
		public List<SceneShipData> _SceneParts = new();
		public static ShipManager _Instance { get; private set; }

		void Awake()
		{
			if (_Instance == null)
			{
				_Instance = this;
			}
			else
			{
				Destroy(gameObject);
			}
		}

		void Start()
		{
			if (_SceneParts.Count == 0)
			{
				Debug.Log("No ship parts in the level, are you sure?");
				return;
			}

			for (int i = _SceneParts.Count - 1; i >= 0; i--)
			{
				SceneShipData scenePart = _SceneParts[i];

				if (SaveData._CurrentData._ShipPartData.TryGetValue(scenePart._Data._PartType, out ShipPartData savedPart))
				{
					if (savedPart._Collected)
					{
						Destroy(scenePart._Object);
						_SceneParts.RemoveAt(i);
						continue;
					}

					scenePart._Object.transform.position = savedPart._Location;
					scenePart._Data._Discovered = savedPart._Discovered;
				}
				else
				{
					SaveData._CurrentData._ShipPartData.Add(scenePart._Data._PartType, scenePart._Data);
				}
			}

			_SceneParts.TrimExcess();
		}

		public void UpdateSaveData()
		{
			foreach (SceneShipData scenePart in _SceneParts)
			{
				if (!SaveData._CurrentData._ShipPartData.TryGetValue(scenePart._Data._PartType, out ShipPartData savedPart))
				{
					SaveData._CurrentData._ShipPartData.Add(scenePart._Data._PartType, scenePart._Data);
					continue;
				}

				savedPart._Location = scenePart._Object.transform.position;
				savedPart._Collected = scenePart._Data._Collected;
				savedPart._Discovered = scenePart._Data._Discovered;
			}
		}
	}
}
