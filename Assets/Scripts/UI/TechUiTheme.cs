using UnityEngine;
using UnityEngine.UI;

public static class TechUiTheme
{
    public static readonly Color Panel = new Color(0.045f, 0.008f, 0.01f, 0.94f);
    public static readonly Color PanelSoft = new Color(0.075f, 0.012f, 0.014f, 0.9f);
    public static readonly Color Slot = new Color(0.11f, 0.025f, 0.022f, 0.92f);
    public static readonly Color SlotSelected = new Color(0.24f, 0.055f, 0.03f, 0.98f);
    public static readonly Color Accent = new Color(1f, 0.78f, 0.22f, 1f);
    public static readonly Color Danger = new Color(1f, 0.08f, 0.045f, 1f);
    public static readonly Color Safe = new Color(0.18f, 0.88f, 0.78f, 1f);
    public static readonly Color Muted = new Color(0.58f, 0.61f, 0.64f, 0.78f);
    public static readonly Color Text = new Color(0.94f, 0.96f, 0.98f, 1f);

    private static Font runtimeFont;

    public static Font RuntimeFont
    {
        get
        {
            if (runtimeFont == null)
            {
                runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return runtimeFont;
        }
    }

    public static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject target = new GameObject(objectName, typeof(RectTransform));
        target.transform.SetParent(parent, false);
        return target.GetComponent<RectTransform>();
    }

    public static Image CreateImage(string objectName, Transform parent, Color color, bool raycastTarget = false)
    {
        GameObject target = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        target.transform.SetParent(parent, false);
        Image image = target.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    public static Text CreateText(
        string objectName,
        Transform parent,
        string value,
        int fontSize,
        Color color,
        TextAnchor alignment = TextAnchor.MiddleLeft,
        FontStyle fontStyle = FontStyle.Normal)
    {
        GameObject target = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        target.transform.SetParent(parent, false);
        Text text = target.GetComponent<Text>();
        text.text = value;
        text.font = RuntimeFont;
        text.fontSize = Mathf.Max(8, fontSize);
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    public static Button CreateButton(string objectName, Transform parent, Color normalColor, Color highlightedColor, Color pressedColor)
    {
        GameObject target = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        target.transform.SetParent(parent, false);

        Image image = target.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = true;

        Button button = target.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        colors.selectedColor = highlightedColor;
        colors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        return button;
    }

    public static void SetRect(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    public static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    public static void AddPanelChrome(RectTransform parent, Color accentColor, Color cornerColor)
    {
        if (parent == null || parent.Find("TechChrome") != null)
        {
            return;
        }

        RectTransform chrome = CreateRect("TechChrome", parent);
        Stretch(chrome, Vector2.zero, Vector2.zero);
        chrome.SetAsLastSibling();

        Image topLine = CreateImage("AccentLine", chrome, accentColor);
        RectTransform topRect = topLine.rectTransform;
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0f, -10f);
        topRect.sizeDelta = new Vector2(-30f, 2f);

        CreateCorner("CornerTopLeftH", chrome, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(70f, 3f), cornerColor);
        CreateCorner("CornerTopLeftV", chrome, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(3f, 32f), cornerColor);
        CreateCorner("CornerBottomRightH", chrome, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(70f, 3f), cornerColor);
        CreateCorner("CornerBottomRightV", chrome, new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(3f, 32f), cornerColor);
    }

    public static void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        if (target == null)
        {
            return;
        }

        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
        {
            outline = target.AddComponent<Outline>();
        }

        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static void CreateCorner(
        string objectName,
        Transform parent,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        Image image = CreateImage(objectName, parent, color);
        SetRect(image.rectTransform, anchor, pivot, position, size);
    }
}

[DisallowMultipleComponent]
public class TechUiReveal : MonoBehaviour
{
    [SerializeField] private float duration = 0.18f;
    [SerializeField] private float startScale = 0.97f;

    private CanvasGroup canvasGroup;
    private float elapsed;
    private bool revealing;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        elapsed = 0f;
        revealing = true;
        transform.localScale = Vector3.one * startScale;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void Update()
    {
        if (!revealing)
        {
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
        float smooth = t * t * (3f - 2f * t);
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, smooth);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = smooth;
        }

        if (t >= 1f)
        {
            revealing = false;
        }
    }
}
