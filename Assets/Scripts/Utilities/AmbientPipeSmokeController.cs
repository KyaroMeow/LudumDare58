using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class AmbientPipeSmokeController : MonoBehaviour
{
    private const string SmokePointName = "SmokePoint_Runtime";

    private sealed class SmokeSource
    {
        public Transform Point;
        public ParticleSystem[] Effects;
        public AudioSource Audio;
        public float Weight;
        public bool Active;
    }

    [Header("Smoke")]
    [SerializeField] private GameObject smokePrefab;
    [SerializeField] private bool disableLooseRootSmoke = true;
    [SerializeField] private float smokeScaleMin = 1.15f;
    [SerializeField] private float smokeScaleMax = 1.9f;
    [SerializeField] private float emissionMultiplier = 1.05f;
    [SerializeField] private float speedMultiplier = 2.4f;
    [SerializeField] private float lifetimeMultiplier = 1.1f;
    [SerializeField] private float sizeMultiplier = 1.12f;
    [SerializeField] private Color minSmokeColor = new Color(0.58f, 0.61f, 0.64f, 0.24f);
    [SerializeField] private Color maxSmokeColor = new Color(0.88f, 0.9f, 0.92f, 0.42f);

    [Header("Audio")]
    [SerializeField] private AudioClip steamClip1;
    [SerializeField] private AudioClip steamClip2;
    [SerializeField] private Vector2 steamVolumeRange = new Vector2(0.14f, 0.24f);
    [SerializeField] private Vector2 steamPitchRange = new Vector2(0.9f, 1.1f);
    [SerializeField] private Vector2 steamStartOffsetRange = new Vector2(0f, 0.08f);

    [Header("Timing")]
    [SerializeField] private Vector2 idleDelayRange = new Vector2(3.8f, 7.2f);
    [SerializeField] private Vector2 activeDurationRange = new Vector2(0.45f, 0.95f);
    [SerializeField] private int maxSimultaneousSources = 3;

    [Header("Pipe Search")]
    [SerializeField] private float minimumWorldHeight = -6f;
    [SerializeField] private float pipeExitOffset = 0.03f;
    [SerializeField] private float minimumSourceSpacing = 0.65f;
    [SerializeField] private int maxAutoSources = 28;

    private readonly List<SmokeSource> sources = new List<SmokeSource>();
    private Coroutine loopRoutine;

    private void OnEnable()
    {
        RebuildSources();

        if (sources.Count > 0 && loopRoutine == null)
        {
            loopRoutine = StartCoroutine(RandomSmokeLoop());
        }
    }

    private void OnDisable()
    {
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }

        StopAllEffects();
    }

    private void RebuildSources()
    {
        RemoveRuntimePoints();
        sources.Clear();

        if (disableLooseRootSmoke)
        {
            DisableLooseRootSmoke();
        }

        if (smokePrefab == null)
        {
            return;
        }

        List<Transform> pipes = CollectPipes();
        for (int i = 0; i < pipes.Count && sources.Count < maxAutoSources; i++)
        {
            Vector3 axis = GetPrimaryAxisLocal(pipes[i]);
            TryCreateSource(pipes[i], axis);
            TryCreateSource(pipes[i], -axis);
        }
    }

    private List<Transform> CollectPipes()
    {
        List<Transform> result = new List<Transform>();
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] children = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < children.Length; j++)
            {
                Transform current = children[j];
                if (!IsPipe(current))
                {
                    continue;
                }

                result.Add(current);
            }
        }

        return result;
    }

    private void TryCreateSource(Transform pipe, Vector3 localDirection)
    {
        Vector3 emissionDirection = BiasUpward(pipe, localDirection.normalized);
        Vector3 localPoint = ResolveLocalExitPoint(pipe, emissionDirection, pipeExitOffset);
        Vector3 worldPoint = pipe.TransformPoint(localPoint);

        if (IsTooClose(worldPoint))
        {
            return;
        }

        GameObject pointObject = new GameObject(SmokePointName);
        pointObject.transform.SetParent(pipe, false);
        pointObject.transform.localPosition = localPoint;
        pointObject.transform.localRotation = Quaternion.FromToRotation(Vector3.up, emissionDirection);

        GameObject smokeObject = Instantiate(smokePrefab, pointObject.transform);
        smokeObject.name = "SmokeFx";
        smokeObject.transform.localPosition = Vector3.zero;
        smokeObject.transform.localRotation = Quaternion.identity;
        smokeObject.transform.localScale = Vector3.one * Random.Range(smokeScaleMin, smokeScaleMax);

        ParticleSystem[] effects = smokeObject.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < effects.Length; i++)
        {
            TuneParticleSystem(effects[i]);
            effects[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        AudioSource audio = pointObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.loop = false;
        audio.spatialBlend = 1f;
        audio.rolloffMode = AudioRolloffMode.Linear;
        audio.minDistance = 1.2f;
        audio.maxDistance = 8f;
        audio.spread = 18f;
        audio.reverbZoneMix = 0.9f;

        sources.Add(new SmokeSource
        {
            Point = pointObject.transform,
            Effects = effects,
            Audio = audio,
            Weight = Mathf.Clamp01((pipe.position.y + 8f) / 24f) + 0.7f
        });
    }

    private IEnumerator RandomSmokeLoop()
    {
        yield return new WaitForSeconds(Random.Range(0.6f, 1.2f));

        while (enabled)
        {
            int freeSlots = Mathf.Max(0, maxSimultaneousSources - CountActiveSources());
            int triggerCount = freeSlots <= 0 ? 0 : (Random.value < 0.25f ? Mathf.Min(2, freeSlots) : 1);

            for (int i = 0; i < triggerCount; i++)
            {
                SmokeSource source = PickRandomInactiveSource();
                if (source != null)
                {
                    StartCoroutine(PlayBurst(source));
                }
            }

            yield return new WaitForSeconds(Random.Range(idleDelayRange.x, idleDelayRange.y));
        }
    }

    private IEnumerator PlayBurst(SmokeSource source)
    {
        source.Active = true;

        for (int i = 0; i < source.Effects.Length; i++)
        {
            source.Effects[i].Play(true);
        }

        PlaySteam(source.Audio);
        yield return new WaitForSeconds(Random.Range(activeDurationRange.x, activeDurationRange.y));

        for (int i = 0; i < source.Effects.Length; i++)
        {
            source.Effects[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        while (IsAnyEffectAlive(source.Effects))
        {
            yield return null;
        }

        source.Active = false;
    }

    private void PlaySteam(AudioSource audio)
    {
        if (audio == null)
        {
            return;
        }

        AudioClip clip = steamClip1 == null ? steamClip2 : steamClip2 == null ? steamClip1 : (Random.value < 0.5f ? steamClip1 : steamClip2);
        if (clip == null)
        {
            return;
        }

        audio.Stop();
        audio.clip = clip;
        audio.volume = Random.Range(steamVolumeRange.x, steamVolumeRange.y);
        audio.pitch = Random.Range(steamPitchRange.x, steamPitchRange.y);
        audio.spread = Random.Range(12f, 28f);
        audio.reverbZoneMix = Random.Range(0.84f, 1f);
        audio.time = Random.Range(steamStartOffsetRange.x, Mathf.Min(steamStartOffsetRange.y, clip.length * 0.2f));
        audio.Play();
    }

    private void TuneParticleSystem(ParticleSystem particleSystem)
    {
        var main = particleSystem.main;
        main.playOnAwake = false;
        main.loop = true;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.simulationSpeed *= Random.Range(1.02f, 1.14f);
        main.startLifetimeMultiplier *= lifetimeMultiplier;
        main.startSpeedMultiplier *= speedMultiplier;
        main.startSizeMultiplier *= sizeMultiplier;
        main.startColor = new ParticleSystem.MinMaxGradient(minSmokeColor, maxSmokeColor);
        main.gravityModifierMultiplier = 0f;

        var emission = particleSystem.emission;
        emission.rateOverTimeMultiplier *= emissionMultiplier;
        emission.rateOverDistanceMultiplier = 0f;

        var shape = particleSystem.shape;
        if (shape.enabled)
        {
            shape.radius *= 0.62f;
            shape.scale = Vector3.Scale(shape.scale, new Vector3(0.82f, 0.82f, 0.82f));
        }

        var noise = particleSystem.noise;
        if (noise.enabled)
        {
            noise.strengthMultiplier *= 0.72f;
            noise.frequency *= 0.9f;
        }

        var velocity = particleSystem.velocityOverLifetime;
        if (velocity.enabled)
        {
            velocity.xMultiplier *= 0.55f;
            velocity.yMultiplier *= 1.9f;
            velocity.zMultiplier *= 0.55f;
        }

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.lengthScale *= 1.1f;
            renderer.velocityScale *= 1.05f;
        }
    }

    private SmokeSource PickRandomInactiveSource()
    {
        float totalWeight = 0f;
        for (int i = 0; i < sources.Count; i++)
        {
            if (!sources[i].Active)
            {
                totalWeight += sources[i].Weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = Random.value * totalWeight;
        for (int i = 0; i < sources.Count; i++)
        {
            if (sources[i].Active)
            {
                continue;
            }

            roll -= sources[i].Weight;
            if (roll <= 0f)
            {
                return sources[i];
            }
        }

        return null;
    }

    private int CountActiveSources()
    {
        int activeCount = 0;
        for (int i = 0; i < sources.Count; i++)
        {
            if (sources[i].Active)
            {
                activeCount++;
            }
        }

        return activeCount;
    }

    private bool IsTooClose(Vector3 worldPoint)
    {
        for (int i = 0; i < sources.Count; i++)
        {
            if (Vector3.Distance(sources[i].Point.position, worldPoint) < minimumSourceSpacing)
            {
                return true;
            }
        }

        return false;
    }

    private void RemoveRuntimePoints()
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] children = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = children.Length - 1; j >= 0; j--)
            {
                if (children[j].name != SmokePointName)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(children[j].gameObject);
                }
                else
                {
                    DestroyImmediate(children[j].gameObject);
                }
            }
        }
    }

    private void DisableLooseRootSmoke()
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name != "vfx_Smoke_01")
            {
                continue;
            }

            ParticleSystem[] effects = roots[i].GetComponentsInChildren<ParticleSystem>(true);
            for (int j = 0; j < effects.Length; j++)
            {
                effects[j].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            roots[i].SetActive(false);
        }
    }

    private void StopAllEffects()
    {
        for (int i = 0; i < sources.Count; i++)
        {
            for (int j = 0; j < sources[i].Effects.Length; j++)
            {
                if (sources[i].Effects[j] != null)
                {
                    sources[i].Effects[j].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }
    }

    private static bool IsAnyEffectAlive(ParticleSystem[] effects)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i] != null && effects[i].IsAlive(true))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPipe(Transform candidate)
    {
        return candidate != null
            && candidate.position.y >= minimumWorldHeight
            && candidate.GetComponent<MeshFilter>() != null
            && (candidate.name.Contains("PipeSegment") || candidate.name.Contains("Cylinder"));
    }

    private static Vector3 GetPrimaryAxisLocal(Transform pipe)
    {
        MeshFilter meshFilter = pipe.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return Vector3.up;
        }

        Bounds bounds = meshFilter.sharedMesh.bounds;
        if (bounds.extents.x > bounds.extents.y && bounds.extents.x > bounds.extents.z)
        {
            return Vector3.right;
        }

        return bounds.extents.z > bounds.extents.y ? Vector3.forward : Vector3.up;
    }

    private static Vector3 ResolveLocalExitPoint(Transform pipe, Vector3 localDirection, float extraOffset)
    {
        MeshFilter meshFilter = pipe.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return localDirection.normalized * extraOffset;
        }

        Bounds bounds = meshFilter.sharedMesh.bounds;
        Vector3 axis = new Vector3(Mathf.Abs(localDirection.x), Mathf.Abs(localDirection.y), Mathf.Abs(localDirection.z));
        float extent = axis.x > axis.y && axis.x > axis.z ? bounds.extents.x : axis.z > axis.y ? bounds.extents.z : bounds.extents.y;
        return bounds.center + localDirection.normalized * (extent + extraOffset);
    }

    private static Vector3 BiasUpward(Transform pipe, Vector3 localDirection)
    {
        Vector3 upwardBias = pipe.InverseTransformDirection(Vector3.up).normalized;
        if (upwardBias.sqrMagnitude < 0.001f)
        {
            return localDirection.normalized;
        }

        return Vector3.Slerp(localDirection.normalized, upwardBias, 0.14f).normalized;
    }
}
