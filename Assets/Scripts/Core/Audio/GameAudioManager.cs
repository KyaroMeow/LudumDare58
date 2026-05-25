using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class GameAudioManager : MonoBehaviour
{
    private enum MusicState
    {
        None,
        SceneStart,
        MainShift
    }

    [Header("Scene Start Music")]
    [SerializeField] private AudioClip sceneStartMusic;
    [Range(0f, 1f)]
    [SerializeField] private float sceneStartMusicVolume = 0.35f;
    [SerializeField] private bool playSceneStartMusicOnStart = true;
    [SerializeField] private bool loopSceneStartMusic = true;

    [Header("Main Shift Music")]
    [SerializeField] private AudioClip mainShiftMusic;
    [Range(0f, 1f)]
    [SerializeField] private float mainShiftMusicVolume = 0.4f;
    [SerializeField] private bool loopMainShiftMusic = true;
    [SerializeField] private float musicCrossfadeDuration = 1.5f;

    [Header("Timer Music Intensity")]
    [SerializeField] private bool enableTimerMusicIntensity = true;
    [Range(0.85f, 1.2f)]
    [SerializeField] private float baseMusicPitch = 1.0f;
    [Range(0.85f, 1.2f)]
    [SerializeField] private float warningMusicPitch = 1.06f;
    [Range(0.85f, 1.2f)]
    [SerializeField] private float criticalMusicPitch = 1.14f;

    [Range(0f, 1f)]
    [SerializeField] private float normalMusicVolume = 0.4f;
    [Range(0f, 1f)]
    [SerializeField] private float warningMusicVolume = 0.48f;
    [Range(0f, 1f)]
    [SerializeField] private float criticalMusicVolume = 0.56f;

    [SerializeField] private float warningTimeThreshold = 30f;
    [SerializeField] private float criticalTimeThreshold = 10f;
    [SerializeField] private float pitchSmoothSpeed = 3f;
    [SerializeField] private float volumeSmoothSpeed = 3f;

    [Header("Blackout Music")]
    [Range(0f, 1f)]
    [SerializeField] private float blackoutMusicVolumeMultiplier = 0.6f;
    [Range(0.85f, 1.2f)]
    [SerializeField] private float blackoutWarningPitch = 1.08f;

    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private AudioSource activeMusicSource;
    private AudioSource inactiveMusicSource;
    private Coroutine crossfadeRoutine;
    private MusicState currentMusicState = MusicState.None;
    private float targetMusicPitch = 1f;
    private float targetMusicVolume = 0.65f;
    private bool isCrossfading;
    private bool isBlackoutMusicDucked;
    private float preBlackoutTargetPitch = 1f;
    private float preBlackoutTargetVolume = 0.4f;

    private void Awake()
    {
        musicSourceA = CreateMusicSource("Music Source A");
        musicSourceB = CreateMusicSource("Music Source B");
        activeMusicSource = musicSourceA;
        inactiveMusicSource = musicSourceB;
        targetMusicPitch = Mathf.Clamp(baseMusicPitch, 0.85f, 1.2f);
        targetMusicVolume = Mathf.Clamp01(sceneStartMusicVolume);
    }

    private void Start()
    {
        if (playSceneStartMusicOnStart)
        {
            PlaySceneStartMusic();
        }
    }

    private void Update()
    {
        if (activeMusicSource == null || !activeMusicSource.isPlaying)
        {
            return;
        }

        activeMusicSource.pitch = Mathf.Lerp(activeMusicSource.pitch, targetMusicPitch, Time.deltaTime * pitchSmoothSpeed);

        if (!isCrossfading)
        {
            activeMusicSource.volume = Mathf.Lerp(activeMusicSource.volume, targetMusicVolume, Time.deltaTime * volumeSmoothSpeed);
        }
    }

    private AudioSource CreateMusicSource(string sourceName)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.pitch = 1f;
        return source;
    }

    private void PlaySceneStartMusic()
    {
        if (sceneStartMusic == null)
        {
            return;
        }

        StopCrossfade();
        ConfigureMusicSource(activeMusicSource, sceneStartMusic, loopSceneStartMusic);
        inactiveMusicSource.Stop();
        inactiveMusicSource.clip = null;

        activeMusicSource.volume = Mathf.Clamp01(sceneStartMusicVolume);
        activeMusicSource.pitch = Mathf.Clamp(baseMusicPitch, 0.85f, 1.2f);
        activeMusicSource.Play();

        currentMusicState = MusicState.SceneStart;
        ApplyTargetMusicIntensity(baseMusicPitch, sceneStartMusicVolume);
    }

    public void StartShiftMusic()
    {
        currentMusicState = MusicState.MainShift;
        float shiftVolume = GetBaseShiftMusicVolume();
        ApplyTargetMusicIntensity(baseMusicPitch, shiftVolume);

        if (mainShiftMusic == null)
        {
            return;
        }

        StopCrossfade();
        crossfadeRoutine = StartCoroutine(CrossfadeTo(mainShiftMusic, shiftVolume, loopMainShiftMusic, musicCrossfadeDuration));
    }

    public void UpdateTimerMusicIntensity(float remainingTime, float totalTime)
    {
        if (!enableTimerMusicIntensity || currentMusicState != MusicState.MainShift || isBlackoutMusicDucked)
        {
            return;
        }

        if (totalTime <= 0f)
        {
            ApplyTargetMusicIntensity(baseMusicPitch, GetBaseShiftMusicVolume());
            return;
        }

        float targetPitch = baseMusicPitch;
        float baseShiftVolume = GetBaseShiftMusicVolume();
        float warningShiftVolume = GetWarningShiftMusicVolume(baseShiftVolume);
        float criticalShiftVolume = GetCriticalShiftMusicVolume(baseShiftVolume, warningShiftVolume);
        float targetVolume = baseShiftVolume;

        if (remainingTime <= criticalTimeThreshold)
        {
            float criticalT = Mathf.InverseLerp(criticalTimeThreshold, 0f, Mathf.Max(0f, remainingTime));
            targetPitch = Mathf.Lerp(warningMusicPitch, criticalMusicPitch, criticalT);
            targetVolume = Mathf.Lerp(warningShiftVolume, criticalShiftVolume, criticalT);
        }
        else if (remainingTime <= warningTimeThreshold)
        {
            float warningT = Mathf.InverseLerp(warningTimeThreshold, criticalTimeThreshold, remainingTime);
            targetPitch = Mathf.Lerp(baseMusicPitch, warningMusicPitch, warningT);
            targetVolume = Mathf.Lerp(baseShiftVolume, warningShiftVolume, warningT);
        }

        ApplyTargetMusicIntensity(targetPitch, targetVolume);
    }

    public void ResetTimerMusicIntensity()
    {
        ApplyTargetMusicIntensity(baseMusicPitch, GetBaseShiftMusicVolume());
    }

    public void OnBlackoutStarted()
    {
        if (currentMusicState != MusicState.MainShift)
        {
            return;
        }

        if (!isBlackoutMusicDucked)
        {
            preBlackoutTargetPitch = targetMusicPitch;
            preBlackoutTargetVolume = targetMusicVolume;
            isBlackoutMusicDucked = true;
        }

        ApplyTargetMusicIntensity(baseMusicPitch, preBlackoutTargetVolume * blackoutMusicVolumeMultiplier);
        Debug.Log("Game music ducked for blackout.");
    }

    public void OnBlackoutRestoreWarning()
    {
        if (currentMusicState != MusicState.MainShift)
        {
            return;
        }

        float warningVolume = isBlackoutMusicDucked
            ? Mathf.Max(preBlackoutTargetVolume * blackoutMusicVolumeMultiplier, preBlackoutTargetVolume * 0.75f)
            : targetMusicVolume;
        ApplyTargetMusicIntensity(blackoutWarningPitch, warningVolume);
        Debug.Log("Game music restore warning intensity applied.");
    }

    public void OnBlackoutEnded()
    {
        if (currentMusicState != MusicState.MainShift)
        {
            isBlackoutMusicDucked = false;
            return;
        }

        float restorePitch = isBlackoutMusicDucked ? preBlackoutTargetPitch : baseMusicPitch;
        float restoreVolume = isBlackoutMusicDucked ? preBlackoutTargetVolume : GetBaseShiftMusicVolume();
        isBlackoutMusicDucked = false;
        ApplyTargetMusicIntensity(restorePitch, restoreVolume);
        Debug.Log("Game music restored after blackout.");
    }

    public void StopMusic(float fadeDuration = 1f)
    {
        StopCrossfade();
        crossfadeRoutine = StartCoroutine(CrossfadeTo(null, 0f, false, fadeDuration));
        currentMusicState = MusicState.None;
    }

    private IEnumerator CrossfadeTo(AudioClip clip, float targetVolume, bool loop, float duration)
    {
        isCrossfading = true;

        AudioSource fromSource = activeMusicSource;
        AudioSource toSource = inactiveMusicSource;
        float fromStartVolume = fromSource != null ? fromSource.volume : 0f;
        float targetClampedVolume = Mathf.Clamp01(targetVolume);

        if (clip != null)
        {
            ConfigureMusicSource(toSource, clip, loop);
            toSource.volume = 0f;
            toSource.pitch = Mathf.Clamp(baseMusicPitch, 0.85f, 1.2f);
            toSource.Play();
        }

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);

            if (fromSource != null)
            {
                fromSource.volume = Mathf.Lerp(fromStartVolume, 0f, t);
            }

            if (clip != null)
            {
                toSource.volume = Mathf.Lerp(0f, targetClampedVolume, t);
            }

            yield return null;
        }

        if (fromSource != null)
        {
            fromSource.Stop();
            fromSource.clip = null;
            fromSource.volume = 0f;
        }

        if (clip != null)
        {
            toSource.volume = targetClampedVolume;
            activeMusicSource = toSource;
            inactiveMusicSource = fromSource;
            ApplyTargetMusicIntensity(baseMusicPitch, targetClampedVolume);
        }

        isCrossfading = false;
        crossfadeRoutine = null;
    }

    private void ApplyTargetMusicIntensity(float targetPitch, float targetVolume)
    {
        targetMusicPitch = Mathf.Clamp(targetPitch, 0.85f, 1.2f);
        targetMusicVolume = Mathf.Clamp01(targetVolume);
    }

    private float GetBaseShiftMusicVolume()
    {
        normalMusicVolume = Mathf.Clamp01(mainShiftMusicVolume);
        return normalMusicVolume;
    }

    private float GetWarningShiftMusicVolume(float baseShiftVolume)
    {
        float configuredWarningVolume = Mathf.Clamp01(warningMusicVolume);
        return Mathf.Clamp01(Mathf.Max(baseShiftVolume, Mathf.Min(configuredWarningVolume, baseShiftVolume + 0.1f)));
    }

    private float GetCriticalShiftMusicVolume(float baseShiftVolume, float warningShiftVolume)
    {
        float configuredCriticalVolume = Mathf.Clamp01(criticalMusicVolume);
        float cappedCriticalVolume = Mathf.Min(configuredCriticalVolume, baseShiftVolume + 0.18f);
        return Mathf.Clamp01(Mathf.Max(warningShiftVolume, cappedCriticalVolume));
    }

    private void ConfigureMusicSource(AudioSource source, AudioClip clip, bool loop)
    {
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    private void StopCrossfade()
    {
        if (crossfadeRoutine == null)
        {
            return;
        }

        StopCoroutine(crossfadeRoutine);
        crossfadeRoutine = null;
        isCrossfading = false;
    }
}
