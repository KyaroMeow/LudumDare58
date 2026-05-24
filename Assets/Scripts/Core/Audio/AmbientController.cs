using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class AmbientLayer
{
    public string layerName = "Ambient Layer";
    public AudioClip clip;

    public bool playOnStart = true;
    public bool loop = true;

    [Range(0f, 1f)]
    public float volume = 0.35f;

    [Range(0.01f, 3f)]
    public float pitch = 1f;

    public bool randomizeVolume;
    public Vector2 volumeRange = new Vector2(0.25f, 0.45f);

    public bool randomizePitch;
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    public bool randomStartTime = true;
    public float startDelay;

    public bool spatial3D;
    [Range(0f, 1f)]
    public float spatialBlend;
    public Transform spatialTarget;
    public float minDistance = 1f;
    public float maxDistance = 12f;

    public float fadeInDuration = 2f;
    public float fadeOutDuration = 1f;

    public bool isRandomOneShot;
    public Vector2 randomIntervalRange = new Vector2(8f, 20f);
}

[DisallowMultipleComponent]
public class AmbientController : MonoBehaviour
{
    private const float MinPitch = 0.01f;

    private class LayerRuntime
    {
        public AudioSource Source;
        public Coroutine FadeRoutine;
        public Coroutine RandomRoutine;
        public Coroutine DelayedRoutine;
        public bool IsPlaying;
        public float TargetVolume;
    }

    [Header("Ambient Layers")]
    [SerializeField] private AmbientLayer[] ambientLayers;

    [Header("Global Settings")]
    [SerializeField] private bool playOnStart = true;
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private bool stopAllOnDisable = true;

    [Header("Debug")]
    [SerializeField] private bool showWarnings = true;

    private LayerRuntime[] layerRuntimes;

