/*
 * IPikminInteractable.cs
 * Created by: Ambrosia
 * Created on: 1/5/2020 (dd/mm/yy)
 */

// Interface used when an object wants to be fully interactable with the PikminAI class
public interface IPikminInteractable
{
	// What the Pikmin does to/for this object (pull weeds, attack, carry, etc.)
	PikminIntention IntentionType { get; }
}
