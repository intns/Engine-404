/*
 * Onion.cs
 * Created by: Ambrosia, Helodity
 * Created on: 12/4/2020 (dd/mm/yy)
 * Created for: Needing an object to store our Pikmin in
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class PikminSpawnData
{
	public Vector3 _OriginPosition = Vector3.zero;
	public Vector3 _EndPosition = Vector3.zero;
	public PikminColour _Colour = PikminColour.Red;
}

public class Onion : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Animator _Animator = null;
	[SerializeField] private SkinnedMeshRenderer _BodyRenderer = null;
	[Space]
	[SerializeField] private Canvas _OnionCanvas = null;
	[SerializeField] private TextMeshProUGUI _InOnionText;
	[SerializeField] private TextMeshProUGUI _InSquadText;
	[SerializeField] private TextMeshProUGUI _InFieldText;
	[SerializeField] private CanvasGroup _CanvasGroup = null;
	[Space]
	[SerializeField] private GameObject _DiscoverObject = null;

	[Header("Debug")]
	[SerializeField] private GameObject _PikminSprout = null;

	[Header("Settings")]
	[SerializeField] private LayerMask _MapMask = 0;
	[SerializeField] private LayerMask _PikminMask = 0;
	[SerializeField] private float _PikminEjectionHeight = 3;
	public PikminColour _PikminColour { get; private set; } = PikminColour.Red;

	[Header("Dispersal")]
	[SerializeField] private float _DisperseRadius = 2;
	[SerializeField] private float _DisperseRate = 2;
	private Vector3Int _SeedsToDisperse = Vector3Int.zero;
	private float _TimeSinceLastDisperse = 0;

	private int _CurrentSeedIdx = 0;
	Dictionary<int, GameObject> _SpawnedSprouts = new Dictionary<int, GameObject>();

	public Transform _CarryEndpoint = null;
	private bool _CanUse = false;
	private bool _InMenu = false;

	// First number is coming out of the onion,
	// Second number is total in the onion

	private struct PikminAmount
	{
		public int _InSquad;
		public int _InOnion;
	};

	private PikminAmount _CurPikminAmounts;
	private PikminAmount _OldPikminAmounts;

	private float _InputTimer = 0;

	private IEnumerator FadeInCanvas()
	{
		float t = 0;
		float time = 0.5f;
		while (t <= time)
		{
			t += Time.deltaTime;
			_CanvasGroup.alpha = Mathf.Lerp(0, 1, t / time);
			yield return null;
		}
	}

	private IEnumerator FadeOutCanvas()
	{
		float t = 0;
		float time = 0.5f;
		while (t <= time)
		{
			t += Time.deltaTime;
			_CanvasGroup.alpha = Mathf.Lerp(1, 0, t / time);
			yield return null;
		}
	}

	private void Awake()
	{
		_OnionCanvas.gameObject.SetActive(false);

		_CurrentSeedIdx = UnityEngine.Random.Range(0, 101);

		_SpawnedSprouts.Clear();
		for (int i = 0; i < 100; i++)
		{
			_SpawnedSprouts.Add(i, null);
		}

		if (PlayerPrefs.HasKey("ONION_Discovered") && PlayerPrefs.GetInt("ONION_Discovered") == 1)
		{
			_DiscoverObject.SetActive(false);
			_Animator.SetTrigger("EmptyIdle");
			_BodyRenderer.material.color = Color.white;
		}
		else
		{
			PlayerPrefs.SetInt("ONION_Discovered", 0);
			_BodyRenderer.material.color = new Color(0.4078f, 0.4078f, 0.4078f);
		}
	}

	private void Update()
	{
		if (_CanUse)
		{
			if (Input.GetButtonDown("A Button") && !_InMenu)
			{
				Player._Instance.Pause(true);
				FadeManager._Instance.FadeInOut(0.25f, 0.5f, new Action(() =>
				{
					_OnionCanvas.gameObject.SetActive(true);

					_CurPikminAmounts = new PikminAmount
					{
						_InSquad = PikminStatsManager.GetInSquad(_PikminColour),
						_InOnion = PikminStatsManager.GetInOnion(_PikminColour)
					};

					_OldPikminAmounts = new PikminAmount
					{
						_InOnion = _CurPikminAmounts._InOnion,
						_InSquad = _CurPikminAmounts._InSquad
					};

					_InMenu = true;

					StartCoroutine(FadeInCanvas());
				}));
			}
			else if (Input.GetButtonDown("B Button") && _InMenu)
			{
				FadeManager._Instance.FadeInOut(0.25f, 0.5f, new Action(() =>
				{
					_OnionCanvas.gameObject.SetActive(false);
					Player._Instance.Pause(false);
					_InMenu = false;

					StartCoroutine(FadeOutCanvas());
				}));
			}
		}

		// Handle in-menu input processing
		if (_InMenu)
		{
			float vInput = Input.GetAxis("Vertical");
			if (_InputTimer <= 0)
			{
				switch (vInput)
				{
					// Up on the stick / W
					case > 0.25f:
						if (_CurPikminAmounts._InSquad > 0)
						{
							if (vInput > 0.8f)
							{
								_InputTimer = 0.05f;
							}
							else
							{
								_InputTimer = 0.1f;
							}

							_CurPikminAmounts._InSquad--;
							_CurPikminAmounts._InOnion++;
						}
						break;
					// Down on the stick / S
					case < -0.25f:
						if (_CurPikminAmounts._InOnion > 0)
						{
							if (vInput < -0.8f)
							{
								_InputTimer = 0.05f;
							}
							else
							{
								_InputTimer = 0.1f;
							}

							int totalAdd = _CurPikminAmounts._InSquad - _OldPikminAmounts._InSquad;
							if (_CurPikminAmounts._InSquad + 1 <= 100
								&& totalAdd + PikminStatsManager.GetTotalOnField() < 100)
							{
								_CurPikminAmounts._InOnion--;
								_CurPikminAmounts._InSquad++;
							}
						}
						break;
					default:
						break;
				}
			}
			else
			{
				_InputTimer -= Time.deltaTime;
			}

			if (Input.GetButtonDown("A Button")
				&& (_CurPikminAmounts._InSquad != _OldPikminAmounts._InSquad
					|| _CurPikminAmounts._InOnion != _OldPikminAmounts._InOnion))
			{
				FadeManager._Instance.FadeInOut(0.25f, 0.5f, new Action(() =>
				{
					_OnionCanvas.gameObject.SetActive(false);
					Player._Instance.Pause(false);
					_InMenu = false;

					int fieldDifference = _CurPikminAmounts._InSquad - _OldPikminAmounts._InSquad;

					if (fieldDifference > 0)
					{
						StartCoroutine(IE_SpawnPikmin(fieldDifference));
					}
					else if (fieldDifference < 0)
					{
						Collider[] pikmin = Physics.OverlapSphere(_CarryEndpoint.position, 50f, _PikminMask);
						if (pikmin.Length == 0)
						{
							return;
						}

						for (int i = 0; i < Mathf.Abs(fieldDifference); i++)
						{
							var pikai = pikmin[i].GetComponent<PikminAI>();
							if (pikai._InSquad)
							{
								pikai.RemoveFromSquad();
								PikminStatsManager.Add(pikai._Data._PikminColour, pikai._CurrentMaturity, PikminStatSpecifier.InOnion);
								PikminStatsManager.Remove(pikai._Data._PikminColour, pikai._CurrentMaturity, PikminStatSpecifier.OnField);
								Destroy(pikmin[i].gameObject);
							}
						}
					}

					StartCoroutine(FadeOutCanvas());
				}));
			}

			_InOnionText.text = $"{_CurPikminAmounts._InOnion}";
			_InSquadText.text = $"{_CurPikminAmounts._InSquad}";
			_InFieldText.text = $"{PikminStatsManager.GetTotalOnField()}";
		}

		// X is red, Y is yellow, Z is blue
		int totalToDisperse = _SeedsToDisperse.x + _SeedsToDisperse.y + _SeedsToDisperse.z;
		if (totalToDisperse > 0)
		{
			_TimeSinceLastDisperse += Time.deltaTime;
			if (_TimeSinceLastDisperse > 1 / (totalToDisperse * _DisperseRate))
			{
				_TimeSinceLastDisperse -= 1 / (totalToDisperse * _DisperseRate);
				if (_SeedsToDisperse.x > 0)
				{
					TryCreateSprout(PikminColour.Red);
					_SeedsToDisperse.x--;
				}
				else if (_SeedsToDisperse.y > 0)
				{
					TryCreateSprout(PikminColour.Yellow);
					_SeedsToDisperse.y--;
				}
				else if (_SeedsToDisperse.z > 0)
				{
					TryCreateSprout(PikminColour.Blue);
					_SeedsToDisperse.z--;
				}
			}
		}
	}

	private IEnumerator IE_SpawnPikmin(int amount)
	{
		_CanUse = false;
		yield return new WaitForSeconds(0.25f);

		PikminTypeStats stats = PikminStatsManager.GetStats(_PikminColour);
		List<PikminMaturity> toSpawn = new List<PikminMaturity>();

		for (int i = 0; i < amount; i++)
		{
			if (stats._Flower._InOnion > 0)
			{
				stats._Flower._InOnion--;
				toSpawn.Add(PikminMaturity.Flower);
			}
			else if (stats._Bud._InOnion > 0)
			{
				stats._Bud._InOnion--;
				toSpawn.Add(PikminMaturity.Bud);
			}
			else if (stats._Leaf._InOnion > 0)
			{
				stats._Leaf._InOnion--;
				toSpawn.Add(PikminMaturity.Leaf);
			}
		}

		for (int i = 0; i < amount; i++)
		{
			_CanUse = false;
			TryCreatePikmin(_PikminColour, toSpawn[i]);
			yield return new WaitForSeconds(0.02f);
		}

		_CanUse = true;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			_CanUse = true;
			Player._Instance._PikminController._CanThrowPikmin = false;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			_CanUse = false;
			Player._Instance._PikminController._CanThrowPikmin = true;
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 50, _MapMask, QueryTriggerInteraction.Ignore))
		{
			for (int i = 0; i < 100; i++)
			{
				Vector2 offset = MathUtil.PositionInUnit(100, i);
				Gizmos.DrawSphere(hit.point + MathUtil.XZToXYZ(offset) * _DisperseRadius, 0.25f);
			}
		}

		for (int i = 0; i < 100; i++)
		{
			Vector2 offset = MathUtil.PositionInUnit(100, i);
			Gizmos.DrawSphere(transform.position + MathUtil.XZToXYZ(offset) + Vector3.up * _PikminEjectionHeight, 0.15f);
		}
	}

	private Vector3 GetRandomBaseSpawnPosition(int idx = -1)
	{
		if (idx == -1)
		{
			idx = UnityEngine.Random.Range(0, 100);
		}

		return MathUtil.XZToXYZ(MathUtil.PositionInUnit(100, idx));
	}

	public void AddSproutsToSpawn(int toProduce, PikminColour colour)
	{
		_ = colour switch
		{
			PikminColour.Red => _SeedsToDisperse.x += toProduce,
			PikminColour.Yellow => _SeedsToDisperse.y += toProduce,
			PikminColour.Blue => _SeedsToDisperse.z += toProduce,
			PikminColour.Size => throw new NotImplementedException(),
			_ => throw new NotImplementedException()
		};
	}

	/// <summary>
	/// For spawning in Pikmin through the menu usage
	/// </summary>
	/// <param name="colour">Colour of the Pikmin to spawn</param>
	/// <param name="maturity">Maturity of the Pikmin to spawn</param>
	private void TryCreatePikmin(PikminColour colour, PikminMaturity maturity)
	{
		if (PikminStatsManager.GetTotalOnField() == 100)
		{
			PikminStatsManager.Add(colour, PikminMaturity.Leaf, PikminStatSpecifier.InOnion);
		}
		else if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 50, _MapMask, QueryTriggerInteraction.Ignore))
		{
			int pos = UnityEngine.Random.Range(0, 101);

			// Get offset on a circle, converted to 3d coords
			Vector3 basePos = GetRandomBaseSpawnPosition(pos);

			// Spawn the sprout just above the onion
			GameObject pikmin = Instantiate(_PikminSprout, hit.point + basePos * _DisperseRadius, Quaternion.identity);
			PikminSprout sproutData = pikmin.GetComponent<PikminSprout>();

			PikminSpawnData data = new()
			{
				_OriginPosition = pikmin.transform.position,
				_EndPosition = hit.point + basePos * _DisperseRadius,
				_Colour = colour,
			};

			sproutData.OnSpawn(data);
			if (maturity == PikminMaturity.Bud)
			{
				sproutData.PromoteMaturity();
			}
			else if (maturity == PikminMaturity.Flower)
			{
				sproutData.PromoteMaturity();
				sproutData.PromoteMaturity();
			}

			sproutData.OnPluck();
			Destroy(sproutData.gameObject);
		}
	}

	/// <summary>
	/// For spawning a Pikmin through a carried object
	/// </summary>
	/// <param name="colour">The colour of the Pikmin to spawn</param>
	private void TryCreateSprout(PikminColour colour)
	{
		if (PikminStatsManager.GetTotalOnField() == 100)
		{
			PikminStatsManager.Add(colour, PikminMaturity.Leaf, PikminStatSpecifier.InOnion);
		}
		else if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 50, _MapMask, QueryTriggerInteraction.Ignore))
		{
			while (_SpawnedSprouts[_CurrentSeedIdx] != null)
			{
				_CurrentSeedIdx += UnityEngine.Random.Range(1, 15);
				_CurrentSeedIdx %= 100;
			}

			// Get offset on a circle, converted to 3d coords
			Vector3 basePos = GetRandomBaseSpawnPosition(_CurrentSeedIdx);

			// Spawn the sprout just above the onion
			GameObject newPikmin = _SpawnedSprouts[_CurrentSeedIdx] = Instantiate(_PikminSprout, transform.position + basePos + Vector3.up * _PikminEjectionHeight, Quaternion.identity);
			PikminSprout sproutData = newPikmin.GetComponent<PikminSprout>();

			Physics.Raycast(Vector3.up * 15 + (hit.point + basePos * _DisperseRadius), Vector3.down, out RaycastHit hit2, 50, _MapMask);

			PikminSpawnData data = new()
			{
				_OriginPosition = newPikmin.transform.position,
				_EndPosition = hit2.point,
				_Colour = colour,
			};

			sproutData.OnSpawn(data);
		}
	}
}