    private void Awake()
    {
        CreateRuntimeLayers();
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayAll();
        }
    }

    private void Update()
    {
        if (ambientLayers == null || layerRuntimes == null)
        {
            return;
        }

        for (int i = 0; i < ambientLayers.Length; i++)
        {
            AmbientLayer layer = ambientLayers[i];
            LayerRuntime runtime = layerRuntimes[i];

            if (layer == null || runtime.Source == null || !layer.spatial3D || layer.spatialTarget == null)
            {
                continue;
            }

            runtime.Source.transform.position = layer.spatialTarget.position;
        }
    }

    private void OnDisable()
    {
        if (stopAllOnDisable)
        {
            StopAllImmediate();
        }
    }

    public void PlayAll()
    {
        if (ambientLayers == null)
        {
            return;
        }

        for (int i = 0; i < ambientLayers.Length; i++)
        {
            if (ambientLayers[i] != null && ambientLayers[i].playOnStart)
            {
                PlayLayer(i);
            }
        }
    }

    public void StopAll()
    {
        StopAll(-1f);
    }

    public void StopAll(float fadeOverride)
    {
        if (ambientLayers == null)
        {
            return;
        }

        for (int i = 0; i < ambientLayers.Length; i++)
        {
            StopLayer(i, fadeOverride);
        }
    }

    public void PlayLayer(string layerName)
    {
        int index = FindLayerIndex(layerName);
        if (index >= 0)
        {
            PlayLayer(index);
        }
    }

    public void StopLayer(string layerName)
    {
        int index = FindLayerIndex(layerName);
        if (index >= 0)
        {
            StopLayer(index);
        }
    }

    public void PlayLayer(int index)
    {
        if (!TryGetLayer(index, out AmbientLayer layer, out LayerRuntime runtime))
        {
            return;
        }

        StopDelayedRoutine(runtime);

        if (layer.startDelay > 0f)
        {
            runtime.DelayedRoutine = StartCoroutine(PlayLayerDelayed(index, layer.startDelay));
            return;
        }

        PlayLayerNow(index);
    }

    public void StopLayer(int index)
    {
        StopLayer(index, -1f);
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);

        if (ambientLayers == null || layerRuntimes == null)
        {
            return;
        }

        for (int i = 0; i < ambientLayers.Length; i++)
        {
            LayerRuntime runtime = layerRuntimes[i];
            AmbientLayer layer = ambientLayers[i];

            if (runtime == null || runtime.Source == null || layer == null || layer.isRandomOneShot)
            {
                continue;
            }

            runtime.Source.volume = Mathf.Clamp01(runtime.TargetVolume * masterVolume);
        }
    }

    public void SetLayerVolume(string layerName, float volume)
    {
        int index = FindLayerIndex(layerName);
        if (index >= 0)
        {
            SetLayerVolume(index, volume);
        }
    }

    public void SetLayerPitch(string layerName, float pitch)
    {
        int index = FindLayerIndex(layerName);
        if (index >= 0)
        {
            SetLayerPitch(index, pitch);
        }
    }

    public void SetLayerVolume(int index, float volume)
    {
        if (!TryGetLayer(index, out AmbientLayer layer, out LayerRuntime runtime))
        {
            return;
        }

        layer.volume = Mathf.Clamp01(volume);
        runtime.TargetVolume = layer.volume;

        if (!layer.isRandomOneShot && runtime.Source != null)
        {
            runtime.Source.volume = Mathf.Clamp01(runtime.TargetVolume * masterVolume);
        }
    }

    public void SetLayerPitch(int index, float pitch)
    {
        if (!TryGetLayer(index, out AmbientLayer layer, out LayerRuntime runtime))
        {
            return;
        }

        layer.pitch = Mathf.Max(MinPitch, pitch);

        if (runtime.Source != null)
        {
            runtime.Source.pitch = layer.pitch;
        }
    }

    private void CreateRuntimeLayers()
    {
        int count = ambientLayers != null ? ambientLayers.Length : 0;
        layerRuntimes = new LayerRuntime[count];

        for (int i = 0; i < count; i++)
        {
            AmbientLayer layer = ambientLayers[i];
            string layerName = layer != null && !string.IsNullOrWhiteSpace(layer.layerName) ? layer.layerName : $"Layer {i}";
            GameObject sourceObject = new GameObject($"AmbientLayer_{SanitizeName(layerName)}");
            sourceObject.transform.SetParent(transform, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 0f;
            source.pitch = 1f;

            layerRuntimes[i] = new LayerRuntime
            {
                Source = source
            };

            if (layer != null)
            {
                ApplySourceSettings(source, layer);
            }
        }
    }

    private IEnumerator PlayLayerDelayed(int index, float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));
        PlayLayerNow(index);
    }

    private void PlayLayerNow(int index)
    {
        if (!TryGetLayer(index, out AmbientLayer layer, out LayerRuntime runtime))
        {
            return;
        }

        if (layer.clip == null)
        {
            WarnMissingClip(layer);
            return;
        }

        ApplySourceSettings(runtime.Source, layer);

        if (layer.isRandomOneShot)
        {
            runtime.IsPlaying = true;
            StopRandomRoutine(runtime);
            runtime.RandomRoutine = StartCoroutine(RandomOneShotRoutine(layer, runtime));
            return;
        }

        runtime.IsPlaying = true;
        runtime.TargetVolume = ResolveLayerVolume(layer);
        runtime.Source.clip = layer.clip;
        runtime.Source.loop = layer.loop;
        runtime.Source.pitch = ResolveLayerPitch(layer);
        ApplyRandomStartTime(runtime.Source, layer);
        runtime.Source.volume = layer.fadeInDuration > 0f ? 0f : Mathf.Clamp01(runtime.TargetVolume * masterVolume);
        runtime.Source.Play();

        if (layer.fadeInDuration > 0f)
        {
            StartFade(runtime, runtime.TargetVolume, layer.fadeInDuration, false);
        }
    }

    private void StopLayer(int index, float fadeOverride)
    {
        if (!TryGetLayer(index, out AmbientLayer layer, out LayerRuntime runtime))
        {
            return;
        }

        StopDelayedRoutine(runtime);
        StopRandomRoutine(runtime);
        runtime.IsPlaying = false;

        if (runtime.Source == null || !runtime.Source.isPlaying)
        {
            return;
        }

        float fadeDuration = fadeOverride >= 0f ? fadeOverride : layer.fadeOutDuration;
        if (fadeDuration <= 0f)
        {
            runtime.Source.Stop();
            runtime.Source.clip = null;
            runtime.Source.volume = 0f;
            return;
        }

        StartFade(runtime, 0f, fadeDuration, true);
    }

    private IEnumerator RandomOneShotRoutine(AmbientLayer layer, LayerRuntime runtime)
    {
        while (runtime.IsPlaying)
        {
            Vector2 interval = NormalizeRange(layer.randomIntervalRange, 1f, 3f);
            float waitTime = UnityEngine.Random.Range(interval.x, interval.y);
            yield return new WaitForSeconds(waitTime);

            if (!runtime.IsPlaying || layer.clip == null)
            {
                continue;
            }

            ApplySourceSettings(runtime.Source, layer);
            runtime.Source.pitch = ResolveLayerPitch(layer);
            runtime.Source.volume = 1f;
            runtime.Source.PlayOneShot(layer.clip, Mathf.Clamp01(ResolveLayerVolume(layer) * masterVolume));
        }
    }

    private void StartFade(LayerRuntime runtime, float targetLayerVolume, float duration, bool stopAfterFade)
    {
        if (runtime.FadeRoutine != null)
        {
            StopCoroutine(runtime.FadeRoutine);
        }

        runtime.FadeRoutine = StartCoroutine(FadeLayer(runtime, targetLayerVolume, duration, stopAfterFade));
    }

    private IEnumerator FadeLayer(LayerRuntime runtime, float targetLayerVolume, float duration, bool stopAfterFade)
    {
        AudioSource source = runtime.Source;
        float startVolume = source != null ? source.volume : 0f;
        float targetVolume = Mathf.Clamp01(targetLayerVolume * masterVolume);
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);

            if (source != null)
            {
                source.volume = Mathf.Lerp(startVolume, targetVolume, t);
            }

            yield return null;
        }

        if (source != null)
        {
            source.volume = targetVolume;

            if (stopAfterFade)
            {
                source.Stop();
                source.clip = null;
                source.volume = 0f;
            }
        }

        runtime.FadeRoutine = null;
    }

    private void ApplySourceSettings(AudioSource source, AmbientLayer layer)
    {
        if (source == null || layer == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = layer.loop && !layer.isRandomOneShot;
        source.spatialBlend = layer.spatial3D ? Mathf.Clamp01(layer.spatialBlend) : 0f;
        source.minDistance = Mathf.Max(0.01f, layer.minDistance);
        source.maxDistance = Mathf.Max(source.minDistance, layer.maxDistance);

        if (layer.spatial3D && layer.spatialTarget != null)
        {
            source.transform.position = layer.spatialTarget.position;
        }
        else
        {
            source.transform.localPosition = Vector3.zero;
        }
    }

    private void ApplyRandomStartTime(AudioSource source, AmbientLayer layer)
    {
        if (!layer.randomStartTime || source.clip == null || source.clip.length <= 0.1f)
        {
            return;
        }

        try
        {
            source.time = UnityEngine.Random.Range(0f, Mathf.Max(0f, source.clip.length - 0.05f));
        }
        catch (Exception)
        {
        }
    }

    private float ResolveLayerVolume(AmbientLayer layer)
    {
        if (!layer.randomizeVolume)
        {
            return Mathf.Clamp01(layer.volume);
        }

        Vector2 range = NormalizeRange(layer.volumeRange, layer.volume, layer.volume);
        return Mathf.Clamp01(UnityEngine.Random.Range(range.x, range.y));
    }

    private float ResolveLayerPitch(AmbientLayer layer)
    {
        if (!layer.randomizePitch)
        {
            return Mathf.Max(MinPitch, layer.pitch);
        }

        Vector2 range = NormalizeRange(layer.pitchRange, layer.pitch, layer.pitch);
        return Mathf.Max(MinPitch, UnityEngine.Random.Range(range.x, range.y));
    }

    private Vector2 NormalizeRange(Vector2 range, float fallbackMin, float fallbackMax)
    {
        float min = !float.IsNaN(range.x) && !float.IsInfinity(range.x) ? range.x : fallbackMin;
        float max = !float.IsNaN(range.y) && !float.IsInfinity(range.y) ? range.y : fallbackMax;

        if (min > max)
        {
            float previousMin = min;
            min = max;
            max = previousMin;
        }

        return new Vector2(min, max);
    }

    private int FindLayerIndex(string layerName)
    {
        if (ambientLayers == null || string.IsNullOrWhiteSpace(layerName))
        {
            return -1;
        }

        for (int i = 0; i < ambientLayers.Length; i++)
        {
            AmbientLayer layer = ambientLayers[i];
            if (layer != null && string.Equals(layer.layerName, layerName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private bool TryGetLayer(int index, out AmbientLayer layer, out LayerRuntime runtime)
    {
        layer = null;
        runtime = null;

        if (ambientLayers == null || layerRuntimes == null || index < 0 || index >= ambientLayers.Length || index >= layerRuntimes.Length)
        {
            return false;
        }

        layer = ambientLayers[index];
        runtime = layerRuntimes[index];
        return layer != null && runtime != null;
    }

    private void StopAllImmediate()
    {
        if (layerRuntimes == null)
        {
            return;
        }

        for (int i = 0; i < layerRuntimes.Length; i++)
        {
            LayerRuntime runtime = layerRuntimes[i];
            if (runtime == null)
            {
                continue;
            }

            StopDelayedRoutine(runtime);
            StopRandomRoutine(runtime);

            if (runtime.FadeRoutine != null)
            {
                StopCoroutine(runtime.FadeRoutine);
                runtime.FadeRoutine = null;
            }

            runtime.IsPlaying = false;

            if (runtime.Source != null)
            {
                runtime.Source.Stop();
                runtime.Source.clip = null;
                runtime.Source.volume = 0f;
            }
        }
    }

    private void StopRandomRoutine(LayerRuntime runtime)
    {
        if (runtime.RandomRoutine == null)
        {
            return;
        }

        StopCoroutine(runtime.RandomRoutine);
        runtime.RandomRoutine = null;
    }

    private void StopDelayedRoutine(LayerRuntime runtime)
    {
        if (runtime.DelayedRoutine == null)
        {
            return;
        }

        StopCoroutine(runtime.DelayedRoutine);
        runtime.DelayedRoutine = null;
    }

    private void WarnMissingClip(AmbientLayer layer)
    {
        if (!showWarnings)
        {
            return;
        }

        string layerName = !string.IsNullOrWhiteSpace(layer.layerName) ? layer.layerName : "Unnamed Ambient Layer";
        Debug.LogWarning($"AmbientController skipped '{layerName}' because no AudioClip is assigned.", this);
    }

    private string SanitizeName(string sourceName)
    {
        char[] invalidChars = { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
        string result = sourceName;

        for (int i = 0; i < invalidChars.Length; i++)
        {
            result = result.Replace(invalidChars[i], '_');
        }

        return result.Replace(' ', '_');
    }
}
