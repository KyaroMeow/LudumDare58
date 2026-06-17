using UnityEngine;

[DisallowMultipleComponent]
public class ScannerBarcodeGlow : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color scanColor = new Color(1f, 0.16f, 0.04f, 1f);
    [SerializeField, Min(0f)] private float baseEmissionIntensity = 0.7f;
    [SerializeField, Min(0f)] private float maxEmissionIntensity = 3.5f;
    [SerializeField, Min(0f)] private float pulseSpeed = 9f;

    private MaterialPropertyBlock propertyBlock;
    private bool initialized;

    public void Initialize(Renderer rendererOverride = null)
    {
        if (rendererOverride != null)
        {
            targetRenderer = rendererOverride;
        }

        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        initialized = targetRenderer != null;
    }

    public void SetScanGlow(float progress, bool active)
    {
        if (!active)
        {
            ClearGlow();
            return;
        }

        if (!initialized)
        {
            Initialize();
        }

        if (targetRenderer == null)
        {
            return;
        }

        float normalizedProgress = Mathf.Clamp01(progress);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
        float intensity = Mathf.Lerp(baseEmissionIntensity, maxEmissionIntensity, normalizedProgress) * Mathf.Lerp(0.75f, 1.15f, pulse);
        Color glowColor = scanColor * intensity;
        Color baseColor = Color.Lerp(Color.white, scanColor, Mathf.Lerp(0.25f, 0.65f, normalizedProgress));

        targetRenderer.GetPropertyBlock(propertyBlock);
        Material sharedMaterial = targetRenderer.sharedMaterial;
        if (HasProperty(sharedMaterial, "_EmissionColor"))
        {
            propertyBlock.SetColor("_EmissionColor", glowColor);
        }

        if (HasProperty(sharedMaterial, "_BaseColor"))
        {
            propertyBlock.SetColor("_BaseColor", baseColor);
        }
        else if (HasProperty(sharedMaterial, "_Color"))
        {
            propertyBlock.SetColor("_Color", baseColor);
        }

        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    public void ClearGlow()
    {
        if (!initialized)
        {
            Initialize();
        }

        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.SetPropertyBlock(null);
    }

    private static bool HasProperty(Material material, string propertyName)
    {
        return material != null && material.HasProperty(propertyName);
    }
}
