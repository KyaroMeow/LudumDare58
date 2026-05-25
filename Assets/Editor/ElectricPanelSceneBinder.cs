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

    private readonly List<Renderer> segmentRenderers = new List<Renderer>();

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

    private void Awake()
    {
        InitShaderIds();
        EnsurePropertyBlock();
        CollectSegments();
        ApplySourceVisibility();
    }

    private void Start()
    {
        CollectSegments();
        ApplySourceVisibility();
        ApplyVisual();
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

        if (!Application.isPlaying)
        {
            CollectSegments();
            ApplySourceVisibility();
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
        CollectSegments(force: true);
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
        bool sourceVisible = !hideOriginalOsnovaLight && keepOriginalOsnovaLightAsBackgroundGlow;

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
}