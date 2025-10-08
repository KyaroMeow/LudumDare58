using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutSceneManager : MonoBehaviour
{
    public static CutSceneManager Instance;
    [SerializeField] private DialogSystem dialogSystem;
    [SerializeField] private string[] dialogue;
    [SerializeField] private GameObject startCanvas;
    [SerializeField] private GameObject winCanvas;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void StartInitialCutScene()
    {

    }
    public void StartLooseCutScene()
    {

    }
    public void StartChoiseCutScene()
    {
        PlayerView.Instance.canRotate = false;
        dialogSystem.StartDialogue(dialogue, OnDialogueComplete);
        
    }
    private void OnDialogueComplete()
    {
        dialogSystem.GiveChoise();
    }
    public void StartWinCutScene()
    {
        PlayerView.Instance.canLook = false;
        winCanvas.SetActive(true);
    }
    
}
