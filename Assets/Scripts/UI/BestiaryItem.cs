using UnityEngine;
using System.Collections.Generic;

public class BestiaryItem : MonoBehaviour
{
    private static readonly HashSet<string> warnedMissingItems = new HashSet<string>();

    [SerializeField] private string itemName;
    [SerializeField] private GameObject question;
    [SerializeField] private GameObject itemImage;

    private void OnEnable()
    {
        CloseCard();

        if (string.IsNullOrWhiteSpace(itemName))
        {
            Debug.LogWarning($"BestiaryItem on '{name}' has an empty itemName.", this);
            return;
        }

        if (TabletInteractable.Instance == null)
        {
            Debug.LogWarning($"BestiaryItem '{itemName}' on '{name}' cannot read unlock state because TabletInteractable.Instance is missing.", this);
            return;
        }

        if (TabletInteractable.Instance.BestiaryItems == null)
        {
            Debug.LogWarning($"BestiaryItem '{itemName}' on '{name}' cannot read unlock state because BestiaryItems dictionary is missing.", this);
            return;
        }

        if (!TabletInteractable.Instance.BestiaryItems.TryGetValue(itemName, out bool isOpened))
        {
            WarnMissingItemName();
            return;
        }

        if (isOpened)
        {
            OpenCard();
        }
    }

    private void OpenCard()
    {
        if (question != null)
        {
            question.SetActive(false);
        }

        if (itemImage != null)
        {
            itemImage.SetActive(true);
        }
    }

    private void CloseCard()
    {
        if (question != null)
        {
            question.SetActive(true);
        }

        if (itemImage != null)
        {
            itemImage.SetActive(false);
        }
    }

    private void WarnMissingItemName()
    {
        string warningKey = $"{gameObject.GetInstanceID()}:{itemName}";
        if (!warnedMissingItems.Add(warningKey))
        {
            return;
        }

        Debug.LogWarning($"Bestiary item key '{itemName}' was not found for BestiaryItem on '{name}'. The card will stay closed.", this);
    }
}
