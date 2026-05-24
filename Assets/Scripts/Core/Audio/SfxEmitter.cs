using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SfxEmitter : MonoBehaviour
{
    private const float MinPitch = 0.01f;
    private const float MaxPitch = 3f;
    private const int MaxOneShotSources = 8;

    private readonly Dictionary<SfxCue, AudioSource> loopSources = new Dictionary<SfxCue, AudioSource>();
    private readonly List<AudioSource> oneShotPool = new List<AudioSource>();
    private readonly List<AudioSource> loopPool = new List<AudioSource>();

    private AudioSource oneShotSource;

    private void Awake()
    {
        EnsureOneShotSource();
    }

    public void Play(SfxCue cue)
    {
        if (cue == null)
        {
            return;
        }

        if (cue.Loop)
        {
            StartLoop(cue);
        }
        else
        {
            PlayOneShot(cue);
        }
    }

    public void PlayOneShot(SfxCue cue)
    {
        AudioClip clip = ResolveClip(cue);
        if (clip == null)
        {
            return;
        }

        AudioSource source = GetOneShotSource();
        ApplySettings(source, cue, false);

        float volume = ResolveVolume(cue);
        float delay = Mathf.Max(0f, cue.Delay);
        if (delay > 0f)
        {
            source.clip = clip;
            source.volume = volume;
            source.PlayDelayed(delay);
            return;
        }

        source.volume = 1f;
        source.PlayOneShot(clip, volume);
    }

    public void StartLoop(SfxCue cue)
    {
        AudioClip clip = ResolveClip(cue);
        if (clip == null)
        {
            return;
        }

        AudioSource source;
        if (loopSources.TryGetValue(cue, out source))
        {
            if (source == null)
            {
                loopSources.Remove(cue);
                source = GetLoopSource();
                loopSources.Add(cue, source);
            }
            else
            {
                if (!cue.PlayIfAlreadyPlaying && !cue.StopPreviousLoopBeforeStart)
                {
                    return;
                }

                if (!cue.StopPreviousLoopBeforeStart)
                {
                    return;
                }

                source.Stop();
            }
        }
        else
        {
            source = GetLoopSource();
            loopSources.Add(cue, source);
        }

        ApplySettings(source, cue, true);
        source.clip = clip;
        source.volume = ResolveVolume(cue);

        float delay = Mathf.Max(0f, cue.Delay);
        if (delay > 0f)
        {
            source.PlayDelayed(delay);
        }
        else
        {
            source.Play();
        }
    }

    public void StopLoop(SfxCue cue)
    {
        if (cue == null)
        {
            return;
        }

        AudioSource source;
        if (!loopSources.TryGetValue(cue, out source))
        {
            return;
        }

        if (source != null)
        {
            source.Stop();
            source.clip = null;
        }

        loopSources.Remove(cue);
    }

    public void StopAllLoops()
    {
        foreach (var pair in loopSources)
        {
            if (pair.Value == null)
            {
                continue;
            }

            pair.Value.Stop();
            pair.Value.clip = null;
        }

        loopSources.Clear();
    }

    private AudioSource EnsureOneShotSource()
    {
        if (oneShotSource != null)
        {
            return oneShotSource;
        }

        AudioSource[] sources = GetComponents<AudioSource>();
        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null && sources[i].clip == null && !sources[i].loop)
            {
                oneShotSource = sources[i];
                break;
            }
        }

        if (oneShotSource == null)
        {
            oneShotSource = gameObject.AddComponent<AudioSource>();
        }

        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;

        if (!oneShotPool.Contains(oneShotSource))
        {
            oneShotPool.Add(oneShotSource);
        }

        return oneShotSource;
    }

    private AudioSource GetOneShotSource()
    {
        EnsureOneShotSource();

        for (int i = 0; i < oneShotPool.Count; i++)
        {
            if (oneShotPool[i] != null && !oneShotPool[i].isPlaying)
            {
                return oneShotPool[i];
            }
        }

        if (oneShotPool.Count < MaxOneShotSources)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            oneShotPool.Add(source);
            return source;
        }

        return oneShotSource;
    }

    private AudioSource GetLoopSource()
    {
        for (int i = 0; i < loopPool.Count; i++)
        {
            if (loopPool[i] != null && !loopSources.ContainsValue(loopPool[i]))
            {
                return loopPool[i];
            }
        }

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        loopPool.Add(source);
        return source;
    }

    private static void ApplySettings(AudioSource source, SfxCue cue, bool loop)
    {
        source.playOnAwake = false;
        source.loop = loop;
        source.pitch = ResolvePitch(cue);
        source.spatialBlend = cue.Spatial ? Mathf.Clamp01(cue.SpatialBlend) : 0f;

        if (!cue.Spatial)
        {
            return;
        }

        float minDistance = Mathf.Max(0.01f, cue.MinDistance);
        float maxDistance = Mathf.Max(minDistance, cue.MaxDistance);
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
    }

    private static AudioClip ResolveClip(SfxCue cue)
    {
        if (cue == null)
        {
            return null;
        }

        if (!cue.UseVariations || cue.Variations == null || cue.Variations.Length == 0)
        {
            return cue.Clip;
        }

        int validCount = 0;
        for (int i = 0; i < cue.Variations.Length; i++)
        {
            if (cue.Variations[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return cue.Clip;
        }

        int selectedIndex = Random.Range(0, validCount);
        for (int i = 0; i < cue.Variations.Length; i++)
        {
            if (cue.Variations[i] == null)
            {
                continue;
            }

            if (selectedIndex == 0)
            {
                return cue.Variations[i];
            }

            selectedIndex--;
        }

        return cue.Clip;
    }

    private static float ResolveVolume(SfxCue cue)
    {
        if (!cue.RandomizeVolume)
        {
            return Mathf.Clamp01(cue.Volume);
        }

        float min = Mathf.Clamp01(Mathf.Min(cue.VolumeRange.x, cue.VolumeRange.y));
        float max = Mathf.Clamp01(Mathf.Max(cue.VolumeRange.x, cue.VolumeRange.y));
        return Random.Range(min, max);
    }

    private static float ResolvePitch(SfxCue cue)
    {
        if (!cue.RandomizePitch)
        {
            return Mathf.Clamp(cue.Pitch, MinPitch, MaxPitch);
        }

        float min = Mathf.Clamp(Mathf.Min(cue.PitchRange.x, cue.PitchRange.y), MinPitch, MaxPitch);
        float max = Mathf.Clamp(Mathf.Max(cue.PitchRange.x, cue.PitchRange.y), MinPitch, MaxPitch);
        return Random.Range(min, max);
    }
}
