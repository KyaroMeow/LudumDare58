using System.Collections.Generic;
using UnityEngine;

public class ElectricPanelVoltageSegmentIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer sourceFillRenderer;
    [SerializeField] private Renderer sourceBackgroundRenderer;
    [SerializeField] private Transform segmentParent;

    [Header("Existing Scene Segments")]
    [SerializeField] private int segmentCount = 14;
    [SerializeField] private bool autoCollectSegments = true;
    [SerializeField] private string segmentNamePrefix = "VoltageSegment_";

    [Tooltip("Never rebuild, create, delete, move or rescale segments at runtime. Use already authored scene objects only.")]
    [SerializeField] private bool preserveAuthoredSegments = true;

    [Tooltip("If true, inactive segments stay visible but dark. If false, inactive segments are disabled.")]
    [SerializeField] private bool showInactiveSegmentsDim = true;

    [Header("Shift Visibility")]
    [SerializeField] private bool hideUntilShiftStarted = true;
    [SerializeField] private bool showWhenGameManagerMissing = false;

    [Tooltip("Optional root of the whole indicator visual. If empty, script tries to find indicator_line / voltage root automatically.")]
    [SerializeField] private Transform visibilityRoot;

    [SerializeField] private bool autoResolveVisibilityRoot = true;
    [SerializeField] private bool includeSourceRenderersInVisibility = true;
    [SerializeField] private bool includeThisObjectRenderersInVisibility = true;
    [SerializeField] private bool includeSceneFallbackRenderers = true;

    [SerializeField]
    private string[] visibilityRootNameKeywords =
    {
        "ElectricPanel_VoltageIndicator",
        "VoltageIndicator",
        "indicator_line",
        "VoltageSegment"
    };

    [Header("Color Control")]
    [SerializeField] private bool overrideSegmentColors = true;

    [SerializeField] private Color lowColor = new Color(0.0f, 1.0f, 0.25f, 1f);
    [SerializeField] private Color mediumColor = new Color(1.0f, 0.72f, 0.0f, 1f);
    [SerializeField] private Color highColor = new Color(1.0f, 0.05f, 0.0f, 1f);
    [SerializeField] private Color inactiveColor = new Color(0.0f, 0.08f, 0.025f, 1f);

    [Tooltip("Point where yellow/orange zone starts.")]
    [Range(0f, 1f)]
    [SerializeField] private float mediumThreshold = 0.35f;

    [Tooltip("Point where red zone starts.")]
    [Range(0f, 1f)]
    [SerializeField] private float highThreshold = 0.65f;

    [Header("Voltage Motion")]
    [SerializeField] private bool enableVoltageJitter = true;

    [Tooltip("Only affects brightness of the last active segment. Does not move geometry.")]
    [SerializeField] private float edgeFlickerIntensity = 0.25f;

    [SerializeField] private float edgeFlickerSpeed = 12f;

    [Tooltip("Ready state uses full active bar and subtle pulse.")]
    [SerializeField] private bool pulseOnReady = true;

    [Tooltip("Restore warning blinks active segments.")]
    [SerializeField] private bool blinkOnRestoreWarning = true;

    [Header("Source Art Visibility")]
    [SerializeField] private bool keepOriginalOsnovaLightAsBackgroundGlow = true;
    [SerializeField] private bool hideOriginalOsnovaLight = false;

    [Header("Debug")]
    [SerializeField] private bool logMissingSegmentsOnce = true;

    private struct VisibilityRendererState
    {
        public Renderer Renderer;
        public bool InitiallyEnabled;
    }

    private readonly List<Renderer> segmentRenderers = new List<Renderer>();
    private readonly List<VisibilityRendererState> visibilityRendererStates = new List<VisibilityRendererState>();

    private MaterialPropertyBlock propertyBlock;

    private int colorPropertyId;
    private int baseColorPropertyId;
    private int emissionColorPropertyId;
    private bool shaderIdsReady;

    private float charge01;
    private bool isReady;
    private bool isBlackout;
    private bool isRestoreWarning;
    private bool isLockedOrCooldown;

    private bool missingSegmentsWarningPrinted;
    private bool visibilityRenderersCached;
    private bool indicatorVisible = true;

    private void Awake()
    {
        InitShaderIds();
        EnsurePropertyBlock();
        CollectSegments();
        ResolveVisibilityRoot();
        CacheVisibilityRenderers();
        ApplyVisual();
    }

    private void Start()
    {
        CollectSegments();
        ResolveVisibilityRoot();
        CacheVisibilityRenderers();
        ApplyVisual();
    }

    private void Update()
    {
        if (!Application.isPlaying || !hideUntilShiftStarted)
        {
            return;
        }

        if (!ShouldIndicatorBeVisible())
        {
            SetIndicatorRenderersVisible(false, true);
            return;
        }

        if (!indicatorVisible)
        {
            ApplyVisual();
        }
    }

    private void OnValidate()
    {
        segmentCount = Mathf.Max(1, segmentCount);
        mediumThreshold = Mathf.Clamp01(mediumThreshold);
        highThreshold = Mathf.Clamp01(highThreshold);

        if (highThreshold < mediumThreshold)
        {
            highThreshold = mediumThreshold;
        }

        visibilityRenderersCached = false;

        if (!Application.isPlaying)
        {
            CollectSegments();
            ResolveVisibilityRoot();
            CacheVisibilityRenderers();
            ApplyVisual();
        }
    }

    public void SetVoltage(float value, bool ready, bool blackout, bool restoreWarning, bool lockedOrCooldown)
    {
        charge01 = Mathf.Clamp01(value);
        isReady = ready;
        isBlackout = blackout;
        isRestoreWarning = restoreWarning;
        isLockedOrCooldown = lockedOrCooldown;

        ApplyVisual();
    }

    public void SetWarning(bool warning)
    {
        isRestoreWarning = warning;
        ApplyVisual();
    }

    public void SetBlackout(float blackout01)
    {
        charge01 = Mathf.Clamp01(blackout01);
        isBlackout = true;
        ApplyVisual();
    }

    [ContextMenu("Refresh Existing Segments")]
    public void RefreshExistingSegments()
    {
        visibilityRenderersCached = false;
        CollectSegments(force: true);
        ResolveVisibilityRoot();
        CacheVisibilityRenderers();
        ApplyVisual();
    }

    [ContextMenu("Preview Voltage 0%")]
    private void PreviewVoltage0()
    {
        SetVoltage(0f, false, false, false, true);
    }

    [ContextMenu("Preview Voltage 50%")]
    private void PreviewVoltage50()
    {
        SetVoltage(0.5f, false, false, false, false);
    }

    [ContextMenu("Preview Voltage 100%")]
    private void PreviewVoltage100()
    {
        SetVoltage(1f, true, false, false, false);
    }

    private bool ShouldIndicatorBeVisible()
    {
        if (!hideUntilShiftStarted)
        {
            return true;
        }

        if (!Application.isPlaying)
        {
            return true;
        }

        GameManager gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            return showWhenGameManagerMissing;
        }

        return gameManager.isGameStarted;
    }

    private void SetIndicatorRenderersVisible(bool visible, bool force = false)
    {
        if (!force && indicatorVisible == visible)
        {
            return;
        }

        ResolveVisibilityRoot();
        CacheVisibilityRenderers();

        indicatorVisible = visible;

        for (int i = 0; i < visibilityRendererStates.Count; i++)
        {
            Renderer rendererItem = visibilityRendererStates[i].Renderer;

            if (rendererItem == null)
            {
                continue;
            }

            rendererItem.enabled = visible && visibilityRendererStates[i].InitiallyEnabled;
        }
    }

    private void ResolveVisibilityRoot()
    {
        if (visibilityRoot != null)
        {
            return;
        }

        if (!autoResolveVisibilityRoot)
        {
            return;
        }

        Transform resolvedRoot = FindIndicatorRoot(sourceFillRenderer != null ? sourceFillRenderer.transform : null);

        if (resolvedRoot == null)
        {
            resolvedRoot = FindIndicatorRoot(sourceBackgroundRenderer != null ? sourceBackgroundRenderer.transform : null);
        }

        if (resolvedRoot == null)
        {
            resolvedRoot = FindIndicatorRoot(segmentParent);
        }

        if (resolvedRoot == null)
        {
            resolvedRoot = FindIndicatorRoot(transform);
        }

        if (resolvedRoot != null)
        {
            visibilityRoot = resolvedRoot;
        }
    }

    private Transform FindIndicatorRoot(Transform start)
    {
        Transform current = start;

        while (current != null)
        {
            if (NameContainsKeyword(current.name, visibilityRootNameKeywords))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private void CacheVisibilityRenderers()
    {
        if (visibilityRenderersCached && !HasNullVisibilityRenderer())
        {
            return;
        }

        visibilityRendererStates.Clear();

        if (includeSourceRenderersInVisibility)
        {
            AddVisibilityRenderer(sourceFillRenderer);
            AddVisibilityRenderer(sourceBackgroundRenderer);
        }

        if (visibilityRoot != null)
        {
            AddVisibilityRenderers(visibilityRoot.GetComponentsInChildren<Renderer>(true));
        }

        if (segmentParent != null)
        {
            AddVisibilityRenderers(segmentParent.GetComponentsInChildren<Renderer>(true));
        }

        if (includeThisObjectRenderersInVisibility)
        {
            AddVisibilityRenderers(GetComponentsInChildren<Renderer>(true));
        }

        if (includeSceneFallbackRenderers)
        {
            AddSceneFallbackVisibilityRenderers();
        }

        visibilityRenderersCached = true;
    }

    private void AddSceneFallbackVisibilityRenderers()
    {
        Renderer[] sceneRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        for (int i = 0; i < sceneRenderers.Length; i++)
        {
            Renderer rendererItem = sceneRenderers[i];

            if (rendererItem == null)
            {
                continue;
            }

            if (TransformChainContainsKeyword(rendererItem.transform, visibilityRootNameKeywords))
            {
                AddVisibilityRenderer(rendererItem);
            }
        }
    }

    private bool TransformChainContainsKeyword(Transform start, string[] keywords)
    {
        Transform current = start;

        while (current != null)
        {
            if (NameContainsKeyword(current.name, keywords))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void AddVisibilityRenderers(Renderer[] renderers)
    {
        if (renderers == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            AddVisibilityRenderer(renderers[i]);
        }
    }

    private void AddVisibilityRenderer(Renderer rendererItem)
    {
        if (rendererItem == null || HasVisibilityRenderer(rendererItem))
        {
            return;
        }

        visibilityRendererStates.Add(new VisibilityRendererState
        {
            Renderer = rendererItem,
            InitiallyEnabled = rendererItem.enabled
        });
    }

    private bool HasVisibilityRenderer(Renderer rendererItem)
    {
        for (int i = 0; i < visibilityRendererStates.Count; i++)
        {
            if (visibilityRendererStates[i].Renderer == rendererItem)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasNullVisibilityRenderer()
    {
        for (int i = 0; i < visibilityRendererStates.Count; i++)
        {
            if (visibilityRendererStates[i].Renderer == null)
            {
                return true;
            }
        }

        return false;
    }

    private void CollectSegments(bool force = false)
    {
        if (!autoCollectSegments && !force && segmentRenderers.Count > 0)
        {
            return;
        }

        if (!force && segmentRenderers.Count > 0 && !HasNullRenderer())
        {
            return;
        }

        segmentRenderers.Clear();

        if (segmentParent == null)
        {
            if (logMissingSegmentsOnce && !missingSegmentsWarningPrinted)
            {
                missingSegmentsWarningPrinted = true;
                Debug.LogWarning("ElectricPanelVoltageSegmentIndicator: Segment Parent is not assigned. Assign ElectricPanel_VoltageSegments.", this);
            }

            return;
        }

        for (int i = 0; i < segmentCount; i++)
        {
            Transform segment = segmentParent.Find($"{segmentNamePrefix}{i:00}");

            if (segment == null)
            {
                continue;
            }

            Renderer renderer = segment.GetComponent<Renderer>();

            if (renderer != null)
            {
                segmentRenderers.Add(renderer);
            }
        }

        if (segmentRenderers.Count == 0)
        {
            for (int i = 0; i < segmentParent.childCount; i++)
            {
                Transform child = segmentParent.GetChild(i);

                if (child == null)
                {
                    continue;
                }

                if (!child.name.StartsWith(segmentNamePrefix))
                {
                    continue;
                }

                Renderer renderer = child.GetComponent<Renderer>();

                if (renderer != null)
                {
                    segmentRenderers.Add(renderer);
                }
            }
        }

        segmentRenderers.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        if (segmentRenderers.Count == 0 && logMissingSegmentsOnce && !missingSegmentsWarningPrinted)
        {
            missingSegmentsWarningPrinted = true;
            Debug.LogWarning("ElectricPanelVoltageSegmentIndicator: no VoltageSegment renderers found under Segment Parent.", this);
        }
    }

    private void ApplyVisual()
    {
        if (!ShouldIndicatorBeVisible())
        {
            SetIndicatorRenderersVisible(false, true);
            return;
        }

        SetIndicatorRenderersVisible(true);

        if (segmentRenderers.Count == 0 || HasNullRenderer())
        {
            CollectSegments();
        }

        ApplySourceVisibility();

        if (segmentRenderers.Count == 0)
        {
            return;
        }

        float visualCharge = ResolveVisualCharge();
        int activeCount = ResolveActiveSegmentCount(visualCharge);

        for (int i = 0; i < segmentRenderers.Count; i++)
        {
            Renderer renderer = segmentRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            bool active = i < activeCount;
            bool isEdge = active && i == activeCount - 1;

            if (isRestoreWarning && blinkOnRestoreWarning)
            {
                bool blinkOn = Mathf.FloorToInt(Time.time * edgeFlickerSpeed) % 2 == 0;

                if (!blinkOn)
                {
                    active = false;
                }
            }

            renderer.enabled = active || showInactiveSegmentsDim;

            if (!overrideSegmentColors)
            {
                renderer.SetPropertyBlock(null);
                continue;
            }

            Color color = active
                ? ResolveSegmentColor(i)
                : inactiveColor;

            float brightness = ResolveBrightness(i, isEdge, active);

            ApplyRendererColor(renderer, color * brightness);
        }
    }

    private float ResolveVisualCharge()
    {
        if (isLockedOrCooldown)
        {
            return 0f;
        }

        if (isReady)
        {
            return 1f;
        }

        float visual = charge01;

        if (enableVoltageJitter && !isLockedOrCooldown)
        {
            float amplitude = Mathf.Lerp(0.005f, 0.025f, charge01);
            float noise = Mathf.PerlinNoise(Time.time * edgeFlickerSpeed, 2.771f) - 0.5f;
            visual += noise * amplitude;
        }

        return Mathf.Clamp01(visual);
    }

    private int ResolveActiveSegmentCount(float visualCharge)
    {
        if (isLockedOrCooldown)
        {
            return 0;
        }

        if (isReady)
        {
            return segmentRenderers.Count;
        }

        int count = Mathf.FloorToInt(visualCharge * segmentRenderers.Count + 0.001f);

        if (visualCharge > 0.02f && count == 0)
        {
            count = 1;
        }

        return Mathf.Clamp(count, 0, segmentRenderers.Count);
    }

    private Color ResolveSegmentColor(int index)
    {
        int count = Mathf.Max(1, segmentRenderers.Count);
        float t = count <= 1 ? 1f : index / (float)(count - 1);

        if (isRestoreWarning)
        {
            return highColor;
        }

        if (t >= highThreshold)
        {
            float highT = Mathf.InverseLerp(highThreshold, 1f, t);
            return Color.Lerp(mediumColor, highColor, highT);
        }

        if (t >= mediumThreshold)
        {
            float midT = Mathf.InverseLerp(mediumThreshold, highThreshold, t);
            return Color.Lerp(lowColor, mediumColor, midT);
        }

        return lowColor;
    }

    private float ResolveBrightness(int index, bool isEdge, bool active)
    {
        if (!active)
        {
            return 1f;
        }

        float brightness = 1f;

        if (isReady && pulseOnReady)
        {
            brightness += Mathf.Sin(Time.time * edgeFlickerSpeed) * 0.08f;
        }

        if (enableVoltageJitter && isEdge)
        {
            float noise = Mathf.PerlinNoise(Time.time * edgeFlickerSpeed, index * 1.317f);
            brightness -= noise * edgeFlickerIntensity;
        }

        if (isBlackout)
        {
            brightness *= 0.85f;
        }

        return Mathf.Clamp(brightness, 0.35f, 1.25f);
    }

    private void ApplySourceVisibility()
    {
        bool sourceVisible = indicatorVisible && !hideOriginalOsnovaLight && keepOriginalOsnovaLightAsBackgroundGlow;

        if (sourceFillRenderer != null)
        {
            sourceFillRenderer.enabled = sourceVisible;
        }

        if (sourceBackgroundRenderer != null)
        {
            sourceBackgroundRenderer.enabled = sourceVisible;
        }
    }

    private void ApplyRendererColor(Renderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        EnsurePropertyBlock();
        InitShaderIds();

        if (propertyBlock == null)
        {
            return;
        }

        renderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetColor(colorPropertyId, color);
        propertyBlock.SetColor(baseColorPropertyId, color);
        propertyBlock.SetColor(emissionColorPropertyId, color);

        renderer.SetPropertyBlock(propertyBlock);
    }

    private void EnsurePropertyBlock()
    {
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }

    private void InitShaderIds()
    {
        if (shaderIdsReady)
        {
            return;
        }

        colorPropertyId = Shader.PropertyToID("_Color");
        baseColorPropertyId = Shader.PropertyToID("_BaseColor");
        emissionColorPropertyId = Shader.PropertyToID("_EmissionColor");

        shaderIdsReady = true;
    }

    private bool HasNullRenderer()
    {
        for (int i = 0; i < segmentRenderers.Count; i++)
        {
            if (segmentRenderers[i] == null)
            {
                return true;
            }
        }

        return false;
    }

    private bool NameContainsKeyword(string objectName, string[] keywords)
    {
        if (string.IsNullOrEmpty(objectName) || keywords == null)
        {
            return false;
        }

        for (int i = 0; i < keywords.Length; i++)
        {
            string keyword = keywords[i];

            if (!string.IsNullOrEmpty(keyword) &&
                objectName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}