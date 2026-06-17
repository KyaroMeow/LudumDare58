using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialWorldHighlight : MonoBehaviour
{
    private readonly System.Collections.Generic.List<TutorialOutlineState> activeOutlines = new System.Collections.Generic.List<TutorialOutlineState>();
    private TutorialRendererPulse[] activeRendererPulses;
    private TutorialUiGlow[] activeUiGlows;

    public void HighlightWorldTarget(Transform target, Color color, float outlineWidth)
    {
        HighlightWorldTargets(new[] { target }, color, outlineWidth);
    }

    public void HighlightWorldTargets(Transform[] targets, Color color, float outlineWidth)
    {
        Clear();

        if (targets == null || targets.Length == 0)
        {
            return;
        }

        System.Collections.Generic.List<TutorialRendererPulse> rendererPulses = new System.Collections.Generic.List<TutorialRendererPulse>();
        for (int i = 0; i < targets.Length; i++)
        {
            Transform target = targets[i];
            if (target == null)
            {
                continue;
            }

            OutlineEffect outline = target.GetComponentInChildren<OutlineEffect>(true);
            if (outline != null)
            {
                activeOutlines.Add(new TutorialOutlineState(outline));
                outline.OutlineColor = color;
                outline.OutlineWidth = Mathf.Max(0.5f, outlineWidth);
                outline.enabled = true;
                continue;
            }

            TutorialRendererPulse pulse = target.GetComponent<TutorialRendererPulse>();
            if (pulse == null)
            {
                pulse = target.gameObject.AddComponent<TutorialRendererPulse>();
            }

            pulse.Configure(color);
            pulse.enabled = true;
            rendererPulses.Add(pulse);
        }

        activeRendererPulses = rendererPulses.ToArray();
    }

    public void HighlightUiTarget(Component target, Color color)
    {
        HighlightUiTargets(new[] { target }, color);
    }

    public void HighlightMixedTargets(Transform[] worldTargets, Component[] uiTargets, Color color, float outlineWidth)
    {
        Clear();
        ApplyWorldTargetsWithoutClearing(worldTargets, color, outlineWidth);
        ApplyUiTargetsWithoutClearing(uiTargets, color);
    }

    public void HighlightUiTargets(Component[] targets, Color color)
    {
        Clear();
        ApplyUiTargetsWithoutClearing(targets, color);
    }

    private void ApplyUiTargetsWithoutClearing(Component[] targets, Color color)
    {
        if (targets == null || targets.Length == 0)
        {
            return;
        }

        activeUiGlows = new TutorialUiGlow[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            Component target = targets[i];
            if (target == null)
            {
                continue;
            }

            RectTransform targetRect = target.transform as RectTransform;
            if (targetRect == null)
            {
                continue;
            }

            GameObject glowObject = new GameObject("TutorialUiGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TutorialUiGlow));
            glowObject.transform.SetParent(targetRect, false);

            RectTransform glowRect = glowObject.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-4f, -4f);
            glowRect.offsetMax = new Vector2(4f, 4f);
            glowRect.SetAsLastSibling();

            Image glowImage = glowObject.GetComponent<Image>();
            glowImage.raycastTarget = false;

            TutorialUiGlow glow = glowObject.GetComponent<TutorialUiGlow>();
            glow.Configure(color);
            activeUiGlows[i] = glow;
        }
    }

    private void ApplyWorldTargetsWithoutClearing(Transform[] targets, Color color, float outlineWidth)
    {
        if (targets == null || targets.Length == 0)
        {
            return;
        }

        System.Collections.Generic.List<TutorialRendererPulse> rendererPulses = new System.Collections.Generic.List<TutorialRendererPulse>();
        for (int i = 0; i < targets.Length; i++)
        {
            Transform target = targets[i];
            if (target == null)
            {
                continue;
            }

            OutlineEffect outline = target.GetComponentInChildren<OutlineEffect>(true);
            if (outline != null)
            {
                activeOutlines.Add(new TutorialOutlineState(outline));
                outline.OutlineColor = color;
                outline.OutlineWidth = Mathf.Max(0.5f, outlineWidth);
                outline.enabled = true;
                continue;
            }

            TutorialRendererPulse pulse = target.GetComponent<TutorialRendererPulse>();
            if (pulse == null)
            {
                pulse = target.gameObject.AddComponent<TutorialRendererPulse>();
            }

            pulse.Configure(color);
            pulse.enabled = true;
            rendererPulses.Add(pulse);
        }

        activeRendererPulses = rendererPulses.ToArray();
    }

    public void Clear()
    {
        for (int i = 0; i < activeOutlines.Count; i++)
        {
            activeOutlines[i].Restore();
        }
        activeOutlines.Clear();

        if (activeRendererPulses != null)
        {
            for (int i = 0; i < activeRendererPulses.Length; i++)
            {
                if (activeRendererPulses[i] != null)
                {
                    activeRendererPulses[i].enabled = false;
                }
            }

            activeRendererPulses = null;
        }

        if (activeUiGlows != null)
        {
            for (int i = 0; i < activeUiGlows.Length; i++)
            {
                if (activeUiGlows[i] != null)
                {
                    Destroy(activeUiGlows[i].gameObject);
                }
            }

            activeUiGlows = null;
        }
    }

    private void OnDisable()
    {
        Clear();
    }

    private void OnDestroy()
    {
        Clear();
    }
}

public readonly struct TutorialOutlineState
{
    private readonly OutlineEffect outline;
    private readonly bool wasEnabled;
    private readonly Color originalColor;
    private readonly float originalWidth;

    public TutorialOutlineState(OutlineEffect outline)
    {
        this.outline = outline;
        wasEnabled = outline != null && outline.enabled;
        originalColor = outline != null ? outline.OutlineColor : Color.white;
        originalWidth = outline != null ? outline.OutlineWidth : 0f;
    }

    public void Restore()
    {
        if (outline == null)
        {
            return;
        }

        outline.OutlineColor = originalColor;
        outline.OutlineWidth = originalWidth;
        outline.enabled = wasEnabled;
    }
}

[DisallowMultipleComponent]
public class TutorialRendererPulse : MonoBehaviour
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private Color pulseColor;

    public void Configure(Color color)
    {
        pulseColor = color;
        CacheRenderers();
    }

    private void OnEnable()
    {
        CacheRenderers();
    }

    private void Update()
    {
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }

        float pulse = (Mathf.Sin(Time.unscaledTime * 4.5f) + 1f) * 0.5f;
        Color color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, Mathf.Lerp(0.14f, 0.32f, pulse));
        Color emission = pulseColor * Mathf.Lerp(0.35f, 0.85f, pulse);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            block.SetColor(EmissionColorId, emission);
            targetRenderer.SetPropertyBlock(block);
        }
    }

    private void OnDisable()
    {
        if (renderers == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].SetPropertyBlock(null);
            }
        }
    }

    private void CacheRenderers()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
    }
}

[DisallowMultipleComponent]
public class TutorialUiGlow : MonoBehaviour
{
    private Image image;
    private Color glowColor;

    public void Configure(Color color)
    {
        image = GetComponent<Image>();
        glowColor = color;
        if (image != null)
        {
            image.color = new Color(color.r, color.g, color.b, 0.12f);
            image.raycastTarget = false;
        }
    }

    private void Update()
    {
        if (image == null)
        {
            return;
        }

        float pulse = (Mathf.Sin(Time.unscaledTime * 4.5f) + 1f) * 0.5f;
        image.color = new Color(glowColor.r, glowColor.g, glowColor.b, Mathf.Lerp(0.08f, 0.22f, pulse));
    }
}
