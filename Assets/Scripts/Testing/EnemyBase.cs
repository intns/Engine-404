using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyFlags
{
	IsFlying = 0,       // Is it flying?
	isLifeguageVisible, // Is the life guage visible?
	ShouldRevive,       // Should it revive itself? (gatling groink or bulbear) 
	ShouldLeaveCarcass, // Should it leave a dead body after dying?
}

[System.Serializable]
public class EnemyBase : EntityBase
{
	public EnemyFlags Flags { get; set; }

	/// NOTE: Inherited enemies will have to override the become-carcass function
	/// with their own code of how the carcass will spawn and act, etc.

}
