using UnityEngine;

[CreateAssetMenu(fileName = "InventoryItem", menuName = "Scriptable Objects/Inventory Item")]
public class InventoryItemDefinition : ScriptableObject
{
    public string itemId;
    public string displayName;
    public Sprite icon;
}
