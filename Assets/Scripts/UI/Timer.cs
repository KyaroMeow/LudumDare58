using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Timer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    private void Update()
    {
        UpdateTime();
    }
    private void UpdateTime() 
{
    if (GameManager.Instance != null && timerText != null)
    {
        int seconds = Mathf.CeilToInt(GameManager.Instance.currentTime);
        timerText.text = seconds.ToString();
    }
}
}
