using UnityEngine;

public class GridCamera : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] private Vector2 _OffsetFromPoint = new Vector2(5, 7.5f);
	[SerializeField] private float _Speed = 5;
	private GridManager _GridManager = null;

	private void Awake()
	{
		_GridManager = GridManager._Instance;

		Vector3 originPos = _GridManager.GetCurrentPosition();
		Vector3 position = new Vector3(originPos.x, _OffsetFromPoint.y, originPos.z - _OffsetFromPoint.x);
		transform.position = position;
		transform.LookAt(originPos);
	}

	private void Update()
	{
		Vector3 originPos = _GridManager.GetCurrentPosition();
		Vector3 position = new Vector3(originPos.x, _OffsetFromPoint.y, originPos.z - _OffsetFromPoint.x);
		transform.position = Vector3.Lerp(transform.position, position, _Speed * Time.deltaTime);

		if (Input.GetKeyDown(KeyCode.W))
		{
			_GridManager.SetCurrentPosition(GM_Direction.UP);
		}
		else if (Input.GetKeyDown(KeyCode.A))
		{
			_GridManager.SetCurrentPosition(GM_Direction.LEFT);
		}
		else if (Input.GetKeyDown(KeyCode.S))
		{
			_GridManager.SetCurrentPosition(GM_Direction.DOWN);
		}
		else if (Input.GetKeyDown(KeyCode.D))
		{
			_GridManager.SetCurrentPosition(GM_Direction.RIGHT);
		}
	}
}
