using System;
using System.Linq;
using CraftSystem;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [SerializeField] private int slotCount = 2;

    private InventoryItemDefinition[] slots;

    public static InventorySystem Instance { get; private set; }
    public event Action OnInventoryChanged;
    public int SlotCount => slots != null ? slots.Length : 0;

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
                CraftMemory.Instance?.RegisterCollectedItem(item);
                slots[i] = item;
                NotifyChanged();
                return true;
            }
        }

        return false;
    }

    public bool TryRemoveItem(InventoryItemDefinition item)
    {
        if (item == null || slots == null)
        {
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != item)
            {
                continue;
            }

            slots[i] = null;
            NotifyChanged();
            return true;
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

    public bool TryRemoveFirstItem(out InventoryItemDefinition removedItem)
    {
        removedItem = null;

        if (slots == null)
        {
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                removedItem = slots[i];
                slots[i] = null;
                NotifyChanged();
                return true;
            }
        }

        return false;
    }

    public bool HasAnyItem()
    {
        if (slots == null)
        {
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasFreeSlot()
    {
        if (slots == null)
        {
            return false;
        }

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
        return slots != null && slotIndex >= 0 && slotIndex < slots.Length;
    }

    private void NotifyChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}
