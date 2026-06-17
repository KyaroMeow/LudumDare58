using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(-500)]
public class MenuDisclaimerController : MonoBehaviour
{
    private const string FallbackTitleText = "\u0414\u0418\u0421\u041A\u041B\u0415\u0419\u041C\u0415\u0420";
    private const string FallbackBodyText =
        "\u0412\u041D\u0418\u041C\u0410\u041D\u0418\u0415\n\n" +
        "\u0412\u044B \u0437\u0430\u043F\u0443\u0441\u043A\u0430\u0435\u0442\u0435 \u0440\u0430\u043D\u043D\u044E\u044E \u0442\u0435\u0441\u0442\u043E\u0432\u0443\u044E \u0441\u0431\u043E\u0440\u043A\u0443 Sorter.\n\n" +
        "\u042D\u0442\u043E \u043D\u0435 \u0444\u0438\u043D\u0430\u043B\u044C\u043D\u0430\u044F \u0432\u0435\u0440\u0441\u0438\u044F \u0438\u0433\u0440\u044B, \u043D\u0435 \u0430\u043B\u044C\u0444\u0430 \u0438 \u043D\u0435 \u0431\u0435\u0442\u0430. \u041F\u0435\u0440\u0435\u0434 \u0432\u0430\u043C\u0438 \u0432\u043D\u0443\u0442\u0440\u0435\u043D\u043D\u0438\u0439 playtest-\u0431\u0438\u043B\u0434, \u0441\u043E\u0437\u0434\u0430\u043D\u043D\u044B\u0439 \u0434\u043B\u044F \u043F\u0440\u043E\u0432\u0435\u0440\u043A\u0438 \u0431\u0430\u0437\u043E\u0432\u044B\u0445 \u043C\u0435\u0445\u0430\u043D\u0438\u043A, \u0430\u0442\u043C\u043E\u0441\u0444\u0435\u0440\u044B, \u0443\u043F\u0440\u0430\u0432\u043B\u0435\u043D\u0438\u044F, \u0442\u0435\u043C\u043F\u0430 \u0438 \u043E\u0431\u0449\u0435\u0433\u043E \u043E\u0449\u0443\u0449\u0435\u043D\u0438\u044F \u043E\u0442 \u0438\u0433\u0440\u043E\u0432\u043E\u0433\u043E \u043F\u0440\u043E\u0446\u0435\u0441\u0441\u0430.\n\n" +
        "\u041C\u043D\u043E\u0433\u0438\u0435 \u044D\u043B\u0435\u043C\u0435\u043D\u0442\u044B \u043D\u0430\u0445\u043E\u0434\u044F\u0442\u0441\u044F \u0432 \u0440\u0430\u0437\u0440\u0430\u0431\u043E\u0442\u043A\u0435 \u0438 \u043C\u043E\u0433\u0443\u0442 \u0431\u044B\u0442\u044C \u043D\u0435\u043F\u043E\u043B\u043D\u044B\u043C\u0438, \u0432\u0440\u0435\u043C\u0435\u043D\u043D\u044B\u043C\u0438 \u0438\u043B\u0438 \u043D\u0435\u0441\u0442\u0430\u0431\u0438\u043B\u044C\u043D\u044B\u043C\u0438:\n\n" +
        "* \u0432\u0438\u0437\u0443\u0430\u043B\u044C\u043D\u044B\u0435 \u044D\u0444\u0444\u0435\u043A\u0442\u044B\n* \u0438\u043D\u0442\u0435\u0440\u0444\u0435\u0439\u0441\n* \u0430\u043D\u0438\u043C\u0430\u0446\u0438\u0438\n* \u0437\u0432\u0443\u043A\n* \u0431\u0430\u043B\u0430\u043D\u0441\n* \u043E\u0431\u0443\u0447\u0435\u043D\u0438\u0435\n* \u0432\u0437\u0430\u0438\u043C\u043E\u0434\u0435\u0439\u0441\u0442\u0432\u0438\u044F \u0441 \u043F\u0440\u0435\u0434\u043C\u0435\u0442\u0430\u043C\u0438\n* \u0441\u044E\u0436\u0435\u0442\u043D\u044B\u0435 \u0441\u043E\u0431\u044B\u0442\u0438\u044F\n* \u043A\u043E\u043D\u0446\u043E\u0432\u043A\u0438\n* \u043E\u043F\u0442\u0438\u043C\u0438\u0437\u0430\u0446\u0438\u044F\n* \u0442\u0435\u0445\u043D\u0438\u0447\u0435\u0441\u043A\u0430\u044F \u0441\u0442\u0430\u0431\u0438\u043B\u044C\u043D\u043E\u0441\u0442\u044C\n\n" +
        "\u0412 \u0438\u0433\u0440\u0435 \u043C\u043E\u0433\u0443\u0442 \u0432\u0441\u0442\u0440\u0435\u0447\u0430\u0442\u044C\u0441\u044F \u043E\u0448\u0438\u0431\u043A\u0438, \u043D\u0435\u0434\u043E\u0440\u0430\u0431\u043E\u0442\u0430\u043D\u043D\u044B\u0435 \u0441\u0446\u0435\u043D\u044B, \u0432\u0440\u0435\u043C\u0435\u043D\u043D\u044B\u0435 \u0430\u0441\u0441\u0435\u0442\u044B, \u0440\u0435\u0437\u043A\u0438\u0435 \u043F\u0435\u0440\u0435\u0445\u043E\u0434\u044B, \u043E\u0442\u0441\u0443\u0442\u0441\u0442\u0432\u0443\u044E\u0449\u0438\u0435 \u044D\u0444\u0444\u0435\u043A\u0442\u044B, \u043D\u0435\u0437\u0430\u0432\u0435\u0440\u0448\u0435\u043D\u043D\u044B\u0435 \u043C\u0435\u0445\u0430\u043D\u0438\u043A\u0438 \u0438 \u043F\u043E\u0432\u0435\u0434\u0435\u043D\u0438\u0435, \u043A\u043E\u0442\u043E\u0440\u043E\u0435 \u043D\u0435 \u043E\u0442\u0440\u0430\u0436\u0430\u0435\u0442 \u043A\u0430\u0447\u0435\u0441\u0442\u0432\u043E \u0431\u0443\u0434\u0443\u0449\u0435\u0439 \u0432\u0435\u0440\u0441\u0438\u0438.\n\n" +
        "\u0426\u0435\u043B\u044C \u044D\u0442\u043E\u0439 \u0441\u0431\u043E\u0440\u043A\u0438 - \u043D\u0435 \u043F\u043E\u043A\u0430\u0437\u0430\u0442\u044C \u0433\u043E\u0442\u043E\u0432\u0443\u044E \u0438\u0433\u0440\u0443, \u0430 \u0434\u0430\u0442\u044C \u0432\u043E\u0437\u043C\u043E\u0436\u043D\u043E\u0441\u0442\u044C \u043F\u043E\u0447\u0443\u0432\u0441\u0442\u0432\u043E\u0432\u0430\u0442\u044C \u043E\u0441\u043D\u043E\u0432\u0443 \u043F\u0440\u043E\u0435\u043A\u0442\u0430: \u0440\u0430\u0431\u043E\u0442\u0443 \u0441 \u043F\u0440\u0435\u0434\u043C\u0435\u0442\u0430\u043C\u0438, \u0434\u0430\u0432\u043B\u0435\u043D\u0438\u0435 \u0441\u0438\u0441\u0442\u0435\u043C\u044B, \u0430\u0442\u043C\u043E\u0441\u0444\u0435\u0440\u0443 \u043F\u043E\u043C\u0435\u0449\u0435\u043D\u0438\u044F, \u043F\u0435\u0440\u0432\u044B\u0435 \u044D\u043B\u0435\u043C\u0435\u043D\u0442\u044B \u0432\u0437\u0430\u0438\u043C\u043E\u0434\u0435\u0439\u0441\u0442\u0432\u0438\u044F \u0438 \u043D\u0430\u043F\u0440\u0430\u0432\u043B\u0435\u043D\u0438\u0435, \u0432 \u043A\u043E\u0442\u043E\u0440\u043E\u043C \u0440\u0430\u0437\u0432\u0438\u0432\u0430\u0435\u0442\u0441\u044F Sorter.\n\n" +
        "\u041F\u043E\u0436\u0430\u043B\u0443\u0439\u0441\u0442\u0430, \u0432\u043E\u0441\u043F\u0440\u0438\u043D\u0438\u043C\u0430\u0439\u0442\u0435 \u044D\u0442\u043E\u0442 \u0431\u0438\u043B\u0434 \u043A\u0430\u043A \u0440\u0430\u043D\u043D\u0438\u0439 \u043F\u0440\u043E\u0438\u0437\u0432\u043E\u0434\u0441\u0442\u0432\u0435\u043D\u043D\u044B\u0439 \u0442\u0435\u0441\u0442. \u0412\u0441\u0435, \u0447\u0442\u043E \u0432\u044B \u0432\u0438\u0434\u0438\u0442\u0435 \u0441\u0435\u0439\u0447\u0430\u0441, \u043C\u043E\u0436\u0435\u0442 \u0431\u044B\u0442\u044C \u0438\u0437\u043C\u0435\u043D\u0435\u043D\u043E, \u0443\u043B\u0443\u0447\u0448\u0435\u043D\u043E, \u043F\u0435\u0440\u0435\u0440\u0430\u0431\u043E\u0442\u0430\u043D\u043E \u0438\u043B\u0438 \u043F\u043E\u043B\u043D\u043E\u0441\u0442\u044C\u044E \u0437\u0430\u043C\u0435\u043D\u0435\u043D\u043E \u0432 \u0441\u043B\u0435\u0434\u0443\u044E\u0449\u0438\u0445 \u0432\u0435\u0440\u0441\u0438\u044F\u0445.\n\n" +
        "\u0421\u043F\u0430\u0441\u0438\u0431\u043E, \u0447\u0442\u043E \u0443\u0447\u0430\u0441\u0442\u0432\u0443\u0435\u0442\u0435 \u0432 \u0442\u0435\u0441\u0442\u0438\u0440\u043E\u0432\u0430\u043D\u0438\u0438.\n\n" +
        "\u0412\u0430\u0448 \u043E\u043F\u044B\u0442, \u0437\u0430\u043C\u0435\u0447\u0430\u043D\u0438\u044F \u0438 \u043E\u0449\u0443\u0449\u0435\u043D\u0438\u044F \u043F\u043E\u043C\u043E\u0433\u0443\u0442 \u0441\u0434\u0435\u043B\u0430\u0442\u044C \u0438\u0433\u0440\u0443 \u043B\u0443\u0447\u0448\u0435.";
    private const string FallbackContinueText = "\u041D\u0430\u0436\u043C\u0438\u0442\u0435 \u043B\u044E\u0431\u0443\u044E \u043A\u043B\u0430\u0432\u0438\u0448\u0443 \u0438\u043B\u0438 \u043A\u043D\u043E\u043F\u043A\u0443 \u043C\u044B\u0448\u0438, \u0447\u0442\u043E\u0431\u044B \u043F\u0440\u043E\u0434\u043E\u043B\u0436\u0438\u0442\u044C";

