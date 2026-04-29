using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class TabletInteractable : MonoBehaviour, IInteractable
{
    public static TabletInteractable Instance;

    public Transform holdTabletPosition;
    public GameObject homePage;
    public GameObject[] otherPages;
    public Dictionary<string, bool> BestiaryItems;

    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private string[] BestiaryItemNames;

    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Transform _originalParent;

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

        BestiaryItems = new Dictionary<string, bool>();

        foreach (string name in BestiaryItemNames)
        {
            BestiaryItems[name] = false;
        }
    }


    public void Interact(Transform holdPosition)
    {
        PlayerView.Instance.BlockMovement();
        SavePosition();

        transform.SetParent(holdTabletPosition);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private void SavePosition()
    {
        _originalPosition = transform.position;
        _originalRotation = transform.rotation;
        _originalParent = transform.parent;
    }

    public void OpenBestiaryItem(string itemName)
    {
        if (BestiaryItems.ContainsKey(itemName))
        {
            BestiaryItems[itemName] = true;
        }
    }

    public void StopInteract()
    {
        PlayerView.Instance.UnlockMovement();
        SetDefaultPosition();
    }

    private void SetDefaultPosition()
    {
        transform.SetParent(_originalParent);
        transform.localPosition = _originalPosition;
        transform.localRotation = _originalRotation;
    }

    public void SetHeader(string text)
    {
        headerText.text = text;
    }

    public void GoHome()
    {
        foreach (var page in otherPages)
        {
            page.SetActive(false);
        }
        homePage.SetActive(true);
        SetHeader("https://home");
    }
    public void StartGame()
    {
        if (GameManager.Instance.isGameStarted == false)
        {
            GameManager.Instance.StartGame();
        }
    }
}