using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CraftSystem
{
    public class CraftCell : MonoBehaviour
    {
        [SerializeField] private InventoryItemDefinition item;
        [SerializeField] private Image itemIcon;
        [SerializeField] private Color lockedColor;
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.64f);
        [Header("Terminal UI")]
        [SerializeField] private string slotCode;
        [SerializeField] private bool showItemName = true;

        private bool warnedMissingIcon;
        private Text codeText;
        private Text nameText;
        private Image background;

        public InventoryItemDefinition Item => item;

        private void Awake()
        {
            EnsureConsistentVisual();
        }

        private void OnEnable()
        {
            EnsureConsistentVisual();
            Refresh();
        }

        public void Refresh()
        {
            EnsureConsistentVisual();

            if (item == null)
            {
                if (itemIcon != null)
                {
                    itemIcon.enabled = false;
                }

                if (nameText != null)
                {
                    nameText.text = "НЕТ ДАННЫХ";
                    nameText.color = TechUiTheme.Muted;
                }

                return;
            }

            if (itemIcon == null)
            {
                if (!warnedMissingIcon)
                {
                    warnedMissingIcon = true;
                    Debug.LogWarning($"CraftCell '{name}' has item '{item.displayName}' but no itemIcon reference.");
                }

                return;
            }

            bool isUnlocked = CraftMemory.Instance != null && CraftMemory.Instance.IsItemUnlocked(item);
            itemIcon.sprite = item.icon;
            itemIcon.enabled = item.icon != null;
            itemIcon.color = isUnlocked ? Color.white : lockedColor;

            if (nameText != null)
            {
                nameText.text = showItemName && !string.IsNullOrWhiteSpace(item.displayName)
                    ? item.displayName.ToUpperInvariant()
                    : string.Empty;
                nameText.color = isUnlocked ? TechUiTheme.Text : TechUiTheme.Muted;
            }
        }

        public void ConfigureTerminalLayout(Vector2 position, string fallbackCode)
        {
            EnsureConsistentVisual();
            RectTransform rect = transform as RectTransform;
            TechUiTheme.SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(112f, 102f));

            if (string.IsNullOrWhiteSpace(slotCode))
            {
                slotCode = fallbackCode;
            }

            if (codeText != null)
            {
                codeText.text = slotCode;
            }

            Refresh();
        }

        private void EnsureConsistentVisual()
        {
            background = GetComponent<Image>();
            if (background == null)
            {
                return;
            }

            if (itemIcon == null || itemIcon == background)
            {
                itemIcon = CreateRuntimeIcon(background);
            }

            background.sprite = null;
            background.color = backgroundColor;
            background.raycastTarget = false;

            TechUiTheme.AddOutline(gameObject, new Color(TechUiTheme.Danger.r, TechUiTheme.Danger.g, TechUiTheme.Danger.b, 0.38f), new Vector2(1f, -1f));
            ConfigureIconRect();
            EnsureLabels();
        }

        private void ConfigureIconRect()
        {
            if (itemIcon == null)
            {
                return;
            }

            RectTransform iconRect = itemIcon.transform as RectTransform;
            TechUiTheme.SetRect(iconRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(58f, 58f));
            itemIcon.preserveAspect = true;
            itemIcon.raycastTarget = false;
        }

        private void EnsureLabels()
        {
            if (codeText != null && nameText != null)
            {
                return;
            }

            TMP_Text[] oldLabels = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < oldLabels.Length; i++)
            {
                if (oldLabels[i] != null)
                {
                    oldLabels[i].enabled = false;
                }
            }

            codeText = TechUiTheme.CreateText("CellCode", transform, slotCode, 9, TechUiTheme.Muted, TextAnchor.UpperLeft, FontStyle.Bold);
            TechUiTheme.SetRect(codeText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -5f), new Vector2(92f, 16f));

            nameText = TechUiTheme.CreateText("CellName", transform, string.Empty, 9, TechUiTheme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            TechUiTheme.SetRect(nameText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(104f, 24f));
        }

        private Image CreateRuntimeIcon(Image background)
        {
            Transform existing = transform.Find("RuntimeItemIcon");
            if (existing != null)
            {
                Image existingImage = existing.GetComponent<Image>();
                if (existingImage != null)
                {
                    return existingImage;
                }
            }

            GameObject iconObject = new GameObject("RuntimeItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(transform, false);

            RectTransform iconTransform = iconObject.GetComponent<RectTransform>();
            iconTransform.anchorMin = Vector2.zero;
            iconTransform.anchorMax = Vector2.one;
            iconTransform.offsetMin = Vector2.zero;
            iconTransform.offsetMax = Vector2.zero;
            iconTransform.pivot = new Vector2(0.5f, 0.5f);

            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;

            if (background != null && background.sprite != null)
            {
                iconImage.sprite = background.sprite;
                iconImage.color = background.color;
            }

            return iconImage;
        }
    }
}
