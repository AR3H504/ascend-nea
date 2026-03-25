using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HeavenPlatformTools
{
    private const string HeavenSpritePath = "Assets/Art/Heaven/Platforms/HeavenPlatform.png";
    private const string VisualPrefabPath = "Assets/Prefabs/Visual.prefab";
    private const string VisualChildName = "Visual";
    private const float SurfaceYFromCenterUnits = 1.15f;

    [MenuItem("Tools/Heaven/Apply Heaven Sprite To Selected Platforms")]
    public static void ApplyHeavenSpriteToSelectedPlatforms()
    {
        ApplyHeavenSprite(selectedOnly: true);
    }

    [MenuItem("Tools/Heaven/Apply Heaven Sprite To All Ground Platforms")]
    public static void ApplyHeavenSpriteToAllPlatforms()
    {
        ApplyHeavenSprite(selectedOnly: false);
    }

    private static void ApplyHeavenSprite(bool selectedOnly)
    {
        var heavenSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HeavenSpritePath);
        if (heavenSprite == null)
        {
            Debug.LogError($"Could not load Heaven platform sprite at {HeavenSpritePath}.");
            return;
        }

        var targets = selectedOnly ? Selection.transforms : GetAllPlatformTransformsInScene();
        if (targets.Length == 0)
        {
            Debug.LogWarning(selectedOnly
                ? "No platform objects selected. Select one or more GroundPlatform objects and try again."
                : "No GroundPlatform objects were found in the active scene.");
            return;
        }

        int updatedCount = 0;

        foreach (var target in targets)
        {
            if (!LooksLikeGroundPlatform(target))
            {
                continue;
            }

            if (!IsUnderHeavenHierarchy(target))
            {
                continue;
            }

            var collider = target.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                continue;
            }

            var visual = GetOrCreateVisual(target);
            var visualRenderer = visual.GetComponent<SpriteRenderer>();
            if (visualRenderer == null)
            {
                visualRenderer = Undo.AddComponent<SpriteRenderer>(visual.gameObject);
            }

            Undo.RecordObject(visual, "Apply Heaven platform sprite");
            Undo.RecordObject(visualRenderer, "Apply Heaven platform sprite");

            visualRenderer.sprite = heavenSprite;
            visualRenderer.drawMode = SpriteDrawMode.Simple;
            visualRenderer.sortingOrder = 2;

            FitVisualToColliderWidth(target, visual, collider, visualRenderer.sprite);
            updatedCount++;
        }

        if (updatedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        Debug.Log($"Applied Heaven platform visuals to {updatedCount} platform(s) in {EditorSceneManager.GetActiveScene().name}.");
    }

    private static void FitVisualToColliderWidth(Transform platform, Transform visual, BoxCollider2D collider, Sprite sprite)
    {
        Vector2 spriteSize = sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float parentScaleX = Mathf.Abs(platform.lossyScale.x);
        float parentScaleY = Mathf.Abs(platform.lossyScale.y);
        if (parentScaleX <= Mathf.Epsilon || parentScaleY <= Mathf.Epsilon)
        {
            return;
        }

        float desiredWorldWidth = collider.size.x * parentScaleX;
        float worldUniformScale = desiredWorldWidth / spriteSize.x;
        float localScaleX = worldUniformScale / parentScaleX;
        float localScaleY = worldUniformScale / parentScaleY;

        float colliderTop = collider.offset.y + collider.size.y * 0.5f;
        float localSurfaceY = SurfaceYFromCenterUnits * localScaleY;

        visual.localRotation = Quaternion.identity;
        visual.localScale = new Vector3(localScaleX, localScaleY, 1f);
        visual.localPosition = new Vector3(
            collider.offset.x,
            colliderTop - localSurfaceY,
            0f);
    }

    private static Transform GetOrCreateVisual(Transform platform)
    {
        var visual = platform.Find(VisualChildName);
        if (visual != null)
        {
            return visual;
        }

        var visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
        GameObject visualObject;

        if (visualPrefab != null)
        {
            visualObject = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, platform);
            visualObject.name = VisualChildName;
        }
        else
        {
            visualObject = new GameObject(VisualChildName);
            Undo.RegisterCreatedObjectUndo(visualObject, "Create platform visual");
            visualObject.transform.SetParent(platform, false);
        }

        return visualObject.transform;
    }

    private static Transform[] GetAllPlatformTransformsInScene()
    {
        var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        var transforms = new System.Collections.Generic.List<Transform>();

        foreach (var root in roots)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (LooksLikeGroundPlatform(transform))
                {
                    transforms.Add(transform);
                }
            }
        }

        return transforms.ToArray();
    }

    private static bool IsUnderHeavenHierarchy(Transform transform)
    {
        var current = transform;

        while (current != null)
        {
            if (current.name.Contains("Heaven"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool LooksLikeGroundPlatform(Transform transform)
    {
        if (transform == null || !transform.name.Contains("GroundPlatform"))
        {
            return false;
        }

        return transform.GetComponent<BoxCollider2D>() != null &&
               transform.GetComponent<SpriteRenderer>() != null;
    }
}

