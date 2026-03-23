using System.Collections;
using UnityEngine;

public class WarpPortal : MonoBehaviour
{
    // The relic needed to unlock and use this portal.
    [SerializeField] private string requiredRelicId;

    // Where the player will appear after teleporting.
    [SerializeField] private Transform destinationPoint;

    // Short delay to stop instant repeat triggers.
    [SerializeField] private float triggerCooldown = 0.5f;

    // Prevents the portal from firing again during the cooldown.
    private bool isOnCooldown;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore trigger calls while the portal is cooling down.
        if (isOnCooldown)
        {
            return;
        }

        // Only the player should activate the portal.
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // Check required references before doing anything else.
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("WarpPortal could not find InventoryManager in the scene.", this);
            return;
        }

        if (destinationPoint == null)
        {
            Debug.LogWarning("WarpPortal is missing a destinationPoint.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(requiredRelicId))
        {
            Debug.LogWarning("WarpPortal is missing a requiredRelicId.", this);
            return;
        }

        // Stop here if the player does not have the required relic.
        if (!InventoryManager.Instance.HasRelic(requiredRelicId))
        {
            Debug.Log("Portal is locked. Required relic is missing: " + requiredRelicId, this);
            return;
        }

        // Consume the relic before teleporting.
        bool consumed = InventoryManager.Instance.ConsumeRelic(requiredRelicId);

        if (!consumed)
        {
            Debug.LogWarning("WarpPortal could not consume relic: " + requiredRelicId, this);
            return;
        }

        // Move the player to the destination point in the same scene.
        other.transform.position = destinationPoint.position;

        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(triggerCooldown);
        isOnCooldown = false;
    }
}
