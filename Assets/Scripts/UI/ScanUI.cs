using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScanUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Image progressFill;
    [SerializeField] private Color scanningColor = new Color(1f, 0.35f, 0.08f, 1f);
    [SerializeField] private Color goodColor = Color.green;
    [SerializeField] private Color badColor = Color.red;

    private Coroutine hideCoroutine;
    private bool showingScanning;
    private bool showingResult;

    public void ShowScanning(float normalizedProgress)
    {
        float progress = Mathf.Clamp01(normalizedProgress);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        showingScanning = true;
        showingResult = false;

        if (progressFill != null)
        {
            progressFill.gameObject.SetActive(true);
            progressFill.fillAmount = progress;
            progressFill.color = scanningColor;
        }

        if (resultText == null)
        {
            return;
        }

        resultText.color = scanningColor;
        resultText.text = $"SCANNING {Mathf.RoundToInt(progress * 100f)}%";
    }

    public void HideScanning()
    {
        HideScanningOnly();
    }

    public void HideScanningOnly()
    {
        showingScanning = false;

        if (progressFill != null)
        {
            progressFill.fillAmount = 0f;
            progressFill.gameObject.SetActive(false);
        }

        if (!showingResult && resultText != null)
        {
            resultText.text = "";
        }
    }

    public void ResetScanUI()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        showingScanning = false;
        showingResult = false;

        if (progressFill != null)
        {
            progressFill.fillAmount = 0f;
            progressFill.gameObject.SetActive(false);
        }

        if (resultText != null)
        {
            resultText.text = "";
        }
    }
    
    public void ShowResult(bool isNormal)
    {
        HideScanningOnly();
        showingResult = true;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (resultText == null)
        {
            return;
        }

        if (isNormal)
        {
            resultText.color = goodColor;
            resultText.text = "GOOD";
        }
        else
        {
            resultText.color = badColor;
            resultText.text = "BAD";
        }

        hideCoroutine = StartCoroutine(HideAfterDelay(2f));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (resultText != null)
        {
            resultText.text = "";
        }

        showingResult = false;
        hideCoroutine = null;
    }
}