    [SerializeField] private MenuAudioManager menuAudioManager;
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue disclaimerShowSfx;
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private string titleText = "ДИСКЛЕЙМЕР";
    [SerializeField, TextArea(8, 22)] private string bodyText =
        "ВНИМАНИЕ\n\n" +
        "Вы запускаете раннюю тестовую сборку Sorter.\n\n" +
        "Это не финальная версия игры, не альфа и не бета. Перед вами внутренний playtest-билд, созданный для проверки базовых механик, атмосферы, управления, темпа и общего ощущения от игрового процесса.\n\n" +
        "Многие элементы находятся в разработке и могут быть неполными, временными или нестабильными:\n\n" +
        "* визуальные эффекты\n" +
        "* интерфейс\n" +
        "* анимации\n" +
        "* звук\n" +
        "* баланс\n" +
        "* обучение\n" +
        "* взаимодействия с предметами\n" +
        "* сюжетные события\n" +
        "* концовки\n" +
        "* оптимизация\n" +
        "* техническая стабильность\n\n" +
        "В игре могут встречаться ошибки, недоработанные сцены, временные ассеты, резкие переходы, отсутствующие эффекты, незавершенные механики и поведение, которое не отражает качество будущей версии.\n\n" +
        "Цель этой сборки — не показать готовую игру, а дать возможность почувствовать основу проекта: работу с предметами, давление системы, атмосферу помещения, первые элементы взаимодействия и направление, в котором развивается Sorter.\n\n" +
        "Пожалуйста, воспринимайте этот билд как ранний производственный тест. Все, что вы видите сейчас, может быть изменено, улучшено, переработано или полностью заменено в следующих версиях.\n\n" +
        "Спасибо, что участвуете в тестировании.\n\n" +
        "Ваш опыт, замечания и ощущения помогут сделать игру лучше.";
    [SerializeField] private string continueText = "Нажмите любую клавишу или кнопку мыши, чтобы продолжить";
    [SerializeField] private Color backgroundColor = new Color(0.015f, 0.012f, 0.012f, 0.98f);
    [SerializeField] private Color titleColor = new Color(0.86f, 0.04f, 0.035f, 1f);
    [SerializeField] private Color bodyColor = new Color(0.88f, 0.86f, 0.8f, 1f);
    [SerializeField] private Color continueColor = new Color(0.72f, 0.15f, 0.12f, 1f);

