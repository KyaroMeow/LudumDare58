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
        [Header("Recipe UI Text")]
        [SerializeField] private string recipeTitle;
        [SerializeField] private string recipeCode;
        [SerializeField] private string craftActionText = "СОБРАТЬ";
        [SerializeField] private string readyText = "ДОСТУПНО";
        [SerializeField] private string unavailableText = "НЕТ КОМПОНЕНТОВ";

        private RectTransform rectTransform;
        private Text plusSymbol;
        private Text equalsSymbol;
        private Text recipeTitleText;
        private Text recipeCodeText;
        private Text statusText;
        private Text actionText;
        private Image panelImage;
        private CutsceneHintPulse readyPulse;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            EnsureTerminalVisual();
            EnsureRecipeSymbols();
        }

        private void OnEnable()
        {
            EnsureRecipeSymbols();
            PositionRecipeSymbols();
            EnsureTerminalVisual();
            RefreshTerminalState();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanCraft(false))
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
            EnsureTerminalVisual();
            EnsureRecipeSymbols();
            PositionRecipeSymbols();
            RefreshTerminalState();
        }

        public void ConfigureTerminalLayout(int index)
        {
            EnsureTerminalVisual();
            TechUiTheme.SetRect(
                rectTransform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 92f - index * 188f),
                new Vector2(554f, 168f));

            input1?.ConfigureTerminalLayout(new Vector2(-178f, -20f), "INPUT A");
            input2?.ConfigureTerminalLayout(new Vector2(-42f, -20f), "INPUT B");
            result?.ConfigureTerminalLayout(new Vector2(126f, -20f), "RESULT");
            PositionRecipeSymbols();
            RefreshTerminalState();
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

            if (!CanCraft(false))
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

        public bool CanCraftNow()
        {
            return CanCraft(false);
        }

        private bool CanCraft(bool logWarnings)
        {
            InventorySystem inventory = InventorySystem.Instance;
            if (inventory == null)
            {
                if (logWarnings)
                {
                    Debug.LogWarning("Cannot craft because InventorySystem is missing.");
                }
                return false;
            }

            if (input1 == null || input2 == null || result == null)
            {
                if (logWarnings)
                {
                    Debug.LogWarning($"Cannot craft from '{name}' because one or more CraftCell references are missing.");
                }
                return false;
            }

            InventoryItemDefinition inputItem1 = input1.Item;
            InventoryItemDefinition inputItem2 = input2.Item;
            if (inputItem1 == null || inputItem2 == null)
            {
                if (logWarnings)
                {
                    Debug.LogWarning($"Cannot craft from '{name}' because one or more input items are not assigned.");
                }
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

        private void EnsureTerminalVisual()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            panelImage = GetComponent<Image>();
            if (panelImage == null)
            {
                panelImage = gameObject.AddComponent<Image>();
            }

            panelImage.sprite = null;
            panelImage.color = TechUiTheme.PanelSoft;
            panelImage.raycastTarget = true;
            TechUiTheme.AddOutline(gameObject, new Color(TechUiTheme.Danger.r, TechUiTheme.Danger.g, TechUiTheme.Danger.b, 0.34f), new Vector2(1f, -1f));

            if (recipeTitleText != null)
            {
                return;
            }

            recipeTitleText = TechUiTheme.CreateText("RecipeTitle", transform, string.Empty, 14, TechUiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            TechUiTheme.SetRect(recipeTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -10f), new Vector2(290f, 24f));

            recipeCodeText = TechUiTheme.CreateText("RecipeCode", transform, string.Empty, 9, TechUiTheme.Muted, TextAnchor.MiddleRight);
            TechUiTheme.SetRect(recipeCodeText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -10f), new Vector2(190f, 20f));

            statusText = TechUiTheme.CreateText("RecipeStatus", transform, unavailableText, 10, TechUiTheme.Muted, TextAnchor.MiddleRight, FontStyle.Bold);
            TechUiTheme.SetRect(statusText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 14f), new Vector2(190f, 22f));

            actionText = TechUiTheme.CreateText("CraftAction", transform, craftActionText, 12, TechUiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            TechUiTheme.SetRect(actionText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-66f, -14f), new Vector2(116f, 42f));
            Image actionBackground = TechUiTheme.CreateImage("ActionBackground", actionText.transform, new Color(TechUiTheme.Accent.r, TechUiTheme.Accent.g, TechUiTheme.Accent.b, 0.12f));
            TechUiTheme.Stretch(actionBackground.rectTransform, Vector2.zero, Vector2.zero);
            actionBackground.transform.SetAsFirstSibling();
            TechUiTheme.AddOutline(actionBackground.gameObject, new Color(TechUiTheme.Accent.r, TechUiTheme.Accent.g, TechUiTheme.Accent.b, 0.5f), new Vector2(1f, -1f));

            readyPulse = actionText.gameObject.AddComponent<CutsceneHintPulse>();
            readyPulse.Configure(TechUiTheme.Safe, 3.6f, 0.035f, CutsceneHintPulse.PulseStyle.Glow);
            readyPulse.enabled = false;
        }

        private void RefreshTerminalState()
        {
            if (recipeTitleText == null)
            {
                return;
            }

            string fallbackTitle = result != null && result.Item != null && !string.IsNullOrWhiteSpace(result.Item.displayName)
                ? result.Item.displayName.ToUpperInvariant()
                : name.ToUpperInvariant();
            recipeTitleText.text = string.IsNullOrWhiteSpace(recipeTitle) ? fallbackTitle : recipeTitle;
            recipeCodeText.text = string.IsNullOrWhiteSpace(recipeCode)
                ? name.ToUpperInvariant().Replace("CRAFTRECIPE_", "SCHEMA // ")
                : recipeCode;
            actionText.text = craftActionText;

            bool ready = CanCraft(false);
            statusText.text = ready ? readyText : unavailableText;
            statusText.color = ready ? TechUiTheme.Safe : TechUiTheme.Muted;
            if (panelImage != null)
            {
                panelImage.color = ready
                    ? new Color(0.11f, 0.04f, 0.02f, 0.96f)
                    : TechUiTheme.PanelSoft;
            }

            if (readyPulse != null)
            {
                readyPulse.enabled = ready;
            }

            actionText.color = ready ? TechUiTheme.Accent : TechUiTheme.Muted;
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
