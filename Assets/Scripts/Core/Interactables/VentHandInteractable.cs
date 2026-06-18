using CraftSystem;
using UnityEngine;
using UnityEngine.UI;

public class VentHandInteractable : MonoBehaviour, IInteractable, IOneShotInteractable
{
    [SerializeField] private InventoryUIController inventoryUIController;
    [SerializeField] private GameObject craftMenu;
    [SerializeField] private VentHandIntroController introController;
    [SerializeField] private bool logWhenCraftIsUnavailable = true;

    [Header("Craft UI Text")]
    [SerializeField] private string craftTitle = "СБОРКА";
    [SerializeField] private string craftChannel = "MODULE 02 // HAND LINK";
    [SerializeField] private string craftDescription = "ВЫБЕРИТЕ ДОСТУПНУЮ СХЕМУ";
    [SerializeField] private string closeHintText = "TAB / E / ESC / SPACE / ENTER  //  ЗАКРЫТЬ";
    
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;
    private bool isOpen;
    private bool craftVisualPrepared;
    
    public static bool IsCraftUiOpen { get; private set; }

    public void Interact(Transform holdPosition)
    {
        if (introController == null)
        {
            introController = VentHandIntroController.Instance;
        }

        if (introController != null && introController.HandleHandInteractionClick())
        {
            return;
        }

        if (introController == null || !introController.EnableCraftInteractionAfterIntro)
        {
            if (logWhenCraftIsUnavailable)
            {
                Debug.Log("Hand crafting is unavailable until the vent hand introduction is completed.", this);
            }

            return;
        }

        
        if(!isOpen)
            OpenCraftUi();
    }

    public void StopInteract()
    {
        if(isOpen)
            CloseCraftUi();
    }

    private void OnDisable()
    {
        if(isOpen)
            CloseCraftUi();
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
            CloseCraftUi();
        }
    }

    private void OpenCraftUi()
    {
        if (craftMenu == null)
        {
            Debug.LogWarning("Cannot open hand crafting UI because CraftPanel is not assigned.", this);
            return;
        }

        isOpen = true;
        IsCraftUiOpen = true;

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerView.Instance?.BlockMovement();
        inventoryUIController?.OpenInventory(InventoryPresentationMode.Craft);
        craftMenu.SetActive(true);
        EnsureCraftVisual();
        RefreshCraftGroups();

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged += RefreshCraftGroups;
        }
    }

    private void CloseCraftUi()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        IsCraftUiOpen = false;

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged -= RefreshCraftGroups;
        }

        inventoryUIController?.CloseInventory();
        PlayerView.Instance?.UnlockMovement();

        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;

        PlayerInteraction.Instance?.ClearCurrentInteractable(this);
        if (craftMenu != null)
        {
            craftMenu.SetActive(false);
        }

        Debug.Log("Hand crafting UI closed.");
    }

    private void EnsureCraftVisual()
    {
        if (craftVisualPrepared || craftMenu == null)
        {
            return;
        }

        RectTransform panelRect = craftMenu.transform as RectTransform;
        Vector2 panelPosition = inventoryUIController != null
            ? inventoryUIController.ContextModulePosition
            : new Vector2(330f, 0f);
        Vector2 panelSize = inventoryUIController != null
            ? inventoryUIController.ContextModuleSize
            : new Vector2(610f, 500f);
        TechUiTheme.SetRect(panelRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), panelPosition, panelSize);

        Image panelImage = craftMenu.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = craftMenu.AddComponent<Image>();
        }

        panelImage.sprite = null;
        panelImage.color = TechUiTheme.Panel;
        panelImage.raycastTarget = true;
        TechUiTheme.AddOutline(craftMenu, new Color(TechUiTheme.Danger.r, TechUiTheme.Danger.g, TechUiTheme.Danger.b, 0.5f), new Vector2(1.5f, -1.5f));
        TechUiTheme.AddPanelChrome(panelRect, TechUiTheme.Accent, TechUiTheme.Danger);

        Text title = TechUiTheme.CreateText("Title", panelRect, craftTitle, 22, TechUiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
        TechUiTheme.SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(260f, 30f));

        Text channel = TechUiTheme.CreateText("Channel", panelRect, craftChannel, 10, TechUiTheme.Muted, TextAnchor.MiddleRight);
        TechUiTheme.SetRect(channel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -22f), new Vector2(260f, 22f));

        Text description = TechUiTheme.CreateText("Description", panelRect, craftDescription, 11, TechUiTheme.Muted);
        TechUiTheme.SetRect(description.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -54f), new Vector2(440f, 22f));

        Text closeHint = TechUiTheme.CreateText("CloseHint", panelRect, closeHintText, 10, TechUiTheme.Muted);
        TechUiTheme.SetRect(closeHint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 14f), new Vector2(520f, 22f));

        if (craftMenu.GetComponent<TechUiReveal>() == null)
        {
            craftMenu.AddComponent<TechUiReveal>();
        }

        CraftGroupView[] groups = craftMenu.GetComponentsInChildren<CraftGroupView>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            groups[i]?.ConfigureTerminalLayout(i);
        }

        craftVisualPrepared = true;
    }

    private void RefreshCraftGroups()
    {
        if (craftMenu == null)
        {
            return;
        }

        CraftGroupView[] groups = craftMenu.GetComponentsInChildren<CraftGroupView>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            groups[i]?.Refresh();
        }
    }
}
