using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TextBox_Page
{
	[TextArea(15, 20)]
	public string _Text;
}

[CreateAssetMenu(fileName = "NewTextBoxEntry", menuName = "New Text Box Entry")]
public class TextBoxEntry : ScriptableObject
{
	public List<TextBox_Page> _Pages = new List<TextBox_Page>();
}