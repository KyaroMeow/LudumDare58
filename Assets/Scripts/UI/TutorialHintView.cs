using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialHintView : MonoBehaviour
{
    private const float HiddenAlpha = 0f;
    private const float VisibleAlpha = 1f;

    private CanvasGroup canvasGroup;
    private RectTransform rootRect;
    private Text iconText;
    private Text bodyText;
    private Image accentImage;
    private Button skipButton;
    private Text skipText;

    private float fadeDuration = 0.2f;
    private float fadeTime;
    private bool targetVisible;
    private Action skipRequested;

    public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0.01f;

    public void Initialize(
        Canvas targetCanvas,
        Color panelColor,
        Color accentColor,
        Vector2 screenOffset,
        Action onSkipRequested)
    {
        skipRequested = onSkipRequested;
        BuildView(targetCanvas, panelColor, accentColor, screenOffset);
        HideImmediate();
    }

    public void Show(string text, string iconLabel, Color accentColor)
    {
        if (bodyText == null)
        {
            return;
        }

        bodyText.text = text;
        iconText.text = iconLabel;
        accentImage.color = accentColor;
        skipText.color = Color.Lerp(Color.white, accentColor, 0.35f);
        gameObject.SetActive(true);
        targetVisible = true;
        fadeTime = 0f;
    }

    public void SetScreenOffset(Vector2 screenOffset)
    {
        if (rootRect == null)
        {
            return;
        }

        rootRect.anchoredPosition = screenOffset == Vector2.zero ? new Vector2(28f, 34f) : screenOffset;
    }

    public void Hide()
    {
        targetVisible = false;
        fadeTime = 0f;
    }

    public void HideImmediate()
    {
        targetVisible = false;
        fadeTime = fadeDuration;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = HiddenAlpha;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (canvasGroup == null)
        {
            return;
        }

        fadeTime += Time.unscaledDeltaTime;
        float progress = fadeDuration <= 0f ? 1f : Mathf.Clamp01(fadeTime / fadeDuration);
        float targetAlpha = targetVisible ? VisibleAlpha : HiddenAlpha;
        float startAlpha = targetVisible ? HiddenAlpha : VisibleAlpha;
        canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Smooth(progress));
        canvasGroup.interactable = targetVisible;
        canvasGroup.blocksRaycasts = true;

        if (!targetVisible && progress >= 1f)
        {
            gameObject.SetActive(false);
        }
    }

    private void BuildView(Canvas targetCanvas, Color panelColor, Color accentColor, Vector2 screenOffset)
    {
        transform.SetParent(targetCanvas.transform, false);

        rootRect = GetComponent<RectTransform>();
        if (rootRect == null)
        {
            rootRect = gameObject.AddComponent<RectTransform>();
        }
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(0f, 0f);
        rootRect.pivot = new Vector2(0f, 0f);
        rootRect.anchoredPosition = screenOffset == Vector2.zero ? new Vector2(28f, 34f) : screenOffset;
        rootRect.sizeDelta = new Vector2(640f, 124f);

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        Image panel = gameObject.AddComponent<Image>();
        panel.color = panelColor;
        panel.raycastTarget = false;

        GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        accent.transform.SetParent(transform, false);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(5f, 0f);
        accentImage = accent.GetComponent<Image>();
        accentImage.color = accentColor;
        accentImage.raycastTarget = false;

        iconText = CreateText("Icon", transform, new Vector2(18f, 24f), new Vector2(118f, 72f), 22, FontStyle.Bold);
        iconText.alignment = TextAnchor.MiddleCenter;
        iconText.color = accentColor;
        iconText.horizontalOverflow = HorizontalWrapMode.Overflow;
        iconText.verticalOverflow = VerticalWrapMode.Truncate;

        bodyText = CreateText("Text", transform, new Vector2(154f, 38f), new Vector2(318f, 62f), 18, FontStyle.Normal);
        bodyText.alignment = TextAnchor.MiddleLeft;
        bodyText.color = Color.white;

        GameObject skipObject = new GameObject("SkipTutorialButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        skipObject.transform.SetParent(transform, false);
        RectTransform skipRect = skipObject.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(1f, 0f);
        skipRect.anchorMax = new Vector2(1f, 0f);
        skipRect.pivot = new Vector2(1f, 0f);
        skipRect.anchoredPosition = new Vector2(-14f, 14f);
        skipRect.sizeDelta = new Vector2(138f, 32f);

        Image skipImage = skipObject.GetComponent<Image>();
        skipImage.color = new Color(0.08f, 0.08f, 0.08f, 0.82f);
        skipImage.raycastTarget = true;

        skipButton = skipObject.GetComponent<Button>();
        skipButton.targetGraphic = skipImage;
        skipButton.onClick.AddListener(HandleSkipClicked);

        skipText = CreateText("Text", skipObject.transform, Vector2.zero, Vector2.zero, 13, FontStyle.Normal);
        RectTransform skipTextRect = skipText.transform as RectTransform;
        skipTextRect.anchorMin = Vector2.zero;
        skipTextRect.anchorMax = Vector2.one;
        skipTextRect.offsetMin = Vector2.zero;
        skipTextRect.offsetMax = Vector2.zero;
        skipText.alignment = TextAnchor.MiddleCenter;
        skipText.text = "Пропустить обучение";
        skipText.raycastTarget = false;
    }

    private Text CreateText(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private void HandleSkipClicked()
    {
        skipRequested?.Invoke();
    }

    private static float Smooth(float value)
    {
        return value * value * (3f - 2f * value);
    }

    private void OnDestroy()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(HandleSkipClicked);
        }
    }
}
