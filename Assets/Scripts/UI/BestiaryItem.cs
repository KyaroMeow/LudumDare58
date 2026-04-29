using UnityEngine;

public class BestiaryItem : MonoBehaviour
{
    [SerializeField] private string itemName;
    [SerializeField] private GameObject question;
    [SerializeField] private GameObject itemImage;

    private void OnEnable()
    {
        if (TabletInteractable.Instance.BestiaryItems[itemName])
        {
            OpenCard();
        }
    }

    private void OpenCard()
    {
        question.SetActive(false);
        itemImage.SetActive(true);
    }
}
