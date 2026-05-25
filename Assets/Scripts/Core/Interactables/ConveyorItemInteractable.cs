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

    private void Awake()
    {
        cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    public void Interact(Transform holdPosition)
    {
        TabletInteractable.Instance.OpenBestiaryItem(itemName);

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

        SetCollidersEnabled(false);

        HUDManager.Instance.showItemScanHUD(GetComponentInChildren<Item>(true));
        PlayerItemInspection.Instance.BeginInspection(gameObject);
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

        if (toolType == ToolType.Wrench && toolTypeForDisassemble == toolType)
        {
            PlaySfx(wrenchDisassembleSfx);
        }

        GameManager.Instance?.securitySystem?.ReportViolation($"Use tool {toolType}");
        Destroy(gameObject);
        GameManager.Instance.currentItem = null;
        GameManager.Instance?.SpawnNextItemAfterBypass();
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
