/*
 * Plant.cs
 * Created by: Ambrosia & Kman
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
	[SerializeField] private AudioClip _InteractSound = null;
	private SphereCollider _Collider = null;
	private AudioSource _Audio = null;
	private Animator _Animator = null;

	[Header("Settings")]
	// The prepended I expands to 'Interaction'
	[SerializeField] private LayerMask _ILayers = 0;
	[SerializeField] private Vector3 _IOffset = Vector3.forward;
	[SerializeField] private float _IForwardOffset = 0;
	[SerializeField] private float _IRadius = 2.5f;

	[Header("Animation Randomization")]
	// Sets this to a random float between 0 and 0.75 so the plants don't look the same
	[SerializeField] private Vector2 _AnimStartRandBounds = Vector2.up;
	private readonly List<Collider> _Interacting = new List<Collider>();

	private void Awake()
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

	private IEnumerator StartAnimationAfterTime()
	{
		yield return new WaitForSeconds(Random.Range(_AnimStartRandBounds.x, _AnimStartRandBounds.y));
		_Animator.enabled = true;

		yield return null;
	}

	private void OnTriggerEnter(Collider other)
	{
		// Check if the layer is within the interacting layers
		int layer = other.gameObject.layer;
		if (_ILayers != (_ILayers | (1 << layer)))
		{
			return;
		}

		_Interacting.Add(other);

		_Animator.SetBool("Shake", true);
	}

	private void OnTriggerExit(Collider other)
	{
		// Check if the layer is within the interacting layers
		int layer = other.gameObject.layer;
		if (_ILayers != (_ILayers | (1 << layer)))
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
}
