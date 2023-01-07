
using UnityEngine;

public interface IPikminPush : IPikminInteractable
{
	// Called when a Pikmin starts trying to push the object
	bool OnPikminAdded(PikminAI p);

	// Called when the Pikmin gets called by the whistle, or reached it's destination
	void OnPikminLeave(PikminAI p);

	Vector3 GetPushPosition(PikminAI p);

	void OnPikminReady(PikminAI p);


	// Returns if there is a spot for the Pikmin to move towards
	bool IsPikminSpotAvailable();

	// Get the direction the object is going to move towards
	public Vector3 GetMovementDirection();
}