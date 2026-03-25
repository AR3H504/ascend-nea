using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LedgeGrabbableColorTool
{
    private static readonly Color LedgeColor = new(0.5764706f, 0.75686276f, 0.7372549f, 1f);
    private static readonly Color CityLedgeColor = new(0.72f, 0.96f, 1f, 1f);
    private static readonly Color HeavenLedgeColor = new(1f, 0.92f, 0.72f, 1f);
    private static readonly Color DefaultColor = Color.white;

    [MenuItem("Tools/Hell/Apply Ledge Grabbable Color")]
    public static void ApplyLedgeGrabbableColor()
    {
        GameObject[] gameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int ledgeLayer = LayerMask.NameToLayer("LedgeGrabbable");
        int updatedCount = 0;

        foreach (GameObject gameObject in gameObjects)
        {
            if (gameObject == null || !gameObject.name.StartsWith("GroundPlatform"))
            {
                continue;
            }

            Transform visual = gameObject.transform.Find("Visual");
            if (visual == null)
            {
                continue;
            }

            SpriteRenderer spriteRenderer = visual.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                continue;
            }

            Color targetColor = gameObject.layer == ledgeLayer ? LedgeColor : DefaultColor;

            if (spriteRenderer.color == targetColor)
            {
                continue;
            }

            Undo.RecordObject(spriteRenderer, "Apply ledge grabbable color");
            spriteRenderer.color = targetColor;
            EditorUtility.SetDirty(spriteRenderer);
            updatedCount++;
        }

        if (updatedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        Debug.Log($"Applied ledge color to {updatedCount} platform visuals.");
    }

    [MenuItem("Tools/City/Apply Ledge Grabbable Highlight")]
    public static void ApplyCityLedgeGrabbableHighlight()
    {
        ApplyHighlight(LooksLikeCityLedgeObject, GetTargetSpriteRenderer, CityLedgeColor, "Apply city ledge grabbable highlight", "Applied city ledge highlight to {0} object(s).");
    }

    [MenuItem("Tools/Heaven/Apply Ledge Grabbable Tint")]
    public static void ApplyHeavenLedgeGrabbableTint()
    {
        ApplyHighlight(LooksLikeHeavenLedgeObject, GetHeavenTargetSpriteRenderer, HeavenLedgeColor, "Apply Heaven ledge grabbable tint", "Applied Heaven ledge tint to {0} object(s).");
    }

    private static void ApplyHighlight(
        System.Func<GameObject, bool> objectFilter,
        System.Func<GameObject, SpriteRenderer> rendererSelector,
        Color ledgeColor,
        string undoLabel,
        string logFormat)
    {
        GameObject[] gameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int ledgeLayer = LayerMask.NameToLayer("LedgeGrabbable");
        int updatedCount = 0;

        foreach (GameObject gameObject in gameObjects)
        {
            if (gameObject == null || !objectFilter(gameObject))
            {
                continue;
            }

            SpriteRenderer spriteRenderer = rendererSelector(gameObject);
            if (spriteRenderer == null)
            {
                continue;
            }

            Color targetColor = gameObject.layer == ledgeLayer ? ledgeColor : DefaultColor;

            if (spriteRenderer.color == targetColor)
            {
                continue;
            }

            Undo.RecordObject(spriteRenderer, undoLabel);
            spriteRenderer.color = targetColor;
            EditorUtility.SetDirty(spriteRenderer);
            updatedCount++;
        }

        if (updatedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        Debug.Log(string.Format(logFormat, updatedCount));
    }

    private static bool LooksLikeCityLedgeObject(GameObject gameObject)
    {
        if (gameObject.GetComponent<MovingPlatform2D>() != null)
        {
            return true;
        }

        return gameObject.name.StartsWith("City") ||
               gameObject.name.StartsWith("Platform") ||
               gameObject.name.StartsWith("MovingPlatform") ||
               gameObject.name.StartsWith("HorizontalBlock") ||
               gameObject.name.StartsWith("VerticalBlock") ||
               gameObject.name.StartsWith("Horizontal Block") ||
               gameObject.name.StartsWith("Vertical Block");
    }

    private static bool LooksLikeHeavenLedgeObject(GameObject gameObject)
    {
        if (gameObject.name.StartsWith("HeavenPlatform"))
        {
            return true;
        }

        if (gameObject.name.StartsWith("HorizontalBlock") ||
            gameObject.name.StartsWith("VerticalBlock") ||
            gameObject.name.StartsWith("Horizontal Block") ||
            gameObject.name.StartsWith("Vertical Block"))
        {
            return HasHeavenSprite(gameObject);
        }

        if (gameObject.name.StartsWith("GroundPlatform") && HasHeavenSprite(gameObject))
        {
            return true;
        }

        if (gameObject.GetComponent<MovingPlatform2D>() != null && HasHeavenSprite(gameObject))
        {
            return true;
        }

        return false;
    }

    private static bool HasHeavenSprite(GameObject gameObject)
    {
        SpriteRenderer[] renderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer.sprite != null && IsHeavenSprite(renderer.sprite))
            {
                return true;
            }
        }

        return false;
    }

    private static SpriteRenderer GetHeavenTargetSpriteRenderer(GameObject gameObject)
    {
        SpriteRenderer[] renderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer.sprite != null && IsHeavenSprite(renderer.sprite))
            {
                return renderer;
            }
        }

        return GetTargetSpriteRenderer(gameObject);
    }

    private static bool IsHeavenSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            return false;
        }

        return sprite.name.Contains("HeavenPlatform") ||
               sprite.name.Contains("HorizontalBlock") ||
               sprite.name.Contains("VerticalBlock") ||
               sprite.name.Contains("VeticalBlock");
    }

    private static SpriteRenderer GetTargetSpriteRenderer(GameObject gameObject)
    {
        Transform visual = gameObject.transform.Find("Visual");
        if (visual != null)
        {
            SpriteRenderer visualRenderer = visual.GetComponent<SpriteRenderer>();
            if (visualRenderer != null)
            {
                return visualRenderer;
            }
        }

        SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            return spriteRenderer;
        }

        return gameObject.GetComponentInChildren<SpriteRenderer>(true);
    }
}
