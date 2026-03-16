using System.IO;
using UnityEngine;

// This class is responsible for loading and saving
// the user database from a JSON file stored locally.

public static class AccountDatabaseManager
{
    // File location where user accounts will be saved
    private static string FilePath =>
        Path.Combine(Application.persistentDataPath, "users.json");

    // Loads the database from disk
    public static UserDatabase LoadDatabase()
    {
        // If the file does not exist yet
        if (!File.Exists(FilePath))
        {
            // Create a new empty database
            UserDatabase newDatabase = new UserDatabase();

            // Save it so the file is created
            SaveDatabase(newDatabase);

            return newDatabase;
        }

        // Read JSON text from file
        string json = File.ReadAllText(FilePath);

        // If the file is empty, return a new database
        if (string.IsNullOrWhiteSpace(json))
        {
            return new UserDatabase();
        }

        // Convert JSON back into a UserDatabase object
        UserDatabase database = JsonUtility.FromJson<UserDatabase>(json);

        // Safety check
        if (database == null)
        {
            return new UserDatabase();
        }

        return database;
    }

    // Saves the database to disk
    public static void SaveDatabase(UserDatabase database)
    {
        // Convert database object into formatted JSON
        string json = JsonUtility.ToJson(database, true);

        // Write JSON to file
        File.WriteAllText(FilePath, json);
    }
}