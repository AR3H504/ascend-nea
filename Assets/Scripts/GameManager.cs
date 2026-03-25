using UnityEngine;

// This script manages deaths, respawning,
// and loading/saving checkpoint positions.

public class GameManager : MonoBehaviour
{
    private static bool forceExplicitLevelStartOnNextLoad;

    public static event System.Action PlayerRespawned;

    // Reference to the player object
    public Transform player;

    // Stores the current respawn point
    public Vector2 respawnPoint;

    // Optional explicit level start so testing by moving the player in the editor
    // does not accidentally change the game's real starting location.
    public bool useExplicitLevelStart;
    public Vector2 explicitLevelStartPoint;

    // Stores the original start position of the level
    private Vector2 levelStartPoint;

    // Height below which the player is considered dead
    public float deathHeight = -10f;

    void Start()
    {
        // Use the configured level start when set, otherwise fall back to the
        // player's scene position for older scenes that have not been updated yet.
        levelStartPoint = useExplicitLevelStart
            ? explicitLevelStartPoint
            : player.position;

        // In editor play mode, keep the manually placed player position unless a
        // saved checkpoint exists or another system explicitly requests a full restart.
        bool shouldUseExplicitStart = useExplicitLevelStart &&
            (!Application.isEditor || forceExplicitLevelStartOnNextLoad);

        if (TryLoadSavedCheckpoint())
        {
            forceExplicitLevelStartOnNextLoad = false;
            return;
        }

        if (shouldUseExplicitStart && player != null)
        {
            player.position = levelStartPoint;
            respawnPoint = levelStartPoint;
        }
        else
        {
            respawnPoint = player != null ? (Vector2)player.position : levelStartPoint;
        }

        forceExplicitLevelStartOnNextLoad = false;
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
        if (AccountManager.Instance != null)
        {
            AccountManager.Instance.ClearSavedCheckpoint();
        }

        Debug.Log("Respawn reset to level start: " + levelStartPoint);
    }

    bool TryLoadSavedCheckpoint()
    {
        if (AccountManager.Instance == null || AccountManager.Instance.CurrentProgress == null)
        {
            return false;
        }

        ProgressData progress = AccountManager.Instance.CurrentProgress;

        if (!progress.hasSavedCheckpoint)
        {
            return false;
        }

        respawnPoint = new Vector2(progress.checkpointX, progress.checkpointY);
        player.position = respawnPoint;

        Debug.Log("Loaded saved checkpoint: " + respawnPoint);
        return true;
    }

    public static void ForceExplicitLevelStartOnNextLoad()
    {
        forceExplicitLevelStartOnNextLoad = true;
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

        PlayerRespawned?.Invoke();
    }

}
