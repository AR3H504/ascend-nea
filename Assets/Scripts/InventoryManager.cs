using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    // Simple singleton access so other scripts can use InventoryManager.Instance.
    public static InventoryManager Instance { get; private set; }

    // Optional UI text for showing the current relic inventory.
    [SerializeField] private TMP_Text inventoryText;

    // HashSet keeps relic IDs unique and is easy to check quickly.
    private readonly HashSet<string> relics = new HashSet<string>();

    private void Awake()
    {
        // If another InventoryManager already exists, remove this duplicate.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate InventoryManager found. Destroying the extra one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Update the UI when the game starts.
        RefreshInventoryText();
    }

    public void AddRelic(string relicId)
    {
        // Ignore empty relic IDs to avoid bad data.
        if (string.IsNullOrWhiteSpace(relicId))
        {
            Debug.LogWarning("Tried to add a relic with an empty relicId.");
            return;
        }

        // Only refresh if the relic was actually added.
        if (relics.Add(relicId))
        {
            RefreshInventoryText();
        }
    }

    public bool HasRelic(string relicId)
    {
        // Return false safely if the ID is blank.
        if (string.IsNullOrWhiteSpace(relicId))
        {
            return false;
        }

        return relics.Contains(relicId);
    }

    public bool ConsumeRelic(string relicId)
    {
        // Do nothing if the relic ID is blank.
        if (string.IsNullOrWhiteSpace(relicId))
        {
            Debug.LogWarning("Tried to consume a relic with an empty relicId.");
            return false;
        }

        // Remove returns true only if the relic existed.
        bool removed = relics.Remove(relicId);

        if (removed)
        {
            RefreshInventoryText();
        }

        return removed;
    }

    public void RefreshInventoryText()
    {
        // The inventory should still work even if no TMP_Text is assigned.
        if (inventoryText == null)
        {
            return;
        }

        if (relics.Count == 0)
        {
            inventoryText.text = "Relic: None";
            return;
        }

        // Build a simple comma-separated list of relic IDs.
        StringBuilder builder = new StringBuilder("Relics: ");
        bool firstRelic = true;

        foreach (string relic in relics)
        {
            if (!firstRelic)
            {
                builder.Append(", ");
            }

            builder.Append(relic);
            firstRelic = false;
        }

        inventoryText.text = builder.ToString();
    }
}
