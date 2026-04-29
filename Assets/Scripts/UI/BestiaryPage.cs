using UnityEngine;
using UnityEngine.Rendering;

public class BestiaryPage : MonoBehaviour
{
    [SerializeField] private GameObject[] pages;

    private int currentPage = 0;

    public void NextPage()
    {
        if (pages.Length - 1 == currentPage)
        {
            pages[currentPage].SetActive(false);
            currentPage = 0;
            pages[currentPage].SetActive(true);
        }
        else
        {
            pages[currentPage].SetActive(false);
            currentPage++;
            pages[currentPage].SetActive(true);
        }
    }

    public void PrevPage() 
    {
        if (currentPage == 0)
        {
            pages[currentPage].SetActive(false);
            currentPage = pages.Length-1;
            pages[currentPage].SetActive(true);
        }
        else
        {
            pages[currentPage].SetActive(false);
            currentPage--;
            pages[currentPage].SetActive(true);
        }
    }
}
