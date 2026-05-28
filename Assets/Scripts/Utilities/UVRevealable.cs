using UnityEngine;

[DisallowMultipleComponent]
public class UVRevealable : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int RevealId = Shader.PropertyToID("_Reveal");
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
    private const float VisibleThreshold = 0.01f;

    [SerializeField] private Renderer targetRenderer;
    [SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0f;
    [SerializeField, Min(0f)] private float hiddenIntensity = 0f;
    [SerializeField, Range(0f, 1f)] private float revealedAlpha = 1f;
    [SerializeField, Min(0f)] private float revealedIntensity = 5.5f;
    [SerializeField, Min(0f)] private float revealSpeed = 1.85f;
    [SerializeField, Min(0f)] private float hideSpeed = 3.25f;
    [SerializeField] private bool keepVisibleWhileRevealed = true;
    [SerializeField] private bool ensureTriggerCollider = true;
    [SerializeField] private bool useRuntimeGlowMaterial = true;
    [SerializeField] private Color glowColor = new Color(0.55f, 0.12f, 1f, 1f);

    private MaterialPropertyBlock propertyBlock;
    private Material runtimeGlowMaterial;
    private Color baseColor = Color.white;
    private Color emissionColor = Color.white;
    private bool hasAlphaProperty;
    private bool hasEmissionProperty;
    private bool hasRevealProperty;
    private bool hasGlowColorProperty;
    private bool hasGlowIntensityProperty;
    private bool warnedMissingRenderer;
    private float currentReveal;
    private float targetReveal;

    public Renderer TargetRenderer
    {
        get
        {
            EnsureInitialized();
            return targetRenderer;
        }
    }

    private void Awake()
    {
        EnsureInitialized();
        ForceHidden();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        ApplyReveal();
    }

    private void OnDisable()
    {
        ForceHidden();
    }

    private void OnDestroy()
    {
        if (runtimeGlowMaterial != null)
        {
            Destroy(runtimeGlowMaterial);
            runtimeGlowMaterial = null;
        }
    }

    private void Update()
    {
        EnsureInitialized();
        if (targetRenderer == null)
        {
            WarnMissingRendererOnce();
            return;
        }

        float speed = targetReveal > currentReveal ? revealSpeed : hideSpeed;
        currentReveal = Mathf.MoveTowards(currentReveal, targetReveal, speed * Time.deltaTime);
        ApplyReveal();

        if (!keepVisibleWhileRevealed && targetReveal > 0f && Mathf.Approximately(currentReveal, targetReveal))
        {
            targetReveal = 0f;
        }
    }

    public void Reveal(float strength = 1f)
    {
        EnsureInitialized();
        targetReveal = Mathf.Max(targetReveal, Mathf.Clamp01(strength));
        ApplyReveal();
    }

    public void Hide()
    {
        targetReveal = 0f;
    }

    public void ForceHidden()
    {
        currentReveal = 0f;
        targetReveal = 0f;
        ApplyReveal();
    }

    public void ForceVisible()
    {
        currentReveal = 1f;
        targetReveal = 1f;
        ApplyReveal();
    }

    private void EnsureInitialized()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        if (useRuntimeGlowMaterial)
        {
            EnsureRuntimeGlowMaterial();
        }

        Material sharedMaterial = targetRenderer != null ? targetRenderer.sharedMaterial : null;
        hasAlphaProperty = sharedMaterial != null && (sharedMaterial.HasProperty(BaseColorId) || sharedMaterial.HasProperty(ColorId));
        hasEmissionProperty = sharedMaterial != null && sharedMaterial.HasProperty(EmissionColorId);
        hasRevealProperty = sharedMaterial != null && sharedMaterial.HasProperty(RevealId);
        hasGlowColorProperty = sharedMaterial != null && sharedMaterial.HasProperty(GlowColorId);
        hasGlowIntensityProperty = sharedMaterial != null && sharedMaterial.HasProperty(GlowIntensityId);

        if (sharedMaterial == null)
        {
            return;
        }

        if (sharedMaterial.HasProperty(BaseColorId))
        {
            baseColor = sharedMaterial.GetColor(BaseColorId);
        }
        else if (sharedMaterial.HasProperty(ColorId))
        {
            baseColor = sharedMaterial.GetColor(ColorId);
        }

        if (hasEmissionProperty)
        {
            emissionColor = sharedMaterial.GetColor(EmissionColorId);
        }

        if (ensureTriggerCollider)
        {
            EnsureCollider();
        }
    }

    private void EnsureRuntimeGlowMaterial()
    {
        if (targetRenderer == null || runtimeGlowMaterial != null)
        {
            return;
        }

        Shader glowShader = Shader.Find("Sorter/UV Stain Glow");
        if (glowShader == null)
        {
            return;
        }

        runtimeGlowMaterial = new Material(glowShader)
        {
            name = $"{name}_RuntimeUVGlow"
        };

        Material sourceMaterial = targetRenderer.sharedMaterial;
        if (sourceMaterial != null)
        {
            CopyMaterialColor(sourceMaterial, runtimeGlowMaterial);
            CopyMaterialTexture(sourceMaterial, runtimeGlowMaterial);
        }

        targetRenderer.material = runtimeGlowMaterial;
    }

    private static void CopyMaterialColor(Material source, Material target)
    {
        if (source == null || target == null)
        {
            return;
        }

        Color sourceColor = Color.white;
        if (source.HasProperty(BaseColorId))
        {
            sourceColor = source.GetColor(BaseColorId);
        }
        else if (source.HasProperty(ColorId))
        {
            sourceColor = source.GetColor(ColorId);
        }

        if (target.HasProperty(BaseColorId))
        {
            target.SetColor(BaseColorId, sourceColor);
        }
        else if (target.HasProperty(ColorId))
        {
            target.SetColor(ColorId, sourceColor);
        }
    }

    private static void CopyMaterialTexture(Material source, Material target)
    {
        if (source == null || target == null)
        {
            return;
        }

        Texture sourceTexture = null;
        if (source.HasProperty(BaseMapId))
        {
            sourceTexture = source.GetTexture(BaseMapId);
        }
        else if (source.HasProperty(MainTexId))
        {
            sourceTexture = source.GetTexture(MainTexId);
        }

        if (sourceTexture == null)
        {
            return;
        }

        if (target.HasProperty(BaseMapId))
        {
            target.SetTexture(BaseMapId, sourceTexture);
        }
        else if (target.HasProperty(MainTexId))
        {
            target.SetTexture(MainTexId, sourceTexture);
        }
    }

    private void EnsureCollider()
    {
        if (targetRenderer == null)
        {
            return;
        }

        Collider existingCollider = targetRenderer.GetComponent<Collider>();
        if (existingCollider != null)
        {
            existingCollider.isTrigger = true;
            return;
        }

        BoxCollider boxCollider = targetRenderer.gameObject.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        Bounds localBounds = targetRenderer.localBounds;
        Vector3 size = localBounds.size;
        if (size.sqrMagnitude <= 0.0001f)
        {
            size = Vector3.one * 0.1f;
        }

        boxCollider.center = localBounds.center;
        boxCollider.size = new Vector3(
            Mathf.Max(0.02f, size.x),
            Mathf.Max(0.02f, size.y),
            Mathf.Max(0.02f, size.z));
    }

    private void ApplyReveal()
    {
        if (targetRenderer == null)
        {
            return;
        }

        bool isVisible = currentReveal > VisibleThreshold || targetReveal > VisibleThreshold;
        targetRenderer.enabled = isVisible;

        if (!hasAlphaProperty && !hasEmissionProperty && !hasRevealProperty && !hasGlowColorProperty && !hasGlowIntensityProperty)
        {
            return;
        }

        targetRenderer.GetPropertyBlock(propertyBlock);

        if (hasAlphaProperty)
        {
            Color color = baseColor;
            color.a = Mathf.Lerp(hiddenAlpha, revealedAlpha, currentReveal);

            Material sharedMaterial = targetRenderer.sharedMaterial;
            if (sharedMaterial != null && sharedMaterial.HasProperty(BaseColorId))
            {
                propertyBlock.SetColor(BaseColorId, color);
            }
            else
            {
                propertyBlock.SetColor(ColorId, color);
            }
        }

        if (hasEmissionProperty)
        {
            float intensity = Mathf.Lerp(hiddenIntensity, revealedIntensity, currentReveal);
            propertyBlock.SetColor(EmissionColorId, emissionColor * intensity);
        }

        if (hasRevealProperty)
        {
            propertyBlock.SetFloat(RevealId, currentReveal);
        }

        if (hasGlowColorProperty)
        {
            propertyBlock.SetColor(GlowColorId, glowColor);
        }

        if (hasGlowIntensityProperty)
        {
            propertyBlock.SetFloat(GlowIntensityId, Mathf.Lerp(hiddenIntensity, revealedIntensity, currentReveal));
        }

        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void WarnMissingRendererOnce()
    {
        if (warnedMissingRenderer)
        {
            return;
        }

        warnedMissingRenderer = true;
        Debug.LogWarning($"UVRevealable on '{name}' has no Renderer target.", this);
    }
}
