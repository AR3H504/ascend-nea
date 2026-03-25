using System;
using System.Linq;
using UnityEngine;

// This class controls the account system.
// It handles sign-up, login, logout,
// and stores the currently logged-in user.

public class AccountManager : MonoBehaviour
{
    // Singleton instance so other scripts can access the account system
    public static AccountManager Instance;

    // Stores the currently logged-in user
    public UserAccount CurrentUser { get; private set; }

    // Stores the current logged-in user's progress data
    public ProgressData CurrentProgress { get; private set; }

    // Reference to the loaded user database
    private UserDatabase database;

    private void Awake()
    {
        // Ensure only one AccountManager exists
        if (Instance == null)
        {
            Instance = this;

            // Prevent this object from being destroyed between scenes
            DontDestroyOnLoad(gameObject);

            Debug.Log(Application.persistentDataPath);

            // Load the user database from JSON
            database = AccountDatabaseManager.LoadDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Creates a new account
    public bool SignUp(string username, string password, string email, out string message)
    {
        username = username.Trim();
        email = email.Trim();

        // Validate username
        if (string.IsNullOrWhiteSpace(username))
        {
            message = "Username cannot be empty.";
            return false;
        }

        if (username.Length < 3 || username.Length > 20)
        {
            message = "Username must be between 3 and 20 characters.";
            return false;
        }

        // Validate password
        if (string.IsNullOrWhiteSpace(password))
        {
            message = "Password cannot be empty.";
            return false;
        }

        if (!TryValidateStrongPassword(password, out message))
        {
            return false;
        }

        // Check if username already exists
        bool usernameExists =
            database.users.Any(u => u.username.Equals(username, StringComparison.OrdinalIgnoreCase));

        if (usernameExists)
        {
            message = "Username already exists.";
            return false;
        }

        // Generate new user ID
        int newId =
            database.users.Count > 0 ? database.users.Max(u => u.userId) + 1 : 1;

        // Create new user account
        UserAccount newUser = new UserAccount
        {
            userId = newId,
            username = username,
            passwordHash = PasswordHasher.HashPassword(password),
            email = email,
            dateCreated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            lastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // Add user to database
        database.users.Add(newUser);

        // Save database to JSON file
        AccountDatabaseManager.SaveDatabase(database);

        // Create a default progress file for the new user
        ProgressData defaultProgress = ProgressSaveManager.CreateDefaultProgress(newUser);
        ProgressSaveManager.SaveProgress(defaultProgress);

        message = "Account created successfully.";
        return true;
    }

    // Enforces the sign-up password rules and returns a user-facing message
    // that can be shown directly on the sign-up screen.
    private bool TryValidateStrongPassword(string password, out string message)
    {
        // Require at least 6 characters before checking the more specific rules.
        if (password.Length < 6)
        {
            message = "Password too weak must contain 1 lower case 1 highercase 1 special and 1 number and at least 6 characters.";
            return false;
        }

        // Check each required character category so weak passwords are rejected.
        bool hasLowercase = password.Any(char.IsLower);
        bool hasUppercase = password.Any(char.IsUpper);
        bool hasNumber = password.Any(char.IsDigit);
        bool hasSpecialCharacter = password.Any(character => !char.IsLetterOrDigit(character));

        if (!hasLowercase || !hasUppercase || !hasNumber || !hasSpecialCharacter)
        {
            message = "Password too weak must contain 1 lower case 1 highercase 1 special and 1 number and at least 6 characters.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    // Logs a user into the system
    public bool Login(string username, string password, out string message)
    {
        username = username.Trim();

        // Search for user in database
        UserAccount foundUser =
            database.users.FirstOrDefault(u =>
                u.username.Equals(username, StringComparison.OrdinalIgnoreCase));

        // If username not found
        if (foundUser == null)
        {
            message = "Username not found.";
            return false;
        }

        // Hash the entered password
        string hashedInput = PasswordHasher.HashPassword(password);

        // Compare password hashes
        if (foundUser.passwordHash != hashedInput)
        {
            message = "Incorrect password.";
            return false;
        }

        // Update last login time
        foundUser.lastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Save updated database
        AccountDatabaseManager.SaveDatabase(database);

        // Store logged-in user
        CurrentUser = foundUser;

        // Try to load this user's saved progress
        CurrentProgress = ProgressSaveManager.LoadProgress(foundUser.userId);

        // If no progress file exists yet, create default progress and save it
        if (CurrentProgress == null)
        {
            CurrentProgress = ProgressSaveManager.CreateDefaultProgress(foundUser);
            ProgressSaveManager.SaveProgress(CurrentProgress);
        }

        message = "Login successful.";
        return true;
    }

    // Adds one death to the current player's progress and saves it
    public void RecordDeath()
    {
        // Make sure progress exists
        if (CurrentProgress != null)
        {
            CurrentProgress.deaths++;
            ProgressSaveManager.SaveProgress(CurrentProgress);
        }
    }

    // Updates the saved checkpoint position for the current player
    public void UpdateCheckpoint(Vector2 checkpointPosition)
    {
        // Make sure progress exists
        if (CurrentProgress != null)
        {
            CurrentProgress.checkpointX = checkpointPosition.x;
            CurrentProgress.checkpointY = checkpointPosition.y;
            CurrentProgress.hasSavedCheckpoint = true;
            ProgressSaveManager.SaveProgress(CurrentProgress);
        }
    }

    // Clears the saved checkpoint for the current player and saves the change
    public void ClearSavedCheckpoint()
    {
        if (CurrentProgress != null)
        {
            CurrentProgress.checkpointX = 0f;
            CurrentProgress.checkpointY = 0f;
            CurrentProgress.hasSavedCheckpoint = false;
            ProgressSaveManager.SaveProgress(CurrentProgress);
        }
    }

    // Records a completed run and stores the best time when improved.
    public void RecordCompletionTime(float completionTimeSeconds)
    {
        if (CurrentProgress == null)
        {
            return;
        }

        if (completionTimeSeconds <= 0f)
        {
            return;
        }

        CurrentProgress.hasFinishedGame = true;

        if (CurrentProgress.bestTime <= 0f || completionTimeSeconds < CurrentProgress.bestTime)
        {
            CurrentProgress.bestTime = completionTimeSeconds;
        }

        ProgressSaveManager.SaveProgress(CurrentProgress);
    }

    // Logs the current user out
    public void Logout()
    {
        CurrentUser = null;
        CurrentProgress = null;
    }
}
