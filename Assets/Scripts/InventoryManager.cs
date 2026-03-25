using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    private static readonly Color HudOutlineColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    private static readonly Color HudUnderlayColor = new Color(0f, 0f, 0f, 0.9f);
    private const float HudOutlineWidth = 0.35f;
    private const float HudUnderlaySoftness = 0.15f;
    private const float HudUnderlayDilate = 0.1f;
    private const float HudUnderlayOffsetX = 0.5f;
    private const float HudUnderlayOffsetY = -0.5f;

    public static InventoryManager Instance { get; private set; }

    [SerializeField] private TMP_Text inventoryText;
    private TMP_Text inventoryDisplayText;

    private readonly HashSet<string> relics = new HashSet<string>();

    // Tracks how long the active jump boost still has left to run.
    private float jumpBoostTimer;
    // Multiplies the player's base jump while the boost is active.
    private float jumpBoostMultiplier = 1f;
    // Stores the pickup sprite so the same art can be shown in the HUD icon.
    private Sprite jumpBoostSprite;
    // Runtime-created HUD text shown in the top-right while the boost is active.
    private TMP_Text jumpBoostTimerText;
    // Runtime-created HUD text shown under the jump boost label for the live run timer.
    private TMP_Text runTimerText;
    // Runtime-created HUD icon shown near the bottom of the screen while active.
    private Image jumpBoostInventoryIcon;
    // Shadow copies that sit behind the primary HUD labels for stronger readability.
    private TMP_Text inventoryShadowText;
    private TMP_Text jumpBoostShadowText;
    private TMP_Text runTimerShadowText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate InventoryManager found. Destroying the extra one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        GameManager.PlayerRespawned += HandlePlayerRespawned;
    }

    private void OnDisable()
    {
        GameManager.PlayerRespawned -= HandlePlayerRespawned;
    }

    private void Start()
    {
        NormalizeInventoryTextStyle();
        EnsureInventoryDisplayExists();
        RefreshInventoryText();
        RefreshJumpBoostUI();
    }

    private void Update()
    {
        RefreshRunTimerUI();

        // No work is needed while no boost is active.
        if (jumpBoostTimer <= 0f)
        {
            return;
        }

        // Count the boost down in real time.
        jumpBoostTimer = Mathf.Max(0f, jumpBoostTimer - Time.deltaTime);

        if (jumpBoostTimer <= 0f)
        {
            // Reset back to normal jumping once the timer expires.
            jumpBoostMultiplier = 1f;
        }

        // Keep the timer text and icon in sync with the remaining duration.
        RefreshJumpBoostUI();
    }

    public void AddRelic(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
        {
            Debug.LogWarning("Tried to add a relic with an empty relicId.");
            return;
        }

        if (relics.Add(relicId))
        {
            RefreshInventoryText();
        }
    }

    public bool HasRelic(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
        {
            return false;
        }

        return relics.Contains(relicId);
    }

    public bool ConsumeRelic(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
        {
            Debug.LogWarning("Tried to consume a relic with an empty relicId.");
            return false;
        }

        bool removed = relics.Remove(relicId);

        if (removed)
        {
            RefreshInventoryText();
        }

        return removed;
    }

    public void ActivateJumpBoost(float durationSeconds, float multiplier, Sprite iconSprite)
    {
        // Reject invalid pickups so we do not create a broken timed state.
        if (durationSeconds <= 0f)
        {
            Debug.LogWarning("Jump boost duration must be greater than zero.");
            return;
        }

        // If the player picks up another boost while one is active, keep the longer duration.
        jumpBoostTimer = Mathf.Max(jumpBoostTimer, durationSeconds);
        // Never allow a multiplier below 1 so the boost cannot weaken the player's jump.
        jumpBoostMultiplier = Mathf.Max(1f, multiplier);

        // Reuse the pickup's sprite for the inventory icon when one is available.
        if (iconSprite != null)
        {
            jumpBoostSprite = iconSprite;
        }

        // Update the runtime HUD immediately so the player sees the boost start.
        RefreshJumpBoostUI();
    }

    public void ClearJumpBoost()
    {
        jumpBoostTimer = 0f;
        jumpBoostMultiplier = 1f;
        RefreshJumpBoostUI();
    }

    public bool HasActiveJumpBoost()
    {
        // Other systems can use this to check if the timed boost is still running.
        return jumpBoostTimer > 0f;
    }

    public float GetJumpBoostMultiplier()
    {
        // Return the live boost multiplier, or normal jump strength when inactive.
        return HasActiveJumpBoost() ? jumpBoostMultiplier : 1f;
    }

    public void RefreshInventoryText()
    {
        TMP_Text targetInventoryText = GetPrimaryInventoryText();

        if (targetInventoryText == null)
        {
            return;
        }

        if (relics.Count == 0)
        {
            targetInventoryText.text = "Relic: None";
            SyncShadowText(targetInventoryText, inventoryShadowText);
            return;
        }

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

        targetInventoryText.text = builder.ToString();
        SyncShadowText(targetInventoryText, inventoryShadowText);
    }

    private void RefreshJumpBoostUI()
    {
        // Lazily create the HUD elements the first time they are needed.
        EnsureJumpBoostUIExists();

        bool isActive = HasActiveJumpBoost();

        if (jumpBoostTimerText != null)
        {
            // Only show the timer while the boost is active.
            jumpBoostTimerText.gameObject.SetActive(isActive);
            if (jumpBoostShadowText != null)
            {
                jumpBoostShadowText.gameObject.SetActive(isActive);
            }

            if (isActive)
            {
                // Display the remaining boost time in mm:ss format.
                jumpBoostTimerText.text = $"Jump Boost: {FormatTime(jumpBoostTimer)}";
                SyncShadowText(jumpBoostTimerText, jumpBoostShadowText);
            }
        }

        if (jumpBoostInventoryIcon != null)
        {
            // The inventory icon only makes sense if the boost is active and has art to show.
            bool showIcon = isActive && jumpBoostSprite != null;
            jumpBoostInventoryIcon.gameObject.SetActive(showIcon);

            if (showIcon)
            {
                // Keep the icon art synced with the pickup sprite.
                jumpBoostInventoryIcon.sprite = jumpBoostSprite;
                jumpBoostInventoryIcon.preserveAspect = true;
            }
        }
    }

    private void RefreshRunTimerUI()
    {
        EnsureJumpBoostUIExists();

        if (runTimerText == null)
        {
            return;
        }

        runTimerText.text = $"Time: {FormatRunTime(GameManager.CurrentRunTimeSeconds)}";
        SyncShadowText(runTimerText, runTimerShadowText);
    }

    private void HandlePlayerRespawned()
    {
        // Dying or checkpoint-respawning clears the current boost so the pickup can be taken again
        // for a fresh full-duration timer.
        ClearJumpBoost();
    }

    private void EnsureJumpBoostUIExists()
    {
        // Once both runtime HUD elements exist there is nothing else to build.
        if (jumpBoostTimerText != null && runTimerText != null && jumpBoostInventoryIcon != null)
        {
            return;
        }

        // Prefer the main gameplay canvas so the boost HUD appears with the rest of the HUD.
        Canvas targetCanvas = FindTargetCanvas();
        if (targetCanvas == null)
        {
            return;
        }

        if (jumpBoostTimerText == null)
        {
            // Create the top-right timer label at runtime so the scene does not need manual UI wiring.
            GameObject timerObject = new GameObject("JumpBoostTimer", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            timerObject.transform.SetParent(targetCanvas.transform, false);

            RectTransform rectTransform = timerObject.GetComponent<RectTransform>();
            ConfigureJumpBoostTimerTransform(rectTransform);

            jumpBoostTimerText = timerObject.GetComponent<TextMeshProUGUI>();
            jumpBoostTimerText.alignment = TextAlignmentOptions.TopRight;
            jumpBoostTimerText.fontSize = 28f;
            jumpBoostTimerText.color = new Color(1f, 0.95f, 0.72f, 1f);

            if (TMP_Settings.defaultFontAsset != null)
            {
                jumpBoostTimerText.font = TMP_Settings.defaultFontAsset;
            }

            ApplyHudTextStyle(jumpBoostTimerText);
            jumpBoostShadowText = CreateShadowText(jumpBoostTimerText, "JumpBoostTimerShadow");
        }

        if (runTimerText == null)
        {
            GameObject timerObject = new GameObject("RunTimerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            timerObject.transform.SetParent(targetCanvas.transform, false);

            RectTransform rectTransform = timerObject.GetComponent<RectTransform>();
            ConfigureRunTimerTransform(rectTransform);

            runTimerText = timerObject.GetComponent<TextMeshProUGUI>();
            runTimerText.alignment = TextAlignmentOptions.TopRight;
            runTimerText.fontSize = 28f;
            runTimerText.color = new Color(0.82f, 0.95f, 1f, 1f);

            if (TMP_Settings.defaultFontAsset != null)
            {
                runTimerText.font = TMP_Settings.defaultFontAsset;
            }

            ApplyHudTextStyle(runTimerText);
            runTimerShadowText = CreateShadowText(runTimerText, "RunTimerShadow");
        }

        if (jumpBoostInventoryIcon == null)
        {
            // Create the bottom-screen icon at runtime so the pickup automatically gets inventory feedback.
            GameObject iconObject = new GameObject("JumpBoostInventoryIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(targetCanvas.transform, false);

            RectTransform rectTransform = iconObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, 24f);
            rectTransform.sizeDelta = new Vector2(72f, 72f);

            jumpBoostInventoryIcon = iconObject.GetComponent<Image>();
            jumpBoostInventoryIcon.color = Color.white;
            jumpBoostInventoryIcon.preserveAspect = true;
        }
    }

    private void ConfigureRunTimerTransform(RectTransform rectTransform)
    {
        if (inventoryText != null)
        {
            RectTransform inventoryRect = inventoryText.rectTransform;
            rectTransform.anchorMin = inventoryRect.anchorMin;
            rectTransform.anchorMax = inventoryRect.anchorMax;
            rectTransform.pivot = inventoryRect.pivot;
            rectTransform.anchoredPosition = inventoryRect.anchoredPosition + new Vector2(0f, -132f);
            rectTransform.sizeDelta = inventoryRect.sizeDelta;
            return;
        }

        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-32f, -164f);
        rectTransform.sizeDelta = new Vector2(280f, 42f);
    }

    private void ConfigureJumpBoostTimerTransform(RectTransform rectTransform)
    {
        if (inventoryText != null)
        {
            // Match the relic text anchoring so the jump boost timer sits in the same HUD region.
            RectTransform inventoryRect = inventoryText.rectTransform;
            rectTransform.anchorMin = inventoryRect.anchorMin;
            rectTransform.anchorMax = inventoryRect.anchorMax;
            rectTransform.pivot = inventoryRect.pivot;
            // Place the jump boost timer directly under the relic text.
            rectTransform.anchoredPosition = inventoryRect.anchoredPosition + new Vector2(0f, -56f);
            rectTransform.sizeDelta = inventoryRect.sizeDelta;
            return;
        }

        // Fall back to a safe top-right placement if the relic text is missing.
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-32f, -88f);
        rectTransform.sizeDelta = new Vector2(280f, 42f);
    }

    private Canvas FindTargetCanvas()
    {
        // Look for the main gameplay canvas first and avoid using the pause menu canvas.
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas fallback = null;

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || !canvas.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (canvas.name == "Canvas")
            {
                return canvas;
            }

            if (fallback == null && canvas.name != "PauseCanvas")
            {
                fallback = canvas;
            }
        }

        return fallback;
    }

    private static string FormatTime(float seconds)
    {
        // Convert the remaining boost duration into the mm:ss format shown in the HUD.
        int totalSeconds = Mathf.CeilToInt(seconds);
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }

    private static string FormatRunTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int minutes = Mathf.FloorToInt(seconds / 60f);
        float remainingSeconds = seconds % 60f;
        return $"{minutes:00}:{remainingSeconds:00.00}";
    }

    private void EnsureInventoryShadowExists()
    {
        TMP_Text targetInventoryText = GetPrimaryInventoryText();

        if (targetInventoryText == null || inventoryShadowText != null)
        {
            return;
        }

        inventoryShadowText = CreateShadowText(targetInventoryText, "InventoryTextShadow");
        SyncShadowText(targetInventoryText, inventoryShadowText);
    }

    private void NormalizeInventoryTextStyle()
    {
        if (inventoryText == null)
        {
            return;
        }

        // Keep the relic label on the same visual footing as the other HUD labels.
        inventoryText.fontStyle = FontStyles.Normal;
        inventoryText.fontWeight = FontWeight.Regular;
    }

    private void EnsureInventoryDisplayExists()
    {
        if (inventoryText == null || inventoryDisplayText != null)
        {
            return;
        }

        GameObject displayObject = new GameObject("InventoryDisplayText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        displayObject.transform.SetParent(inventoryText.transform.parent, false);

        RectTransform sourceRect = inventoryText.rectTransform;
        RectTransform displayRect = displayObject.GetComponent<RectTransform>();
        displayRect.anchorMin = sourceRect.anchorMin;
        displayRect.anchorMax = sourceRect.anchorMax;
        displayRect.pivot = sourceRect.pivot;
        displayRect.anchoredPosition = sourceRect.anchoredPosition;
        displayRect.sizeDelta = sourceRect.sizeDelta;
        displayRect.localScale = sourceRect.localScale;

        TextMeshProUGUI displayText = displayObject.GetComponent<TextMeshProUGUI>();
        displayText.font = inventoryText.font;
        displayText.fontSize = inventoryText.fontSize;
        displayText.alignment = inventoryText.alignment;
        displayText.fontStyle = FontStyles.Normal;
        displayText.fontWeight = FontWeight.Regular;
        displayText.enableWordWrapping = inventoryText.enableWordWrapping;
        displayText.overflowMode = inventoryText.overflowMode;
        displayText.raycastTarget = false;
        displayText.color = inventoryText.color;

        ApplyHudTextStyle(displayText);

        displayObject.transform.SetSiblingIndex(inventoryText.transform.GetSiblingIndex());
        inventoryText.enabled = false;
        inventoryDisplayText = displayText;

        EnsureInventoryShadowExists();
    }

    private TMP_Text GetPrimaryInventoryText()
    {
        return inventoryDisplayText != null ? inventoryDisplayText : inventoryText;
    }

    private TMP_Text CreateShadowText(TMP_Text sourceText, string objectName)
    {
        if (sourceText == null)
        {
            return null;
        }

        GameObject shadowObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        shadowObject.transform.SetParent(sourceText.transform.parent, false);

        RectTransform sourceRect = sourceText.rectTransform;
        RectTransform shadowRect = shadowObject.GetComponent<RectTransform>();
        shadowRect.anchorMin = sourceRect.anchorMin;
        shadowRect.anchorMax = sourceRect.anchorMax;
        shadowRect.pivot = sourceRect.pivot;
        shadowRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(1.4f, -1.4f);
        shadowRect.sizeDelta = sourceRect.sizeDelta;
        shadowRect.localScale = sourceRect.localScale;

        TextMeshProUGUI shadowText = shadowObject.GetComponent<TextMeshProUGUI>();
        shadowText.font = sourceText.font;
        shadowText.fontSize = sourceText.fontSize;
        shadowText.alignment = sourceText.alignment;
        shadowText.fontStyle = sourceText.fontStyle;
        shadowText.fontWeight = sourceText.fontWeight;
        shadowText.enableWordWrapping = sourceText.enableWordWrapping;
        shadowText.overflowMode = sourceText.overflowMode;
        shadowText.raycastTarget = false;
        shadowText.color = new Color(0f, 0f, 0f, 0.92f);

        if (sourceText.fontSharedMaterial != null)
        {
            Material shadowMaterial = new Material(sourceText.fontSharedMaterial);
            shadowMaterial.SetColor(ShaderUtilities.ID_FaceColor, new Color(0f, 0f, 0f, 1f));
            shadowMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 1f));
            shadowMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.08f);
            shadowText.fontMaterial = shadowMaterial;
        }

        shadowObject.transform.SetSiblingIndex(sourceText.transform.GetSiblingIndex());
        sourceText.transform.SetAsLastSibling();

        return shadowText;
    }

    private void SyncShadowText(TMP_Text sourceText, TMP_Text shadowText)
    {
        if (sourceText == null || shadowText == null)
        {
            return;
        }

        shadowText.text = sourceText.text;
        shadowText.gameObject.SetActive(sourceText.gameObject.activeSelf);
    }

    private void ApplyHudTextStyle(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        if (text.fontSharedMaterial == null)
        {
            return;
        }

        Material outlinedMaterial = new Material(text.fontSharedMaterial);
        outlinedMaterial.SetColor(ShaderUtilities.ID_OutlineColor, HudOutlineColor);
        outlinedMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, HudOutlineWidth);
        outlinedMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, HudUnderlayColor);
        outlinedMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, HudUnderlaySoftness);
        outlinedMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, HudUnderlayDilate);
        outlinedMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, HudUnderlayOffsetX);
        outlinedMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, HudUnderlayOffsetY);
        text.fontMaterial = outlinedMaterial;
    }
}

