using UnityEngine;

public class TrashBinInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private InventoryUIController inventoryUIController;
    [SerializeField] private Rect inventoryPanelRect = new Rect(24f, 120f, 280f, 260f);
    [SerializeField] private Rect trashSlotRect = new Rect(328f, 190f, 160f, 80f);

    private int selectedSlotIndex = -1;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;
    private bool isOpen;

    public static bool IsTrashUiOpen { get; private set; }

    public void Interact(Transform holdPosition)
    {
        if (isOpen)
        {
            return;
        }

        SecuritySystem securitySystem = GameManager.Instance != null ? GameManager.Instance.securitySystem : null;
        securitySystem?.ReportViolation("Trash bin interaction");

        OpenTrashUi();
    }

    public void StopInteract()
    {
        CloseTrashUi();
    }

    public void DiscardSlot(int slotIndex)
    {
        InventorySystem.Instance?.DiscardItemAt(slotIndex);
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.E))
        {
            CloseTrashUi();
        }
    }

    private void OpenTrashUi()
    {
        isOpen = true;
        IsTrashUiOpen = true;
        selectedSlotIndex = -1;

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerView.Instance?.BlockMovement();
        inventoryUIController?.OpenInventory(false);
        Debug.Log("Trash bin UI opened.");
    }

    private void CloseTrashUi()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        IsTrashUiOpen = false;
        selectedSlotIndex = -1;

        inventoryUIController?.CloseInventory();
        PlayerView.Instance?.UnlockMovement();

        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;

        PlayerInteraction.Instance?.ClearCurrentInteractable(this);
        Debug.Log("Trash bin UI closed.");
    }

    private void OnGUI()
    {
        if (!isOpen)
        {
            return;
        }

        GUI.Box(inventoryPanelRect, "Inventory");
        DrawInventorySlots();

        GUI.Box(trashSlotRect, selectedSlotIndex >= 0 ? "Trash Slot\nClick to discard" : "Trash Slot\nSelect item first");
        if (GUI.Button(new Rect(trashSlotRect.x, trashSlotRect.yMax + 8f, trashSlotRect.width, 28f), "Discard Selected"))
        {
            DiscardSelectedSlot();
        }

        GUI.Label(new Rect(inventoryPanelRect.x, inventoryPanelRect.yMax + 8f, 420f, 24f), "Tab/E: close trash UI");
    }

    private void DrawInventorySlots()
    {
        InventorySystem inventory = InventorySystem.Instance;
        if (inventory == null)
        {
            GUI.Label(new Rect(inventoryPanelRect.x + 12f, inventoryPanelRect.y + 32f, 240f, 24f), "InventorySystem missing");
            return;
        }

        if (inventory.SlotCount == 0)
        {
            GUI.Label(new Rect(inventoryPanelRect.x + 12f, inventoryPanelRect.y + 32f, 240f, 24f), "Inventory is empty");
            return;
        }

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            InventoryItemDefinition item = inventory.GetItemInSlot(i);
            string itemName = item != null ? item.displayName : "Empty";
            string prefix = selectedSlotIndex == i ? "> " : string.Empty;
            Rect slotRect = new Rect(inventoryPanelRect.x + 12f, inventoryPanelRect.y + 32f + i * 34f, inventoryPanelRect.width - 24f, 28f);

            if (GUI.Button(slotRect, $"{prefix}Slot {i + 1}: {itemName}") && item != null)
            {
                selectedSlotIndex = i;
                Debug.Log($"Trash bin selected inventory slot {i + 1}: {itemName}.");
            }
        }
    }

    private void DiscardSelectedSlot()
    {
        if (selectedSlotIndex < 0)
        {
            Debug.LogWarning("Trash discard skipped: no inventory item selected.");
            return;
        }

        InventorySystem inventory = InventorySystem.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("Trash discard skipped: InventorySystem is missing.");
            return;
        }

        InventoryItemDefinition removedItem = inventory.RemoveItemAt(selectedSlotIndex);
        if (removedItem == null)
        {
            Debug.LogWarning($"Trash discard skipped: selected slot {selectedSlotIndex + 1} is empty.");
            selectedSlotIndex = -1;
            return;
        }

        Debug.Log($"Trash discarded item '{removedItem.displayName}' from slot {selectedSlotIndex + 1}.");
        selectedSlotIndex = -1;
    }
}
