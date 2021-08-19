using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum GM_Direction
{
	RIGHT,
	LEFT,
	UP,
	DOWN
}

public enum GM_Stage
{
	MeshCreation,
	Texturing,
	EntityPlacement,
	Compiling,
	Play
}

public class GridManager : MonoBehaviour
{
	public static GridManager _Instance = null;

	[Header("Stage Specific")]
	[SerializeField] private Texture _GrassTexture = null;
	[SerializeField] private Texture _StoneTexture = null;
	[SerializeField] private Texture _SandTexture = null;
	[SerializeField] private TMP_Dropdown _DropDown = null;

	[SerializeField] private GameObject _PlayerDummyPrefab = null;
	private readonly GameObject _PlacedPlayer = null;
	[SerializeField] private GameObject _PikminDummyPrefab = null;
	private readonly GameObject[] _PlacedPikmin = null;
	[SerializeField] private GameObject _OnionDummyPrefab = null;
	private readonly GameObject _PlacedOnion = null;

	[Header("UI Components")]
	[SerializeField] private GameObject _UIMeshCreation = null;
	[SerializeField] private GameObject _UITexturing = null;
	[SerializeField] private GameObject _UIEntityPlacement = null;
	[SerializeField] private GameObject _UICompiling = null;

	[Header("Settings")]
	[SerializeField] private GameObject[] _DeleteOnPlay = null;
	public GM_Stage _Stage = GM_Stage.MeshCreation;

	public int _GridObjectSize = 5;
	[SerializeField] private Vector2Int _GridSize;
	[SerializeField] private Vector2Int _CurrentPosition;

	[Header("Debugging")]
	[SerializeField] private bool _ResetPositionInDebugger = false;

	public GridObject _CurrentObject = null;
	public List<GridObject> _GridObjects = new List<GridObject>();
	private GameObject _GridParent = null;

	public GridManager()
	{
		_Instance = this;
	}

	private void Awake()
	{
		_GridParent = new GameObject("GRID_PARENT");
		for (int x = 0; x < _GridSize.x; x++)
		{
			for (int y = 0; y < _GridSize.y; y++)
			{
				GameObject newObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				newObj.transform.position = new Vector3(_GridObjectSize * x, 0, _GridObjectSize * y);
				newObj.transform.SetParent(_GridParent.transform);
				newObj.name = $"grid_obj({x},{y})";

				GridObject obj = newObj.AddComponent<GridObject>();
				obj._Position = new Vector2Int(x, y);
				_GridObjects.Add(obj);
				if (obj._Position == _CurrentPosition)
				{
					_CurrentObject = obj;
					_CurrentObject.SetState(GO_State.Selected);
				}
			}
		}

		HandleStageChange(GM_Stage.MeshCreation);
	}

