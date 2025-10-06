using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tablet : MonoBehaviour
{
    [SerializeField] private GameObject[] pages;
    private int currentPageId = 0;
    public void NextPages()
    {
        
        if (currentPageId < pages.Length - 1)
        {
            pages[currentPageId].SetActive(false);
            currentPageId++;
            pages[currentPageId].SetActive(true);
        }
        else
        {
            pages[currentPageId].SetActive(false);
            currentPageId = 0;
            pages[currentPageId].SetActive(true);
            if (GameManager.Instance.isGameStarted == false)
            {
                GameManager.Instance.StartGame();
            }
        }
    } 
    
    
}
