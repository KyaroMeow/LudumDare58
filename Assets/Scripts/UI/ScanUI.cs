using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScanUI : MonoBehaviour
{
    [SerializeField]private TextMeshProUGUI resultText;
    private Coroutine hideCoroutine;
    
    public void ShowResult(bool isNormal)
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        if (isNormal)
        {
            resultText.color = Color.green;
            resultText.text = "GOOD";
        }
        else
        {
            resultText.color = Color.red;
            resultText.text = "BAD";
        }

        hideCoroutine = StartCoroutine(HideAfterDelay(2f));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        resultText.text = ""; 
        hideCoroutine = null;
    }
}