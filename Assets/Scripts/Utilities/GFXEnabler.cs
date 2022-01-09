/*
 * GFXEnabler.cs
 * Created by: Ambrosia
 * Created on: 10/2/2020 (dd/mm/yy)
 * Created for: disabling and enabling PostProcessing
 */

using UnityEngine;
using UnityEngine.Rendering;

public class GFXEnabler : MonoBehaviour {
  [Header ("Components")]
  [SerializeField] private KeyCode _OptionMenuKey = KeyCode.Return;
  [SerializeField] private Volume _PostProcessVolume = null;

  private void Update () {
    if (Input.GetKeyDown (_OptionMenuKey)) {
      _PostProcessVolume.enabled = !_PostProcessVolume.enabled;
    }
  }
}
