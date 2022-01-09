/*
 * Billboard.cs
 * Created by: Ambrosia
 * Created on: 15/2/2020 (dd/mm/yy)
 * Created for: needing a script that forced an object to constantly look at the Camera
 */

using UnityEngine;

public class Billboard : MonoBehaviour {
  private Camera _MainCamera;

  private void Awake () {
    _MainCamera = Camera.main;
  }

  private void Update () {
    transform.LookAt (transform.position + _MainCamera.transform.forward);
  }
}
