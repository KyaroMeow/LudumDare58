using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    public GameObject itemScanHUD;
    public ItemMarkerUI itemMarkerUI;

    [Header("Mistake Counter HUD")]
    [SerializeField] private bool createRuntimeMistakeCounter = true;
    [SerializeField] private Vector2 mistakeCounterOffset = new Vector2(34f, -30f);
    [SerializeField] private Vector2 mistakeCounterSize = new Vector2(330f, 96f);
    [SerializeField] private Color mistakePanelColor = new Color(0.015f, 0.018f, 0.025f, 0.9f);
    [SerializeField] private Color mistakeAccentColor = new Color(1f, 0.18f, 0.12f, 1f);
    [SerializeField] private Color mistakeSafeColor = new Color(0.18f, 0.92f, 0.82f, 1f);
    [SerializeField] private Color mistakeEmptyColor = new Color(0.13f, 0.145f, 0.17f, 0.95f);
    [SerializeField] private Color mistakeTextColor = new Color(0.94f, 0.96f, 0.98f, 1f);
    [SerializeField] private Color mistakeGlowColor = new Color(1f, 0.08f, 0.04f, 0.24f);

    private RectTransform mistakeCounterRoot;
    private Image mistakePanelImage;
    private Image mistakeGlowImage;
    private TextMeshProUGUI mistakeTitleText;
    private TextMeshProUGUI mistakeCaptionText;
    private TextMeshProUGUI mistakeValueText;
    private TextMeshProUGUI mistakeIconText;
    private RectTransform mistakeIconTransform;
    private Image[] mistakeSegments = Array.Empty<Image>();
    private int lastMistakes = -1;
    private int lastMaxMistakes = -1;
    private bool lastGameStarted;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        EnsureMistakeCounterHud();
        RefreshMistakeCounter(force: true);
    }

    private void Update()
    {
        RefreshMistakeCounter(force: false);
        AnimateMistakeCounter();
    }

    public void showItemScanHUD(Item currentItem = null)
    {
        itemScanHUD.SetActive(true);
        itemMarkerUI?.BeginItem(currentItem);
    }

    public void hideItemScanHUD()
    {
        itemMarkerUI?.EndItem();
        itemScanHUD.SetActive(false);
    }

    private void EnsureMistakeCounterHud()
    {
        if (!createRuntimeMistakeCounter || mistakeCounterRoot != null)
        {
            return;
        }

        Canvas canvas = ResolveMistakeCounterCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("Mistake counter HUD skipped because no Canvas was found.", this);
            return;
        }

        GameObject glowObject = CreateImageObject("RuntimeMistakeCounterGlow", canvas.transform, mistakeGlowColor);
        RectTransform glowTransform = glowObject.GetComponent<RectTransform>();
        ConfigureTopLeftRect(glowTransform, mistakeCounterOffset + new Vector2(-9f, 9f), mistakeCounterSize + new Vector2(18f, 18f));
        mistakeGlowImage = glowObject.GetComponent<Image>();

        GameObject rootObject = CreateImageObject("RuntimeMistakeCounter", canvas.transform, mistakePanelColor);
        mistakeCounterRoot = rootObject.GetComponent<RectTransform>();
        ConfigureTopLeftRect(mistakeCounterRoot, mistakeCounterOffset, mistakeCounterSize);

        mistakePanelImage = rootObject.GetComponent<Image>();
        AddUiOutline(rootObject, new Color(1f, 0.18f, 0.1f, 0.55f), new Vector2(2f, -2f));

        CreateCounterFrame(rootObject.transform);
        CreateCounterAccent(rootObject.transform);
        CreateCounterIcon(rootObject.transform);
        mistakeTitleText = CreateCounterText("Title", rootObject.transform, new Vector2(86f, -13f), new Vector2(150f, 24f), "\u041e\u0428\u0418\u0411\u041a\u0418", 15, FontStyles.Bold, TextAlignmentOptions.Left);
        mistakeCaptionText = CreateCounterText("Caption", rootObject.transform, new Vector2(86f, -36f), new Vector2(170f, 18f), "\u041f\u0420\u041e\u0422\u041e\u041a\u041e\u041b \u041a\u041e\u041d\u0422\u0420\u041e\u041b\u042f", 9, FontStyles.Bold, TextAlignmentOptions.Left);
        mistakeValueText = CreateCounterText("Value", rootObject.transform, new Vector2(238f, -9f), new Vector2(72f, 36f), "0/0", 24, FontStyles.Bold, TextAlignmentOptions.Right);
        CreateCounterSegments(rootObject.transform);

        glowObject.transform.SetAsLastSibling();
        rootObject.transform.SetAsLastSibling();
    }

    private Canvas ResolveMistakeCounterCanvas()
    {
        Canvas fallback = null;
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        Canvas timeCanvas = null;
        Canvas hudCanvas = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (canvas.name == "TimeCanvas")
            {
                timeCanvas = canvas;
            }
            else if (canvas.name == "HudCanvas")
            {
                hudCanvas = canvas;
            }

            if (fallback == null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                fallback = canvas;
            }
        }

        if (timeCanvas != null)
        {
            return timeCanvas;
        }

        if (hudCanvas != null)
        {
            return hudCanvas;
        }

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.gameObject.activeInHierarchy)
        {
            return parentCanvas;
        }

        return fallback != null ? fallback : FindFirstObjectByType<Canvas>();
    }

    private GameObject CreateImageObject(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return imageObject;
    }

    private void ConfigureTopLeftRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    private void CreateCounterAccent(Transform parent)
    {
        GameObject accentObject = CreateImageObject("Accent", parent, mistakeAccentColor);
        RectTransform accentTransform = accentObject.GetComponent<RectTransform>();
        accentTransform.anchorMin = new Vector2(0f, 0f);
        accentTransform.anchorMax = new Vector2(0f, 1f);
        accentTransform.pivot = new Vector2(0f, 0.5f);
        accentTransform.anchoredPosition = Vector2.zero;
        accentTransform.sizeDelta = new Vector2(6f, 0f);
    }

    private void CreateCounterFrame(Transform parent)
    {
        CreateFrameLine(parent, "TopLine", new Vector2(20f, -8f), new Vector2(290f, 2f), mistakeAccentColor);
        CreateFrameLine(parent, "BottomLine", new Vector2(20f, 9f), new Vector2(290f, 2f), new Color(0.18f, 0.92f, 0.82f, 0.55f));
        CreateFrameLine(parent, "RightTick", new Vector2(314f, -18f), new Vector2(3f, 54f), new Color(1f, 0.18f, 0.1f, 0.42f));
    }

    private void CreateFrameLine(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject lineObject = CreateImageObject(objectName, parent, color);
        RectTransform lineTransform = lineObject.GetComponent<RectTransform>();
        ConfigureTopLeftRect(lineTransform, anchoredPosition, size);
    }

    private void CreateCounterIcon(Transform parent)
    {
        GameObject iconBackObject = CreateImageObject("IconBack", parent, new Color(0.09f, 0.015f, 0.012f, 0.94f));
        RectTransform iconBackTransform = iconBackObject.GetComponent<RectTransform>();
        ConfigureTopLeftRect(iconBackTransform, new Vector2(22f, -17f), new Vector2(48f, 48f));
        AddUiOutline(iconBackObject, new Color(1f, 0.1f, 0.06f, 0.82f), new Vector2(1.5f, -1.5f));

        mistakeIconText = CreateCounterText("Icon", parent, new Vector2(22f, -12f), new Vector2(48f, 50f), "!", 34, FontStyles.Bold, TextAlignmentOptions.Center);
        mistakeIconText.color = mistakeAccentColor;
        mistakeIconTransform = mistakeIconText.rectTransform;
    }

    private void CreateCounterSegments(Transform parent)
    {
        const int maxSegments = 10;
        const float width = 21f;
        const float gap = 6f;

        mistakeSegments = new Image[maxSegments];
        for (int i = 0; i < maxSegments; i++)
        {
            GameObject segmentObject = CreateImageObject($"MistakeSegment_{i + 1}", parent, mistakeEmptyColor);
            RectTransform segmentTransform = segmentObject.GetComponent<RectTransform>();
            segmentTransform.anchorMin = new Vector2(0f, 0f);
            segmentTransform.anchorMax = new Vector2(0f, 0f);
            segmentTransform.pivot = new Vector2(0f, 0f);
            segmentTransform.anchoredPosition = new Vector2(86f + i * (width + gap), 16f);
            segmentTransform.sizeDelta = new Vector2(width, 12f);
            AddUiOutline(segmentObject, new Color(0f, 0f, 0f, 0.5f), new Vector2(1f, -1f));
            mistakeSegments[i] = segmentObject.GetComponent<Image>();
        }
    }

    private TextMeshProUGUI CreateCounterText(
        string objectName,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        string text,
        int fontSize,
        FontStyles style,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        ConfigureTopLeftRect(rectTransform, anchoredPosition, size);

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = mistakeTextColor;
        label.raycastTarget = false;
        AddTextShadow(textObject, new Color(0f, 0f, 0f, 0.8f), new Vector2(1.5f, -1.5f));
        return label;
    }

    private void RefreshMistakeCounter(bool force)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        EnsureMistakeCounterHud();
        if (mistakeCounterRoot == null)
        {
            return;
        }

        bool gameStarted = GameManager.Instance.isGameStarted;
        int mistakes = Mathf.Max(0, GameManager.Instance.currentMistakes);
        int maxMistakes = Mathf.Max(1, ResolveMaxMistakes());
        if (!force && mistakes == lastMistakes && maxMistakes == lastMaxMistakes && gameStarted == lastGameStarted)
        {
            return;
        }

        lastMistakes = mistakes;
        lastMaxMistakes = maxMistakes;
        lastGameStarted = gameStarted;

        mistakeCounterRoot.gameObject.SetActive(gameStarted);
        if (mistakeGlowImage != null)
        {
            mistakeGlowImage.gameObject.SetActive(gameStarted);
        }

        float normalizedMistakes = Mathf.Clamp01((float)mistakes / maxMistakes);
        if (mistakeValueText != null)
        {
            mistakeValueText.text = $"{mistakes}/{maxMistakes}";
            mistakeValueText.color = mistakes >= maxMistakes ? mistakeAccentColor : mistakeTextColor;
        }

        if (mistakeTitleText != null)
        {
            mistakeTitleText.color = mistakes > 0 ? mistakeAccentColor : mistakeTextColor;
        }

        if (mistakeCaptionText != null)
        {
            mistakeCaptionText.color = new Color(mistakeTextColor.r, mistakeTextColor.g, mistakeTextColor.b, mistakes > 0 ? 0.86f : 0.58f);
        }

        if (mistakeIconText != null)
        {
            mistakeIconText.color = mistakes > 0 ? mistakeAccentColor : mistakeSafeColor;
        }

        if (mistakePanelImage != null)
        {
            mistakePanelImage.color = Color.Lerp(mistakePanelColor, new Color(0.12f, 0.025f, 0.02f, 0.94f), normalizedMistakes);
        }

        if (mistakeGlowImage != null)
        {
            mistakeGlowImage.color = Color.Lerp(new Color(0.18f, 0.9f, 0.8f, 0.12f), mistakeGlowColor, Mathf.Max(0.15f, normalizedMistakes));
        }

        for (int i = 0; i < mistakeSegments.Length; i++)
        {
            Image segment = mistakeSegments[i];
            if (segment == null)
            {
                continue;
            }

            float segmentThreshold = (i + 1f) / mistakeSegments.Length;
            bool filled = normalizedMistakes >= segmentThreshold || i < mistakes && maxMistakes <= mistakeSegments.Length;
            segment.color = filled ? Color.Lerp(mistakeSafeColor, mistakeAccentColor, normalizedMistakes) : mistakeEmptyColor;
        }
    }

    private void AnimateMistakeCounter()
    {
        if (mistakeCounterRoot == null || !mistakeCounterRoot.gameObject.activeSelf)
        {
            return;
        }

        float normalizedMistakes = lastMaxMistakes > 0 ? Mathf.Clamp01((float)Mathf.Max(0, lastMistakes) / lastMaxMistakes) : 0f;
        float pulse = (Mathf.Sin(Time.unscaledTime * Mathf.Lerp(2.1f, 5.5f, normalizedMistakes)) + 1f) * 0.5f;

        if (mistakeGlowImage != null)
        {
            Color baseGlow = Color.Lerp(new Color(0.18f, 0.9f, 0.8f, 0.13f), mistakeGlowColor, Mathf.Max(0.18f, normalizedMistakes));
            baseGlow.a *= Mathf.Lerp(0.76f, 1.24f, pulse);
            mistakeGlowImage.color = baseGlow;
        }

        if (mistakeIconTransform != null)
        {
            float scale = 1f + Mathf.Lerp(0.012f, 0.045f, normalizedMistakes) * pulse;
            mistakeIconTransform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private int ResolveMaxMistakes()
    {
        SettingManager settings = SettingManager.EnsureInstance();
        Difficult difficulty = settings != null ? settings.currentDifficulty : null;
        return difficulty != null ? difficulty.maxMistakes : 10;
    }

    private void AddUiOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private void AddTextShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }
}
