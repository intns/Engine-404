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
		return _Animations.Count - 1;
	}


	/// <summary>
	/// Changes the current state of the animator to that of animIdx
	/// </summary>
	/// <param name="animIdx"></param>
	public void ChangeState(int animIdx, bool finishAnim = false, bool overRide = false)
	{
		if (_CurrentState == animIdx)
		{
			return;
		}

		if (!overRide)
		{
			if (_FinishCurrent)
			{
				// Check the animator has finished the clip
				AnimatorStateInfo stateInfo = _ParentAnimator.GetCurrentAnimatorStateInfo(0);

				if (stateInfo.normalizedTime < 1)
				{
					// It hasn't finished the clip yet, so we return and wait
					return;
				}

				_FinishCurrent = false;
			}

			if (finishAnim)
			{
				_FinishCurrent = true;
			}
		}
		else
		{
			_FinishCurrent = finishAnim;
		}


		_CurrentState = animIdx;
		_ParentAnimator.Play(_Animations[animIdx].name, 0, 0.5f);
	}


	public Animator _ParentAnimator;

	List<AnimationClip> _Animations = new List<AnimationClip>();
	int _CurrentState = 0;
	bool _FinishCurrent = false;
}