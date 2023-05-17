/*
 * Plant.cs
 * Created by: Ambrosia, Helodity
 * Created on: 10/4/2020 (dd/mm/yy)
 * Created for: needing plants to be animated
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider), typeof(AudioSource), typeof(Animator))]
public class Plant : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] AudioClip _InteractSound;

	[Header("Settings")]
	// The prepended I expands to 'Interaction'
	[SerializeField] LayerMask _ILayers = 0;
	[SerializeField] Vector3 _IOffset = Vector3.forward;
	[SerializeField] float _IForwardOffset;
	[SerializeField] float _IRadius = 2.5f;

	[Header("Animation Randomization")]
	// Sets this to a random float between 0 and 0.75 so the plants don't look the same
	[SerializeField] Vector2 _AnimStartRandBounds = Vector2.up;
	readonly List<Collider> _Interacting = new();
	Animator _Animator;
	AudioSource _Audio;
	SphereCollider _Collider;

	void Awake()
	{
		// Set the collider radius to the interaction radius, if theres a mismatch between the two in scene this'll fix it
		_Collider = GetComponent<SphereCollider>();
		_Collider.radius = _IRadius;
		_Collider.center = _IOffset + transform.forward * _IForwardOffset;

		_Animator = GetComponent<Animator>();
		_Animator.enabled = false;
		StartCoroutine(StartAnimationAfterTime());

		_Audio = GetComponent<AudioSource>();
	}

	void OnTriggerEnter(Collider other)
	{
		// Check if the layer is within the interacting layers
		int layer = other.gameObject.layer;

		if (_ILayers != (_ILayers | 1 << layer))
		{
			return;
		}

		_Interacting.Add(other);

		_Animator.SetBool("Shake", true);
	}

	void OnTriggerExit(Collider other)
	{
		// Check if the layer is within the interacting layers
		int layer = other.gameObject.layer;

		if (_ILayers != (_ILayers | 1 << layer))
		{
			return;
		}

		_Interacting.Remove(other);

		if (_Interacting.Count == 0)
		{
			_Animator.SetBool("Shake", false);
		}
	}

	public void PlayShakeAudio()
	{
		_Audio.clip = _InteractSound;
		_Audio.Play();
	}

	IEnumerator StartAnimationAfterTime()
	{
		yield return new WaitForSeconds(Random.Range(_AnimStartRandBounds.x, _AnimStartRandBounds.y));
		_Animator.enabled = true;

		yield return null;
	}
}
