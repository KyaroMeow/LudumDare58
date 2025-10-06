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
    private string[] difficulties = { "Easy", "Normal", "Hard" };
    private string[] timerState = { "On", "Off" };
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
        SettingManager.Instance.SetDifficult(diffText.text);
        SettingManager.Instance.volumeValue = volumeSlider.value;
        if (currentTimerIndex == 0)
        {

            SettingManager.Instance.timer = true;
        }
        else
        {
            SettingManager.Instance.timer = false;
        }
    }
    private void UpdateDisplay()
    {
        diffText.text = difficulties[currentDiffIndex];
        timerText.text = timerState[currentTimerIndex];
    }





}
