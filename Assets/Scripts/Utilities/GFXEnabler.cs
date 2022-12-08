/*
 * GFXEnabler.cs
 * Created by: Ambrosia
 * Created on: 10/2/2020 (dd/mm/yy)
 * Created for: disabling and enabling PostProcessing
 */

using UnityEngine;
using UnityEngine.Rendering;

public class GFXEnabler : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] KeyCode _OptionMenuKey = KeyCode.Return;
	[SerializeField] Volume _PostProcessVolume = null;

	void Update()
	{
		if (Input.GetKeyDown(_OptionMenuKey))
		{
			_PostProcessVolume.enabled = !_PostProcessVolume.enabled;
		}
	}
}
