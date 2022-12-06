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
	public void ChangeState(int animIdx, bool finishAnim = false)
	{
		if (_CurrentState == animIdx)
		{
			return;
		}

		if (_FinishCurrent)
		{
			// Check the animator has finished the clip
			AnimatorClipInfo[] clipsInfo = _ParentAnimator.GetCurrentAnimatorClipInfo(0);
			AnimatorStateInfo stateInfo = _ParentAnimator.GetCurrentAnimatorStateInfo(0);
			if (stateInfo.normalizedTime >= clipsInfo[0].clip.length)
			{
				_FinishCurrent = false;
			}
			else
			{
				// It hasn't finished the clip yet, so we return and wait
				return;
			}
		}

		if (finishAnim)
		{
			_FinishCurrent = true;
		}


		_CurrentState = animIdx;
		_ParentAnimator.Play(_Animations[animIdx].name, 0, 0.5f);
	}


	public Animator _ParentAnimator;

	private List<AnimationClip> _Animations = new List<AnimationClip>();
	private int _CurrentState = 0;
	private bool _FinishCurrent = false;
}