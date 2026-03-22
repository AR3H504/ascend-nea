using UnityEngine;

public class RelicPickup : MonoBehaviour
{
    // The relic ID that will be given to the player.
    [SerializeField] private string relicId;

    // Stops the pickup from running twice before it is destroyed.
    private bool hasBeenCollected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore extra trigger calls after the pickup has already been taken.
        if (hasBeenCollected)
        {
            return;
        }

        // Only the player should be able to collect relics.
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // Warn clearly if the InventoryManager is missing from the scene.
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("RelicPickup could not find InventoryManager in the scene.");
            return;
        }

        // Make sure the pickup has a valid relic ID before adding it.
        if (string.IsNullOrWhiteSpace(relicId))
        {
            Debug.LogWarning("RelicPickup has an empty relicId.", this);
            return;
        }

        hasBeenCollected = true;
        InventoryManager.Instance.AddRelic(relicId);

        // Remove the pickup after a successful collection.
        Destroy(gameObject);
    }
}
