using UnityEngine;

public class BestiaryItem : MonoBehaviour
{
    [SerializeField] private GameObject question;
    [SerializeField] private GameObject itemImage;

    public void OpenCard()
    {
        question.SetActive(false);
        itemImage.SetActive(true);
    }
}
