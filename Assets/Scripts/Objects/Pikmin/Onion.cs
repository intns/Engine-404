/*
 * Onion.cs
 * Created by: Ambrosia
 * Created on: 12/4/2020 (dd/mm/yy)
 * Created for: Needing an object to store our Pikmin in
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Onion : MonoBehaviour
{
	public enum OnionType
	{
		Classic, // When first finding an onion, it will be this
		Master // Main onion that has the combination of other onions
	}

	[Header("References")]
	[SerializeField] private Canvas _OnionCanvas = null;
	[SerializeField] private TextMeshProUGUI _AmountText;

	[Header("Debug")]
	[SerializeField] private GameObject _Pikmin = null;

	[Header("Settings")]
	[SerializeField] private OnionType _Type = OnionType.Classic;
	public PikminColour _PikminColour = PikminColour.Red;
	[SerializeField] private LayerMask _MapMask = 0;
	[SerializeField] private LayerMask _PikminMask = 0;

	[Header("Dispersal")]
	[SerializeField] private float _DisperseRadius = 2;

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

	private void Awake()
	{
		_OnionCanvas.gameObject.SetActive(false);
	}

	private void Update()
	{
		if (_CanUse)
		{
			if (Input.GetButtonDown("A Button") && !_InMenu)
			{
				Player._Instance.Pause(true);
				FadeManager._Instance.FadeInOut(0.25f, 0.25f, new Action(() =>
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
				}));
			}
			else if (Input.GetButtonDown("B Button") && _InMenu)
			{
				FadeManager._Instance.FadeInOut(0.25f, 0.25f, new Action(() =>
				{
					_OnionCanvas.gameObject.SetActive(false);
					Player._Instance.Pause(false);
					_InMenu = false;
				}));
			}
		}

		// Handle in-menu input processing
		if (_InMenu)
		{
			float vInput = Input.GetAxis("Vertical") * 5;
			// Up on the stick / W
			if (vInput > 0.75f)
			{
				if (_CurPikminAmounts._InSquad > 0)
				{
					_CurPikminAmounts._InSquad--;
					_CurPikminAmounts._InOnion++;
				}
			}
			// Down on the stick / S
			else if (vInput < -0.75f)
			{
				if (_CurPikminAmounts._InOnion > 0)
				{
					_CurPikminAmounts._InOnion--;
					_CurPikminAmounts._InSquad++;
				}
			}

			if (Input.GetButtonDown("A Button")
				&& (_CurPikminAmounts._InSquad != _OldPikminAmounts._InSquad
					|| _CurPikminAmounts._InOnion != _OldPikminAmounts._InOnion))
			{
				FadeManager._Instance.FadeInOut(0.25f, 0.25f, new Action(() =>
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
				}));
			}

			_AmountText.text = $"{_CurPikminAmounts._InSquad} / {_CurPikminAmounts._InOnion}";
		}
	}

	private IEnumerator IE_SpawnPikmin(int amount)
	{
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

		if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 50, _MapMask, QueryTriggerInteraction.Ignore))
		{
			for (int i = 0; i < amount; i++)
			{
				GameObject pikminObj = Instantiate(_Pikmin, hit.point + GetRandomPikminSpawnPosition(), Quaternion.identity);
				PikminAI ai = pikminObj.GetComponent<PikminAI>();
				ai._Data._PikminColour = _PikminColour;
				ai.SetMaturity(toSpawn[i]);
				yield return new WaitForSeconds(0.02f);
			}
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
			for (int i = 0; i < 50; i++)
			{
				Vector2 offset = MathUtil.PositionInUnit(50, i);
				Gizmos.DrawSphere(hit.point + MathUtil.XZToXYZ(offset) * _DisperseRadius, 1);
			}
		}
	}

	private Vector3 GetRandomPikminSpawnPosition()
	{
		return MathUtil.XZToXYZ(MathUtil.PositionInUnit(100, UnityEngine.Random.Range(0, 100))) * _DisperseRadius;
	}

	public void EnterOnion(int toProduce, PikminColour colour)
	{
		// Create seeds to pluck...
		if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 50, _MapMask, QueryTriggerInteraction.Ignore))
		{
			for (int i = 0; i < toProduce; i++)
			{
				Instantiate(_Pikmin, hit.point + GetRandomPikminSpawnPosition(), Quaternion.identity);
			}
		}
	}
}
