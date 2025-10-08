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

    private string[] currentDialogue;
    private int currentLine = 0;
    private bool isTyping = false;
    private System.Action onDialogueEnd;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                StopAllCoroutines();
                dialogueText.text = currentDialogue[currentLine];
                isTyping = false;
            }
            else
            {
                ShowNextLine();
            }
        }
    }

    public void StartDialogue(string[] dialogueLines, System.Action onEnd = null)
    {
        currentDialogue = dialogueLines;
        currentLine = 0;
        onDialogueEnd = onEnd;
        
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

        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }
        isTyping = false;
    }

    private void ShowNextLine()
    {
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
        dialogueText.text = "";
        currentDialogue = null;
        currentLine = 0;
        
        onDialogueEnd?.Invoke();
    }
}

