/*
 * IPikminAttack.cs
 * Created by: Ambrosia
 * Created on: 30/4/2020 (dd/mm/yy)
 */

public interface IPikminAttack : IPikminInteractable {
  // Called when the Pikmin starts attacking the object, like when latched
  void OnAttackStart (PikminAI pikmin);
  // Called when the Pikmin changes states, or the object dies
  void OnAttackEnd (PikminAI pikmin);

  // Called when the Pikmin hits the object
  void OnAttackRecieve (float damage);
}