	private void Update()
	{
		if (_Stage == GM_Stage.MeshCreation)
		{
			if (Input.GetMouseButtonDown(0))
			{
				_CurrentObject.SetupMesh(_GridObjectSize, _GridSize);
			}
			else if (Input.GetMouseButtonDown(1))
			{
				_CurrentObject.DestroyMesh();
			}
		}

		if (_Stage == GM_Stage.EntityPlacement)
		{

		}

		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			HandleStageChange(GM_Stage.MeshCreation);
		}
		else if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			HandleStageChange(GM_Stage.Texturing);
		}
		else if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			HandleStageChange(GM_Stage.EntityPlacement);
		}
		else if (Input.GetKeyDown(KeyCode.Alpha4))
		{
			HandleStageChange(GM_Stage.Compiling);
		}
		else if (Input.GetKeyDown(KeyCode.Alpha5))
		{
			HandleStageChange(GM_Stage.Play);
		}
	}

	private void OnDrawGizmos()
	{
		if (_ResetPositionInDebugger)
		{
			_CurrentPosition = new Vector2Int(Mathf.FloorToInt(_GridSize.x / 2), Mathf.FloorToInt(_GridSize.y / 2));

			_ResetPositionInDebugger = false;
		}

		for (int x = 0; x < _GridSize.x; x++)
		{
			for (int y = 0; y < _GridSize.y; y++)
			{
				// Draw current position
				if (x == _CurrentPosition.x && y == _CurrentPosition.y)
				{
					Gizmos.color = Color.red;
					Gizmos.DrawSphere(new Vector3(_GridObjectSize * x, 0, _GridObjectSize * y), 1);
					Gizmos.color = Color.white;
					continue;
				}

				Gizmos.DrawSphere(new Vector3(_GridObjectSize * x, 0, _GridObjectSize * y), 1);
			}
		}
	}

	public void SetCurrentPosition(GM_Direction dir)
	{
		switch (dir)
		{
			case GM_Direction.RIGHT:
				_CurrentPosition.x++;
				break;
			case GM_Direction.LEFT:
				_CurrentPosition.x--;
				break;
			case GM_Direction.UP:
				_CurrentPosition.y++;
				break;
			case GM_Direction.DOWN:
				_CurrentPosition.y--;
				break;
			default:
				break;
		}

		_CurrentPosition.x = Mathf.Clamp(_CurrentPosition.x, 0, _GridSize.x - 1);
		_CurrentPosition.y = Mathf.Clamp(_CurrentPosition.y, 0, _GridSize.y - 1);

		if (_CurrentObject._Mesh != null)
		{
			_CurrentObject.SetState(GO_State.Mesh);
		}
		else
		{
			_CurrentObject.SetState(GO_State.Unselected);
		}

		foreach (GridObject obj in _GridObjects)
		{
			if (obj._Position == _CurrentPosition)
			{
				_CurrentObject = obj;
				break;
			}
		}

		_CurrentObject.SetState(GO_State.Selected);
	}

	private void HandleStageChange(GM_Stage stage)
	{
		// Handle old stage
		switch (_Stage)
		{
			case GM_Stage.MeshCreation:
				_UIMeshCreation.SetActive(false);
				break;
			case GM_Stage.Texturing:
				_UITexturing.SetActive(false);
				foreach (GridObject obj in _GridObjects)
				{
					if (obj._GridMesh != null)
					{
						obj._GridMesh._Renderer.material.SetColor("_BaseColor", Color.white);
					}
				}
				break;
			case GM_Stage.EntityPlacement:
				_UIEntityPlacement.SetActive(false);
				break;
			case GM_Stage.Compiling:
				_UICompiling.SetActive(false);
				break;
			default:
				break;
		}

		_Stage = stage;

		// Handle new stage
		switch (_Stage)
		{
			case GM_Stage.MeshCreation:
				_UIMeshCreation.SetActive(true);
				break;
			case GM_Stage.Texturing:
				_UITexturing.SetActive(true);
				break;
			case GM_Stage.EntityPlacement:
				_UIEntityPlacement.SetActive(true);
				break;
			case GM_Stage.Compiling:
				_UICompiling.SetActive(true);
				break;
			case GM_Stage.Play:
				Globals._FadeManager.FadeInOut(1, 1, new Action(() =>
				{
					for (int i = 0; i < _DeleteOnPlay.Length; i++)
					{
						Destroy(_DeleteOnPlay[i]);
					}
					Destroy(_GridParent);

					GameObject player = null;
					GameObject sceneMaster = null;
					SceneHelper.SetupNewScene(ref player, ref sceneMaster);
					player.transform.position = GetCurrentPosition() + Vector3.up;
					Destroy(gameObject);
				}));
				break;
			default:
				break;
		}

		Debug.Log(_Stage.ToString());
	}


	public Vector3 GetCurrentPosition()
	{
		return new Vector3(_GridObjectSize * _CurrentPosition.x, 0, _GridObjectSize * _CurrentPosition.y);
	}

	#region Stage Handling
	public Texture Texturing_GetTex()
	{
		TMP_Dropdown.OptionData opt = _DropDown.options[_DropDown.value];
		if (opt.text == "Grass")
		{
			return _GrassTexture;
		}
		else if (opt.text == "Stone")
		{
			return _StoneTexture;
		}
		else if (opt.text == "Sand")
		{
			return _SandTexture;
		}
		return null;
	}
	#endregion
}
