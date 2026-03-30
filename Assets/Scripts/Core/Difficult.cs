using UnityEngine;

[CreateAssetMenu(fileName = "Difficult", menuName = "Scriptable Objects/Difficult")]
public class Difficult : ScriptableObject
{
    [Header("Name")]
    public string difficultyName = "NORMAL";

    [Header("Time And Limits")]
    public float timePerItem = 60f;
    public int maxMistakes = 10;

    [Header("Special Spawn Timings")]
    public int anomalyItemNum = 20;
    public int bombNum = 40;

    [Header("Defect Chances (0-1)")]
    [Range(0f, 1f)] public float noBarcodeChance = 0.1f;
    [Range(0f, 1f)] public float wrongBarcodeChance = 0.1f;
    [Range(0f, 1f)] public float defectChance = 0.1f;
    [Range(0f, 1f)] public float scratchesChance = 0.1f;

    [Header("Security")]
    public int protocolViolationPenaltyDefault = 5;
    public int protocolViolationPenaltyHard = 10;

    [TextArea(2, 4)]
    public string description;
}
