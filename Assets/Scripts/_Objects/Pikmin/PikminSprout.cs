using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PikminSproutState
{
	Dropping,
	Planted
}

public class PikminSproutPath
{
	private Vector3 startPosition, endPosition, distance;
	private double height, width;
	private double position, speed;

	public PikminSproutPath(Vector3 startPosition, Vector3 endPosition, double speed, double height)
	{
		this.startPosition = startPosition;
		this.endPosition = endPosition;
		this.speed = speed;
		this.height = height;
		distance = endPosition - startPosition;
		width = .5;
	}

	public Vector3 GetPosition()
	{
		position += GetSpeed() * Time.fixedDeltaTime;
		return startPosition + new Vector3((float)GetXPosition(), (float)GetYPosition(), (float)GetZPosition());
	}

	public double GetSpeed()
	{
		return speed;
	}

	public Vector3 GetEndPosition()
	{
		return endPosition;
	}

	public bool IsFinished()
	{
		return position >= 1.0;
	}

	private double GetXPosition()
	{
		return position * distance.x;
	}

	private double GetYPosition()
	{
		double yOffset = distance.y * position;
		double yPosition = GetSquareRoot();
		if (double.IsNaN(yPosition)) yPosition = 0;
		return yPosition + yOffset;
	}

	private double GetZPosition()
	{
		return position * distance.z;
	}

	private double GetSquareRoot()
	{
		double fraction = Squared(position - width) / Squared(width);
		double toBeSquareRooted = (1 - fraction) * Squared(height);
		return Math.Sqrt(toBeSquareRooted);
	}

	private double Squared(double value)
	{
		return Math.Pow(value, 2);
	}
}

public class PikminSprout : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] float _PeakHeight = 5;
	[SerializeField] float _DropSpeed = 0.2f;
	[Space]
	[SerializeField] float _TimeNeededForBud = 180;
	[SerializeField] float _TimeNeededForFlower = 240;

	[Header("Components")]
	[SerializeField] GameObject _RedPikminPrefab;
	[SerializeField] GameObject _BluePikminPrefab;
	[SerializeField] GameObject _YellowPikminPrefab;
	[Space]
	[SerializeField] GameObject _LeafHeadPrefab;
	[SerializeField] GameObject _BudHeadPrefab;
	[SerializeField] GameObject _FlowerHeadPrefab;
	[Space]
	[SerializeField] Transform _MaturityPlaceholderObj;

	[Header("Debug")]
	[SerializeField] PikminSproutState _CurrentState;
	[SerializeField] PikminSpawnData _SpawnData;

	PikminMaturity _Maturity = PikminMaturity.Leaf;
	GameObject[] _MaturityObjects = new GameObject[3];

	ParticleSystem _ParticleSystem = null;
	MeshRenderer _MeshRenderer = null;

	Vector3 _OldPosition = Vector3.zero;
	float _Timer = 0;

	private void OnEnable()
	{
		_CurrentState = PikminSproutState.Dropping;

		_MaturityObjects[0] = Instantiate(_LeafHeadPrefab, _MaturityPlaceholderObj);

		_MaturityObjects[1] = Instantiate(_BudHeadPrefab, _MaturityPlaceholderObj);
		_MaturityObjects[1].SetActive(false);

		_MaturityObjects[2] = Instantiate(_FlowerHeadPrefab, _MaturityPlaceholderObj);
		_MaturityObjects[2].SetActive(false);

		foreach (GameObject obj in _MaturityObjects)
		{
			obj.transform.localPosition = Vector3.up * 0.35f;
		}

		_MeshRenderer = GetComponent<MeshRenderer>();
		_ParticleSystem = GetComponent<ParticleSystem>();

		_TimeNeededForBud += UnityEngine.Random.Range(60, -30);
		_TimeNeededForFlower += UnityEngine.Random.Range(60, -30);
	}

	private void Update()
	{
		if (GameManager._IsPaused)
		{
			return;
		}

		switch (_CurrentState)
		{
			case PikminSproutState.Dropping:
				transform.rotation = Quaternion.identity;
				break;
			case PikminSproutState.Planted:
				_Timer += Time.deltaTime;

				if (_Timer >= _TimeNeededForBud && _Maturity == PikminMaturity.Leaf)
				{
					PromoteMaturity();
				}
				else if (_Timer >= _TimeNeededForFlower && _Maturity == PikminMaturity.Bud)
				{
					PromoteMaturity();
				}
				break;
			default:
				break;
		}

		_OldPosition = transform.position;
	}

	public PikminAI OnPluck()
	{
		GameObject toInstantiate = null;
		switch (_SpawnData._Colour)
		{
			case PikminColour.Red:
				toInstantiate = _RedPikminPrefab;
				break;
			case PikminColour.Yellow:
				toInstantiate = _YellowPikminPrefab;
				break;
			case PikminColour.Blue:
				toInstantiate = _BluePikminPrefab;
				break;
			default:
				break;
		}

		// Remove from on field because we're about to be added
		//  to the squad when the Pikmin is instantiated
		PikminStatsManager.Remove(_SpawnData._Colour, _Maturity, PikminStatSpecifier.OnField);

		GameObject newPiki = Instantiate(toInstantiate, transform.position, Quaternion.identity);
		PikminAI ai = newPiki.GetComponent<PikminAI>();
		ai.SetMaturity(_Maturity);
		return ai;
	}

	public void PromoteMaturity()
	{
		// Cap at 3
		if (_Maturity == PikminMaturity.Flower)
		{
			return;
		}

		_MaturityObjects[(int)_Maturity].SetActive(false);
		PikminStatsManager.Remove(_SpawnData._Colour, _Maturity, PikminStatSpecifier.OnField);

		_Maturity = (PikminMaturity)((int)_Maturity + 1);

		_MaturityObjects[(int)_Maturity].SetActive(true);
		PikminStatsManager.Add(_SpawnData._Colour, _Maturity, PikminStatSpecifier.OnField);
	}

	public void OnSpawn(PikminSpawnData data)
	{
		_SpawnData = data;
		_MeshRenderer.material.color = GameUtil.PikminColorToColor(data._Colour);

		ParticleSystem.MainModule settings = _ParticleSystem.main;
		settings.startColor = _MeshRenderer.material.color;

		PikminStatsManager.Add(_SpawnData._Colour, _Maturity, PikminStatSpecifier.OnField);
		StartCoroutine(IE_DropAnimation());
	}

	public bool CanPluck()
	{
		return _CurrentState == PikminSproutState.Planted;
	}

	// Handles the animation from the onion origin towards the floor placement
	IEnumerator IE_DropAnimation()
	{
		PikminSproutPath path = new PikminSproutPath(_SpawnData._OriginPosition,
			_SpawnData._EndPosition, _DropSpeed, _PeakHeight);

		transform.position = _SpawnData._OriginPosition;
		_CurrentState = PikminSproutState.Dropping;

		Vector3 nextPosition = _SpawnData._OriginPosition;
		while (!path.IsFinished())
		{
			transform.up = transform.position - nextPosition;
			transform.position = nextPosition;
			nextPosition = path.GetPosition();
			yield return null;
		}

		transform.position = path.GetEndPosition();
		_CurrentState = PikminSproutState.Planted;

		yield return null;
	}
}
