using UnityEngine;

public class DroppedInventoryItemInteractable : MonoBehaviour, IInteractable, IOneShotInteractable
{
    [SerializeField] private InventoryItemDefinition inventoryItem;
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue pickupSfx;
    [SerializeField] private float fallbackColliderRadius = 0.2f;

    public void Configure(InventoryItemDefinition item, SfxCue pickupCue)
    {
        inventoryItem = item;
        pickupSfx = pickupCue;
        EnsurePickupCollider();
    }

    private void Awake()
    {
        EnsurePickupCollider();
    }

    public void Interact(Transform holdPosition)
    {
        if (inventoryItem == null)
        {
            Debug.LogWarning($"Cannot pick up dropped item '{gameObject.name}' because inventoryItem is not assigned.");
            return;
        }

        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning($"Cannot pick up dropped item '{inventoryItem.displayName}' because InventorySystem is missing.");
            return;
        }

        if (!InventorySystem.Instance.TryAddItem(inventoryItem))
        {
            Debug.LogWarning($"Inventory is full. Dropped item '{inventoryItem.displayName}' remains in place.");
            return;
        }

        PlaySfx();
        PlayerInteraction.Instance?.ClearCurrentInteractable(this);
        Destroy(gameObject);
    }

    public void StopInteract()
    {
    }

    private void EnsurePickupCollider()
    {
        if (GetComponentInChildren<Collider>(true) != null)
        {
            return;
        }

        SphereCollider fallbackCollider = gameObject.AddComponent<SphereCollider>();
        fallbackCollider.radius = Mathf.Max(0.01f, fallbackColliderRadius);
        fallbackCollider.isTrigger = false;
    }

    private void PlaySfx()
    {
        if (pickupSfx == null)
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

        sfxEmitter.Play(pickupSfx);
    }
}
