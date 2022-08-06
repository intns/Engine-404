using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles Unity Animator and Animations as part of a single "class"
/// </summary>
[System.Serializable]
class AnimationController
{
	/// <summary>
	/// Adds a state to the list of Animations
	/// </summary>
	/// <returns></returns>
	public int AddState(AnimationClip anim)
	{
		_Animations.Add(anim);
		return _Animations.Count-1;
	}


	/// <summary>
	/// Changes the current state of the animator to that of animIdx
	/// </summary>
	/// <param name="animIdx"></param>
	public void ChangeState(int animIdx)
	{
		if (_CurrentState == animIdx)
		{
			return;
		}

		_CurrentState = animIdx;
		_ParentAnimator.Play(_Animations[animIdx].name, 0);
	}


	public Animator _ParentAnimator;

	private List<AnimationClip> _Animations = new List<AnimationClip>();
	private int _CurrentState = 0;

}