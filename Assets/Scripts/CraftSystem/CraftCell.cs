using UnityEngine;
using UnityEngine.UI;

namespace CraftSystem
{
    public class CraftCell : MonoBehaviour
    {
        [SerializeField] private InventoryItemDefinition item;
        [SerializeField] private Image itemIcon;
        [SerializeField] private Color lockedColor;
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.64f);

        private bool warnedMissingIcon;

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
            if (!item)
            {
                return;
            }

            EnsureConsistentVisual();

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
            itemIcon.color = isUnlocked ? Color.white : lockedColor;
        }

        private void EnsureConsistentVisual()
        {
            Image background = GetComponent<Image>();
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
