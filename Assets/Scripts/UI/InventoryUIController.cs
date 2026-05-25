using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIController : MonoBehaviour
{
    [SerializeField] private GameObject inventoryRoot;
    [SerializeField] private Image[] slotIcons;
    [SerializeField] private Button[] slotButtons;
    [SerializeField] private TextMeshProUGUI[] slotLabels;
    public bool IsInventoryOpen => inventoryRoot != null && inventoryRoot.activeSelf;

    private void Update()
    {
        if (TrashBinInteractable.IsTrashUiOpen)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    private void OnEnable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged += Refresh;
        }

        Refresh();
    }

    private void SetButtonsEnabled(bool isEnabled)
    {
        if (slotButtons == null)
        {
            return;
        }

        foreach (Button button in slotButtons)
        {
            if (button != null)
            {
                button.enabled = isEnabled;
            }
        }
    }
    private void OnDisable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged -= Refresh;
        }
    }

    public void ToggleInventory()
    {
        if (inventoryRoot == null)
        {
            return;
        }

        if (inventoryRoot.activeSelf)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    public void OpenInventory(bool enableSlotButtons = true)
    {
        if (inventoryRoot == null)
        {
            return;
        }

        inventoryRoot.SetActive(true);
        SetButtonsEnabled(enableSlotButtons);
        Refresh();
    }

    public void CloseInventory()
    {
        if (inventoryRoot == null)
        {
            return;
        }

        inventoryRoot.SetActive(false);
        SetButtonsEnabled(true);
        Refresh();
    }

    public void Refresh()
    {
        if (InventorySystem.Instance == null)
        {
            return;
        }

        for (int i = 0; i < slotIcons.Length; i++)
        {
            InventoryItemDefinition item = InventorySystem.Instance.GetItemInSlot(i);

            if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
            {
                slotLabels[i].text = item != null ? item.displayName : "Empty";
            }

            if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
            {
                slotIcons[i].enabled = item != null && item.icon != null;
                slotIcons[i].sprite = item != null ? item.icon : null;
            }
        }
    }
}
