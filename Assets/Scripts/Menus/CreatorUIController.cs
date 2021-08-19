using UnityEngine;

public class CreatorUIController : BaseUIController
{
	// for actual UI go to Scripts/Scene_Specific/Creator/GridManager.cs

	[SerializeField] private GameObject _Canvas = null;
	[SerializeField] private GameObject _GridManager = null;
	[SerializeField] private GridCamera _GridCamera = null;

	private void OnEnable()
	{
		_GridManager.SetActive(false);
		_GridCamera.enabled = false;
	}

	public void OnAcknowledge()
	{
		_Canvas.SetActive(false);
		_GridManager.SetActive(true);
		_GridCamera.enabled = true;
	}
}
