using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MenuAudioManager : MonoBehaviour
{
    private enum MenuMusicBaseState
    {
        MainMenu,
        Settings
    }

    [Header("Music Clips")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip settingsMusic;
    [SerializeField] private AudioClip startHoverMusic;

    [Header("Main Menu State")]
    [Range(0f, 1f)]
    [SerializeField] private float mainMenuVolume = 0.45f;
    [Range(0.85f, 1.2f)]
    [SerializeField] private float mainMenuPitch = 1.0f;

    [Header("Settings State")]
    [Range(0f, 1f)]
    [SerializeField] private float settingsVolume = 0.36f;
    [Range(0.85f, 1.2f)]
    [SerializeField] private float settingsPitch = 0.985f;

    [Header("Start Hover State")]
    [Range(0f, 1f)]
    [SerializeField] private float startHoverVolume = 0.55f;
    [Range(0.85f, 1.2f)]
    [SerializeField] private float startHoverPitch = 1.015f;

    [Header("Exit Hover State")]
    [Range(0f, 1f)]
    [SerializeField] private float exitHoverVolume = 0.27f;
    [Range(0.85f, 1.2f)]
    [SerializeField] private float exitHoverPitch = 0.965f;

    [Header("Transitions")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loopMusic = true;
    [SerializeField] private float initialFadeInDuration = 2.4f;
    [SerializeField] private float stateCrossfadeDuration = 1.35f;
    [SerializeField] private float hoverCrossfadeDuration = 0.55f;
    [SerializeField] private float exitFadeDuration = 0.45f;

    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private AudioSource activeMusicSource;
    private AudioSource inactiveMusicSource;
    private Coroutine transitionRoutine;
    private MenuMusicBaseState currentBaseState = MenuMusicBaseState.MainMenu;

    private void Awake()
    {
        musicSourceA = CreateMusicSource("Menu Music Source A");
        musicSourceB = CreateMusicSource("Menu Music Source B");
        activeMusicSource = musicSourceA;
        inactiveMusicSource = musicSourceB;
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayInitialMainMenuMusic();
        }
    }

    public void EnterMainMenuState()
    {
        currentBaseState = MenuMusicBaseState.MainMenu;
        TransitionTo(mainMenuMusic, mainMenuVolume, mainMenuPitch, stateCrossfadeDuration);
    }

    public void EnterSettingsState()
    {
        currentBaseState = MenuMusicBaseState.Settings;
        AudioClip clip = settingsMusic != null ? settingsMusic : mainMenuMusic;
        TransitionTo(clip, settingsVolume, settingsPitch, stateCrossfadeDuration);
    }

    public void OnStartHoverEnter()
    {
        AudioClip clip = startHoverMusic != null ? startHoverMusic : GetCurrentOrBaseClip();
        TransitionTo(clip, startHoverVolume, startHoverPitch, hoverCrossfadeDuration);
    }

    public void OnStartHoverExit()
    {
        RestoreBaseState(hoverCrossfadeDuration);
    }

    public void OnExitHoverEnter()
    {
        FadeCurrentMusic(exitHoverVolume, exitHoverPitch, exitFadeDuration);
    }

    public void OnExitHoverExit()
    {
        RestoreBaseState(exitFadeDuration);
    }

    public void StopMenuMusic(float fadeDuration)
    {
        StopTransition();
        transitionRoutine = StartCoroutine(FadeOutActiveSource(fadeDuration));
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

    private void PlayInitialMainMenuMusic()
    {
        currentBaseState = MenuMusicBaseState.MainMenu;

        if (mainMenuMusic == null)
        {
            return;
        }

        StopTransition();
        StopInactiveSource();
        ConfigureMusicSource(activeMusicSource, mainMenuMusic);
        activeMusicSource.volume = 0f;
        activeMusicSource.pitch = ClampPitch(mainMenuPitch);
        activeMusicSource.time = 0f;
        activeMusicSource.Play();

        transitionRoutine = StartCoroutine(FadeActiveSourceTo(mainMenuVolume, mainMenuPitch, initialFadeInDuration));
    }

    private void RestoreBaseState(float duration)
    {
        if (currentBaseState == MenuMusicBaseState.Settings)
        {
            AudioClip clip = settingsMusic != null ? settingsMusic : mainMenuMusic;
            TransitionTo(clip, settingsVolume, settingsPitch, duration);
            return;
        }

        TransitionTo(mainMenuMusic, mainMenuVolume, mainMenuPitch, duration);
    }

    private void TransitionTo(AudioClip clip, float volume, float pitch, float duration)
    {
        if (clip == null)
        {
            FadeCurrentMusic(volume, pitch, duration);
            return;
        }

        if (activeMusicSource != null && activeMusicSource.isPlaying && activeMusicSource.clip == clip)
        {
            FadeCurrentMusic(volume, pitch, duration);
            return;
        }

        StopTransition();
        transitionRoutine = StartCoroutine(CrossfadeTo(clip, volume, pitch, duration));
    }

    private void FadeCurrentMusic(float volume, float pitch, float duration)
    {
        if (activeMusicSource == null || !activeMusicSource.isPlaying)
        {
            return;
        }

        StopTransition();
        StopInactiveSource();
        transitionRoutine = StartCoroutine(FadeActiveSourceTo(volume, pitch, duration));
    }

    private IEnumerator CrossfadeTo(AudioClip clip, float volume, float pitch, float duration)
    {
        AudioSource fromSource = activeMusicSource;
        AudioSource toSource = inactiveMusicSource;
        float fromStartVolume = fromSource != null ? fromSource.volume : 0f;
        float fromStartPitch = fromSource != null ? fromSource.pitch : 1f;
        float targetClampedVolume = Mathf.Clamp01(volume);
        float targetClampedPitch = ClampPitch(pitch);
        float safeDuration = Mathf.Max(0.01f, duration);
        float normalizedPosition = GetNormalizedPlaybackPosition(fromSource);

        ConfigureMusicSource(toSource, clip);
        toSource.volume = 0f;
        toSource.pitch = targetClampedPitch;
        ApplyPlaybackPosition(toSource, clip, normalizedPosition);
        toSource.Play();

        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);

            if (fromSource != null)
            {
                fromSource.volume = Mathf.Lerp(fromStartVolume, 0f, t);
                fromSource.pitch = Mathf.Lerp(fromStartPitch, targetClampedPitch, t);
            }

            toSource.volume = Mathf.Lerp(0f, targetClampedVolume, t);
            toSource.pitch = targetClampedPitch;
            yield return null;
        }

        if (fromSource != null)
        {
            fromSource.Stop();
            fromSource.clip = null;
            fromSource.volume = 0f;
            fromSource.pitch = 1f;
        }

        toSource.volume = targetClampedVolume;
        toSource.pitch = targetClampedPitch;
        activeMusicSource = toSource;
        inactiveMusicSource = fromSource;
        transitionRoutine = null;
    }

    private IEnumerator FadeActiveSourceTo(float volume, float pitch, float duration)
    {
        float startVolume = activeMusicSource.volume;
        float startPitch = activeMusicSource.pitch;
        float targetClampedVolume = Mathf.Clamp01(volume);
        float targetClampedPitch = ClampPitch(pitch);
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            activeMusicSource.volume = Mathf.Lerp(startVolume, targetClampedVolume, t);
            activeMusicSource.pitch = Mathf.Lerp(startPitch, targetClampedPitch, t);
            yield return null;
        }

        activeMusicSource.volume = targetClampedVolume;
        activeMusicSource.pitch = targetClampedPitch;
        transitionRoutine = null;
    }

    private IEnumerator FadeOutActiveSource(float duration)
    {
        AudioSource source = activeMusicSource;
        float startVolume = source != null ? source.volume : 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);

            if (source != null)
            {
                source.volume = Mathf.Lerp(startVolume, 0f, t);
            }

            yield return null;
        }

        if (source != null)
        {
            source.Stop();
            source.clip = null;
            source.volume = 0f;
            source.pitch = 1f;
        }

        StopInactiveSource();
        transitionRoutine = null;
    }

    private float GetNormalizedPlaybackPosition(AudioSource source)
    {
        if (source == null || source.clip == null || source.clip.length <= 0f)
        {
            return 0f;
        }

        if (source.clip.samples > 0)
        {
            try
            {
                return Mathf.Repeat((float)source.timeSamples / source.clip.samples, 1f);
            }
            catch (System.Exception)
            {
            }
        }

        return Mathf.Repeat(source.time / source.clip.length, 1f);
    }

    private void ApplyPlaybackPosition(AudioSource source, AudioClip clip, float normalizedPosition)
    {
        if (source == null || clip == null || normalizedPosition <= 0f)
        {
            return;
        }

        float position = Mathf.Repeat(normalizedPosition, 1f);

        if (clip.samples > 0)
        {
            try
            {
                source.timeSamples = Mathf.Clamp(Mathf.RoundToInt(clip.samples * position), 0, clip.samples - 1);
                return;
            }
            catch (System.Exception)
            {
            }
        }

        source.time = Mathf.Clamp(clip.length * position, 0f, Mathf.Max(0f, clip.length - 0.01f));
    }

    private AudioClip GetCurrentOrBaseClip()
    {
        if (activeMusicSource != null && activeMusicSource.clip != null)
        {
            return activeMusicSource.clip;
        }

        return currentBaseState == MenuMusicBaseState.Settings && settingsMusic != null ? settingsMusic : mainMenuMusic;
    }

    private void ConfigureMusicSource(AudioSource source, AudioClip clip)
    {
        source.clip = clip;
        source.loop = loopMusic;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    private float ClampPitch(float pitch)
    {
        return Mathf.Clamp(pitch, 0.85f, 1.2f);
    }

    private void StopTransition()
    {
        if (transitionRoutine == null)
        {
            return;
        }

        StopCoroutine(transitionRoutine);
        transitionRoutine = null;
    }

    private void StopInactiveSource()
    {
        if (inactiveMusicSource == null)
        {
            return;
        }

        inactiveMusicSource.Stop();
        inactiveMusicSource.clip = null;
        inactiveMusicSource.volume = 0f;
        inactiveMusicSource.pitch = 1f;
    }
}
