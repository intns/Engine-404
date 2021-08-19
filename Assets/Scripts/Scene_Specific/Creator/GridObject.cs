using UnityEngine;

public enum GO_State
{
	Selected,
	Unselected,
	Mesh,
}

public class GridObject : MonoBehaviour
{
	[Header("Debugging")]
	public GameObject _Mesh = null;
	public GridMesh _GridMesh = null;

	public Vector2Int _Position = Vector2Int.zero;
	private Renderer _Renderer = null;

	private void Awake()
	{
		_Renderer = GetComponent<Renderer>();
	}

	public void SetState(GO_State state)
	{
		switch (state)
		{
			case GO_State.Selected:
				_Renderer.material.SetColor("_BaseColor", Color.red);
				break;
			case GO_State.Unselected:
				_Renderer.material.SetColor("_BaseColor", Color.white);
				break;
			case GO_State.Mesh:
				_Renderer.material.SetColor("_BaseColor", Color.green);
				break;
			default:
				break;
		}
	}

	public void SetupMesh(float size, Vector2Int gridSize)
	{
		if (_Mesh != null || _Position.x == gridSize.x - 1 || _Position.y == gridSize.y - 1)
		{
			return;
		}

		_Mesh = GameObject.CreatePrimitive(PrimitiveType.Plane);
		_Mesh.transform.localScale = new Vector3(size / 10, 1, size / 10);
		_Mesh.transform.position = new Vector3(transform.position.x + (size / 2), transform.position.y, transform.position.z + (size / 2));

		// Add GridMesh
		_GridMesh = _Mesh.AddComponent<GridMesh>();
		// Change color to solid white
		_Mesh.GetComponent<Renderer>().material.SetColor("_BaseColor", Color.white);
	}

	public void DestroyMesh()
	{
		if (_Mesh == null)
		{
			return;
		}

		Destroy(_Mesh);
		// gridmesh automatically gets set to null
	}
}
