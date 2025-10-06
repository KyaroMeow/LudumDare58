using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance;
    public float volumeValue = 1f;
    public float timePerItem = 20f;
    public int maxMistakes = 5;
    public float defectChance = 0.4f;
    public float stainChance = 0.6f;
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
                defectChance = 0.4f;
                stainChance = 0.6f;
                timePerItem = 90f;
                maxMistakes = 6;
                break;
            case "Normal":
                defectChance = 0.5f;
                stainChance = 0.5f;
                timePerItem = 60f;
                maxMistakes = 5;
                break;
            case "Hard":
                defectChance = 0.6f;
                stainChance = 0.5f;
                timePerItem = 30f;
                maxMistakes = 3;
                break;
        }
    }

}
