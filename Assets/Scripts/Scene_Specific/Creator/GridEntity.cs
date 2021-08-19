using TMPro;
using UnityEngine;

public class GridEntity : MonoBehaviour
{
	public TextMeshPro _Text = null;
	public GameObject _TextObject = null;

	public GridEntity()
	{
		_TextObject = new GameObject();
		_Text = _TextObject.AddComponent<TextMeshPro>();
		_Text.fontSize = 12;
		_Text.alignment = TextAlignmentOptions.Midline;
	}

	public void SetText(string txt)
	{
		_Text.text = txt;
	}
}
