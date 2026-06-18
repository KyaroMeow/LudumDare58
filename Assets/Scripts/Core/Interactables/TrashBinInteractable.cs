using UnityEngine;
using UnityEngine.UI;

public class TrashBinInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private InventoryUIController inventoryUIController;

    [Header("Waste UI Text")]
    [SerializeField] private string trashTitle = "УТИЛИЗАЦИЯ";
    [SerializeField] private string trashChannel = "MODULE 03 // WASTE";
    [SerializeField] private string trashDescription = "ВЫБЕРИТЕ ПРЕДМЕТ В ИНВЕНТАРЕ";
    [SerializeField] private string noSelectionText = "ПРЕДМЕТ НЕ ВЫБРАН";
    [SerializeField] private string warningText = "ВНИМАНИЕ // ОПЕРАЦИЯ НЕОБРАТИМА";
    [SerializeField] private string discardButtonText = "УНИЧТОЖИТЬ";
    [SerializeField] private string closeHintText = "TAB / E / ESC / SPACE / ENTER  //  ЗАКРЫТЬ";

    private int selectedSlotIndex = -1;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;
    private bool isOpen;
    private bool eventsBound;
    private string lastActionText;

    private GameObject trashPanel;
    private Image selectedItemIcon;
    private Text selectedItemName;
    private Text selectedItemCode;
    private Text statusText;
    private Button discardButton;

    public static bool IsTrashUiOpen { get; private set; }

    public void Interact(Transform holdPosition)
    {
        if (!isOpen)
        {
            OpenTrashUi();
        }
    }

    public void StopInteract()
    {
        CloseTrashUi();
    }

    public void DiscardSlot(int slotIndex)
    {
        InventorySystem.Instance?.DiscardItemAt(slotIndex);
    }

    private void OnDisable()
    {
        if (isOpen)
        {
            CloseTrashUi();
        }
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (PlayerInteraction.GetCloseActionKeyDown(includeTab: true))
        {
            PlayerInteraction.MarkCloseActionConsumed();
            CloseTrashUi();
        }
    }

    private void OpenTrashUi()
    {
        if (inventoryUIController == null)
        {
            inventoryUIController = FindFirstObjectByType<InventoryUIController>();
        }

        if (inventoryUIController == null)
        {
            Debug.LogWarning("Cannot open trash UI because InventoryUIController is missing.", this);
            return;
        }

        isOpen = true;
        IsTrashUiOpen = true;
        selectedSlotIndex = -1;
        lastActionText = null;

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerView.Instance?.BlockMovement();
        inventoryUIController.OpenInventory(InventoryPresentationMode.Trash);
        EnsureTrashVisual();
        BindEvents();

        if (trashPanel != null)
        {
            trashPanel.SetActive(true);
        }

        RefreshTrashView();
        Debug.Log("Trash bin UI opened.");
    }

    private void CloseTrashUi()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        IsTrashUiOpen = false;
        selectedSlotIndex = -1;
        lastActionText = null;
        UnbindEvents();

        if (trashPanel != null)
        {
            trashPanel.SetActive(false);
        }

        inventoryUIController?.CloseInventory();
        PlayerView.Instance?.UnlockMovement();

        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;

        PlayerInteraction.Instance?.ClearCurrentInteractable(this);
        Debug.Log("Trash bin UI closed.");
    }

    private void BindEvents()
    {
        if (eventsBound)
        {
            return;
        }

        inventoryUIController.SlotSelected += HandleSlotSelected;
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged += HandleInventoryChanged;
        }

        eventsBound = true;
    }

    private void UnbindEvents()
    {
        if (!eventsBound)
        {
            return;
        }

        if (inventoryUIController != null)
        {
            inventoryUIController.SlotSelected -= HandleSlotSelected;
        }

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged -= HandleInventoryChanged;
        }

        eventsBound = false;
    }

    private void HandleSlotSelected(int slotIndex)
    {
        selectedSlotIndex = slotIndex;
        lastActionText = null;
        RefreshTrashView();
    }

    private void HandleInventoryChanged()
    {
        inventoryUIController?.Refresh();
        RefreshTrashView();
    }

    private void DiscardSelectedSlot()
    {
        if (selectedSlotIndex < 0)
        {
            Debug.LogWarning("Trash discard skipped: no inventory item selected.");
            return;
        }

        InventorySystem inventory = InventorySystem.Instance;
        if (inventory == null)
        {
            Debug.LogWarning("Trash discard skipped: InventorySystem is missing.");
            return;
        }

        InventoryItemDefinition removedItem = inventory.RemoveItemAt(selectedSlotIndex);
        if (removedItem == null)
        {
            Debug.LogWarning($"Trash discard skipped: selected slot {selectedSlotIndex + 1} is empty.");
            selectedSlotIndex = -1;
            inventoryUIController?.SetSelectedSlot(-1);
            RefreshTrashView();
            return;
        }

        lastActionText = $"УНИЧТОЖЕНО // {removedItem.displayName.ToUpperInvariant()}";
        Debug.Log($"Trash discarded item '{removedItem.displayName}' from slot {selectedSlotIndex + 1}.");
        selectedSlotIndex = -1;
        inventoryUIController?.SetSelectedSlot(-1);
        RefreshTrashView();
    }

    private void RefreshTrashView()
    {
        if (selectedItemName == null)
        {
            return;
        }

        InventoryItemDefinition item = null;
        if (selectedSlotIndex >= 0 && InventorySystem.Instance != null)
        {
            item = InventorySystem.Instance.GetItemInSlot(selectedSlotIndex);
        }

        if (item == null)
        {
            selectedSlotIndex = -1;
            selectedItemIcon.enabled = false;
            selectedItemIcon.sprite = null;
            selectedItemName.text = string.IsNullOrWhiteSpace(lastActionText) ? noSelectionText : lastActionText;
            selectedItemName.color = string.IsNullOrWhiteSpace(lastActionText) ? TechUiTheme.Muted : TechUiTheme.Safe;
            selectedItemCode.text = "AWAITING SELECTION";
            statusText.text = trashDescription;
            statusText.color = TechUiTheme.Muted;
            discardButton.interactable = false;
            return;
        }

        selectedItemIcon.sprite = item.icon;
        selectedItemIcon.enabled = item.icon != null;
        selectedItemName.text = string.IsNullOrWhiteSpace(item.displayName)
            ? item.name.ToUpperInvariant()
            : item.displayName.ToUpperInvariant();
        selectedItemName.color = TechUiTheme.Text;
        selectedItemCode.text = $"SLOT {selectedSlotIndex + 1:00} // SELECTED";
        statusText.text = warningText;
        statusText.color = TechUiTheme.Danger;
        discardButton.interactable = true;
    }

    private void EnsureTrashVisual()
    {
        if (trashPanel != null || inventoryUIController == null)
        {
            return;
        }

        Image panelImage = TechUiTheme.CreateImage("TechTrashModule", inventoryUIController.ContextRoot, TechUiTheme.Panel, true);
        trashPanel = panelImage.gameObject;
        RectTransform panelRect = panelImage.rectTransform;
        TechUiTheme.SetRect(
            panelRect,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            inventoryUIController.ContextModulePosition,
            inventoryUIController.ContextModuleSize);
        TechUiTheme.AddOutline(trashPanel, new Color(TechUiTheme.Danger.r, TechUiTheme.Danger.g, TechUiTheme.Danger.b, 0.55f), new Vector2(1.5f, -1.5f));
        TechUiTheme.AddPanelChrome(panelRect, TechUiTheme.Accent, TechUiTheme.Danger);

        Text title = TechUiTheme.CreateText("Title", panelRect, trashTitle, 22, TechUiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
        TechUiTheme.SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(280f, 30f));

        Text channel = TechUiTheme.CreateText("Channel", panelRect, trashChannel, 10, TechUiTheme.Muted, TextAnchor.MiddleRight);
        TechUiTheme.SetRect(channel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -22f), new Vector2(250f, 22f));

        Text description = TechUiTheme.CreateText("Description", panelRect, trashDescription, 11, TechUiTheme.Muted);
        TechUiTheme.SetRect(description.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -54f), new Vector2(480f, 22f));

        Image selectionCard = TechUiTheme.CreateImage("SelectionCard", panelRect, TechUiTheme.Slot);
        TechUiTheme.SetRect(selectionCard.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -96f), new Vector2(562f, 152f));
        TechUiTheme.AddOutline(selectionCard.gameObject, new Color(TechUiTheme.Danger.r, TechUiTheme.Danger.g, TechUiTheme.Danger.b, 0.38f), new Vector2(1f, -1f));

        selectedItemIcon = TechUiTheme.CreateImage("SelectedItemIcon", selectionCard.transform, Color.white);
        selectedItemIcon.preserveAspect = true;
        TechUiTheme.SetRect(selectedItemIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(74f, 0f), new Vector2(92f, 92f));

        selectedItemCode = TechUiTheme.CreateText("SelectedItemCode", selectionCard.transform, "AWAITING SELECTION", 10, TechUiTheme.Muted, TextAnchor.UpperLeft, FontStyle.Bold);
        TechUiTheme.SetRect(selectedItemCode.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -24f), new Vector2(380f, 22f));

        selectedItemName = TechUiTheme.CreateText("SelectedItemName", selectionCard.transform, noSelectionText, 18, TechUiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
        TechUiTheme.SetRect(selectedItemName.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(140f, -10f), new Vector2(390f, 60f));

        statusText = TechUiTheme.CreateText("Status", panelRect, trashDescription, 11, TechUiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
        TechUiTheme.SetRect(statusText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -268f), new Vector2(562f, 30f));

        discardButton = TechUiTheme.CreateButton(
            "DiscardButton",
            panelRect,
            new Color(0.46f, 0.055f, 0.025f, 1f),
            new Color(0.78f, 0.09f, 0.035f, 1f),
            new Color(0.32f, 0.02f, 0.015f, 1f));
        TechUiTheme.SetRect(discardButton.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(360f, 62f));
        TechUiTheme.AddOutline(discardButton.gameObject, TechUiTheme.Danger, new Vector2(1.5f, -1.5f));
        Text discardLabel = TechUiTheme.CreateText("Text", discardButton.transform, discardButtonText, 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        TechUiTheme.Stretch(discardLabel.rectTransform, Vector2.zero, Vector2.zero);
        discardButton.onClick.AddListener(DiscardSelectedSlot);

        Text closeHint = TechUiTheme.CreateText("CloseHint", panelRect, closeHintText, 10, TechUiTheme.Muted);
        TechUiTheme.SetRect(closeHint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 14f), new Vector2(520f, 22f));

        trashPanel.AddComponent<TechUiReveal>();
    }
}
