using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LedgeGrabbableColorTool
{
    private static readonly Color LedgeColor = new(0.5764706f, 0.75686276f, 0.7372549f, 1f);
    private static readonly Color CityLedgeColor = new(0.72f, 0.96f, 1f, 1f);
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
        GameObject[] gameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int ledgeLayer = LayerMask.NameToLayer("LedgeGrabbable");
        int updatedCount = 0;

        foreach (GameObject gameObject in gameObjects)
        {
            if (gameObject == null || !LooksLikeCityLedgeObject(gameObject))
            {
                continue;
            }

            SpriteRenderer spriteRenderer = GetTargetSpriteRenderer(gameObject);
            if (spriteRenderer == null)
            {
                continue;
            }

            Color targetColor = gameObject.layer == ledgeLayer ? CityLedgeColor : DefaultColor;

            if (spriteRenderer.color == targetColor)
            {
                continue;
            }

            Undo.RecordObject(spriteRenderer, "Apply city ledge grabbable highlight");
            spriteRenderer.color = targetColor;
            EditorUtility.SetDirty(spriteRenderer);
            updatedCount++;
        }

        if (updatedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        Debug.Log($"Applied city ledge highlight to {updatedCount} object(s).");
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

    private static SpriteRenderer GetTargetSpriteRenderer(GameObject gameObject)
    {
        SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            return spriteRenderer;
        }

        Transform visual = gameObject.transform.Find("Visual");
        if (visual != null)
        {
            spriteRenderer = visual.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                return spriteRenderer;
            }
        }

        return gameObject.GetComponentInChildren<SpriteRenderer>();
    }
}
