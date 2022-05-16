
public interface IPikminPush : IPikminInteractable
{
  // Called when a Pikmin starts trying to push the object
  void OnPikminAdded(PikminAI p);

  // Called when the Pikmin gets called by the whistle, or reached it's destination
  void OnPikminLeave(PikminAI p);


  // Returns if there is a spot for the Pikmin to move towards
  bool PikminSpotAvailable();
}