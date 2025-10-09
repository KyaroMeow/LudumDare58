using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance;
    public float volumeValue;
    public float timePerItem;
    public int maxMistakes;
    public int anomalyItemNum;
    public int BombNum;
    public float noBarcodeChance;
    public float wrongBarcodeChance;
    public float defectChance;
    public bool timer;
    public string currentDifficulty;
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
    void Start()
    {
        SetDifficult("Normal");
    }
    void Update()
    {
        AudioListener.volume = volumeValue;
    }
    public void SetDifficult(string diffName)
    {
        switch (diffName)
        {
            case "Easy":
                currentDifficulty = "Easy";
                wrongBarcodeChance = 0.2f;
                defectChance = 0.4f;
                noBarcodeChance = 0.4f;
                anomalyItemNum = 10;
                BombNum = 20;
                timePerItem = 90f;
                maxMistakes = 15;
                break;
            case "Normal":
                currentDifficulty = "Normal";
                wrongBarcodeChance = 0.4f;
                defectChance = 0.5f;
                noBarcodeChance = 0.3f;
                anomalyItemNum = 20;
                BombNum = 40;
                timePerItem = 60f;
                maxMistakes = 10;
                break;
            case "Hard":
                currentDifficulty = "Hard";
                wrongBarcodeChance = 0.7f;
                defectChance = 0.6f;
                noBarcodeChance = 0.3f;
                anomalyItemNum = 30;
                BombNum = 50;
                timePerItem = 30f;
                maxMistakes = 5;
                break;
        }
    }

}
