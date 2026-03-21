using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light targetLight;
    [SerializeField] private Renderer emissiveRenderer;
    [SerializeField] private string emissiveProperty = "_EmissionColor";

    [Header("Intensity")]
    [SerializeField] private float minIntensity = 0.35f;
    [SerializeField] private float maxIntensity = 1.4f;
    [SerializeField] private float smoothTime = 0.08f;

    [Header("Rhythm")]
    [SerializeField] private float minPause = 0.04f;
    [SerializeField] private float maxPause = 0.35f;
    [SerializeField] private float irregularity = 0.28f;
    [SerializeField] private float burstChance = 0.2f;

    [Header("Emission")]
    [SerializeField] private Color emissiveColor = new Color(1f, 0.75f, 0.45f);
    [SerializeField] private float emissiveStrength = 1.8f;

    private float baseIntensity = 1f;
    private float currentVelocity;
    private float targetIntensity;
    private float nextSwitchTime;
    private Material runtimeMaterial;
    private float noiseOffset;

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        if (targetLight != null)
            baseIntensity = targetLight.intensity;

        if (emissiveRenderer != null)
            runtimeMaterial = emissiveRenderer.material;

        noiseOffset = Random.Range(0f, 100f);
        targetIntensity = baseIntensity;
    }

    private void OnEnable()
    {
        ScheduleNextPulse(true);
    }

    private void Update()
    {
        if (targetLight == null)
            return;

        if (Time.time >= nextSwitchTime)
            ScheduleNextPulse(false);

        targetLight.intensity = Mathf.SmoothDamp(
            targetLight.intensity,
            targetIntensity,
            ref currentVelocity,
            smoothTime);

        UpdateEmission();
    }

    private void ScheduleNextPulse(bool instant)
    {
        float waveA = Mathf.Sin(Time.time * 2.1f + noiseOffset) * 0.5f + 0.5f;
        float waveB = Mathf.Sin(Time.time * 6.7f + noiseOffset * 0.7f) * 0.5f + 0.5f;
        float noise = Mathf.PerlinNoise(noiseOffset, Time.time * 1.35f);
        float rhythm = Mathf.Clamp01(waveA * 0.45f + waveB * 0.35f + noise * 0.2f);

        float randomJitter = Random.Range(1f - irregularity, 1f + irregularity);
        float intensityFactor = Mathf.Lerp(minIntensity, maxIntensity, rhythm) * randomJitter;

        if (Random.value < burstChance)
            intensityFactor *= Random.value > 0.5f ? 1.35f : 0.45f;

        targetIntensity = Mathf.Clamp(baseIntensity * intensityFactor, 0f, baseIntensity * maxIntensity * 1.4f);

        float pause = Mathf.Lerp(maxPause, minPause, rhythm) * randomJitter;
        nextSwitchTime = Time.time + Mathf.Max(0.015f, pause);

        if (instant)
            targetLight.intensity = targetIntensity;
    }

    private void UpdateEmission()
    {
        if (runtimeMaterial == null || !runtimeMaterial.HasProperty(emissiveProperty))
            return;

        float normalizedIntensity = Mathf.InverseLerp(0f, baseIntensity * maxIntensity, targetLight.intensity);
        Color finalEmission = emissiveColor * Mathf.Lerp(0.15f, emissiveStrength, normalizedIntensity);
        runtimeMaterial.SetColor(emissiveProperty, finalEmission);
    }
}
