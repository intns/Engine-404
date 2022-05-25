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
	[SerializeField] private GameObject _PikminSprout = null;
	[Space]
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

	[Header("Settings")]
	[SerializeField] private LayerMask _MapMask = 0;
	[SerializeField] private LayerMask _PikminMask = 0;
	[SerializeField] private float _PikminSuctionHeight = 4;
	[SerializeField] private float _PikminEjectionHeight = 5.5f;
	[SerializeField] PikminColour _OnionColour = PikminColour.Red;
	public PikminColour OnionColour { get { return _OnionColour; } private set { } }
	public bool OnionActive { get; private set; }

	[Header("Dispersal")]
	[SerializeField] private float _DisperseRadius = 12;
	private Vector3Int _SeedsToDisperse = Vector3Int.zero;

	public static List<Onion> _ActiveOnions = new List<Onion>();

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

	#region Unity Functions
	private void OnEnable() => _ActiveOnions.Add(this);
	private void OnDisable() => _ActiveOnions.Remove(this);
	private void OnDestroy() => _ActiveOnions.Remove(this);

	private void Awake()
	{
		_OnionCanvas.gameObject.SetActive(false);

		_CurrentSeedIdx = UnityEngine.Random.Range(0, PikminStatsManager._MaxOnField);

		_SpawnedSprouts.Clear();
		for (int i = 0; i < PikminStatsManager._MaxOnField; i++)
		{
			_SpawnedSprouts.Add(i, null);
		}

		if (PlayerPrefs.GetInt("ONION_Discovered") == 1)
		{
			_DiscoverObject.SetActive(false);
			OnionActive = true;

			_Animator.SetTrigger("EmptyIdle");
			_BodyRenderer.material.color = Color.white;
		}
		else
		{
			PlayerPrefs.SetInt("ONION_Discovered", 0);

			GetComponent<MeshRenderer>().enabled = false;
			GetComponent<Collider>().enabled = false;
			OnionActive = false;

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
						_InSquad = PikminStatsManager.GetInSquad(_OnionColour),
						_InOnion = PikminStatsManager.GetInOnion(_OnionColour)
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
							if (_CurPikminAmounts._InSquad + 1 <= PikminStatsManager._MaxOnField
								&& totalAdd + PikminStatsManager.GetTotalOnField() < PikminStatsManager._MaxOnField)
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
			for (int i = 0; i < PikminStatsManager._MaxOnField; i++)
			{
				Vector2 offset = MathUtil.PositionInUnit(PikminStatsManager._MaxOnField, i);
				Gizmos.DrawSphere(hit.point + MathUtil.XZToXYZ(offset) * _DisperseRadius, 0.25f);
			}
		}

		for (int i = 0; i < PikminStatsManager._MaxOnField; i++)
		{
			Vector2 offset = MathUtil.PositionInUnit(PikminStatsManager._MaxOnField, i);
			Gizmos.DrawSphere(transform.position + MathUtil.XZToXYZ(offset) + Vector3.up * _PikminEjectionHeight, 0.15f);
		}

		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position + Vector3.up * _PikminSuctionHeight, 0.5f);
	}
	#endregion

	#region Public Functions
	/// <summary>
	/// Played on the animation for the Sprout Eject, this will group 3 seeds together and spawn them at the same time
	/// </summary>
	public void ANIM_TryCreateSprout()
	{
		// Onion spits out in 3s
		for (int i = 0; i < 3; i++)
		{
			// X is red, Y is yellow, Z is blue
			int totalToDisperse = _SeedsToDisperse.x + _SeedsToDisperse.y + _SeedsToDisperse.z;
			if (totalToDisperse <= 0)
			{
				_Animator.SetBool("Spitting", false);
				return;
			}

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

	public void ANIM_EndDiscovery()
	{
		GetComponent<MeshRenderer>().enabled = true;
		GetComponent<Collider>().enabled = true;
		OnionActive = true;
	}

	/// <summary>
	/// For triggering a spitting sequence, it will try to spit X amount of Y colour
	/// </summary>
	/// <param name="toProduce">The amount of Pikmin to spit</param>
	/// <param name="colour">The colour of the Pikmin to spit</param>
	/// <exception cref="NotImplementedException">Tried to spit a colour that hasn't been implemented</exception>
	public void AddSproutsToSpit(int toProduce, PikminColour colour)
	{
		_ = colour switch
		{
			PikminColour.Red => _SeedsToDisperse.x += toProduce,
			PikminColour.Yellow => _SeedsToDisperse.y += toProduce,
			PikminColour.Blue => _SeedsToDisperse.z += toProduce,
			PikminColour.Size => throw new NotImplementedException(),
			_ => throw new NotImplementedException()
		};

		_Animator.SetBool("Spitting", true);
	}

	/// <summary>
	/// For handling sucking up an object, relies on the fact colliders are disabled, and transform (scale and position) altering scripts are disabled
	/// </summary>
	/// <param name="toSuck">The Game Object to suck up</param>
	/// <param name="toProduce">The amount of Pikmin it will produce</param>
	/// <param name="colour">The colour of the Pikmin it will produce</param>
	public void StartSuction(GameObject toSuck, int toProduce, PikminColour colour)
	{
		StartCoroutine(IE_SuctionAnimation(toSuck, toProduce, colour));
	}
	#endregion

	#region IEnumerators
	// For the spawning Pikmin sequence (from the in-game menu, not spitting)
	private IEnumerator IE_SpawnPikmin(int amount)
	{
		_CanUse = false;
		yield return new WaitForSeconds(0.25f);

		PikminTypeStats stats = PikminStatsManager.GetStats(_OnionColour);
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
			TryCreatePikmin(_OnionColour, toSpawn[i]);
			yield return new WaitForSeconds(0.03f);
		}

		_CanUse = true;
	}

	// For handling the suction animation
	private IEnumerator IE_SuctionAnimation(GameObject obj, int toProduce, PikminColour color)
	{
		yield return null;

		_Animator.SetBool("Suction", true);

		float t = 0;
		float timeToTop = 1.5f;
		Vector3 origin = obj.transform.position;
		Vector3 originScale = obj.transform.localScale;

		while (t <= timeToTop)
		{
			obj.transform.position = Vector3.Lerp(origin, transform.position + Vector3.up * _PikminSuctionHeight, MathUtil.EaseIn4(t / timeToTop));
			obj.transform.localScale = Vector3.Lerp(originScale, Vector3.zero, MathUtil.EaseIn3(t / timeToTop));
			_Animator.SetBool("Suction", true);

			t += Time.deltaTime;
			yield return null;
		}

		_Animator.SetBool("SuctionHit", true);
		_Animator.SetBool("Suction", false);

		Destroy(obj);

		AddSproutsToSpit(toProduce, color);

		yield return new WaitForEndOfFrame();
		_Animator.SetBool("SuctionHit", false);
	}
	#endregion

	#region Utility Functions
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

	/// <summary>
	/// Gets the spawn position around a unit circle for a random Pikmin
	/// </summary>
	/// <param name="idx">The index of the Pikmin to be spawned, affects where it will spawn and if it is -1 then will use random value</param>
	/// <returns>Position around a unit circle in 3D space (X, Z coords only)</returns>
	private Vector3 GetRandomBaseSpawnPosition(int idx = -1)
	{
		if (idx == -1)
		{
			idx = UnityEngine.Random.Range(0, PikminStatsManager._MaxOnField);
		}

		return MathUtil.XZToXYZ(MathUtil.PositionInUnit(PikminStatsManager._MaxOnField, idx));
	}


	/// <summary>
	/// For spawning in Pikmin through the menu usage
	/// </summary>
	/// <param name="colour">Colour of the Pikmin to spawn</param>
	/// <param name="maturity">Maturity of the Pikmin to spawn</param>
	private void TryCreatePikmin(PikminColour colour, PikminMaturity maturity)
	{
		if (PikminStatsManager.GetTotalOnField() >= PikminStatsManager._MaxOnField)
		{
			PikminStatsManager.Add(colour, PikminMaturity.Leaf, PikminStatSpecifier.InOnion);
			return;
		}

		if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 50, _MapMask, QueryTriggerInteraction.Ignore))
		{
			return;
		}

		int pos = UnityEngine.Random.Range(0, PikminStatsManager._MaxOnField + 1);

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


	/// <summary>
	/// For spawning a sprout
	/// </summary>
	/// <param name="colour">The colour of the sprout to spawn</param>
	private void TryCreateSprout(PikminColour colour)
	{
		if (PikminStatsManager.GetTotalOnField() == PikminStatsManager._MaxOnField)
		{
			PikminStatsManager.Add(colour, PikminMaturity.Leaf, PikminStatSpecifier.InOnion);
			return;
		}

		if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 50, _MapMask, QueryTriggerInteraction.Ignore))
		{
			return;
		}

		while (_SpawnedSprouts[_CurrentSeedIdx] != null)
		{
			_CurrentSeedIdx += UnityEngine.Random.Range(1, 15);
			_CurrentSeedIdx %= PikminStatsManager._MaxOnField;
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
	#endregion
}
