using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CraftSystem
{
    public class CraftGroupView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CraftCell input1;
        [SerializeField] private CraftCell input2;
        [SerializeField] private CraftCell result;
        [SerializeField] private bool showRecipeSymbols = true;
        [SerializeField] private Color symbolColor = new Color(0.9f, 0.9f, 0.9f, 0.92f);
        [SerializeField] private int symbolFontSize = 34;

        private RectTransform rectTransform;
        private Text plusSymbol;
        private Text equalsSymbol;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            EnsureRecipeSymbols();
        }

        private void OnEnable()
        {
            EnsureRecipeSymbols();
            PositionRecipeSymbols();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanCraft())
            {
                return;
            }

            Craft();
        }

        public void Refresh()
        {
            input1?.Refresh();
            input2?.Refresh();
            result?.Refresh();
            EnsureRecipeSymbols();
            PositionRecipeSymbols();
        }

        private void Craft()
        {
            InventorySystem inventory = InventorySystem.Instance;
            if (inventory == null)
            {
                Debug.LogWarning("Cannot craft because InventorySystem is missing.");
                return;
            }

            if (result == null || result.Item == null)
            {
                Debug.LogWarning($"Cannot craft from '{name}' because result item is not assigned.");
                return;
            }

            if (!CanCraft())
            {
                return;
            }

            InventoryItemDefinition firstInput = inventory.RemoveItemAt(0);
            InventoryItemDefinition secondInput = inventory.RemoveItemAt(1);
            if (firstInput == null || secondInput == null)
            {
                TryRestoreInput(inventory, firstInput);
                TryRestoreInput(inventory, secondInput);
                Debug.LogWarning($"Craft from '{name}' was cancelled because inputs could not be removed safely.");
                return;
            }

            if (!inventory.TryAddItem(result.Item))
            {
                TryRestoreInput(inventory, firstInput);
                TryRestoreInput(inventory, secondInput);
                Debug.LogWarning($"Craft from '{name}' failed because result '{result.Item.displayName}' could not be added. Inputs were restored.");
                return;
            }

            RefreshAllGroups();
        }

        private bool CanCraft()
        {
            InventorySystem inventory = InventorySystem.Instance;
            if (inventory == null)
            {
                Debug.LogWarning("Cannot craft because InventorySystem is missing.");
                return false;
            }

            if (input1 == null || input2 == null || result == null)
            {
                Debug.LogWarning($"Cannot craft from '{name}' because one or more CraftCell references are missing.");
                return false;
            }

            InventoryItemDefinition inputItem1 = input1.Item;
            InventoryItemDefinition inputItem2 = input2.Item;
            if (inputItem1 == null || inputItem2 == null)
            {
                Debug.LogWarning($"Cannot craft from '{name}' because one or more input items are not assigned.");
                return false;
            }

            InventoryItemDefinition slot0 = inventory.GetItemInSlot(0);
            InventoryItemDefinition slot1 = inventory.GetItemInSlot(1);

            return (slot0 == inputItem1 && slot1 == inputItem2) ||
                   (slot0 == inputItem2 && slot1 == inputItem1);
        }

        private static void TryRestoreInput(InventorySystem inventory, InventoryItemDefinition item)
        {
            if (inventory == null || item == null)
            {
                return;
            }

            if (!inventory.TryAddItem(item))
            {
                Debug.LogWarning($"Could not restore craft input '{item.displayName}' after failed craft.");
            }
        }

        private static void RefreshAllGroups()
        {
            CraftGroupView[] groups = FindObjectsByType<CraftGroupView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < groups.Length; i++)
            {
                groups[i]?.Refresh();
            }
        }

        private void EnsureRecipeSymbols()
        {
            if (!showRecipeSymbols)
            {
                SetSymbolActive(plusSymbol, false);
                SetSymbolActive(equalsSymbol, false);
                return;
            }

            if (plusSymbol == null)
            {
                plusSymbol = CreateSymbol("CraftSymbol_Plus", "+");
            }

            if (equalsSymbol == null)
            {
                equalsSymbol = CreateSymbol("CraftSymbol_Equals", "=");
            }
        }

        private Text CreateSymbol(string symbolName, string value)
        {
            GameObject symbolObject = new GameObject(symbolName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            symbolObject.transform.SetParent(transform, false);

            RectTransform symbolTransform = symbolObject.GetComponent<RectTransform>();
            symbolTransform.anchorMin = new Vector2(0.5f, 0.5f);
            symbolTransform.anchorMax = new Vector2(0.5f, 0.5f);
            symbolTransform.pivot = new Vector2(0.5f, 0.5f);
            symbolTransform.sizeDelta = new Vector2(42f, 42f);

            Text symbolText = symbolObject.GetComponent<Text>();
            symbolText.text = value;
            symbolText.alignment = TextAnchor.MiddleCenter;
            symbolText.color = symbolColor;
            symbolText.fontSize = Mathf.Max(8, symbolFontSize);
            symbolText.fontStyle = FontStyle.Bold;
            symbolText.raycastTarget = false;
            symbolText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return symbolText;
        }

        private void PositionRecipeSymbols()
        {
            if (!showRecipeSymbols || rectTransform == null || input1 == null || input2 == null || result == null)
            {
                return;
            }

            PositionSymbolBetween(plusSymbol, input1.transform as RectTransform, input2.transform as RectTransform);
            PositionSymbolBetween(equalsSymbol, input2.transform as RectTransform, result.transform as RectTransform);
        }

        private void PositionSymbolBetween(Text symbol, RectTransform left, RectTransform right)
        {
            if (symbol == null || left == null || right == null)
            {
                return;
            }

            RectTransform symbolTransform = symbol.transform as RectTransform;
            if (symbolTransform != null)
            {
                symbolTransform.localPosition = (GetLocalCenter(left) + GetLocalCenter(right)) * 0.5f;
            }

            symbol.color = symbolColor;
            symbol.fontSize = Mathf.Max(8, symbolFontSize);
            SetSymbolActive(symbol, true);
        }

        private Vector3 GetLocalCenter(RectTransform target)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
            return rectTransform.InverseTransformPoint(worldCenter);
        }

        private static void SetSymbolActive(Text symbol, bool isActive)
        {
            if (symbol != null && symbol.gameObject.activeSelf != isActive)
            {
                symbol.gameObject.SetActive(isActive);
            }
        }
    }
}
