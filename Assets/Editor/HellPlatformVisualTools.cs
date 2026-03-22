using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HellPlatformVisualTools
{
    private const string PlatformPrefabPath = "Assets/Prefabs/GroundPlatform (1).prefab";
    private const string VisualPrefabPath = "Assets/Prefabs/Visual.prefab";
    private const string VisualChildName = "Visual";

    [MenuItem("Tools/Hell/Add Missing Platform Visuals")]
    public static void AddMissingPlatformVisuals()
    {
        var visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
        var platformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlatformPrefabPath);

        if (visualPrefab == null)
        {
            Debug.LogError($"Could not load visual prefab at {VisualPrefabPath}.");
            return;
        }

        if (platformPrefab == null)
        {
            Debug.LogError($"Could not load platform prefab at {PlatformPrefabPath}.");
            return;
        }

        var platformVisual = platformPrefab.transform.Find(VisualChildName);
        if (platformVisual == null)
        {
            Debug.LogError(
                $"The platform prefab at {PlatformPrefabPath} does not contain a child named {VisualChildName}.");
            return;
        }

        int updatedCount = 0;
        var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();

        foreach (var root in roots)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (!LooksLikeGroundPlatform(transform))
                {
                    continue;
                }

                if (transform.Find(VisualChildName) != null)
                {
                    continue;
                }

                var visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, transform);
                visualInstance.name = VisualChildName;

                var visualTransform = visualInstance.transform;
                visualTransform.localPosition = platformVisual.localPosition;
                visualTransform.localRotation = platformVisual.localRotation;
                visualTransform.localScale = platformVisual.localScale;

                Undo.RegisterCreatedObjectUndo(visualInstance, "Add platform visual");
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        Debug.Log($"Added visuals to {updatedCount} platform(s) in {EditorSceneManager.GetActiveScene().name}.");
    }

    [MenuItem("Tools/Hell/Fit Platform Visuals To Colliders")]
    public static void FitPlatformVisualsToColliders()
    {
        int updatedCount = 0;
        var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();

        foreach (var root in roots)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (!LooksLikeGroundPlatform(transform))
                {
                    continue;
                }

                var visual = transform.Find(VisualChildName);
                if (visual == null)
                {
                    continue;
                }

                var collider = transform.GetComponent<BoxCollider2D>();
                var visualRenderer = visual.GetComponent<SpriteRenderer>();
                if (collider == null || visualRenderer == null || visualRenderer.sprite == null)
                {
                    continue;
                }

                Vector2 spriteSize = visualRenderer.sprite.bounds.size;
                if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                {
                    continue;
                }

                Undo.RecordObject(visual, "Fit platform visual");
                Undo.RecordObject(visualRenderer, "Fit platform visual");

                visual.localPosition = new Vector3(collider.offset.x, collider.offset.y, visual.localPosition.z);
                visual.localRotation = Quaternion.identity;
                visual.localScale = new Vector3(
                    collider.size.x / spriteSize.x,
                    collider.size.y / spriteSize.y,
                    1f);

                visualRenderer.sortingOrder = 2;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        Debug.Log($"Fitted {updatedCount} platform visual(s) in {EditorSceneManager.GetActiveScene().name}.");
    }

    private static bool LooksLikeGroundPlatform(Transform transform)
    {
        if (!transform.name.Contains("GroundPlatform"))
        {
            return false;
        }

        return transform.GetComponent<BoxCollider2D>() != null &&
               transform.GetComponent<SpriteRenderer>() != null;
    }
}
