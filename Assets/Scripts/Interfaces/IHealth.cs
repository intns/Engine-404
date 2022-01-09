public interface IHealth {
  float GetCurrentHealth ();
  float GetMaxHealth ();

  void SetHealth (float h);

  // Returns the new value
  float SubtractHealth (float h);
  float AddHealth (float h);
}
