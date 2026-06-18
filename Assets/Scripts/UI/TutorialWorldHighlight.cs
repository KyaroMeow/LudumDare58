using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialWorldHighlight : MonoBehaviour
{
    private readonly System.Collections.Generic.List<TutorialOutlineState> activeOutlines = new System.Collections.Generic.List<TutorialOutlineState>();
    private TutorialRendererPulse[] activeRendererPulses;
    private TutorialUiGlow[] activeUiGlows;

    private void Update()
    {
        if (activeOutlines.Count == 0)
        {
            return;
        }

        float pulse = (Mathf.Sin(Time.unscaledTime * 4.5f) + 1f) * 0.5f;
        for (int i = 0; i < activeOutlines.Count; i++)
        {
            activeOutlines[i].ApplyPulse(pulse);
        }
    }

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

        for (int i = 0; i < targets.Length; i++)
        {
            Transform target = targets[i];
            if (target == null)
            {
                continue;
            }

            OutlineEffect outline = target.GetComponentInChildren<OutlineEffect>(true);
            bool addedForTutorial = false;
            if (outline == null)
            {
                outline = target.gameObject.AddComponent<OutlineEffect>();
                addedForTutorial = true;
            }

            Color neonColor = BoostNeon(color);
            float neonWidth = Mathf.Clamp(outlineWidth, 2.75f, 4f);
            activeOutlines.Add(new TutorialOutlineState(outline, addedForTutorial, neonColor, neonWidth));
            outline.OutlineMode = OutlineEffect.Mode.OutlineAll;
            outline.OutlineColor = neonColor;
            outline.OutlineWidth = neonWidth;
            outline.enabled = true;
        }
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

            RectTransform targetRect = ResolveUiVisualRect(target);
            if (targetRect == null)
            {
                continue;
            }

            GameObject glowObject = new GameObject("TutorialUiGlow", typeof(RectTransform), typeof(TutorialUiGlow));
            glowObject.transform.SetParent(targetRect, false);

            RectTransform glowRect = glowObject.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-2f, -2f);
            glowRect.offsetMax = new Vector2(2f, 2f);
            glowRect.SetAsLastSibling();

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

        for (int i = 0; i < targets.Length; i++)
        {
            Transform target = targets[i];
            if (target == null)
            {
                continue;
            }

            OutlineEffect outline = target.GetComponentInChildren<OutlineEffect>(true);
            bool addedForTutorial = false;
            if (outline == null)
            {
                outline = target.gameObject.AddComponent<OutlineEffect>();
                addedForTutorial = true;
            }

            Color neonColor = BoostNeon(color);
            float neonWidth = Mathf.Clamp(outlineWidth, 2.75f, 4f);
            activeOutlines.Add(new TutorialOutlineState(outline, addedForTutorial, neonColor, neonWidth));
            outline.OutlineMode = OutlineEffect.Mode.OutlineAll;
            outline.OutlineColor = neonColor;
            outline.OutlineWidth = neonWidth;
            outline.enabled = true;
        }
    }

    private static RectTransform ResolveUiVisualRect(Component target)
    {
        if (target == null)
        {
            return null;
        }

        Selectable selectable = target as Selectable ?? target.GetComponent<Selectable>();
        if (selectable != null && selectable.targetGraphic != null)
        {
            return selectable.targetGraphic.rectTransform;
        }

        Graphic graphic = target as Graphic ?? target.GetComponent<Graphic>();
        if (graphic != null)
        {
            return graphic.rectTransform;
        }

        Selectable childSelectable = target.GetComponentInChildren<Selectable>(true);
        if (childSelectable != null && childSelectable.targetGraphic != null)
        {
            return childSelectable.targetGraphic.rectTransform;
        }

        return target.transform as RectTransform;
    }

    private static Color BoostNeon(Color color)
    {
        return new Color(color.r * 1.75f, color.g * 1.75f, color.b * 1.75f, 1f);
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
    private readonly bool removeOnRestore;
    private readonly bool wasEnabled;
    private readonly OutlineEffect.Mode originalMode;
    private readonly Color originalColor;
    private readonly float originalWidth;
    private readonly Color highlightColor;
    private readonly float highlightWidth;

    public TutorialOutlineState(OutlineEffect outline, bool removeOnRestore, Color highlightColor, float highlightWidth)
    {
        this.outline = outline;
        this.removeOnRestore = removeOnRestore;
        wasEnabled = outline != null && outline.enabled;
        originalMode = outline != null ? outline.OutlineMode : OutlineEffect.Mode.OutlineAll;
        originalColor = outline != null ? outline.OutlineColor : Color.white;
        originalWidth = outline != null ? outline.OutlineWidth : 0f;
        this.highlightColor = highlightColor;
        this.highlightWidth = highlightWidth;
    }

    public void ApplyPulse(float pulse)
    {
        if (outline == null)
        {
            return;
        }

        outline.enabled = true;
        outline.OutlineMode = OutlineEffect.Mode.OutlineAll;
        outline.OutlineColor = Color.Lerp(highlightColor * 0.72f, highlightColor * 1.08f, pulse);
        outline.OutlineWidth = highlightWidth * Mathf.Lerp(0.9f, 1.08f, pulse);
    }

    public void Restore()
    {
        if (outline == null)
        {
            return;
        }

        if (removeOnRestore)
        {
            Object.Destroy(outline);
            return;
        }

        outline.OutlineMode = originalMode;
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
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineSizeId = Shader.PropertyToID("_OutlineSize");

    private Image[] coreLines;
    private Image[] haloLines;
    private Image spriteOutline;
    private Material spriteOutlineMaterial;
    private Color glowColor;
    private Vector3 baseScale;

    public void Configure(Color color)
    {
        glowColor = color;
        baseScale = transform.localScale;
        if (!TryCreateSpriteOutline())
        {
            EnsureBorderLines();
        }
    }

    private void Update()
    {
        if (spriteOutline != null && spriteOutlineMaterial != null)
        {
            float pulse = (Mathf.Sin(Time.unscaledTime * 4.5f) + 1f) * 0.5f;
            Color neonColor = new Color(glowColor.r * 1.65f, glowColor.g * 1.65f, glowColor.b * 1.65f, Mathf.Lerp(0.78f, 1f, pulse));
            spriteOutlineMaterial.SetColor(OutlineColorId, neonColor);
            spriteOutlineMaterial.SetFloat(OutlineSizeId, Mathf.Lerp(1.15f, 2.05f, pulse));
            transform.localScale = baseScale * Mathf.Lerp(1f, 1.012f, pulse);
            return;
        }

        if (coreLines == null || haloLines == null)
        {
            return;
        }

        float borderPulse = (Mathf.Sin(Time.unscaledTime * 4.5f) + 1f) * 0.5f;
        Color coreColor = new Color(glowColor.r, glowColor.g, glowColor.b, Mathf.Lerp(0.78f, 1f, borderPulse));
        Color haloColor = new Color(glowColor.r, glowColor.g, glowColor.b, Mathf.Lerp(0.2f, 0.48f, borderPulse));
        for (int i = 0; i < coreLines.Length; i++)
        {
            coreLines[i].color = coreColor;
            haloLines[i].color = haloColor;
        }

        transform.localScale = baseScale * Mathf.Lerp(1f, 1.025f, borderPulse);
    }

    private void EnsureBorderLines()
    {
        if (coreLines != null)
        {
            return;
        }

        coreLines = CreateBorderSet("NeonCore", 2f, 0f);
        haloLines = CreateBorderSet("NeonHalo", 4f, 1.5f);
    }

    private bool TryCreateSpriteOutline()
    {
        Image sourceImage = transform.parent != null ? transform.parent.GetComponent<Image>() : null;
        Shader outlineShader = Resources.Load<Shader>("Shaders/TutorialNeonUiOutline") ??
                               Shader.Find("Sorter/UI/Tutorial Neon Outline");
        if (sourceImage == null || sourceImage.sprite == null || outlineShader == null)
        {
            return false;
        }

        spriteOutline = gameObject.AddComponent<Image>();
        spriteOutline.sprite = sourceImage.sprite;
        spriteOutline.type = sourceImage.type;
        spriteOutline.preserveAspect = sourceImage.preserveAspect;
        spriteOutline.fillMethod = sourceImage.fillMethod;
        spriteOutline.fillAmount = sourceImage.fillAmount;
        spriteOutline.raycastTarget = false;
        spriteOutline.color = Color.white;

        spriteOutlineMaterial = new Material(outlineShader)
        {
            name = "Tutorial Neon UI Outline (Runtime)",
            hideFlags = HideFlags.DontSave
        };
        spriteOutline.material = spriteOutlineMaterial;
        return true;
    }

    private void OnDestroy()
    {
        if (spriteOutlineMaterial != null)
        {
            Destroy(spriteOutlineMaterial);
        }
    }

    private Image[] CreateBorderSet(string prefix, float thickness, float outwardOffset)
    {
        return new[]
        {
            CreateLine(prefix + "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, outwardOffset), new Vector2(0f, thickness)),
            CreateLine(prefix + "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -outwardOffset), new Vector2(0f, thickness)),
            CreateLine(prefix + "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-outwardOffset, 0f), new Vector2(thickness, 0f)),
            CreateLine(prefix + "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(outwardOffset, 0f), new Vector2(thickness, 0f))
        };
    }

    private Image CreateLine(string objectName, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject lineObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.transform.SetParent(transform, false);
        RectTransform rect = lineObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image line = lineObject.GetComponent<Image>();
        line.raycastTarget = false;
        return line;
    }
}
