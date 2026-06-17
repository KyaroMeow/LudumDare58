using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class CutscenePlaybackManager : MonoBehaviour
{
    private sealed class AudioSourceSnapshot
    {
        public AudioSource Source;
        public bool Mute;
        public float Volume;
    }

    public static CutscenePlaybackManager Instance { get; private set; }

    [SerializeField] private VideoClip bookTheftClip;
    [SerializeField] private VideoClip toasterClip;
    [SerializeField] private string creditsSceneName = "Titrs";
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private bool allowSkip = false;
    [SerializeField] private KeyCode skipKey = KeyCode.Space;
    [SerializeField] private Color backgroundColor = Color.black;
    [SerializeField] private float failSafeExtraDelay = 1f;
    [SerializeField] private float preCutsceneFadeDuration = 0.6f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool fadeToBlackBeforeVideo = true;
    [SerializeField] private bool muteSceneAudioDuringCutscene = true;
    [SerializeField] private float audioMuteFadeDuration = 0.25f;

    private readonly List<AudioSourceSnapshot> audioSnapshots = new List<AudioSourceSnapshot>();

    private bool isPlaying;
    private bool warnedMissingBookTheftClip;
    private bool warnedMissingToasterClip;
    private bool warnedMissingGenericClip;

    private GameObject playbackRoot;
    private Image backgroundImage;
    private RawImage rawImage;
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private RenderTexture renderTexture;

    public bool IsPlaying => isPlaying;
    public string MenuSceneName => string.IsNullOrWhiteSpace(menuSceneName) ? "Menu" : menuSceneName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        RestoreSceneAudio();
        CleanupPlaybackObjects();
    }

    public void PlayBookTheftCutscene()
    {
        PlayCutscene(bookTheftClip, ref warnedMissingBookTheftClip, "book theft");
    }

    public void PlayToasterCutscene()
    {
        PlayCutscene(toasterClip, ref warnedMissingToasterClip, "toaster");
    }

    public void PlayCutscene(VideoClip clip)
    {
        PlayCutscene(clip, ref warnedMissingGenericClip, "requested");
    }

    private void PlayCutscene(VideoClip clip, ref bool warnedMissingClip, string clipLabel)
    {
        if (isPlaying)
        {
            return;
        }

        if (clip == null)
        {
            if (!warnedMissingClip)
            {
                warnedMissingClip = true;
                Debug.LogWarning($"Cannot play {clipLabel} cutscene because VideoClip is not assigned. Loading credits scene.");
            }

            StartCoroutine(FallbackToCreditsAfterFadeRoutine());
            return;
        }

        StartCoroutine(PlayCutsceneRoutine(clip));
    }

    private IEnumerator FallbackToCreditsAfterFadeRoutine()
    {
        isPlaying = true;
        ApplyLocalPlaybackLock();
        CreatePlaybackObjects(null);

        yield return FadeBackgroundToBlackRoutine();

        CleanupPlaybackObjects();
        LoadCreditsScene();
    }

    private IEnumerator PlayCutsceneRoutine(VideoClip clip)
    {
        isPlaying = true;
        ApplyLocalPlaybackLock();
        CreatePlaybackObjects(clip);
        CaptureSceneAudioSources();

        bool finished = false;
        bool failed = false;

        VideoPlayer.EventHandler loopHandler = _ => finished = true;
        VideoPlayer.ErrorEventHandler errorHandler = (_, message) =>
        {
            failed = true;
            Debug.LogWarning($"Video cutscene playback failed: {message}");
        };

        videoPlayer.loopPointReached += loopHandler;
        videoPlayer.errorReceived += errorHandler;
        videoPlayer.clip = clip;
        videoPlayer.Prepare();

        yield return FadeBackgroundToBlackRoutine();
        yield return MuteSceneAudioRoutine();

        while (!videoPlayer.isPrepared && !failed)
        {
            yield return null;
        }

        if (!failed)
        {
            SetRawImageAlpha(1f);
            videoPlayer.Play();
        }

        float elapsed = 0f;
        float failSafeDuration = clip.length > 0.01f ? (float)clip.length + Mathf.Max(0f, failSafeExtraDelay) : 0f;

        while (!finished && !failed)
        {
            if (allowSkip && Input.GetKeyDown(skipKey))
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            if (failSafeDuration > 0f && elapsed >= failSafeDuration)
            {
                break;
            }

            yield return null;
        }

        videoPlayer.loopPointReached -= loopHandler;
        videoPlayer.errorReceived -= errorHandler;

        RestoreSceneAudio();
        CleanupPlaybackObjects();
        LoadCreditsScene();
    }

    private void ApplyLocalPlaybackLock()
    {
        GameManager.Instance?.SetStoryInteractionLocked(true);
        PlayerView.Instance?.BlockMovement();

        InventoryUIController inventory = FindFirstObjectByType<InventoryUIController>();
        inventory?.CloseInventory();
    }

    private void CreatePlaybackObjects(VideoClip clip)
    {
        playbackRoot = new GameObject("Runtime Cutscene Playback");

        Canvas canvas = playbackRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = playbackRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        playbackRoot.AddComponent<GraphicRaycaster>();

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backgroundObject.transform.SetParent(playbackRoot.transform, false);
        StretchToParent(backgroundObject.GetComponent<RectTransform>());

        backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = WithAlpha(backgroundColor, fadeToBlackBeforeVideo ? 0f : backgroundColor.a);
        backgroundImage.raycastTarget = true;

        if (clip == null)
        {
            return;
        }

        GameObject rawImageObject = new GameObject("Video", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
        rawImageObject.transform.SetParent(backgroundObject.transform, false);
        StretchToParent(rawImageObject.GetComponent<RectTransform>());

        rawImage = rawImageObject.GetComponent<RawImage>();
        rawImage.raycastTarget = false;
        rawImage.color = WithAlpha(Color.white, 0f);

        AspectRatioFitter fitter = rawImageObject.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = ResolveAspectRatio(clip);

        int width = ResolveRenderTextureSize((int)clip.width, Screen.width);
        int height = ResolveRenderTextureSize((int)clip.height, Screen.height);
        renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        rawImage.texture = renderTexture;

        videoPlayer = playbackRoot.AddComponent<VideoPlayer>();
        audioSource = playbackRoot.AddComponent<AudioSource>();

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
    }

    private IEnumerator FadeBackgroundToBlackRoutine()
    {
        if (!fadeToBlackBeforeVideo || backgroundImage == null)
        {
            SetBackgroundAlpha(backgroundColor.a);
            yield break;
        }

        float duration = Mathf.Max(0f, preCutsceneFadeDuration);
        if (duration <= 0f)
        {
            SetBackgroundAlpha(backgroundColor.a);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float evaluated = fadeCurve != null ? fadeCurve.Evaluate(normalized) : normalized;
            SetBackgroundAlpha(Mathf.Lerp(0f, backgroundColor.a, evaluated));
            yield return null;
        }

        SetBackgroundAlpha(backgroundColor.a);
    }

    private void CaptureSceneAudioSources()
    {
        audioSnapshots.Clear();

        if (!muteSceneAudioDuringCutscene)
        {
            return;
        }

        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null || source == audioSource)
            {
                continue;
            }

            audioSnapshots.Add(new AudioSourceSnapshot
            {
                Source = source,
                Mute = source.mute,
                Volume = source.volume
            });
        }
    }

    private IEnumerator MuteSceneAudioRoutine()
    {
        if (!muteSceneAudioDuringCutscene || audioSnapshots.Count == 0)
        {
            yield break;
        }

        float duration = Mathf.Max(0f, audioMuteFadeDuration);
        if (duration <= 0f)
        {
            SetSceneAudioMuted();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < audioSnapshots.Count; i++)
            {
                AudioSourceSnapshot snapshot = audioSnapshots[i];
                if (snapshot.Source == null || snapshot.Mute)
                {
                    continue;
                }

                snapshot.Source.volume = Mathf.Lerp(snapshot.Volume, 0f, normalized);
            }

            yield return null;
        }

        SetSceneAudioMuted();
    }

    private void SetSceneAudioMuted()
    {
        for (int i = 0; i < audioSnapshots.Count; i++)
        {
            AudioSourceSnapshot snapshot = audioSnapshots[i];
            if (snapshot.Source == null)
            {
                continue;
            }

            snapshot.Source.volume = 0f;
            snapshot.Source.mute = true;
        }
    }

    private void RestoreSceneAudio()
    {
        for (int i = 0; i < audioSnapshots.Count; i++)
        {
            AudioSourceSnapshot snapshot = audioSnapshots[i];
            if (snapshot.Source == null)
            {
                continue;
            }

            snapshot.Source.volume = snapshot.Volume;
            snapshot.Source.mute = snapshot.Mute;
        }

        audioSnapshots.Clear();
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private static float ResolveAspectRatio(VideoClip clip)
    {
        if (clip != null && clip.width > 0 && clip.height > 0)
        {
            return (float)clip.width / clip.height;
        }

        return Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
    }

    private static int ResolveRenderTextureSize(int clipSize, int fallbackSize)
    {
        int size = clipSize > 0 ? clipSize : fallbackSize;
        return Mathf.Clamp(size, 16, SystemInfo.maxTextureSize);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private void SetBackgroundAlpha(float alpha)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = WithAlpha(backgroundColor, alpha);
        }
    }

    private void SetRawImageAlpha(float alpha)
    {
        if (rawImage != null)
        {
            rawImage.color = WithAlpha(Color.white, alpha);
        }
    }

    private void CleanupPlaybackObjects()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.targetTexture = null;
            videoPlayer = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource = null;
        }

        rawImage = null;
        backgroundImage = null;

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }

        if (playbackRoot != null)
        {
            Destroy(playbackRoot);
            playbackRoot = null;
        }
    }

    private void LoadCreditsScene()
    {
        isPlaying = false;
        string targetSceneName = string.IsNullOrWhiteSpace(creditsSceneName) ? "Titrs" : creditsSceneName;
        SceneManager.LoadScene(targetSceneName);
    }
}
