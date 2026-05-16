using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIController : MonoBehaviour
{
    [SerializeField] private GameObject inventoryRoot;
    [SerializeField] private Image[] slotIcons;
    [SerializeField] private Button[] slotButtons;
    [SerializeField] private TextMeshProUGUI[] slotLabels;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
            ToggleButtons();
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

    private void ToggleButtons()
    {
        foreach (Button button in slotButtons)
        {
            button.enabled = !button.enabled;
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

        inventoryRoot.SetActive(!inventoryRoot.activeSelf);
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
