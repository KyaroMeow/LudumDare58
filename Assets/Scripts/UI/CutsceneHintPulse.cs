using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CutsceneHintPulse : MonoBehaviour
{
    public enum PulseStyle
    {
        Glow,
        Sparkles
    }

    [SerializeField] private Color pulseColor = new Color(1f, 0.35f, 0.85f, 1f);
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float scaleAmount = 0.06f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool addShadowOrOutline = true;
    [SerializeField] private PulseStyle pulseStyle = PulseStyle.Sparkles;
    [SerializeField] private int sparkleCount = 5;
    [SerializeField] private float sparkleRadius = 0.24f;
    [SerializeField] private float sparkleFontSize = 12f;

    private Graphic targetGraphic;
    private Color originalColor;
    private Vector3 originalScale;
    private Shadow addedShadow;
    private Text[] sparkles;
    private RectTransform rectTransform;
    private bool capturedState;

    public void Configure(Color color, float speed, float scale)
    {
        Configure(color, speed, scale, pulseStyle);
    }

    public void Configure(Color color, float speed, float scale, PulseStyle style)
    {
        pulseColor = color;
        pulseSpeed = Mathf.Max(0.01f, speed);
        scaleAmount = Mathf.Max(0f, scale);
        pulseStyle = style;

        if (isActiveAndEnabled)
        {
            EnsureSparkles();
        }
    }

    private void OnEnable()
    {
        ResolveTargetGraphic();
        CaptureState();
        EnsureEffect();
        EnsureSparkles();
    }

    private void OnDisable()
    {
        RestoreState();
    }

    private void Update()
    {
        if (!capturedState)
        {
            ResolveTargetGraphic();
            CaptureState();
        }

        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float pulse = (Mathf.Sin(time * Mathf.Max(0.01f, pulseSpeed)) + 1f) * 0.5f;

        if (targetGraphic != null)
        {
            targetGraphic.color = Color.Lerp(originalColor, pulseColor, pulse);
        }

        transform.localScale = originalScale * (1f + scaleAmount * pulse);

        if (addedShadow != null)
        {
            addedShadow.effectColor = Color.Lerp(new Color(pulseColor.r, pulseColor.g, pulseColor.b, 0.25f), pulseColor, pulse);
        }

        UpdateSparkles(time, pulse);
    }

    private void ResolveTargetGraphic()
    {
        targetGraphic = GetComponent<Graphic>();
        rectTransform = transform as RectTransform;

        if (targetGraphic == null)
        {
            Button button = GetComponent<Button>();
            if (button != null)
            {
                targetGraphic = button.targetGraphic;
            }
        }
    }

    private void CaptureState()
    {
        if (capturedState)
        {
            return;
        }

        originalScale = transform.localScale;

        if (targetGraphic != null)
        {
            originalColor = targetGraphic.color;
        }

        capturedState = true;
    }

    private void EnsureEffect()
    {
        if (!addShadowOrOutline)
        {
            return;
        }

        if (addedShadow != null)
        {
            addedShadow.enabled = true;
            return;
        }

        if (GetComponent<Shadow>() != null)
        {
            return;
        }

        addedShadow = gameObject.AddComponent<Shadow>();
        addedShadow.effectDistance = new Vector2(0f, -2f);
        addedShadow.useGraphicAlpha = true;
    }

    private void EnsureSparkles()
    {
        if (pulseStyle != PulseStyle.Sparkles || rectTransform == null)
        {
            SetSparklesActive(false);
            return;
        }

        int count = Mathf.Clamp(sparkleCount, 3, 12);
        if (sparkles != null && sparkles.Length == count)
        {
            SetSparklesActive(true);
            return;
        }

        ClearSparkles();
        sparkles = new Text[count];

        for (int i = 0; i < count; i++)
        {
            GameObject sparkleObject = new GameObject($"PulseSparkle_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            sparkleObject.transform.SetParent(transform, false);

            RectTransform sparkleTransform = sparkleObject.GetComponent<RectTransform>();
            sparkleTransform.anchorMin = new Vector2(0.5f, 0.5f);
            sparkleTransform.anchorMax = new Vector2(0.5f, 0.5f);
            sparkleTransform.pivot = new Vector2(0.5f, 0.5f);
            sparkleTransform.sizeDelta = new Vector2(22f, 22f);

            Text sparkle = sparkleObject.GetComponent<Text>();
            sparkle.text = "*";
            sparkle.alignment = TextAnchor.MiddleCenter;
            sparkle.fontSize = Mathf.Max(8, Mathf.RoundToInt(sparkleFontSize));
            sparkle.fontStyle = FontStyle.Bold;
            sparkle.raycastTarget = false;
            sparkle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sparkles[i] = sparkle;
        }
    }

    private void UpdateSparkles(float time, float pulse)
    {
        if (pulseStyle != PulseStyle.Sparkles || sparkles == null || rectTransform == null)
        {
            SetSparklesActive(false);
            return;
        }

        Vector2 size = rectTransform.rect.size;
        float baseRadius = Mathf.Max(8f, Mathf.Max(size.x, size.y) * Mathf.Clamp(sparkleRadius, 0.08f, 0.42f));

        for (int i = 0; i < sparkles.Length; i++)
        {
            Text sparkle = sparkles[i];
            if (sparkle == null)
            {
                continue;
            }

            float phase = ((float)i / sparkles.Length) * Mathf.PI * 2f;
            float wobble = Mathf.Sin(time * pulseSpeed * 1.37f + phase) * 0.08f;
            Vector2 direction = new Vector2(Mathf.Cos(phase + wobble), Mathf.Sin(phase + wobble));
            float radius = baseRadius * (0.92f + 0.08f * Mathf.Sin(time * pulseSpeed + phase));

            RectTransform sparkleTransform = sparkle.transform as RectTransform;
            if (sparkleTransform != null)
            {
                sparkleTransform.anchoredPosition = direction * radius;
                sparkleTransform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.12f, Mathf.PingPong(time * pulseSpeed + i * 0.33f, 1f));
            }

            float alpha = Mathf.Lerp(0.35f, 0.95f, Mathf.PingPong(time * pulseSpeed * 0.85f + i * 0.21f, 1f));
            sparkle.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, alpha * Mathf.Lerp(0.65f, 1f, pulse));
            sparkle.gameObject.SetActive(true);
        }
    }

    private void SetSparklesActive(bool isActive)
    {
        if (sparkles == null)
        {
            return;
        }

        for (int i = 0; i < sparkles.Length; i++)
        {
            if (sparkles[i] != null)
            {
                sparkles[i].gameObject.SetActive(isActive);
            }
        }
    }

    private void ClearSparkles()
    {
        if (sparkles == null)
        {
            return;
        }

        for (int i = 0; i < sparkles.Length; i++)
        {
            if (sparkles[i] != null)
            {
                Destroy(sparkles[i].gameObject);
            }
        }

        sparkles = null;
    }

    private void RestoreState()
    {
        if (!capturedState)
        {
            return;
        }

        if (targetGraphic != null)
        {
            targetGraphic.color = originalColor;
        }

        transform.localScale = originalScale;

        if (addedShadow != null)
        {
            addedShadow.enabled = false;
        }

        SetSparklesActive(false);

        capturedState = false;
    }
}
