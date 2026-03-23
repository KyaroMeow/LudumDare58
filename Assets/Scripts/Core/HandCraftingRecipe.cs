using UnityEngine;

[CreateAssetMenu(fileName = "HandRecipe", menuName = "Scriptable Objects/Hand Recipe")]
public class HandCraftingRecipe : ScriptableObject
{
    public InventoryItemDefinition firstItem;
    public InventoryItemDefinition secondItem;
    public InventoryItemDefinition resultItem;

    public bool Matches(InventoryItemDefinition a, InventoryItemDefinition b)
    {
        return (a == firstItem && b == secondItem) || (a == secondItem && b == firstItem);
    }
}
