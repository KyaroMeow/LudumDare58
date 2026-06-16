using UnityEngine;
using UnityEngine.UI;

namespace CraftSystem
{
    public class CraftCell : MonoBehaviour
    {
        [SerializeField] private InventoryItemDefinition item;
        [SerializeField] private Image itemIcon;
        [SerializeField] private Color lockedColor;

        public InventoryItemDefinition Item => item;

        public void Refresh()
        {
            if (!item)
                return;

            itemIcon.sprite = item.icon;
            itemIcon.color = CraftMemory.Instance.IsItemUnlocked(item) ? Color.white : lockedColor;
        }
    }
}