using System.Collections.Generic;
using UnityEngine;

namespace CraftSystem
{
    public class CraftMemory : MonoBehaviour
    {
        private readonly List<InventoryItemDefinition> _collectedItems = new();

        public static CraftMemory Instance { get; private set; }

        private void Awake() => Instance = this;

        public void RegisterCollectedItem(InventoryItemDefinition item)
        {
            if (!_collectedItems.Contains(item))
                _collectedItems.Add(item);
        }

        public bool IsItemUnlocked(InventoryItemDefinition item) => _collectedItems.Contains(item);
    }
}