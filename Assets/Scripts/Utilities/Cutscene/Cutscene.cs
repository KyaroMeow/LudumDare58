using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

[System.Serializable]
public class CutsceneStep
{
    public Sprite image;
    public AudioClip audioClip;

    [TextArea]
    public string subtitle;

    public float autoAdvanceDuration;
    public float subtitleDuration;
    public float fadeDuration = 0.5f;
}

public enum CutsceneImageFitMode
{
    FillScreenCrop,
    FitInsideWithBars,
    Stretch
}

public class Cutscene : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    public Image cutsceneImage;
    public Image fadeBlackImage;
    public DialogText dialogText;
    public AudioSource audioSource;
    public GameObject choicePanel;
    public List<CutsceneStep> cutsceneSteps;
    public bool skipCutscene;

    [Header("Resolution Safe Layout")]
    [SerializeField] private bool configureCanvasScaler = true;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [Range(0f, 1f)]
    [SerializeField] private float matchWidthOrHeight = 0.5f;

    [SerializeField] private bool forceFullscreenRoot = true;
    [SerializeField] private bool forceFullscreenFade = true;
    [SerializeField] private CutsceneImageFitMode imageFitMode = CutsceneImageFitMode.FillScreenCrop;

    [Tooltip("Use this if the art must be fully visible. It can create black bars on non-16:9 screens.")]
    [SerializeField] private bool preserveFullImage = false;

    private bool _advance;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private Sprite currentSprite;

    private void Awake()
    {
        ApplyResolutionSafeLayout();
    }

    private void OnEnable()
    {
        ApplyResolutionSafeLayout();
    }

    private void Update()
    {
        if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
        {
            return;
        }

        ApplyResolutionSafeLayout();
    }

    public void Play(Action onComplete = null)
    {
        ApplyResolutionSafeLayout();

        if (cutsceneImage != null)
        {
            cutsceneImage.raycastTarget = true;
        }

        StartCoroutine(PlayCutscene(onComplete));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _advance = true;
    }

    private IEnumerator PlayCutscene(Action onComplete = null)
    {
        if (cutsceneSteps == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        for (int i = 0; i < cutsceneSteps.Count; i++)
        {
            CutsceneStep step = cutsceneSteps[i];

            if (skipCutscene)
            {
                continue;
            }

            if (step != null && step.image != null)
            {
                UnHideImages();
                SetCutsceneSprite(step.image);
                yield return FadeImage(fadeBlackImage, 0f, step.fadeDuration);
            }
            else
            {
                HideImages();
            }

            if (step != null && step.audioClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(step.audioClip);
            }

            if (step != null && !string.IsNullOrEmpty(step.subtitle) && dialogText != null)
            {
                yield return HandleSubtitle(step);
            }

            if (step != null)
            {
                yield return WaitForAdvance(step.autoAdvanceDuration);
            }

            if (step != null && step.image != null)
            {
                yield return FadeImage(fadeBlackImage, 1f, step.fadeDuration);
            }

            if (dialogText != null)
            {
                dialogText.SetText(string.Empty);
            }

            if (audioSource != null)
            {
                audioSource.Stop();
            }

            _advance = false;
        }

        onComplete?.Invoke();

        if (cutsceneImage != null)
        {
            cutsceneImage.enabled = false;
        }

        yield return FadeImage(fadeBlackImage, 0f, 1f);

        if (cutsceneImage != null)
        {
            cutsceneImage.raycastTarget = false;
        }
    }

    private void SetCutsceneSprite(Sprite sprite)
    {
        currentSprite = sprite;

        if (cutsceneImage == null)
        {
            return;
        }

        cutsceneImage.sprite = sprite;
        cutsceneImage.enabled = true;
        cutsceneImage.raycastTarget = true;

        ApplyResolutionSafeLayout();
    }

    private void ApplyResolutionSafeLayout()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        Canvas canvas = GetComponentInParent<Canvas>(true);

        if (configureCanvasScaler && canvas != null)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();

            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
        }

        if (forceFullscreenRoot)
        {
            RectTransform rootRect = transform as RectTransform;
            SetStretchFullscreen(rootRect);
        }

        if (forceFullscreenFade && fadeBlackImage != null)
        {
            SetStretchFullscreen(fadeBlackImage.rectTransform);
            fadeBlackImage.raycastTarget = false;
        }

        ApplyCutsceneImageLayout();
    }

    private void ApplyCutsceneImageLayout()
    {
        if (cutsceneImage == null)
        {
            return;
        }

        RectTransform imageRect = cutsceneImage.rectTransform;

        if (imageFitMode == CutsceneImageFitMode.Stretch)
        {
            SetStretchFullscreen(imageRect);
            cutsceneImage.preserveAspect = false;
            return;
        }

        cutsceneImage.preserveAspect = false;

        RectTransform container = ResolveImageContainer();
        Vector2 containerSize = ResolveContainerSize(container);

        if (containerSize.x <= 0f || containerSize.y <= 0f)
        {
            containerSize = referenceResolution;
        }

        Sprite sprite = currentSprite != null ? currentSprite : cutsceneImage.sprite;

        if (sprite == null)
        {
            SetStretchFullscreen(imageRect);
            return;
        }

        float spriteWidth = Mathf.Max(1f, sprite.rect.width);
        float spriteHeight = Mathf.Max(1f, sprite.rect.height);
        float spriteAspect = spriteWidth / spriteHeight;
        float containerAspect = containerSize.x / containerSize.y;

        bool fitInside = preserveFullImage || imageFitMode == CutsceneImageFitMode.FitInsideWithBars;

        Vector2 imageSize;

        if (fitInside)
        {
            if (spriteAspect > containerAspect)
            {
                imageSize = new Vector2(containerSize.x, containerSize.x / spriteAspect);
            }
            else
            {
                imageSize = new Vector2(containerSize.y * spriteAspect, containerSize.y);
            }
        }
        else
        {
            if (spriteAspect > containerAspect)
            {
                imageSize = new Vector2(containerSize.y * spriteAspect, containerSize.y);
            }
            else
            {
                imageSize = new Vector2(containerSize.x, containerSize.x / spriteAspect);
            }
        }

        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.sizeDelta = imageSize;
        imageRect.localScale = Vector3.one;
        imageRect.localRotation = Quaternion.identity;
    }

    private RectTransform ResolveImageContainer()
    {
        if (cutsceneImage == null)
        {
            return transform as RectTransform;
        }

        RectTransform parentRect = cutsceneImage.rectTransform.parent as RectTransform;

        if (parentRect != null)
        {
            return parentRect;
        }

        Canvas canvas = GetComponentInParent<Canvas>(true);

        if (canvas != null)
        {
            return canvas.transform as RectTransform;
        }

        return transform as RectTransform;
    }

    private Vector2 ResolveContainerSize(RectTransform container)
    {
        if (container != null)
        {
            Rect rect = container.rect;

            if (rect.width > 1f && rect.height > 1f)
            {
                return new Vector2(rect.width, rect.height);
            }
        }

        return new Vector2(
            Mathf.Max(1, Screen.width),
            Mathf.Max(1, Screen.height));
    }

    private void SetStretchFullscreen(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private IEnumerator FadeImage(Image image, float targetAlpha, float duration)
    {
        if (image == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            Color instantColor = image.color;
            instantColor.a = targetAlpha;
            image.color = instantColor;
            yield break;
        }

        yield return image.DOFade(targetAlpha, duration).WaitForCompletion();
    }

    private void ShowChoice()
    {
        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
        }
    }

    private void HideImages()
    {
        if (fadeBlackImage != null)
        {
            fadeBlackImage.color = new Color(0f, 0f, 0f, 0f);
        }

        if (cutsceneImage != null)
        {
            cutsceneImage.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    private void UnHideImages()
    {
        if (fadeBlackImage != null)
        {
            fadeBlackImage.color = new Color(0f, 0f, 0f, 1f);
            fadeBlackImage.enabled = true;
        }

        if (cutsceneImage != null)
        {
            cutsceneImage.color = new Color(1f, 1f, 1f, 1f);
            cutsceneImage.enabled = true;
        }
    }

    private IEnumerator HandleSubtitle(CutsceneStep step)
    {
        if (dialogText == null || step == null || string.IsNullOrEmpty(step.subtitle))
        {
            yield break;
        }

        dialogText.StartPlayText(step.subtitle, step.subtitleDuration, () => { _advance = true; });

        yield return WaitForAdvance();

        dialogText.SetText(step.subtitle);
        _advance = false;
    }

    private IEnumerator WaitForAdvance(float autoAdvanceDuration = 0f)
    {
        if (autoAdvanceDuration > 0f)
        {
            float timer = 0f;

            while (!_advance && timer < autoAdvanceDuration)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            _advance = true;
            yield break;
        }

        while (!_advance)
        {
            yield return null;
        }
    }
}