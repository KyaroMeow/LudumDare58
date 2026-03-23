using UnityEngine;

public class HandTradeSystem : MonoBehaviour
{
    [SerializeField] private HandCraftingRecipe[] recipes;

    private InventoryItemDefinition firstOffer;
    private InventoryItemDefinition secondOffer;

    public bool OfferFromInventorySlot(int inventorySlotIndex)
    {
        if (InventorySystem.Instance == null)
        {
            return false;
        }

        InventoryItemDefinition item = InventorySystem.Instance.RemoveItemAt(inventorySlotIndex);
        if (item == null)
        {
            return false;
        }

        if (firstOffer == null)
        {
            firstOffer = item;
            return true;
        }

        if (secondOffer == null)
        {
            secondOffer = item;
            return true;
        }

        InventorySystem.Instance.TryAddItem(item);
        return false;
    }

    public void ReturnOffer(int offerIndex)
    {
        if (InventorySystem.Instance == null)
        {
            return;
        }

        if (offerIndex == 0 && firstOffer != null)
        {
            if (InventorySystem.Instance.TryAddItem(firstOffer))
            {
                firstOffer = null;
            }
        }
        else if (offerIndex == 1 && secondOffer != null)
        {
            if (InventorySystem.Instance.TryAddItem(secondOffer))
            {
                secondOffer = null;
            }
        }
    }

    public bool SubmitOffer()
    {
        if (firstOffer == null || secondOffer == null)
        {
            return false;
        }

        InventoryItemDefinition result = FindResult(firstOffer, secondOffer);
        firstOffer = null;
        secondOffer = null;

        if (result == null)
        {
            return true;
        }

        if (!InventorySystem.Instance.TryAddItem(result))
        {
            Debug.Log("No free inventory slot for crafted item.");
            return false;
        }

        return true;
    }

    public InventoryItemDefinition GetOfferItem(int offerIndex)
    {
        return offerIndex == 0 ? firstOffer : secondOffer;
    }

    private InventoryItemDefinition FindResult(InventoryItemDefinition first, InventoryItemDefinition second)
    {
        for (int i = 0; i < recipes.Length; i++)
        {
            if (recipes[i] != null && recipes[i].Matches(first, second))
            {
                return recipes[i].resultItem;
            }
        }

        return null;
    }
}
