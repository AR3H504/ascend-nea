using UnityEngine;

public class JumpBoostPickup : MonoBehaviour
{
    // How long the temporary jump boost lasts after pickup.
    [SerializeField] private float durationSeconds = 10f;
    // Multiplies the player's normal jump force while the boost is active.
    [SerializeField] private float jumpMultiplier = 1.35f;
    // Allows special pickups to stay silent when they can be triggered rapidly.
    [SerializeField] private bool playPickupAudio = true;

    // Prevents the pickup from being collected twice before it is destroyed.
    private bool hasBeenCollected;
    private Collider2D pickupCollider;
    private SpriteRenderer[] spriteRenderers;

    private void Awake()
    {
        pickupCollider = GetComponent<Collider2D>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void OnEnable()
    {
        GameManager.PlayerRespawned += HandlePlayerRespawned;
    }

    private void OnDisable()
    {
        GameManager.PlayerRespawned -= HandlePlayerRespawned;
    }

    private void Update()
    {
        if (!hasBeenCollected)
        {
            return;
        }

        if (InventoryManager.Instance != null && InventoryManager.Instance.HasActiveJumpBoost())
        {
            return;
        }

        RestorePickup();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore extra trigger calls after a successful collection.
        if (hasBeenCollected)
        {
            return;
        }

        // Only the player should be able to collect this pickup.
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // The inventory manager owns the active boost state and UI.
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("JumpBoostPickup could not find InventoryManager in the scene.", this);
            return;
        }

        hasBeenCollected = true;
        InventoryManager.Instance.ActivateJumpBoost(durationSeconds, jumpMultiplier, GetPickupSprite());

        if (playPickupAudio)
        {
            GameAudio.PlayItemPickup(transform.position);
        }

        SetPickupVisible(false);
    }

    private Sprite GetPickupSprite()
    {
        // Prefer the sprite on this object for the inventory icon.
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer.sprite;
        }

        // Fall back to a child sprite renderer for nested pickup art.
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    private void HandlePlayerRespawned()
    {
        // Always restore the pickup on respawn so the player can grab a fresh boost at the checkpoint.
        RestorePickup();
    }

    private void SetPickupVisible(bool isVisible)
    {
        if (pickupCollider != null)
        {
            pickupCollider.enabled = isVisible;
        }

        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = isVisible;
            }
        }
    }

    private void RestorePickup()
    {
        hasBeenCollected = false;
        SetPickupVisible(true);
    }
}
