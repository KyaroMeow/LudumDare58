using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance;
    public float volumeValue = 1f;
    public float timePerItem = 60f;
    public int maxMistakes = 5;
    public float noBarcodeChance = 0.2f;
    public float wrongBarcodeChance = 0.4f;
    public float defectChance = 0.5f;
    public bool timer = true;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void SetDifficult(string diffName)
    {
        switch (diffName)
        {
            case "Easy":
                wrongBarcodeChance = 0.2f;
                defectChance = 0.4f;
                noBarcodeChance = 0.4f;
                timePerItem = 90f;
                maxMistakes = 6;
                break;
            case "Normal":
                wrongBarcodeChance = 0.4f;
                defectChance = 0.5f;
                noBarcodeChance = 0.3f;
                timePerItem = 60f;
                maxMistakes = 5;
                break;
            case "Hard":
                wrongBarcodeChance = 0.9f;
                defectChance = 0.6f;
                noBarcodeChance = 0.3f;
                timePerItem = 30f;
                maxMistakes = 3;
                break;
        }
    }

}
