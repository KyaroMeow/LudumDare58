using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogSystem : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialogueCanvas;
    public GameObject coisePanel;
    public TextMeshProUGUI dialogueText;
    
    [Header("Settings")]
    public float textSpeed = 0.05f;

    [Header("Audio")]
    public AudioSource dialogueAudioSource;
    public AudioClip[] dialogueAudioClips; // Аудиофайлы для каждой строки

    private string[] currentDialogue;
    private int currentLine = 0;
    private bool isTyping = false;
    private bool isDialogueActive = false;
    private System.Action onDialogueEnd;

    void Update()
    {
        if (!isDialogueActive) return;
        
        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                StopAllCoroutines();
                dialogueText.text = currentDialogue[currentLine];
                isTyping = false;
                
                // Останавливаем аудио при пропуске
                if (dialogueAudioSource != null)
                {
                    dialogueAudioSource.Stop();
                }
            }
            else
            {
                ShowNextLine();
            }
        }
    }

    public void StartDialogue(string[] dialogueLines, System.Action onEnd = null)
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning("Dialogue lines are empty!");
            return;
        }

        dialogueCanvas.SetActive(true);
        currentDialogue = dialogueLines;
        currentLine = 0;
        onDialogueEnd = onEnd;
        isDialogueActive = true;
        
        StartCoroutine(TypeText(currentDialogue[currentLine]));
    }

    public void GiveChoise()
    {
        coisePanel.SetActive(true);
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        // Проигрываем аудио для текущей строки
        PlayDialogueAudio(currentLine);

        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }
        isTyping = false;
    }

    private void PlayDialogueAudio(int lineIndex)
    {
        if (dialogueAudioSource != null && dialogueAudioClips != null)
        {
            // Проверяем что для этой строки есть аудио
            if (lineIndex >= 0 && lineIndex < dialogueAudioClips.Length)
            {
                if (dialogueAudioClips[lineIndex] != null)
                {
                    dialogueAudioSource.Stop();
                    dialogueAudioSource.clip = dialogueAudioClips[lineIndex];
                    dialogueAudioSource.Play();
                }
            }
            else
            {
                Debug.LogWarning($"No audio clip found for line {lineIndex}");
            }
        }
    }

    private void ShowNextLine()
    {
        if (currentDialogue == null)
        {
            Debug.LogError("Current dialogue is null!");
            EndDialogue();
            return;
        }

        currentLine++;
        
        if (currentLine < currentDialogue.Length)
        {
            StartCoroutine(TypeText(currentDialogue[currentLine]));
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        // Останавливаем аудио
        if (dialogueAudioSource != null)
        {
            dialogueAudioSource.Stop();
        }

        dialogueText.text = "";
        currentDialogue = null;
        currentLine = 0;
        isDialogueActive = false;
        GiveChoise();
        
        onDialogueEnd?.Invoke();
        onDialogueEnd = null;
    }

    public void ForceEndDialogue()
    {
        if (isDialogueActive)
        {
            StopAllCoroutines();
            if (dialogueAudioSource != null)
            {
                dialogueAudioSource.Stop();
            }
            EndDialogue();
        }
    }
}