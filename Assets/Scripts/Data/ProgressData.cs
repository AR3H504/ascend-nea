using System;

// This class stores save data for one specific player.
// It is designed for a continuous vertical climbing game
// with checkpoints rather than separate levels.

[Serializable]
public class ProgressData
{
    // ID of the logged-in user
    public int userId;

    // Username linked to the save file
    public string username;

    // Total number of deaths recorded
    public int deaths;

    // X position of the last activated checkpoint
    public float checkpointX;

    // Y position of the last activated checkpoint
    public float checkpointY;

    // Tracks whether the player has activated and saved a checkpoint yet
    public bool hasSavedCheckpoint;

    // Best completion time for the full climb
    public float bestTime;

    // Records whether the player has completed the game before
    public bool hasFinishedGame;

    // Date and time the save was last updated
    public string lastSaved;
}
