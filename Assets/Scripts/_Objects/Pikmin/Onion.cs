/*
 * Onion.cs
 * Created by: Ambrosia, Helodity
 * Created on: 12/4/2020 (dd/mm/yy)
 * Created for: Needing an object to store our Pikmin in
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Demo;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

[Serializable]
public class PikminSpawnData
{
	public Vector3 _OriginPosition = Vector3.zero;
	public Vector3 _EndPosition = Vector3.zero;
	public PikminColour _Colour = PikminColour.Red;
}

public class Onion : MonoBehaviour, ICarryObjectAbsorb
{
	[Header("References")]
	[SerializeField] GameObject _PikminSprout;
	[Space]
	[SerializeField] Animator _Animator;
	[SerializeField] SkinnedMeshRenderer _BodyRenderer;
	[Space]
	[SerializeField] Canvas _OnionCanvas;
	[SerializeField] TextMeshProUGUI _InOnionText;
	[SerializeField] TextMeshProUGUI _InSquadText;
	[SerializeField] TextMeshProUGUI _InFieldText;
	[SerializeField] CanvasGroup _CanvasGroup;
	[Space]
	[SerializeField] GameObject _DiscoverObject;

	[Header("Settings")]
	[SerializeField] LayerMask _MapMask = 0;
	[SerializeField] LayerMask _PikminMask = 0;
	[SerializeField] float _PikminSuctionHeight = 4;
	[SerializeField] float _PikminEjectionHeight = 5.5f;
	[SerializeField] PikminColour _Colour = PikminColour.Red;

	[Header("Dispersal")]
	[SerializeField] float _DisperseRadius = 12;

	public Transform _CarryEndpoint;

	bool _CanUse;
	int _CurrentSeedIdx;
	bool _InMenu;
	float _InputTimer;

	PikminAmount _OriginalAmount;
	PikminAmount _ResultAmount;
	Vector3Int _SeedsToDisperse = Vector3Int.zero;

	Dictionary<int, GameObject> _SpawnedSprouts = new();
	float _UpDownAxis;

	public PikminColour Colour
	{
		get => _Colour;
		private set { }
	}

	public bool OnionActive { get; set; }

	struct PikminAmount
	{
		public int _InSquad; // How many are meant to come out of the onion
		public int _InOnion; // Total within the onion
	}

	#region Unity Functions

	void OnEnable()
	{
		OnionManager._OnionsInScene.Add(this);
	}

	void OnDisable()
	{
		OnionManager._OnionsInScene.Remove(this);
	}

	void OnDestroy()
	{
		OnionManager._OnionsInScene.Remove(this);
	}

	void Awake()
	{
		_OnionCanvas.gameObject.SetActive(false);

		_CurrentSeedIdx = Random.Range(0, PikminStatsManager._MaxPikminOnField);

		_SpawnedSprouts.Clear();

		for (int i = 0; i < PikminStatsManager._MaxPikminOnField; i++)
		{
			_SpawnedSprouts.Add(i, null);
		}

		if (SaveData._CurrentData._DiscoveredOnions.TryGetValue(_Colour, out bool isOnionDiscovered) && isOnionDiscovered)
		{
			_DiscoverObject.SetActive(false);
			OnionActive = true;

			_Animator.SetTrigger("EmptyIdle");
			_BodyRenderer.material.color = Color.white;
			return;
		}

		// Onion is not found, so we'll set it as such
		SaveData._CurrentData._DiscoveredOnions.TryAdd(_Colour, false);

		GetComponent<MeshRenderer>().enabled = false;
		GetComponent<Collider>().enabled = false;
		OnionActive = false;

		_BodyRenderer.material.color = new(0.4078f, 0.4078f, 0.4078f);
	}

	void Update()
	{
		// Handle in-menu input processing
		if (!_InMenu)
		{
			return;
		}

		if (_InputTimer <= 0)
		{
			switch (_UpDownAxis)
			{
				// Up on the stick / W
				case > 0.25f:
					if (_ResultAmount._InSquad > 0)
					{
						_InputTimer = _UpDownAxis > 0.8f ? 0.05f : 0.1f;

						_ResultAmount._InSquad = Mathf.Max(_ResultAmount._InSquad - 1, 0);
						_ResultAmount._InOnion++;
					}

					break;
				// Down on the stick / S
				case < -0.25f:
					if (_ResultAmount._InOnion > 0)
					{
						_InputTimer = _UpDownAxis < -0.8f ? 0.05f : 0.1f;

						int amount = _ResultAmount._InSquad + 1 - _OriginalAmount._InSquad;
						int toSpawn = Mathf.Abs(amount);

						if (amount < 0 ||
						    PikminStatsManager.GetTotalPikminOnField() + toSpawn <= PikminStatsManager._MaxPikminOnField)
						{
							_ResultAmount._InOnion = Mathf.Max(_ResultAmount._InOnion - 1, 0);
							_ResultAmount._InSquad++;
						}
					}

					break;
			}
		}
		else
		{
			_InputTimer -= Time.deltaTime;
		}

		_InOnionText.text = $"{_ResultAmount._InOnion}";
		_InSquadText.text = $"{_ResultAmount._InSquad}";
		_InFieldText.text = $"{PikminStatsManager.GetTotalPikminOnField()}";
	}

	public void OnMovement(InputAction.CallbackContext context)
	{
		_UpDownAxis = context.ReadValue<Vector2>().y;
	}

	public void OnWhistle(InputAction.CallbackContext context)
	{
		if (context.started && _InMenu)
		{
			FadeManager._Instance.FadeInOut(
				0.25f,
				0.5f,
				() =>
				{
					_OnionCanvas.gameObject.SetActive(false);
					Player._Instance.Pause(PauseType.Unpaused);
					_InMenu = false;

					StartCoroutine(FadeOutCanvas());
				}
			);
		}
	}

	public void OnPrimaryAction(InputAction.CallbackContext context)
	{
		if (!context.started || !_CanUse)
		{
			return;
		}

		if (_InMenu)
		{
			if (_ResultAmount._InSquad == _OriginalAmount._InSquad || _ResultAmount._InOnion == _OriginalAmount._InOnion)
			{
				return;
			}

			StartCoroutine(FadeOutCanvas());

			FadeManager._Instance.FadeInOut(
				0.25f,
				0.5f,
				() =>
				{
					_OnionCanvas.gameObject.SetActive(false);
					Player._Instance.Pause(PauseType.Unpaused);
					_InMenu = false;

					int fieldDifference = _ResultAmount._InSquad - _OriginalAmount._InSquad;
					bool isSpawning = fieldDifference > 0;
					bool isTaking = fieldDifference < 0;

					if (isSpawning)
					{
						StartCoroutine(IE_SpawnPikmin(fieldDifference));
					}
					else if (isTaking)
					{
						List<PikminAI> pikmin = PikminStatsManager._InSquad.Where(x => x.GetColour() == _Colour).ToList();
						int amount = Mathf.Abs(fieldDifference);

						Debug.Assert(amount <= pikmin.Count, "Pikmin squad doesn't have as many Pikmin as the Onion wants to take");

						for (int i = 0; i < amount; i++)
						{
							PikminAI ai = pikmin[i].GetComponent<PikminAI>();
							ai.RemoveFromSquad();
							PikminStatsManager.Add(ai._Data._PikminColour, ai._CurrentMaturity, PikminStatSpecifier.InOnion);
							PikminStatsManager.Remove(ai._Data._PikminColour, ai._CurrentMaturity, PikminStatSpecifier.OnField);
							Destroy(ai.gameObject);
						}
					}
				}
			);

			return;
		}

		Player._Instance.Pause(PauseType.Paused);
		StartCoroutine(FadeInCanvas());

		FadeManager._Instance.FadeInOut(
			0.25f,
			0.5f,
			() =>
			{
				_OnionCanvas.gameObject.SetActive(true);

				_OriginalAmount = new() { _InOnion = PikminStatsManager.GetTotalPikminInOnion(_Colour), _InSquad = PikminStatsManager.GetTotalPikminInSquad(_Colour) };

				_ResultAmount = _OriginalAmount;

				_InMenu = true;
			}
		);
	}


	void OnTriggerStay(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			_CanUse = true;
			Player._Instance._PikminController._CanThrowPikmin = false;
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			_CanUse = false;
			Player._Instance._PikminController._CanThrowPikmin = true;
		}
	}

	void OnDrawGizmosSelected()
	{
		if (Physics.Raycast(
			    transform.position,
			    Vector3.down,
			    out RaycastHit hit,
			    50,
			    _MapMask,
			    QueryTriggerInteraction.Ignore
		    ))
		{
			for (int i = 0; i < PikminStatsManager._MaxPikminOnField; i++)
			{
				Vector2 offset = MathUtil.PositionInUnit(PikminStatsManager._MaxPikminOnField, i);
				Gizmos.DrawSphere(hit.point + MathUtil.XZToXYZ(offset) * _DisperseRadius, 0.25f);
			}
		}

		for (int i = 0; i < PikminStatsManager._MaxPikminOnField; i++)
		{
			Vector2 offset = MathUtil.PositionInUnit(PikminStatsManager._MaxPikminOnField, i);
			Gizmos.DrawSphere(transform.position + MathUtil.XZToXYZ(offset) + Vector3.up * _PikminEjectionHeight, 0.15f);
		}

		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(transform.position + Vector3.up * _PikminSuctionHeight, 0.5f);
	}

	#endregion

	#region Public Functions

	/// <summary>
	///   Played on the animation for the Sprout Eject, this will group 3 seeds together and spawn them at the same time
	/// </summary>
	public void ANIM_TryCreateSprout()
	{
		for (int i = 0; i < 5; i++)
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
		Debug.Log("End Discovery");
		GetComponent<MeshRenderer>().enabled = true;
		GetComponent<Collider>().enabled = true;
		OnionActive = true;
		/*DemoSettings._DiscoveredOnionCutsceneDone = true;*/
	}

	/// <summary>
	///   For triggering a spitting sequence, it will try to spit X amount of Y colour
	/// </summary>
	/// <param name="toProduce">The amount of Pikmin to spit</param>
	/// <param name="colour">The colour of the Pikmin to spit</param>
	/// <exception cref="NotImplementedException">Tried to spit a colour that hasn't been implemented</exception>
	public void AddSproutsToSpit(int toProduce, PikminColour colour)
	{
		switch (colour)
		{
			case PikminColour.Red:
				_SeedsToDisperse.x += toProduce;
				break;
			case PikminColour.Yellow:
				_SeedsToDisperse.y += toProduce;
				break;
			case PikminColour.Blue:
				_SeedsToDisperse.z += toProduce;
				break;

			default:
				throw new NotImplementedException();
		}

		_Animator.SetBool("Spitting", true);
	}

	/// <summary>
	///   For handling sucking up an object, relies on the fact colliders are disabled, and transform (scale and position)
	///   altering scripts are disabled
	/// </summary>
	/// <param name="toSuck">The Game Object to suck up</param>
	/// <param name="toProduce">The amount of Pikmin it will produce</param>
	public void StartAbsorb(PikminCarryObject obj) // (GameObject toSuck, int toProduce)
	{
		StartCoroutine(IE_SuctionAnimation(obj.gameObject, obj.GetPikminSpawnAmount()));
	}

	#endregion

	#region IEnumerators

	// For the spawning Pikmin sequence (from the in-game menu, not spitting)
	IEnumerator IE_SpawnPikmin(int amount)
	{
		_CanUse = false;
		yield return new WaitForSeconds(0.25f);

		PikminTypeStats stats = PikminStatsManager.GetPikminStats(_Colour);
		List<PikminMaturity> toSpawn = new();

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
			TryCreatePikmin(_Colour, toSpawn[i]);
			yield return new WaitForSeconds(0.02f);
		}

		_CanUse = true;
	}

	// For handling the suction animation
	IEnumerator IE_SuctionAnimation(GameObject obj, int toProduce)
	{
		yield return null;

		_Animator.SetBool("Suction", true);

		float t = 0;
		float timeToTop = 1.5f;
		Vector3 origin = obj.transform.position;
		Vector3 originScale = obj.transform.localScale;

		while (t <= timeToTop)
		{
			obj.transform.position = Vector3.Lerp(
				origin,
				transform.position + Vector3.up * _PikminSuctionHeight,
				MathUtil.EaseIn4(t / timeToTop)
			);
			obj.transform.localScale = Vector3.Lerp(originScale, Vector3.zero, MathUtil.EaseIn3(t / timeToTop));
			_Animator.SetBool("Suction", true);

			t += Time.deltaTime;
			yield return null;
		}

		_Animator.SetBool("SuctionHit", true);
		_Animator.SetBool("Suction", false);

		Destroy(obj);

		AddSproutsToSpit(toProduce, _Colour);

		yield return new WaitForEndOfFrame();
		_Animator.SetBool("SuctionHit", false);
	}

	#endregion

	#region Utility Functions

	IEnumerator FadeInCanvas()
	{
		float t = 0;
		float time = 0.25f;

		while (t <= time)
		{
			t += Time.deltaTime;
			_CanvasGroup.alpha = t / time;
			yield return null;
		}
	}

	IEnumerator FadeOutCanvas()
	{
		float t = 0;
		float time = 0.25f;

		while (t <= time)
		{
			t += Time.deltaTime;
			_CanvasGroup.alpha = 1 - t / time;
			yield return null;
		}
	}

	/// <summary>
	///   Gets the spawn position around a unit circle for a random Pikmin
	/// </summary>
	/// <param name="idx">
	///   The index of the Pikmin to be spawned, affects where it will spawn and if it is -1 then will use
	///   random value
	/// </param>
	/// <returns>Position around a unit circle in 3D space (X, Z coords only)</returns>
	Vector3 GetRandomBaseSpawnPosition(int idx = -1)
	{
		if (idx == -1)
		{
			idx = Random.Range(0, PikminStatsManager._MaxPikminOnField);
		}

		return MathUtil.XZToXYZ(MathUtil.PositionInUnit(PikminStatsManager._MaxPikminOnField, idx));
	}


	/// <summary>
	///   For spawning in Pikmin through the menu usage
	/// </summary>
	/// <param name="colour">Colour of the Pikmin to spawn</param>
	/// <param name="maturity">Maturity of the Pikmin to spawn</param>
	void TryCreatePikmin(PikminColour colour, PikminMaturity maturity)
	{
		if (PikminStatsManager.GetTotalPikminOnField() >= PikminStatsManager._MaxPikminOnField)
		{
			PikminStatsManager.Add(colour, PikminMaturity.Leaf, PikminStatSpecifier.InOnion);
			return;
		}

		if (!Physics.Raycast(
			    transform.position,
			    Vector3.down,
			    out RaycastHit hit,
			    50,
			    _MapMask,
			    QueryTriggerInteraction.Ignore
		    ))
		{
			return;
		}

		Vector3 basePos = GetRandomBaseSpawnPosition(Random.Range(0, PikminStatsManager._MaxPikminOnField + 1));
		Vector3 endPos = hit.point + basePos * _DisperseRadius;

		GameObject pikmin = Instantiate(_PikminSprout, endPos, Quaternion.identity);
		PikminSprout sproutData = pikmin.GetComponent<PikminSprout>();

		PikminSpawnData data = new() { _OriginPosition = pikmin.transform.position, _EndPosition = endPos, _Colour = colour };
		sproutData.OnSpawn(data);

		for (int i = 0; i < (int)maturity; i++)
		{
			sproutData.AdvanceMaturity();
		}

		PikminAI ai = sproutData.OnPluck();
		Destroy(sproutData.gameObject);

		bool hitFloor = Physics.Raycast(endPos + Vector3.up * 50, Vector3.down, out RaycastHit hitInfo, float.PositiveInfinity, _MapMask);
		ai.transform.position = hitFloor ? hitInfo.point + Vector3.up * 1.5f : endPos + Vector3.up * 5.0f;
	}


	/// <summary>
	///   For spawning a sprout
	/// </summary>
	/// <param name="colour">The colour of the sprout to spawn</param>
	void TryCreateSprout(PikminColour colour)
	{
		if (PikminStatsManager.GetTotalPikminOnField() == PikminStatsManager._MaxPikminOnField)
		{
			PikminStatsManager.Add(colour, PikminMaturity.Leaf, PikminStatSpecifier.InOnion);
			return;
		}

		if (!Physics.Raycast(
			    transform.position,
			    Vector3.down,
			    out RaycastHit hit,
			    50,
			    _MapMask,
			    QueryTriggerInteraction.Ignore
		    ))
		{
			return;
		}

		while (_SpawnedSprouts[_CurrentSeedIdx] != null)
		{
			_CurrentSeedIdx += Random.Range(1, 15);
			_CurrentSeedIdx %= PikminStatsManager._MaxPikminOnField;
		}

		// Get offset on a circle, converted to 3d coords
		Vector3 basePos = GetRandomBaseSpawnPosition(_CurrentSeedIdx);

		// Spawn the sprout just above the onion
		GameObject newPikmin = _SpawnedSprouts[_CurrentSeedIdx] = Instantiate(
			_PikminSprout,
			transform.position + basePos + Vector3.up * _PikminEjectionHeight,
			Quaternion.identity
		);
		PikminSprout sproutData = newPikmin.GetComponent<PikminSprout>();

		Physics.Raycast(
			Vector3.up * 15 + (hit.point + basePos * _DisperseRadius),
			Vector3.down,
			out RaycastHit hit2,
			50,
			_MapMask
		);

		PikminSpawnData data = new() { _OriginPosition = newPikmin.transform.position, _EndPosition = hit2.point, _Colour = colour };

		sproutData.OnSpawn(data);
	}

	#endregion
}
