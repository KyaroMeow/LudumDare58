using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingButtons : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI diffText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Slider volumeSlider;
    private string[] difficulties = { "EASY", "NORMAL", "HARD" };
    private string[] timerState = { "ON", "OFF" };
    private int currentDiffIndex = 1;
    private int currentTimerIndex = 0;

    public void NextDiff()
    {
        if (currentDiffIndex < difficulties.Length - 1)
        {
            currentDiffIndex++;
            UpdateDisplay();
        }
    }
    public void LastDiff()
    {
        if (currentDiffIndex > 0)
        {
            currentDiffIndex--;
            UpdateDisplay();
        }
    }
    public void Nexttimer()
    {
        if (currentTimerIndex < timerState.Length - 1)
        {
            currentTimerIndex++;
            UpdateDisplay();
        }
    }
    public void LastTimer()
    {
        if (currentTimerIndex > 0)
        {
            currentTimerIndex--;
            UpdateDisplay();
        }
    }
    public void SaveSettings()
    {
        SettingManager settings = SettingManager.EnsureInstance();
        if (settings == null)
        {
            return;
        }

        settings.SetDifficulty(diffText.text);

        if (volumeSlider != null)
        {
            settings.volumeValue = volumeSlider.value;
        }
        else
        {
            Debug.LogWarning("Cannot save volume because volumeSlider is not assigned on SettingButtons.");
        }

        if (currentTimerIndex == 0)
        {
            settings.timer = true;
        }
        else
        {
            settings.timer = false;
        }
    }
    private void UpdateDisplay()
    {
        diffText.text = difficulties[currentDiffIndex];
        timerText.text = timerState[currentTimerIndex];
    }
}
