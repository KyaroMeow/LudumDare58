using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ConveyorItemInteractable : MonoBehaviour, IInteractable
{
    public ToolType toolTypeForDisassemble = ToolType.None;

    [SerializeField] private InventoryItemDefinition detailReward;
    [SerializeField] private InventoryItemDefinition trashReward;
    [SerializeField] private InventoryItemDefinition stealReward;
    [SerializeField] private bool canBeStolen = true;
    [SerializeField] private string itemName;
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue pickupSfx;
    [SerializeField] private SfxCue wrenchDisassembleSfx;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    private Collider[] cachedColliders;
    private bool isBeingInspected;
    private bool warnedStealLocked;

    private void Awake()
    {
        cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    public void Interact(Transform holdPosition)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsStoryInteractionLocked)
        {
            Debug.Log("Item inspection blocked because vent hand intro is running.");
            PlayerInteraction.Instance?.ClearCurrentInteractable(this);
            return;
        }

        TabletInteractable.Instance?.OpenBestiaryItem(itemName);

        PlayerView.Instance?.BlockMovement();

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

        SetCollidersEnabled(false);

        Item item = GetComponentInChildren<Item>(true);
        PlayerHeldItem.Instance?.TrySelectItem(item);
        HUDManager.Instance?.showItemScanHUD(item);
        PlayerItemInspection.Instance?.BeginInspection(gameObject);
        isBeingInspected = true;
        PlaySfx(pickupSfx);
    }

    public void StopInteract()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        SetCollidersEnabled(true);

        PlayerView.Instance?.UnlockMovement();
        transform.SetParent(originalParent);
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;

        GameManager.Instance?.ToggleScanerOff();
        HUDManager.Instance?.hideItemScanHUD();
        PlayerHeldItem.Instance?.ClearItem();
        PlayerItemInspection.Instance?.EndInspection();
        isBeingInspected = false;
    }

    public bool TryDisassemble(ToolType toolType)
    {
        return TryUseTool(toolType);
    }

    public bool TryStealFromInspection()
    {
        if (!IsStealUnlocked())
        {
            if (!warnedStealLocked)
            {
                warnedStealLocked = true;
                Debug.Log("Steal is locked until vent hand intro is completed.");
            }

            return false;
        }

        return TryUseTool(ToolType.Steal);
    }

    private bool TryUseTool(ToolType toolType)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsStoryInteractionLocked)
        {
            Debug.Log($"Tool action {toolType} blocked because vent hand intro is running.");
            return false;
        }

        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("InventorySystem is not present in scene.");
            return false;
        }

        InventoryItemDefinition reward = null;
        string actionName = $"Use tool {toolType}";

        if (toolType == ToolType.Steal)
        {
            if (!canBeStolen)
            {
                Debug.LogWarning($"Cannot steal '{gameObject.name}' because this item is marked as not stealable.");
                return false;
            }

            reward = stealReward;
            actionName = "Steal item";
        }
        else if (toolTypeForDisassemble == toolType)
        {
            reward = detailReward;
        }
        else
        {
            reward = trashReward;
        }

        if (reward == null)
        {
            Debug.LogWarning($"Cannot use tool {toolType} on '{gameObject.name}' because no reward is configured. Item remains in place.");
            return false;
        }

        if (!InventorySystem.Instance.TryAddItem(reward))
        {
            Debug.LogWarning($"Inventory is full. Cannot add reward '{reward.displayName}' from item '{gameObject.name}' using tool {toolType}.");
            return false;
        }

        if (toolType == ToolType.Wrench && toolTypeForDisassemble == toolType)
        {
            PlaySfx(wrenchDisassembleSfx);
        }

        GameManager.Instance?.securitySystem?.ReportViolation(actionName);
        CompleteToolAction();
        return true;
    }

    private void CompleteToolAction()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteCurrentItemAfterToolAction(gameObject);
            return;
        }

        Debug.LogWarning($"Cannot complete tool action for '{gameObject.name}' because GameManager.Instance is missing.");
    }

    private void OnGUI()
    {
        if (!isBeingInspected)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsStoryInteractionLocked)
        {
            return;
        }

        if (!IsStealUnlocked())
        {
            return;
        }

        if (GUI.Button(new Rect(16f, 104f, 120f, 32f), "Steal"))
        {
            TryStealFromInspection();
        }
    }

    private bool IsStealUnlocked()
    {
        VentHandIntroController introController = VentHandIntroController.Instance;
        return introController != null && introController.IsStealUnlocked;
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        if (cachedColliders == null)
        {
            return;
        }

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            if (cachedColliders[i] != null)
            {
                if (cachedColliders[i].CompareTag("Code"))
                {
                    continue;
                }

                cachedColliders[i].enabled = isEnabled;
            }
        }
    }

    private void PlaySfx(SfxCue cue)
    {
        if (cue == null)
        {
            return;
        }

        if (sfxEmitter == null)
        {
            sfxEmitter = GetComponent<SfxEmitter>();
            if (sfxEmitter == null)
            {
                sfxEmitter = gameObject.AddComponent<SfxEmitter>();
            }
        }

        sfxEmitter.Play(cue);
    }
}
