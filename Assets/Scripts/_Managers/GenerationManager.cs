using Demo;
using UnityEngine;

/*
 * Generation works as follows:
 *
 * Persistent data stays regardless of what day it is
 * Day [start] - [end] will work within ranges, e.g. "Day 1 - 5" is day 1 through to 5, the next object should be Day 6
 * Day [number] will work within a day, if this conflicts with a range, it will take precedent
 */
public class GenerationManager : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] Transform _PersistentData;

	void Awake()
	{
		int minDay = int.MaxValue;
		int maxDay = int.MinValue;

		foreach (Transform child in transform)
		{
			if (child == _PersistentData)
			{
				continue;
			}

			string[] childNameParts = child.name.Split(' ');

			if (childNameParts.Length == 4 && childNameParts[2] == "-")
			{
				if (TryParseRange(childNameParts, out int startDay, out int endDay))
				{
					minDay = Mathf.Min(minDay, startDay);
					maxDay = Mathf.Max(maxDay, endDay);

					child.gameObject.SetActive(IsCurrentDayInRange(startDay, endDay));
				}
				else
				{
					Debug.LogError("Invalid range format: " + child.name);
				}
			}
			else if (TryParseDay(childNameParts[1], out int day))
			{
				minDay = Mathf.Min(minDay, day);
				maxDay = Mathf.Max(maxDay, day);

				child.gameObject.SetActive(SaveData._CurrentData._Day == day);
			}
			else
			{
				Debug.LogError("Invalid day format: " + child.name);
			}
		}

		for (int day = 1; day <= 31; day++)
		{
			if (day < minDay || day > maxDay)
			{
				Debug.LogError("Day " + day + " is missing.");
			}
		}
	}

	static bool TryParseRange(string[] nameParts, out int startDay, out int endDay)
	{
		endDay = 0;
		return int.TryParse(nameParts[1], out startDay) && int.TryParse(nameParts[3], out endDay);
	}

	static bool TryParseDay(string name, out int day)
	{
		return int.TryParse(name, out day);
	}

	static bool IsCurrentDayInRange(int startDay, int endDay)
	{
		int currentDay = SaveData._CurrentData._Day;
		return currentDay >= startDay && currentDay <= endDay;
	}
}
