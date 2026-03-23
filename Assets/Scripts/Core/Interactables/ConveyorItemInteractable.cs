using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ConveyorItemInteractable : MonoBehaviour, IInteractable
{
    public ToolType toolTypeForDisassemble = ToolType.None;

    [SerializeField] private InventoryItemDefinition detailReward;
    [SerializeField] private InventoryItemDefinition trashReward;
    [SerializeField] private InventoryItemDefinition stealReward;
    [SerializeField] private bool canBeStolen = true;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;

    public void Interact(Transform holdPosition)
    {
        PlayerView.Instance.BlockMovement();

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;

        transform.SetParent(holdPosition);
        transform.localPosition = Vector3.zero;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        HUDManager.Instance.showItemScanHUD(GetComponent<Item>());
        PlayerItemInspection.Instance.BeginInspection(gameObject);
    }

    public void StopInteract()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        PlayerView.Instance.UnlockMovement();
        transform.SetParent(originalParent);
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;

        GameManager.Instance.ToggleScanerOff();
        HUDManager.Instance.hideItemScanHUD();
        PlayerItemInspection.Instance.EndInspection();
    }

    public void TryDisassemble(ToolType toolType)
    {
        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("InventorySystem is not present in scene.");
            return;
        }

        InventoryItemDefinition reward = null;

        if (toolType == ToolType.Steal)
        {
            if (!canBeStolen)
            {
                Debug.Log("This item cannot be stolen.");
                return;
            }

            reward = stealReward;
        }
        else if (toolTypeForDisassemble == toolType)
        {
            reward = detailReward;
        }
        else
        {
            reward = trashReward;
        }

        if (reward != null && !InventorySystem.Instance.TryAddItem(reward))
        {
            Debug.Log("Inventory is full.");
            return;
        }

        GameManager.Instance?.securitySystem?.ReportViolation($"Use tool {toolType}");
        Destroy(gameObject);
        GameManager.Instance.currentItem = null;
        GameManager.Instance?.SpawnNextItemAfterBypass();
    }
}
