public interface IHealth
{
	float AddHealth(float h);
	float GetCurrentHealth();
	float GetMaxHealth();

	void SetHealth(float h);

	// Returns the new value
	float SubtractHealth(float h);
}
