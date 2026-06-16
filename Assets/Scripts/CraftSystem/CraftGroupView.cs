using UnityEngine;
using UnityEngine.EventSystems;

namespace CraftSystem
{
    public class CraftGroupView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CraftCell input1;
        [SerializeField] private CraftCell input2;
        [SerializeField] private CraftCell result;


        public void OnPointerClick(PointerEventData eventData)
        {
            if ((InventorySystem.Instance.GetItemInSlot(0) == input1.Item &&
                 InventorySystem.Instance.GetItemInSlot(1) == input2.Item) ||
                (InventorySystem.Instance.GetItemInSlot(0) == input2.Item &&
                 InventorySystem.Instance.GetItemInSlot(1) == input1.Item))
            {
                Craft();
            }
        }

        public void Refresh()
        {
            input1.Refresh();
            input2.Refresh();
            result.Refresh();
        }

        private void Craft()
        {
            if (InventorySystem.Instance.RemoveItemAt(0) &&
                InventorySystem.Instance.RemoveItemAt(1))
            {
                InventorySystem.Instance.TryAddItem(result.Item);
                Refresh();
            }
        }
    }
}