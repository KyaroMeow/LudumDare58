using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutSceneManager : MonoBehaviour
{
    public static CutSceneManager Instance;
    [SerializeField] private DialogSystem dialogSystem;
    [SerializeField] private string[] dialogue;
    [SerializeField] private GameObject startCanvas;
    [SerializeField] private GameObject winCanvas;
    [SerializeField] private GameObject looseCanvas;
    
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

    void Start()
    {
        // Проверяем что все ссылки установлены
        if (dialogSystem == null)
        {
            Debug.LogError("DialogSystem reference is not set in CutSceneManager!");
        }
        
        if (dialogue == null || dialogue.Length == 0)
        {
            Debug.LogError("Dialogue array is empty in CutSceneManager!");
        }
    }

    public void StartInitialCutScene()
    {
        startCanvas.SetActive(true);
    }

    public void StartLooseCutScene()
    {
        looseCanvas.SetActive(true);
        StartCoroutine(LoadMenu());
    }
    private IEnumerator LoadMenu()
    {
        yield return new WaitForSeconds(3.5f);
        SceneManager.LoadScene(0);
    }
    

    public void StartChoiseCutScene()
    {
        if (PlayerView.Instance != null)
        {
            PlayerView.Instance.canRotate = false;
        }

        if (dialogSystem != null && dialogue != null && dialogue.Length > 0)
        {
            dialogSystem.StartDialogue(dialogue, OnDialogueComplete);
        }
        else
        {
            Debug.LogError("Cannot start choice cutscene - missing references!");
        }
    }

    private void OnDialogueComplete()
    {
        if (dialogSystem != null)
        {
            dialogSystem.GiveChoise();
        }
    }

    public void StartWinCutScene()
    {
        if (PlayerView.Instance != null)
        {
            PlayerView.Instance.canLook = false;
        }
        
        if (winCanvas != null)
        {
            winCanvas.SetActive(true);
        }
    }
}