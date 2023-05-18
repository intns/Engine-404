using System.IO;
using UnityEngine;

public static class SaveManager
{
	public static T LoadData<T>(string fileName)
	{
		string savePath = GetSavePath(fileName);
		T loadedData = default;

		try
		{
			// Check if the save file exists
			if (File.Exists(savePath))
			{
				// Read the JSON data from the save file
				using (StreamReader reader = new(savePath))
				{
					string jsonData = reader.ReadToEnd();
					// Convert the JSON data back to object of type T
					loadedData = JsonUtility.FromJson<T>(jsonData);
				}

				Debug.Log("Data loaded successfully.");
			}
			else
			{
				Debug.LogWarning("Save file not found.");
			}
		}
		catch (IOException e)
		{
			Debug.LogError($"Failed to load data: {e.Message}");
		}

		return loadedData;
	}

	public static void SaveData<T>(T data, string fileName)
	{
		string savePath = GetSavePath(fileName);

		try
		{
			// Convert the data to JSON format
			string jsonData = JsonUtility.ToJson(data);

			// Write the JSON data to the save file
			using (StreamWriter writer = new(savePath))
			{
				writer.Write(jsonData);
			}

			Debug.Log("Data saved successfully.");
		}
		catch (IOException e)
		{
			Debug.LogError($"Failed to save data: {e.Message}");
		}
	}

	static string GetSavePath(string fileName)
	{
		string saveDirectory = Application.persistentDataPath;
		string savePath = Path.Combine(saveDirectory, fileName);

		return savePath;
	}
}
