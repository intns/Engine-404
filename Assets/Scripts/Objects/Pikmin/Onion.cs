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
	[Header("References")]
	[SerializeField] private Canvas _OnionCanvas = null;
	[SerializeField] private TextMeshProUGUI _InOnionText;
	[SerializeField] private TextMeshProUGUI _InSquadText;
	[SerializeField] private TextMeshProUGUI _InFieldText;
	[SerializeField] private CanvasGroup _CanvasGroup = null;

	[Header("Debug")]
	[SerializeField] private GameObject _Pikmin = null;

	[Header("Settings")]
	[SerializeField] private LayerMask _MapMask = 0;
	[SerializeField] private LayerMask _PikminMask = 0;
	public PikminColour _PikminColour { get; private set; } = PikminColour.Red;

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
				// Up on the stick / W
				switch (vInput)
				{
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

		if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 50, _MapMask, QueryTriggerInteraction.Ignore))
		{
			for (int i = 0; i < amount; i++)
			{
				_CanUse = false;
				TryCreatePikmin(hit.point + GetRandomPikminSpawnPosition(), _PikminColour, toSpawn[i]);
				yield return new WaitForSeconds(0.02f);
			}
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
				TryCreatePikmin(hit.point + GetRandomPikminSpawnPosition(), colour, PikminMaturity.Leaf);
			}
		}
	}

	private void TryCreatePikmin(Vector3 position, PikminColour colour, PikminMaturity maturity)
	{
		if (PikminStatsManager.GetTotalOnField() == 100)
		{
			PikminStatsManager.Add(colour, maturity, PikminStatSpecifier.InOnion);
		}
		else
		{
			GameObject pikmin = Instantiate(_Pikmin, position, Quaternion.identity);
			PikminAI ai = pikmin.GetComponent<PikminAI>();
			ai._Data._PikminColour = colour;
			ai.SetMaturity(maturity);
		}
	}
}
