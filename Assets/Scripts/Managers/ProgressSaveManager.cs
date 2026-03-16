using System;
using System.IO;
using UnityEngine;

// This static class handles saving and loading progress data
// for the currently logged-in user using JSON files.

public static class ProgressSaveManager
{
    // Returns the correct file path for a specific user ID
    private static string GetFilePath(int userId)
    {
        return Path.Combine(Application.persistentDataPath, "progress_" + userId + ".json");
    }

    // Saves progress data for a specific user
    public static void SaveProgress(ProgressData progressData)
    {
        string filePath = GetFilePath(progressData.userId);

        // Update last saved time before writing
        progressData.lastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Convert object to formatted JSON
        string json = JsonUtility.ToJson(progressData, true);

        // Write JSON to file
        File.WriteAllText(filePath, json);

        Debug.Log("Progress saved to: " + filePath);
    }

    // Loads progress data for a specific user
    public static ProgressData LoadProgress(int userId)
    {
        string filePath = GetFilePath(userId);

        // If no save exists, return null
        if (!File.Exists(filePath))
        {
            Debug.Log("No save file found for user ID: " + userId);
            return null;
        }

        try
        {
            // Read JSON text from file
            string json = File.ReadAllText(filePath);

            // Convert JSON back to ProgressData object
            ProgressData loadedData = JsonUtility.FromJson<ProgressData>(json);

            // Support older save files that stored checkpoint coordinates
            // before the explicit checkpoint flag existed.
            if (
                !loadedData.hasSavedCheckpoint &&
                (Mathf.Abs(loadedData.checkpointX) > Mathf.Epsilon ||
                 Mathf.Abs(loadedData.checkpointY) > Mathf.Epsilon)
            )
            {
                loadedData.hasSavedCheckpoint = true;
            }

            return loadedData;
        }
        catch (Exception error)
        {
            Debug.LogError("Error loading progress: " + error.Message);
            return null;
        }
    }

    // Creates default progress data for a new user
    public static ProgressData CreateDefaultProgress(UserAccount user)
    {
        ProgressData defaultData = new ProgressData
        {
            userId = user.userId,
            username = user.username,
            deaths = 0,
            checkpointX = 0f,
            checkpointY = 0f,
            hasSavedCheckpoint = false,
            bestTime = 0f,
            hasFinishedGame = false,
            lastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        return defaultData;
    }
}
