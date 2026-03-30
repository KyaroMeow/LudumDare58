using UnityEngine;

public class TrashBinInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private InventoryUIController inventoryUIController;

    public void Interact(Transform holdPosition)
    {
        SecuritySystem securitySystem = GameManager.Instance != null ? GameManager.Instance.securitySystem : null;
        securitySystem?.ReportViolation("Trash bin interaction");
        inventoryUIController?.ToggleInventory();
    }

    public void StopInteract()
    {
    }

    public void DiscardSlot(int slotIndex)
    {
        InventorySystem.Instance?.DiscardItemAt(slotIndex);
    }
}
