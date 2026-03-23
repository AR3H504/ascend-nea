using UnityEditor;
using UnityEngine;

public static class CityHazardTools
{
    [MenuItem("Tools/City/Fit Selected Box Collider To Sprite")]
    public static void FitSelectedBoxColliderToSprite()
    {
        int updatedCount = 0;

        foreach (GameObject gameObject in Selection.gameObjects)
        {
            if (gameObject == null)
            {
                continue;
            }

            SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            BoxCollider2D boxCollider = gameObject.GetComponent<BoxCollider2D>();

            if (spriteRenderer == null || boxCollider == null || spriteRenderer.sprite == null)
            {
                continue;
            }

            Bounds localBounds = spriteRenderer.sprite.bounds;

            Undo.RecordObject(boxCollider, "Fit box collider to sprite");
            boxCollider.offset = localBounds.center;
            boxCollider.size = localBounds.size;
            EditorUtility.SetDirty(boxCollider);
            updatedCount++;
        }

        Debug.Log($"Fitted box collider on {updatedCount} selected object(s).");
    }
}
