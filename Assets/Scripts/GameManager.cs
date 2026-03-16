using UnityEngine;

// This script manages deaths, respawning,
// and loading/saving checkpoint positions.

public class GameManager : MonoBehaviour
{
    // Reference to the player object
    public Transform player;

    // Stores the current respawn point
    public Vector2 respawnPoint;

    // Stores the original start position of the level
    private Vector2 levelStartPoint;

    // Height below which the player is considered dead
    public float deathHeight = -10f;

    void Start()
    {
        // Store the player's starting position as the true level start
        levelStartPoint = player.position;

        // Default respawn is the start of the level
        respawnPoint = levelStartPoint;

        LoadSavedCheckpoint();
    }

    void Update()
    {
        // If the player falls below the death height, respawn them
        if (player.position.y < deathHeight)
        {
            RespawnPlayer(true);
        }
    }

    // Called when the player reaches a checkpoint
    public void UpdateCheckpoint(Vector2 newCheckpoint)
    {
        // Update the current respawn point
        respawnPoint = newCheckpoint;

        // Save the checkpoint to the logged-in user's progress
        if (AccountManager.Instance != null)
        {
            AccountManager.Instance.UpdateCheckpoint(newCheckpoint);
        }

        Debug.Log("Checkpoint saved at: " + newCheckpoint);
    }

    // Allows menus and other systems to respawn without recording a death
    public void RespawnAtCheckpoint()
    {
        RespawnPlayer(false);
    }

    // Resets the respawn point back to the start of the level
    public void ResetToLevelStart()
    {
        respawnPoint = levelStartPoint;

        // Also clear the saved checkpoint from account progress
        if (AccountManager.Instance != null && AccountManager.Instance.CurrentProgress != null)
        {
            AccountManager.Instance.CurrentProgress.hasSavedCheckpoint = false;
            AccountManager.Instance.CurrentProgress.checkpointX = levelStartPoint.x;
            AccountManager.Instance.CurrentProgress.checkpointY = levelStartPoint.y;
        }

        Debug.Log("Respawn reset to level start: " + levelStartPoint);
    }

    void LoadSavedCheckpoint()
    {
        if (AccountManager.Instance == null || AccountManager.Instance.CurrentProgress == null)
        {
            return;
        }

        ProgressData progress = AccountManager.Instance.CurrentProgress;

        if (!progress.hasSavedCheckpoint)
        {
            return;
        }

        respawnPoint = new Vector2(progress.checkpointX, progress.checkpointY);
        player.position = respawnPoint;

        Debug.Log("Loaded saved checkpoint: " + respawnPoint);
    }

    // Respawns the player and optionally records a death
    void RespawnPlayer(bool countDeath)
    {
        // Record death in account progress
        if (countDeath && AccountManager.Instance != null)
        {
            AccountManager.Instance.RecordDeath();
        }

        // Move player back to respawn point
        player.position = respawnPoint;

        // Reset velocity so the player does not keep falling
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
