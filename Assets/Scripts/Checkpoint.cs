using UnityEngine;

// This script is attached to checkpoint objects.
// When the player touches the checkpoint it becomes active,
// updates the respawn position, and changes colour to indicate activation.

public class Checkpoint : MonoBehaviour
{
    // Reference to the GameManager so we can update the respawn position
    private GameManager gameManager;

    // Stores the sprite renderer so we can change colour
    private SpriteRenderer spriteRenderer;

    // Static variable stores the currently active checkpoint
    private static Checkpoint activeCheckpoint;

    // Colour for inactive checkpoints
    public Color inactiveColor = Color.red;

    // Colour for the active checkpoint
    public Color activeColor = Color.green;


    void Start()
    {
        // Find the GameManager in the scene
        gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager == null)
        {
            Debug.LogError("Checkpoint could not find a GameManager in the scene.", this);
        }

        // Get the SpriteRenderer attached to this checkpoint
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError("Checkpoint requires a SpriteRenderer component.", this);
            return;
        }

        // Set the starting colour to inactive
        spriteRenderer.color = inactiveColor;
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player touched the checkpoint
        if (other.CompareTag("Player"))
        {
            if (gameManager == null)
            {
                Debug.LogError("Checkpoint cannot update because no GameManager was found.", this);
                return;
            }

            if (spriteRenderer == null)
            {
                Debug.LogError("Checkpoint cannot activate because no SpriteRenderer was found.", this);
                return;
            }

            // Update the respawn point in the GameManager
            Vector2 checkpointPosition = new Vector2(transform.position.x, transform.position.y);
            gameManager.UpdateCheckpoint(checkpointPosition);

            // Reset the previously active checkpoint colour
            if (activeCheckpoint != null)
            {
                activeCheckpoint.spriteRenderer.color = inactiveColor;
            }

            // Set this checkpoint as the new active checkpoint
            activeCheckpoint = this;

            // Change this checkpoint's colour to show it is active
            spriteRenderer.color = activeColor;

            Debug.Log("Checkpoint activated!");
        }
    }

    public static void ResetActiveCheckpoint()
    {
        activeCheckpoint = null;
    }
}
