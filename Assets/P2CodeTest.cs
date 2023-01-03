using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

public class P2CodeTest : Entity
{
	public override void Update()
	{
		base.Update();

		ChangeFaceDirection(Player._Instance.transform);
	}
}
