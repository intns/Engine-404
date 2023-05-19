using Demo;
using UnityEngine;

/*
 * Generation works as follows:
 *
 * Persistent data stays regardless of what day it is
 * Day [start] - [end] will work within ranges, e.g. "Day 1 - 5" is day 1 through to 5, the next object should be Day 6
 * Day [number], aka CONSTANT, will only enable a singular game object. If this constant number falls within the range of another game object, this game object will stay enabled, and the range will be disabled.
 */
public class GenerationManager : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] Transform _PersistentData;

	void Awake()
	{
		foreach (Transform child in transform)
		{
			// Ignore persistent data
			if (child == _PersistentData)
			{
				continue;
			}

			string rangeString = child.name;
			string[] splitStr = rangeString.Split(' ');

			if (splitStr.Length == 4)
			{
				// Range: Day 1 - 31 (for example)
				if (int.TryParse(splitStr[1], out int start) && int.TryParse(splitStr[3], out int end))
				{
					int currentDay = SaveData._CurrentData._Day;
					child.gameObject.SetActive(currentDay >= start && currentDay <= end);
				}
				else
				{
					Debug.LogError("Invalid range format: " + rangeString);
				}
			}
			else if (splitStr.Length == 2)
			{
				// Constant: Day 1 (for example)
				if (int.TryParse(splitStr[1], out int constantDay))
				{
					bool isActive = SaveData._CurrentData._Day == constantDay;
					bool takePrecedence = false;

					if (isActive)
					{
						foreach (Transform otherChild in transform)
						{
							if (otherChild == child || otherChild == _PersistentData)
								continue;

							string otherRangeString = otherChild.name;
							string[] otherSplitStr = otherRangeString.Split(' ');

							if (otherSplitStr.Length == 4)
							{
								if (int.TryParse(otherSplitStr[1], out int otherStart) && int.TryParse(otherSplitStr[3], out int otherEnd))
								{
									if (constantDay < otherStart || constantDay > otherEnd)
									{
										continue;
									}

									takePrecedence = true;
									otherChild.gameObject.SetActive(false);
								}
							}
						}
					}

					child.gameObject.SetActive(isActive || takePrecedence);
				}
				else
				{
					Debug.LogError("Invalid constant format: " + rangeString);
				}
			}
			else
			{
				Debug.LogError("Invalid format: " + rangeString);
			}
		}
	}
}
