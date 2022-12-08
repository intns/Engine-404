/*
 * Billboard.cs
 * Created by: Ambrosia
 * Created on: 15/2/2020 (dd/mm/yy)
 * Created for: needing a script that forced an object to constantly look at the Camera
 */

using UnityEngine;

public class Billboard : MonoBehaviour
{
	Camera _MainCamera;

	void Awake()
	{
		_MainCamera = Camera.main;
	}

	void Update()
	{
		transform.LookAt(transform.position + _MainCamera.transform.forward);
	}
}
