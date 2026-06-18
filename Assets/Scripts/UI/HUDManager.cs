using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    public GameObject itemScanHUD;
    public ItemMarkerUI itemMarkerUI;

    [Header("Mistake Meter")]
    [SerializeField] private bool createRuntimeMistakeCounter = true;
    [SerializeField] private bool showMistakeMeterOnlyAfterShiftStarted = true;
    [SerializeField] private Vector2 mistakeCounterOffset = new Vector2(28f, -24f);
    [SerializeField] private Vector2 mistakeMeterSize = new Vector2(286f, 70f);
    [SerializeField] private Color meterPanelColor = new Color(0.012f, 0.014f, 0.02f, 0.6f);
    [SerializeField] private Color meterSafeColor = new Color(0.16f, 0.88f, 0.78f, 1f);
    [SerializeField] private Color meterWarningColor = new Color(0.95f, 0.63f, 0.25f, 1f);
    [SerializeField] private Color meterDangerColor = new Color(1f, 0.12f, 0.08f, 1f);
    [SerializeField] private Color meterEmptyColor = new Color(0.11f, 0.125f, 0.145f, 1f);
    [SerializeField] private int mistakeCounterSortingOrder = 32000;
    [SerializeField] private float pointerRiseTime = 0.14f;
    [SerializeField] private float pointerFallTime = 0.75f;

    private const float TickStartX = 17f;
    private const float TickEndPadding = 17f;

    private Canvas mistakeCounterCanvas;
    private RectTransform meterRoot;
    private RectTransform panelTransform;
    private RectTransform pointerTransform;
    private Image panelImage;
    private Image[] meterTicks;
    private Color[] tickColors;
    private TextMeshProUGUI pointerText;
    private TextMeshProUGUI valueText;
    private TextMeshProUGUI titleText;
    private int meterTickCount = 10;
    private int lastCounter = -1;
    private int lastThreshold = -1;
    private int lastPunishments = -1;
    private float targetMeterValue;
    private float displayedMeterValue;
    private float pointerVelocity;
    private float impactPulse;
    private float punishmentFlashUntil;
    private bool pointerInitialized;
    private bool mistakeMeterVisible;

    public RectTransform MistakeCounterTutorialTarget => IsMistakeMeterVisibleNow() ? meterRoot : null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        EnsureMistakeMeter();
        ApplyMistakeMeterVisibility(true);

        if (IsMistakeMeterVisibleNow())
        {
            RefreshMistakeMeter(true);
        }
    }

    private void Update()
    {
        EnsureMistakeMeter();
        ApplyMistakeMeterVisibility(false);

        if (!IsMistakeMeterVisibleNow())
        {
            return;
        }

        RefreshMistakeMeter(false);
        AnimateMistakeMeter();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void showItemScanHUD(Item currentItem = null)
    {
        if (itemScanHUD != null)
        {
            itemScanHUD.SetActive(true);
        }

        itemMarkerUI?.BeginItem(currentItem);
    }

    public void hideItemScanHUD()
    {
        itemMarkerUI?.EndItem();

        if (itemScanHUD != null)
        {
            itemScanHUD.SetActive(false);
        }
    }

    private bool ShouldShowMistakeMeter()
    {
        if (!createRuntimeMistakeCounter)
        {
            return false;
        }

        if (!showMistakeMeterOnlyAfterShiftStarted)
        {
            return true;
        }

        return GameManager.Instance != null && GameManager.Instance.isGameStarted;
    }

    private bool IsMistakeMeterVisibleNow()
    {
        return meterRoot != null && meterRoot.gameObject.activeSelf;
    }

    private void ApplyMistakeMeterVisibility(bool force)
    {
        if (meterRoot == null)
        {
            return;
        }

        bool shouldShow = ShouldShowMistakeMeter();

        if (!force && mistakeMeterVisible == shouldShow)
        {
            return;
        }

        mistakeMeterVisible = shouldShow;
        meterRoot.gameObject.SetActive(shouldShow);

        if (mistakeCounterCanvas != null)
        {
            mistakeCounterCanvas.enabled = shouldShow;
        }

        if (shouldShow)
        {
            ForceRefreshMistakeMeterState();
            RefreshMistakeMeter(true);
        }
    }

    private void ForceRefreshMistakeMeterState()
    {
        lastCounter = -1;
        lastThreshold = -1;
        lastPunishments = -1;
        pointerInitialized = false;
        pointerVelocity = 0f;
        impactPulse = 0f;
        punishmentFlashUntil = -1f;
    }

    private void EnsureMistakeMeter()
    {
        if (!createRuntimeMistakeCounter || meterRoot != null)
        {
            return;
        }

        Canvas canvas = ResolveMistakeCounterCanvas();

        GameObject rootObject = CreateRectObject("RuntimeMistakeMeter", canvas.transform);
        meterRoot = rootObject.GetComponent<RectTransform>();
        ConfigureTopLeft(meterRoot, mistakeCounterOffset, mistakeMeterSize);

        CreateScannerCorners(meterRoot);

        GameObject panelObject = CreateImageObject("MeterPanel", meterRoot, meterPanelColor);
        panelTransform = panelObject.GetComponent<RectTransform>();
        panelTransform.anchorMin = Vector2.zero;
        panelTransform.anchorMax = Vector2.one;
        panelTransform.offsetMin = new Vector2(5f, 5f);
        panelTransform.offsetMax = new Vector2(-5f, -5f);
        panelImage = panelObject.GetComponent<Image>();

        Outline panelOutline = panelObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(meterDangerColor.r, meterDangerColor.g, meterDangerColor.b, 0.64f);
        panelOutline.effectDistance = new Vector2(1.5f, -1.5f);

        titleText = CreateTopLeftText("Title", panelTransform, new Vector2(17f, -4f), new Vector2(150f, 18f), "РИСК НАКАЗАНИЯ", 10f, TextAlignmentOptions.Left);
        titleText.color = new Color(0.78f, 0.82f, 0.86f, 0.82f);

        valueText = CreateTopLeftText("Value", panelTransform, new Vector2(216f, -3f), new Vector2(50f, 18f), "0/10", 13f, TextAlignmentOptions.Right);
        valueText.color = Color.white;

        CreateTicks(panelTransform);
        CreatePointer(panelTransform);

        ApplyMistakeMeterVisibility(true);
    }

    private Canvas ResolveMistakeCounterCanvas()
    {
        if (mistakeCounterCanvas != null)
        {
            return mistakeCounterCanvas;
        }

        GameObject canvasObject = new GameObject("RuntimeMistakeCounterCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        canvasObject.transform.SetParent(null, false);

        mistakeCounterCanvas = canvasObject.GetComponent<Canvas>();
        mistakeCounterCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mistakeCounterCanvas.overrideSorting = true;
        mistakeCounterCanvas.sortingOrder = mistakeCounterSortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return mistakeCounterCanvas;
    }

    private void CreateScannerCorners(Transform parent)
    {
        Color color = new Color(meterDangerColor.r, meterDangerColor.g, meterDangerColor.b, 0.88f);
        CreateImageRect("CornerTop", parent, Vector2.zero, new Vector2(58f, 2f), color);
        CreateImageRect("CornerLeft", parent, Vector2.zero, new Vector2(2f, 22f), color);
        CreateBottomRightLine(parent, "CornerBottom", new Vector2(-42f, 0f), new Vector2(42f, 2f), color);
        CreateBottomRightLine(parent, "CornerRight", Vector2.zero, new Vector2(2f, 22f), color);
    }

    private void CreateTicks(Transform parent)
    {
        meterTickCount = Mathf.Clamp(GameManager.Instance != null ? GameManager.Instance.CurrentHandDamageThreshold : 10, 1, 20);
        meterTicks = new Image[meterTickCount];
        tickColors = new Color[meterTickCount];

        float availableWidth = mistakeMeterSize.x - 20f - TickStartX - TickEndPadding;
        float tickStep = meterTickCount > 1 ? availableWidth / (meterTickCount - 1) : 0f;
        float baseTickWidth = Mathf.Clamp(tickStep * 0.55f, 8f, 15f);

        for (int i = 0; i < meterTickCount; i++)
        {
            float normalized = meterTickCount > 1 ? i / (float)(meterTickCount - 1) : 1f;
            Color activeColor = normalized < 0.55f
                ? Color.Lerp(meterSafeColor, meterWarningColor, normalized / 0.55f)
                : Color.Lerp(meterWarningColor, meterDangerColor, (normalized - 0.55f) / 0.45f);

            tickColors[i] = activeColor;

            GameObject tickObject = CreateImageObject($"MeterTick_{i + 1:00}", parent, meterEmptyColor);
            RectTransform tick = tickObject.GetComponent<RectTransform>();
            tick.anchorMin = tick.anchorMax = new Vector2(0f, 0f);
            tick.pivot = new Vector2(0.5f, 0f);
            tick.anchoredPosition = new Vector2(TickStartX + i * tickStep, 12f);

            float height = i % 3 == 0 ? 25f : i % 2 == 0 ? 21f : 17f;
            float width = i % 3 == 0 ? baseTickWidth + 2f : baseTickWidth;
            tick.sizeDelta = new Vector2(width, height);

            Outline outline = tickObject.AddComponent<Outline>();
            outline.effectColor = new Color(activeColor.r, activeColor.g, activeColor.b, 0.35f);
            outline.effectDistance = new Vector2(1f, -1f);

            meterTicks[i] = tickObject.GetComponent<Image>();
        }
    }

    private void CreatePointer(Transform parent)
    {
        pointerText = CreateTopLeftText("MeterPointer", parent, Vector2.zero, new Vector2(18f, 18f), "▲", 15f, TextAlignmentOptions.Center);
        pointerText.color = meterSafeColor;
        pointerTransform = pointerText.rectTransform;
        pointerTransform.anchorMin = pointerTransform.anchorMax = new Vector2(0f, 0f);
        pointerTransform.pivot = new Vector2(0.5f, 0f);
        pointerTransform.anchoredPosition = new Vector2(TickStartX, 0f);
    }

    private void RefreshMistakeMeter(bool force)
    {
        if (meterRoot == null)
        {
            return;
        }

        int counter = GameManager.Instance != null ? Mathf.Max(0, GameManager.Instance.CurrentHandDamageCounter) : 0;
        int threshold = GameManager.Instance != null ? Mathf.Max(1, GameManager.Instance.CurrentHandDamageThreshold) : 10;
        int punishments = GameManager.Instance != null ? GameManager.Instance.HandPunishmentsApplied : 0;

        if (!force && counter == lastCounter && threshold == lastThreshold && punishments == lastPunishments)
        {
            return;
        }

        bool increased = lastCounter >= 0 && counter > lastCounter;
        bool punishmentTriggered = lastPunishments >= 0 && punishments > lastPunishments;

        lastCounter = counter;
        lastThreshold = threshold;
        lastPunishments = punishments;
        targetMeterValue = Mathf.Clamp01(counter / (float)threshold);

        if (!pointerInitialized)
        {
            displayedMeterValue = targetMeterValue;
            pointerInitialized = true;
        }

        if (punishmentTriggered)
        {
            displayedMeterValue = 1f;
            pointerVelocity = 0f;
            impactPulse = 1f;
            punishmentFlashUntil = Time.unscaledTime + 0.55f;
        }
        else if (increased)
        {
            impactPulse = 1f;
        }

        if (valueText != null)
        {
            valueText.text = $"{counter}/{threshold}";
            valueText.color = EvaluateMeterColor(targetMeterValue);
        }
    }

    private void AnimateMistakeMeter()
    {
        if (meterRoot == null || pointerTransform == null || meterTicks == null)
        {
            return;
        }

        float smoothTime = targetMeterValue >= displayedMeterValue ? pointerRiseTime : pointerFallTime;
        displayedMeterValue = Mathf.SmoothDamp(displayedMeterValue, targetMeterValue, ref pointerVelocity, Mathf.Max(0.01f, smoothTime), Mathf.Infinity, Time.unscaledDeltaTime);
        displayedMeterValue = Mathf.Clamp01(displayedMeterValue);

        float availableWidth = mistakeMeterSize.x - 20f - TickStartX - TickEndPadding;
        float pointerX = TickStartX + displayedMeterValue * availableWidth;

        pointerTransform.anchoredPosition = new Vector2(pointerX, 0f);

        Color currentColor = EvaluateMeterColor(displayedMeterValue);

        if (pointerText != null)
        {
            pointerText.color = currentColor;
        }

        float fill = displayedMeterValue * meterTickCount;

        for (int i = 0; i < meterTicks.Length; i++)
        {
            if (meterTicks[i] == null)
            {
                continue;
            }

            float amount = Mathf.Clamp01(fill - i);
            Color empty = meterEmptyColor;
            empty.a = 0.46f;
            meterTicks[i].color = Color.Lerp(empty, tickColors[i], amount);
        }

        if (Time.unscaledTime < punishmentFlashUntil)
        {
            if (valueText != null)
            {
                valueText.text = "10/10";
                valueText.color = meterDangerColor;
            }

            if (titleText != null)
            {
                titleText.text = "НАКАЗАНИЕ";
                titleText.color = meterDangerColor;
            }
        }
        else
        {
            if (valueText != null)
            {
                valueText.text = $"{Mathf.RoundToInt(displayedMeterValue * Mathf.Max(1, lastThreshold))}/{Mathf.Max(1, lastThreshold)}";
                valueText.color = currentColor;
            }

            if (titleText != null)
            {
                titleText.text = "РИСК НАКАЗАНИЯ";
                titleText.color = new Color(0.78f, 0.82f, 0.86f, 0.82f);
            }
        }

        impactPulse = Mathf.MoveTowards(impactPulse, 0f, Time.unscaledDeltaTime * 3.1f);

        float dangerPulse = ((Mathf.Sin(Time.unscaledTime * 4.4f) + 1f) * 0.5f) * displayedMeterValue;
        float pulse = Mathf.Max(impactPulse, dangerPulse * 0.2f);
        float scale = 1f + pulse * 0.025f;

        if (panelTransform != null)
        {
            panelTransform.localScale = new Vector3(scale, scale, 1f);
        }

        if (panelImage != null)
        {
            panelImage.color = Color.Lerp(meterPanelColor, new Color(0.075f, 0.012f, 0.009f, 0.7f), displayedMeterValue * 0.52f);
        }
    }

    private Color EvaluateMeterColor(float normalized)
    {
        return normalized < 0.55f
            ? Color.Lerp(meterSafeColor, meterWarningColor, normalized / 0.55f)
            : Color.Lerp(meterWarningColor, meterDangerColor, (normalized - 0.55f) / 0.45f);
    }

    private GameObject CreateRectObject(string objectName, Transform parent)
    {
        GameObject result = new GameObject(objectName, typeof(RectTransform));
        result.layer = LayerMask.NameToLayer("UI");
        result.transform.SetParent(parent, false);
        return result;
    }

    private GameObject CreateImageObject(string objectName, Transform parent, Color color)
    {
        GameObject result = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        result.layer = LayerMask.NameToLayer("UI");
        result.transform.SetParent(parent, false);

        Image image = result.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return result;
    }

    private void CreateImageRect(string objectName, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject result = CreateImageObject(objectName, parent, color);
        RectTransform rect = result.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void CreateBottomRightLine(Transform parent, string objectName, Vector2 position, Vector2 size, Color color)
    {
        GameObject result = CreateImageObject(objectName, parent, color);
        RectTransform rect = result.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private TextMeshProUGUI CreateTopLeftText(string objectName, Transform parent, Vector2 position, Vector2 size, string text, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject result = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        result.layer = LayerMask.NameToLayer("UI");
        result.transform.SetParent(parent, false);

        RectTransform rect = result.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI label = result.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.alignment = alignment;
        label.raycastTarget = false;

        return label;
    }

    private static void ConfigureTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}