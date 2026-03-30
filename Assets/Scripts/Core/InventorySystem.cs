using System;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [SerializeField] private int slotCount = 2;

    private InventoryItemDefinition[] slots;

    public static InventorySystem Instance { get; private set; }
    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            slots = new InventoryItemDefinition[Mathf.Max(1, slotCount)];
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public InventoryItemDefinition GetItemInSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
        {
            return null;
        }

        return slots[slotIndex];
    }

    public bool TryAddItem(InventoryItemDefinition item)
    {
        if (item == null)
        {
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                NotifyChanged();
                return true;
            }
        }

        return false;
    }

    public InventoryItemDefinition RemoveItemAt(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
        {
            return null;
        }

        InventoryItemDefinition item = slots[slotIndex];
        slots[slotIndex] = null;
        NotifyChanged();
        return item;
    }

    public bool DiscardItemAt(int slotIndex)
    {
        if (!IsValidSlot(slotIndex) || slots[slotIndex] == null)
        {
            return false;
        }

        slots[slotIndex] = null;
        NotifyChanged();
        return true;
    }

    public bool HasFreeSlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < slots.Length;
    }

    private void NotifyChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}
