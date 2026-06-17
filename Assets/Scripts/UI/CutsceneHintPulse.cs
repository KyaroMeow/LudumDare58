using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CutsceneHintPulse : MonoBehaviour
{
    [SerializeField] private Color pulseColor = new Color(1f, 0.35f, 0.85f, 1f);
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float scaleAmount = 0.06f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool addShadowOrOutline = true;

    private Graphic targetGraphic;
    private Color originalColor;
    private Vector3 originalScale;
    private Shadow addedShadow;
    private bool capturedState;

    public void Configure(Color color, float speed, float scale)
    {
        pulseColor = color;
        pulseSpeed = Mathf.Max(0.01f, speed);
        scaleAmount = Mathf.Max(0f, scale);
    }

    private void OnEnable()
    {
        ResolveTargetGraphic();
        CaptureState();
        EnsureEffect();
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
    }

    private void ResolveTargetGraphic()
    {
        targetGraphic = GetComponent<Graphic>();

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

        capturedState = false;
    }
}
