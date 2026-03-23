using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class CitySectionGeneratorWindow : EditorWindow
{
    private enum DifficultyMode
    {
        Easy,
        Medium,
        Hard,
        Mixed
    }

    private enum ChallengePattern
    {
        Sprint,
        Slide,
        Ledge,
        SprintLedge,
        SprintSlide,
        RecoveryDip
    }

    private struct PlatformPlacement
    {
        public Vector2 TopCenter;
        public float Width;
        public float Height;
        public bool IsLedge;
    }

    private const string CityPlatformSpritePath = "Assets/Art/City/Platforms/PlatformNoBG.png";
    private const string HorizontalBlockSpritePath = "Assets/Art/City/Hazards/Horizontal Block.png";
    private const string VerticalBlockSpritePath = "Assets/Art/City/Hazards/Vertical Block.png";

    private const float StandardPlatformHeight = 1.05f;
    private const float LedgePlatformWidth = 1.1f;
    private const float LedgePlatformHeight = 1.9f;
    private const float SlideCeilingHeight = 0.72f;
    private const float SlideClearance = 0.5f;
    private const float VerticalPostWidth = 0.55f;
    private const float SlideExitRunway = 2.25f;
    private const float OccupiedHorizontalPadding = 3.4f;
    private const float OccupiedVerticalPadding = 2.1f;
    private const float HazardBlockerOffset = 1.15f;
    private const float SmallBlockerHeight = 2.2f;
    private const float LargeBlockerHeight = 4.6f;

    // Movement-derived planning caps from the current controller values in Level_01.
    private const float MaxNormalJumpEdgeGap = 5.9f;
    private const float MaxSprintJumpEdgeGap = 7.2f;
    private const float MaxSlideJumpEdgeGap = 6.5f;
    private const float MaxJumpRise = 2.15f;
    private const float MaxLedgeRise = 1.28f;
    private const float JumpLaneHeight = 2.7f;
    private const float LandingHeadroom = 1.15f;

    private DifficultyMode difficulty = DifficultyMode.Mixed;
    private bool useSelectionAsStart = true;
    private Vector2 manualStart = new Vector2(18.96f, 51.11f);
    private int challengeCount = 9;
    private float overallHalfWidth = 125f;
    private int seed = 12345;
    private bool randomizeSeed = true;
    private string containerName = "Generated City Segment";

    private Sprite cityPlatformSprite;
    private Sprite horizontalBlockSprite;
    private Sprite verticalBlockSprite;
    private float routeDirection = 1f;

    private readonly List<Rect> occupiedRects = new();
    private readonly Dictionary<GameObject, Rect> platformRects = new();

    [MenuItem("Tools/City/Generate Upward Segment")]
    public static void OpenWindow()
    {
        GetWindow<CitySectionGeneratorWindow>("City Segment Generator");
    }

    private void OnEnable()
    {
        cityPlatformSprite = LoadSpriteAtPath(CityPlatformSpritePath);
        horizontalBlockSprite = LoadSpriteAtPath(HorizontalBlockSpritePath);
        verticalBlockSprite = LoadSpriteAtPath(VerticalBlockSpritePath);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("City Segment Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Builds one upward city route with bigger sideways travel, stronger climb, and placements clamped to reachable sprint/jump/ledge bands.",
            MessageType.Info);

        difficulty = (DifficultyMode)EditorGUILayout.EnumPopup("Difficulty", difficulty);
        challengeCount = EditorGUILayout.IntSlider("Challenge Count", challengeCount, 6, 24);
        overallHalfWidth = EditorGUILayout.Slider("Horizontal Drift", overallHalfWidth, 40f, 170f);
        useSelectionAsStart = EditorGUILayout.Toggle("Use Selection As Start", useSelectionAsStart);
        using (new EditorGUI.DisabledScope(useSelectionAsStart))
        {
            manualStart = EditorGUILayout.Vector2Field("Manual Start", manualStart);
        }

        randomizeSeed = EditorGUILayout.Toggle("Randomize Seed", randomizeSeed);
        using (new EditorGUI.DisabledScope(randomizeSeed))
        {
            seed = EditorGUILayout.IntField("Seed", seed);
        }

        containerName = EditorGUILayout.TextField("Container Name", containerName);

        using (new EditorGUI.DisabledScope(cityPlatformSprite == null))
        {
            if (GUILayout.Button("Generate Segment"))
            {
                GenerateSegment();
            }
        }

        if (cityPlatformSprite == null)
        {
            EditorGUILayout.HelpBox($"Missing city platform sprite at {CityPlatformSpritePath}", MessageType.Error);
        }
    }

    private void GenerateSegment()
    {
        if (cityPlatformSprite == null)
        {
            Debug.LogError($"Could not load city platform sprite at {CityPlatformSpritePath}.");
            return;
        }

        occupiedRects.Clear();
        platformRects.Clear();

        Vector2 start = ResolveStartPosition();
        int activeSeed = randomizeSeed ? Environment.TickCount : seed;
        System.Random random = new(activeSeed);
        routeDirection = random.NextDouble() < 0.5 ? -1f : 1f;

        GameObject container = new GameObject($"{containerName} {DateTime.Now:HHmmss}");
        Undo.RegisterCreatedObjectUndo(container, "Generate city segment");

        float originX = start.x;
        float highestY = start.y;
        ChallengePattern? lastPattern = null;

        PlatformPlacement currentPlacement = new()
        {
            TopCenter = start,
            Width = 11f,
            Height = StandardPlatformHeight,
            IsLedge = false
        };

        GameObject currentPlatform = CreatePlatformTop(container.transform, currentPlacement, "City Start Platform");
        RegisterPlatformRect(currentPlatform, currentPlacement);

        for (int i = 0; i < challengeCount; i++)
        {
            ChallengePattern pattern = ChoosePattern(random, i, lastPattern);
            PlatformPlacement nextPlacement = BuildPattern(container.transform, random, originX, ref highestY, currentPlacement, currentPlatform, pattern, i + 1);
            currentPlatform = CreatePlatformTop(container.transform, nextPlacement, GetPlatformName(pattern, i + 1));
            RegisterPlatformRect(currentPlatform, nextPlacement);
            currentPlacement = nextPlacement;
            lastPattern = pattern;
        }

        Selection.activeGameObject = container;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Generated city segment with seed {activeSeed} and {challengeCount} challenges.");
    }

    private PlatformPlacement BuildPattern(
        Transform parent,
        System.Random random,
        float originX,
        ref float highestY,
        PlatformPlacement current,
        GameObject currentPlatform,
        ChallengePattern pattern,
        int index)
    {
        float currentWidth;
        float nextWidth;
        float nextHeight = StandardPlatformHeight;
        float targetGap;
        float targetRise;
        float maxGap;
        float maxRise;
        bool nextIsLedge = false;
        bool addSlide = false;
        bool addFrontBlocker = false;
        bool allowLargeBlocker = false;

        switch (pattern)
        {
            case ChallengePattern.Sprint:
                currentWidth = Range(random, 11.6f, 13.4f);
                nextWidth = Range(random, 3.2f, 3.9f);
                targetGap = Range(random, 6.7f, 7f);
                targetRise = Range(random, 1.75f, 2f);
                maxGap = MaxSprintJumpEdgeGap;
                maxRise = MaxJumpRise;
                addFrontBlocker = true;
                allowLargeBlocker = true;
                break;
            case ChallengePattern.Slide:
                currentWidth = Range(random, 12.4f, 14f);
                nextWidth = Range(random, 4f, 4.7f);
                targetGap = Range(random, 5.7f, 6.1f);
                targetRise = Range(random, 1.45f, 1.75f);
                maxGap = MaxSlideJumpEdgeGap;
                maxRise = MaxJumpRise;
                addSlide = true;
                break;
            case ChallengePattern.Ledge:
                currentWidth = Range(random, 10.4f, 11.8f);
                nextWidth = LedgePlatformWidth;
                nextHeight = LedgePlatformHeight;
                targetGap = Range(random, 5.2f, 5.5f);
                targetRise = Range(random, 1.02f, 1.18f);
                maxGap = MaxNormalJumpEdgeGap;
                maxRise = MaxLedgeRise;
                nextIsLedge = true;
                addFrontBlocker = true;
                break;
            case ChallengePattern.SprintLedge:
                currentWidth = Range(random, 11.8f, 13.2f);
                nextWidth = LedgePlatformWidth;
                nextHeight = LedgePlatformHeight;
                targetGap = Range(random, 6.3f, 6.7f);
                targetRise = Range(random, 1.08f, 1.22f);
                maxGap = MaxSprintJumpEdgeGap - 0.1f;
                maxRise = MaxLedgeRise;
                nextIsLedge = true;
                addFrontBlocker = true;
                allowLargeBlocker = true;
                break;
            case ChallengePattern.SprintSlide:
                currentWidth = Range(random, 12.6f, 14.2f);
                nextWidth = Range(random, 3.2f, 3.9f);
                targetGap = Range(random, 6.2f, 6.6f);
                targetRise = Range(random, 1.65f, 1.95f);
                maxGap = MaxSprintJumpEdgeGap - 0.15f;
                maxRise = MaxJumpRise;
                addSlide = true;
                addFrontBlocker = true;
                allowLargeBlocker = true;
                break;
            case ChallengePattern.RecoveryDip:
                currentWidth = Range(random, 9.8f, 11.2f);
                nextWidth = Range(random, 3.8f, 4.5f);
                targetGap = Range(random, 5.4f, 5.8f);
                targetRise = -Range(random, 0.25f, 0.45f);
                maxGap = MaxNormalJumpEdgeGap;
                maxRise = 0.6f;
                break;
            default:
                currentWidth = 11f;
                nextWidth = 4f;
                targetGap = 5.5f;
                targetRise = 1.4f;
                maxGap = MaxNormalJumpEdgeGap;
                maxRise = MaxJumpRise;
                break;
        }

        UpdatePlatformWidth(currentPlatform, current, currentWidth);
        current.Width = currentWidth;

        float direction = ChooseRouteDirection(random, originX, current.TopCenter.x);

        if (addSlide)
        {
            CreateSlideTunnel(parent, current, direction, index);
        }

        if (addFrontBlocker)
        {
            CreateFrontBlocker(parent, current, direction, index, allowLargeBlocker && index % 4 == 0);
        }

        return FindPlacement(random, originX, ref highestY, current, nextWidth, nextHeight, targetGap, targetRise, maxGap, maxRise, direction, nextIsLedge);
    }

    private PlatformPlacement FindPlacement(
        System.Random random,
        float originX,
        ref float highestY,
        PlatformPlacement current,
        float nextWidth,
        float nextHeight,
        float targetGap,
        float targetRise,
        float maxGap,
        float maxRise,
        float preferredDirection,
        bool nextIsLedge)
    {
        for (int attempt = 0; attempt < 18; attempt++)
        {
            float direction = attempt < 12 ? preferredDirection : -preferredDirection;
            float gap = Mathf.Min(targetGap + ((attempt / 3) * 0.18f), maxGap);
            float rise = targetRise >= 0f
                ? Mathf.Min(targetRise + ((attempt / 4) * 0.08f), maxRise)
                : targetRise;

            float dx = (current.Width * 0.5f) + (nextWidth * 0.5f) + gap;
            float x = ClampX(originX, current.TopCenter.x + (dx * direction));
            float y = targetRise >= 0f
                ? Mathf.Max(current.TopCenter.y + rise, highestY + 1.55f)
                : current.TopCenter.y + rise;

            PlatformPlacement candidate = new()
            {
                TopCenter = new Vector2(x, y),
                Width = nextWidth,
                Height = nextHeight,
                IsLedge = nextIsLedge
            };

            Rect rect = GetPlatformRect(candidate);
            if (!IsAreaClear(rect))
            {
                continue;
            }

            if (!IsTransitionPlayable(current, candidate, gap, rise, nextIsLedge))
            {
                continue;
            }

            if (!HasJumpLaneClear(current, candidate, direction))
            {
                continue;
            }

            highestY = Mathf.Max(highestY, candidate.TopCenter.y);
            routeDirection = direction;
            return candidate;
        }

        PlatformPlacement fallback = new()
        {
            TopCenter = new Vector2(
                ClampX(originX, current.TopCenter.x + (preferredDirection * ((current.Width * 0.5f) + (nextWidth * 0.5f) + Mathf.Min(targetGap + 0.4f, maxGap)))),
                Mathf.Max(current.TopCenter.y + Mathf.Min(Mathf.Max(targetRise, 1.2f), maxRise), highestY + 1.6f)),
            Width = nextWidth,
            Height = nextHeight,
            IsLedge = nextIsLedge
        };

        highestY = Mathf.Max(highestY, fallback.TopCenter.y);
        routeDirection = preferredDirection;
        return fallback;
    }

    private bool IsTransitionPlayable(PlatformPlacement current, PlatformPlacement candidate, float gap, float rise, bool nextIsLedge)
    {
        if (rise < 0f)
        {
            return gap <= MaxNormalJumpEdgeGap;
        }

        float allowedGap = nextIsLedge
            ? (gap > MaxNormalJumpEdgeGap ? MaxSprintJumpEdgeGap : MaxNormalJumpEdgeGap)
            : (gap > MaxNormalJumpEdgeGap ? MaxSprintJumpEdgeGap : MaxNormalJumpEdgeGap);

        float allowedRise = nextIsLedge ? MaxLedgeRise : MaxJumpRise;
        return gap <= allowedGap && rise <= allowedRise;
    }

    private bool HasJumpLaneClear(PlatformPlacement current, PlatformPlacement candidate, float direction)
    {
        float takeoffX = current.TopCenter.x + (direction * ((current.Width * 0.5f) - 0.4f));
        float landingX = candidate.TopCenter.x - (direction * ((candidate.Width * 0.5f) - 0.2f));
        float minX = Mathf.Min(takeoffX, landingX);
        float maxX = Mathf.Max(takeoffX, landingX);
        float laneBottom = Mathf.Min(current.TopCenter.y + 0.8f, candidate.TopCenter.y + 0.45f);
        float laneTop = Mathf.Max(current.TopCenter.y + JumpLaneHeight, candidate.TopCenter.y + LandingHeadroom);
        Rect lane = new Rect(minX, laneBottom, Mathf.Max(maxX - minX, 0.1f), Mathf.Max(laneTop - laneBottom, 0.1f));

        for (int i = 0; i < occupiedRects.Count; i++)
        {
            if (occupiedRects[i].Overlaps(lane))
            {
                return false;
            }
        }

        return true;
    }

    private void CreateSlideTunnel(Transform parent, PlatformPlacement platform, float direction, int index)
    {
        if (horizontalBlockSprite == null || verticalBlockSprite == null)
        {
            return;
        }

        float tunnelWidth = 3.4f;
        float edgeX = platform.TopCenter.x + (direction * (platform.Width * 0.5f));
        float tunnelEndX = edgeX - (direction * SlideExitRunway);
        float tunnelCenterX = tunnelEndX - (direction * (tunnelWidth * 0.5f));
        float ceilingCenterY = platform.TopCenter.y + SlideClearance + (SlideCeilingHeight * 0.5f);
        float postHeight = SlideClearance + SlideCeilingHeight;

        CreateBlock(parent, horizontalBlockSprite, new Vector2(tunnelCenterX, ceilingCenterY), new Vector2(tunnelWidth, SlideCeilingHeight), $"City Slide Ceiling {index}", false, 3);

        float nearPostX = tunnelCenterX - (direction * ((tunnelWidth * 0.5f) - (VerticalPostWidth * 0.5f)));
        float farPostX = tunnelCenterX + (direction * ((tunnelWidth * 0.5f) - (VerticalPostWidth * 0.5f)));

        CreateFloorPost(parent, nearPostX, platform.TopCenter.y, postHeight, $"City Slide Post A {index}");
        CreateFloorPost(parent, farPostX, platform.TopCenter.y, postHeight, $"City Slide Post B {index}");
    }

    private void CreateFrontBlocker(Transform parent, PlatformPlacement platform, float direction, int index, bool large)
    {
        if (verticalBlockSprite == null)
        {
            return;
        }

        float blockerHeight = large ? LargeBlockerHeight : SmallBlockerHeight;
        float blockerX = platform.TopCenter.x + (direction * ((platform.Width * 0.5f) + HazardBlockerOffset));
        float blockerCenterY = platform.TopCenter.y + (blockerHeight * 0.5f) - 0.15f;
        CreateBlock(parent, verticalBlockSprite, new Vector2(blockerX, blockerCenterY), new Vector2(VerticalPostWidth, blockerHeight), $"City Front Blocker {index}", true, 3);
    }

    private void CreateFloorPost(Transform parent, float centerX, float floorTopY, float height, string objectName)
    {
        float centerY = floorTopY + (height * 0.5f);
        CreateBlock(parent, verticalBlockSprite, new Vector2(centerX, centerY), new Vector2(VerticalPostWidth, height), objectName, true, 3);
    }

    private Vector2 ResolveStartPosition()
    {
        if (useSelectionAsStart && Selection.activeTransform != null)
        {
            return Selection.activeTransform.position;
        }

        return manualStart;
    }

    private ChallengePattern ChoosePattern(System.Random random, int index, ChallengePattern? lastPattern)
    {
        List<ChallengePattern> patterns = difficulty switch
        {
            DifficultyMode.Easy => new List<ChallengePattern>
            {
                ChallengePattern.Sprint,
                ChallengePattern.Slide,
                ChallengePattern.Ledge,
                ChallengePattern.RecoveryDip
            },
            DifficultyMode.Medium => new List<ChallengePattern>
            {
                ChallengePattern.Sprint,
                ChallengePattern.Slide,
                ChallengePattern.Ledge,
                ChallengePattern.SprintLedge,
                ChallengePattern.SprintSlide,
                ChallengePattern.RecoveryDip
            },
            DifficultyMode.Hard => new List<ChallengePattern>
            {
                ChallengePattern.Sprint,
                ChallengePattern.Slide,
                ChallengePattern.SprintLedge,
                ChallengePattern.SprintSlide
            },
            _ => new List<ChallengePattern>
            {
                ChallengePattern.Sprint,
                ChallengePattern.Slide,
                ChallengePattern.Ledge,
                ChallengePattern.SprintLedge,
                ChallengePattern.SprintSlide,
                ChallengePattern.RecoveryDip
            }
        };

        if (index == 0)
        {
            return ChallengePattern.Sprint;
        }

        if (index == 1)
        {
            return ChallengePattern.Slide;
        }

        if (index == 2)
        {
            return ChallengePattern.Ledge;
        }

        if (lastPattern.HasValue)
        {
            patterns.Remove(lastPattern.Value);
        }

        return patterns[random.Next(patterns.Count)];
    }

    private float ChooseRouteDirection(System.Random random, float originX, float currentX)
    {
        if (currentX > originX + (overallHalfWidth * 0.72f))
        {
            routeDirection = -1f;
        }
        else if (currentX < originX - (overallHalfWidth * 0.72f))
        {
            routeDirection = 1f;
        }
        else if (random.NextDouble() < 0.22f)
        {
            routeDirection *= -1f;
        }

        return routeDirection;
    }

    private float ClampX(float originX, float x)
    {
        return Mathf.Clamp(x, originX - overallHalfWidth, originX + overallHalfWidth);
    }

    private GameObject CreatePlatformTop(Transform parent, PlatformPlacement placement, string objectName)
    {
        GameObject platform = new(objectName);
        Undo.RegisterCreatedObjectUndo(platform, "Create city platform");
        platform.transform.SetParent(parent);
        platform.transform.position = new Vector3(placement.TopCenter.x, placement.TopCenter.y - (placement.Height * 0.5f), 0f);
        platform.transform.localScale = Vector3.one;
        platform.layer = LayerMask.NameToLayer(placement.IsLedge ? "LedgeGrabbable" : "Ground");

        SpriteRenderer spriteRenderer = platform.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = cityPlatformSprite;
        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        spriteRenderer.size = new Vector2(placement.Width, placement.Height);
        spriteRenderer.sortingOrder = 2;

        BoxCollider2D boxCollider = platform.AddComponent<BoxCollider2D>();
        boxCollider.size = new Vector2(placement.Width, placement.Height);
        boxCollider.offset = Vector2.zero;

        return platform;
    }

    private void UpdatePlatformWidth(GameObject platform, PlatformPlacement placement, float width)
    {
        SpriteRenderer spriteRenderer = platform.GetComponent<SpriteRenderer>();
        BoxCollider2D boxCollider = platform.GetComponent<BoxCollider2D>();

        if (spriteRenderer != null)
        {
            spriteRenderer.size = new Vector2(width, placement.Height);
        }

        if (boxCollider != null)
        {
            boxCollider.size = new Vector2(width, placement.Height);
        }

        UnregisterPlatformRect(platform);
        placement.Width = width;
        RegisterPlatformRect(platform, placement);
    }

    private void CreateBlock(Transform parent, Sprite sprite, Vector2 center, Vector2 size, string objectName, bool vertical, int sortingOrder)
    {
        GameObject block = new(objectName);
        Undo.RegisterCreatedObjectUndo(block, "Create city block");
        block.transform.SetParent(parent);
        block.transform.position = new Vector3(center.x, center.y, 0f);
        block.transform.localScale = Vector3.one;
        block.layer = LayerMask.NameToLayer("Ground");

        SpriteRenderer spriteRenderer = block.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.drawMode = SpriteDrawMode.Sliced;
        spriteRenderer.size = size;
        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.flipX = !vertical;

        BoxCollider2D boxCollider = block.AddComponent<BoxCollider2D>();
        boxCollider.size = size;
        boxCollider.offset = Vector2.zero;

        occupiedRects.Add(ExpandRect(new Rect(center.x - (size.x * 0.5f), center.y - (size.y * 0.5f), size.x, size.y), 0.4f, 0.3f));
    }

    private void RegisterPlatformRect(GameObject platform, PlatformPlacement placement)
    {
        Rect rect = GetPlatformRect(placement);
        occupiedRects.Add(rect);
        platformRects[platform] = rect;
    }

    private void UnregisterPlatformRect(GameObject platform)
    {
        if (!platformRects.TryGetValue(platform, out Rect rect))
        {
            return;
        }

        occupiedRects.Remove(rect);
        platformRects.Remove(platform);
    }

    private bool IsAreaClear(Rect candidate)
    {
        for (int i = 0; i < occupiedRects.Count; i++)
        {
            Rect expanded = ExpandRect(occupiedRects[i], OccupiedHorizontalPadding, OccupiedVerticalPadding);
            if (expanded.Overlaps(candidate))
            {
                return false;
            }
        }

        return true;
    }

    private static Rect GetPlatformRect(PlatformPlacement placement)
    {
        return new Rect(
            placement.TopCenter.x - (placement.Width * 0.5f),
            placement.TopCenter.y - placement.Height,
            placement.Width,
            placement.Height);
    }

    private static Rect ExpandRect(Rect rect, float xPadding, float yPadding)
    {
        return new Rect(
            rect.xMin - xPadding,
            rect.yMin - yPadding,
            rect.width + (xPadding * 2f),
            rect.height + (yPadding * 2f));
    }

    private static float Range(System.Random random, float min, float max)
    {
        return min + ((float)random.NextDouble() * (max - min));
    }

    private static Sprite LoadSpriteAtPath(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static string GetPlatformName(ChallengePattern pattern, int index)
    {
        return pattern switch
        {
            ChallengePattern.Sprint => $"City Sprint Platform {index}",
            ChallengePattern.Slide => $"City Slide Platform {index}",
            ChallengePattern.Ledge => $"City Ledge Platform {index}",
            ChallengePattern.SprintLedge => $"City Sprint Ledge Platform {index}",
            ChallengePattern.SprintSlide => $"City Sprint Slide Platform {index}",
            ChallengePattern.RecoveryDip => $"City Recovery Platform {index}",
            _ => $"City Platform {index}"
        };
    }
}