    private GameObject overlayRoot;
    private bool isShowing;

    private void Start()
    {
        ResolveReferences();

        if (!showOnStart)
        {
            StartMenu();
            return;
        }

        ShowDisclaimer();
    }

    private void Update()
    {
        if (!isShowing)
        {
            return;
        }

        if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            DismissDisclaimer();
        }
    }

    private void OnGUI()
    {
        if (!isShowing)
        {
            return;
        }

        GUI.depth = -10000;
        float width = Screen.width;
        float height = Screen.height;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = Mathf.Clamp(Mathf.RoundToInt(height * 0.075f), 38, 86),
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = titleColor }
        };

        GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = Mathf.Clamp(Mathf.RoundToInt(height * 0.022f), 14, 24),
            fontStyle = FontStyle.Normal,
            wordWrap = true,
            normal = { textColor = bodyColor }
        };

        GUIStyle continueStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.LowerCenter,
            fontSize = Mathf.Clamp(Mathf.RoundToInt(height * 0.025f), 16, 26),
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = continueColor }
        };

        GUI.Label(new Rect(width * 0.08f, height * 0.06f, width * 0.84f, height * 0.12f), ResolveText(titleText, FallbackTitleText), titleStyle);
        GUI.Label(new Rect(width * 0.13f, height * 0.17f, width * 0.74f, height * 0.68f), ResolveText(bodyText, FallbackBodyText), bodyStyle);
        GUI.Label(new Rect(width * 0.08f, height * 0.88f, width * 0.84f, height * 0.08f), ResolveText(continueText, FallbackContinueText), continueStyle);
    }

    private void ResolveReferences()
    {
        if (menuAudioManager == null)
        {
            menuAudioManager = FindFirstObjectByType<MenuAudioManager>();
        }
    }

    private void ShowDisclaimer()
    {
        isShowing = true;
        BuildOverlay();
        PlaySfx(disclaimerShowSfx);
    }

    private void DismissDisclaimer()
    {
        if (!isShowing)
        {
            return;
        }

        isShowing = false;
        if (overlayRoot != null)
        {
            Destroy(overlayRoot);
            overlayRoot = null;
        }

        StartMenu();
    }

    private void StartMenu()
    {
        ResolveReferences();
        menuAudioManager?.EnterMainMenuState();
    }

    private void BuildOverlay()
    {
        if (overlayRoot != null)
        {
            return;
        }

        overlayRoot = new GameObject("Menu Disclaimer Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = overlayRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;

        CanvasScaler scaler = overlayRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootRect = overlayRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        backgroundObject.transform.SetParent(overlayRoot.transform, false);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.color = backgroundColor;
        backgroundImage.raycastTarget = true;

        Button dismissButton = backgroundObject.GetComponent<Button>();
        dismissButton.transition = Selectable.Transition.None;
        dismissButton.onClick.AddListener(DismissDisclaimer);

        CreateText("Title", ResolveText(titleText, FallbackTitleText), overlayRoot.transform, titleColor, 70, FontStyles.Bold, TextAlignmentOptions.Top,
            new Vector2(0.08f, 0.8f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);

        CreateText("Body", ResolveText(bodyText, FallbackBodyText), overlayRoot.transform, bodyColor, 20, FontStyles.Normal, TextAlignmentOptions.TopLeft,
            new Vector2(0.13f, 0.15f), new Vector2(0.87f, 0.79f), Vector2.zero, Vector2.zero);

        CreateText("ContinueHint", ResolveText(continueText, FallbackContinueText), overlayRoot.transform, continueColor, 24, FontStyles.Bold, TextAlignmentOptions.Bottom,
            new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.12f), Vector2.zero, Vector2.zero);
    }

    private TextMeshProUGUI CreateText(
        string objectName,
        string text,
        Transform parent,
        Color color,
        int fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TextMeshProUGUI uiText = textObject.GetComponent<TextMeshProUGUI>();
        uiText.text = text;
        uiText.color = color;
        uiText.fontSize = fontSize;
        uiText.fontStyle = fontStyle;
        uiText.alignment = alignment;
        uiText.textWrappingMode = TextWrappingModes.Normal;
        uiText.overflowMode = TextOverflowModes.Overflow;
        uiText.enableAutoSizing = true;
        uiText.fontSizeMin = 14;
        uiText.fontSizeMax = fontSize;
        uiText.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
        {
            uiText.font = TMP_Settings.defaultFontAsset;
        }

        return uiText;
    }

    private static string ResolveText(string serializedText, string fallbackText)
    {
        if (string.IsNullOrWhiteSpace(serializedText) || LooksLikeBrokenEncoding(serializedText))
        {
            return fallbackText;
        }

        return serializedText;
    }

    private static bool LooksLikeBrokenEncoding(string value)
    {
        return value.Contains("Р") || value.Contains("СЃ") || value.Contains("вЂ");
    }

    private void PlaySfx(SfxCue cue)
    {
        if (cue == null)
        {
            return;
        }

        if (sfxEmitter == null)
        {
            sfxEmitter = GetComponent<SfxEmitter>();
            if (sfxEmitter == null)
            {
                sfxEmitter = gameObject.AddComponent<SfxEmitter>();
            }
        }

        sfxEmitter.Play(cue);
    }
}
