using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum InventoryPresentationMode
{
    Standalone,
    Craft,
    Trash
}

public class InventoryUIController : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameObject inventoryRoot;
    [SerializeField] private Image[] slotIcons;
    [SerializeField] private Button[] slotButtons;
    [SerializeField] private TextMeshProUGUI[] slotLabels;
    [SerializeField] private InventoryItemDefinition cutsceneClickItem;
    [SerializeField] private InventoryItemDefinition bombCutsceneClickItem;
    [SerializeField] private float bombDoubleClickInterval = 1.5f;

    [Header("Interface Text")]
    [SerializeField] private string inventoryTitle = "ЛИЧНЫЙ ИНВЕНТАРЬ";
    [SerializeField] private string inventoryChannel = "MODULE 01 // CARGO";
    [SerializeField] private string inventoryDescription = "СЛУЖЕБНОЕ ХРАНИЛИЩЕ // 2 ЯЧЕЙКИ";
    [SerializeField] private string emptySlotText = "ПУСТО";
    [SerializeField] private string closeHintText = "TAB / E / ESC / SPACE / ENTER  //  ЗАКРЫТЬ";

    [Header("Terminal Layout")]
    [SerializeField] private Vector2 inventoryModuleSize = new Vector2(500f, 430f);
    [SerializeField] private Vector2 standaloneModulePosition = new Vector2(-310f, 0f);
    [SerializeField] private Vector2 contextModulePosition = new Vector2(330f, 0f);
    [SerializeField] private Vector2 contextModuleSize = new Vector2(610f, 500f);
    [SerializeField] private Color panelColor = new Color(0.045f, 0.008f, 0.01f, 0.94f);
    [SerializeField] private Color accentColor = new Color(1f, 0.78f, 0.22f, 1f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.08f, 0.045f, 1f);
    [SerializeField] private Color selectedSlotColor = new Color(0.24f, 0.055f, 0.03f, 0.98f);

    private int lastBombClickSlot = -1;
    private float lastBombClickTime = -100f;
    private int selectedSlotIndex = -1;
    private bool sceneButtonsBound;
    private InventoryPresentationMode currentMode = InventoryPresentationMode.Standalone;

    private RectTransform terminalRoot;
    private RectTransform inventoryModule;
    private Button[] terminalSlotButtons;
    private Image[] terminalSlotIcons;
    private Text[] terminalSlotNames;
    private Text[] terminalSlotCodes;

    public event Action<int> SlotSelected;

    public bool IsInventoryOpen => inventoryRoot != null && inventoryRoot.activeSelf;
    public InventoryPresentationMode CurrentMode => currentMode;
    public int SelectedSlotIndex => selectedSlotIndex;
    public Transform ContextRoot => inventoryRoot != null ? inventoryRoot.transform : transform;
    public Vector2 ContextModulePosition => contextModulePosition;
    public Vector2 ContextModuleSize => contextModuleSize;

    private void Start()
    {
        ConfigureCanvas();
        EnsureTerminalVisual();
        Bind();
        Refresh();
    }

    private void OnDestroy()
    {
        Expose();
    }

    private void Update()
    {
        if (TrashBinInteractable.IsTrashUiOpen || VentHandInteractable.IsCraftUiOpen)
        {
            return;
        }

        if (IsInventoryOpen && PlayerInteraction.GetCloseActionKeyDown(includeTab: true))
        {
            PlayerInteraction.MarkCloseActionConsumed();
            CloseInventory();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            OpenInventory(InventoryPresentationMode.Standalone);
        }
    }

    private void OnEnable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged -= Refresh;
        }
    }

    public void ToggleInventory()
    {
        if (inventoryRoot == null)
        {
            return;
        }

        if (inventoryRoot.activeSelf)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory(InventoryPresentationMode.Standalone);
        }
    }

    public void OpenInventory(bool enableSlotButtons = true)
    {
        OpenInventory(enableSlotButtons ? InventoryPresentationMode.Standalone : InventoryPresentationMode.Craft);
    }

    public void OpenInventory(InventoryPresentationMode mode)
    {
        if (inventoryRoot == null)
        {
            return;
        }

        currentMode = mode;
        selectedSlotIndex = -1;
        inventoryRoot.SetActive(true);
        EnsureTerminalVisual();
        if (inventoryModule != null)
        {
            inventoryModule.anchoredPosition = standaloneModulePosition;
        }

        SetButtonsEnabled(mode != InventoryPresentationMode.Craft);
        Refresh();
    }

    public void CloseInventory()
    {
        if (inventoryRoot == null)
        {
            return;
        }

        inventoryRoot.SetActive(false);
        selectedSlotIndex = -1;
        currentMode = InventoryPresentationMode.Standalone;
        lastBombClickSlot = -1;
        SetButtonsEnabled(true);
    }

    public void SetSelectedSlot(int slotIndex)
    {
        selectedSlotIndex = slotIndex;
        RefreshSlotSelection();
    }

    public void Refresh()
    {
        if (InventorySystem.Instance == null)
        {
            return;
        }

        EnsureTerminalVisual();
        int slotCount = GetVisibleSlotCount();
        if (selectedSlotIndex >= slotCount ||
            (selectedSlotIndex >= 0 && InventorySystem.Instance.GetItemInSlot(selectedSlotIndex) == null))
        {
            selectedSlotIndex = -1;
        }

        for (int i = 0; i < slotCount; i++)
        {
            InventoryItemDefinition item = InventorySystem.Instance.GetItemInSlot(i);
            UpdateLegacySlot(i, item);
            UpdateTerminalSlot(i, item);
            SetSlotPulse(i, item);
        }

        RefreshSlotSelection();
    }

    private void UpdateLegacySlot(int index, InventoryItemDefinition item)
    {
        if (slotLabels != null && index < slotLabels.Length && slotLabels[index] != null)
        {
            slotLabels[index].text = item != null ? item.displayName : emptySlotText;
        }

        if (slotIcons != null && index < slotIcons.Length && slotIcons[index] != null)
        {
            slotIcons[index].enabled = item != null && item.icon != null;
            slotIcons[index].sprite = item != null ? item.icon : null;
        }
    }

    private void UpdateTerminalSlot(int index, InventoryItemDefinition item)
    {
        if (terminalSlotNames == null || index >= terminalSlotNames.Length)
        {
            return;
        }

        terminalSlotNames[index].text = item != null && !string.IsNullOrWhiteSpace(item.displayName)
            ? item.displayName.ToUpperInvariant()
            : emptySlotText;
        terminalSlotNames[index].color = item != null ? TechUiTheme.Text : TechUiTheme.Muted;

        terminalSlotCodes[index].text = $"SLOT {index + 1:00}";
        terminalSlotIcons[index].sprite = item != null ? item.icon : null;
        terminalSlotIcons[index].enabled = item != null && item.icon != null;
    }

    private void OnInventoryButtonClicked(int slotIndex)
    {
        if (InventorySystem.Instance == null)
        {
            return;
        }

        InventoryItemDefinition item = InventorySystem.Instance.GetItemInSlot(slotIndex);
        if (currentMode == InventoryPresentationMode.Trash)
        {
            selectedSlotIndex = item != null ? slotIndex : -1;
            RefreshSlotSelection();
            SlotSelected?.Invoke(selectedSlotIndex);
            return;
        }

        if (currentMode == InventoryPresentationMode.Craft || item == null)
        {
            return;
        }

        if (IsCutsceneToasterItem(item))
        {
            CutscenePlaybackManager manager = ResolveCutscenePlaybackManager();
            if (manager == null)
            {
                Debug.LogWarning("Cannot start toaster cutscene because CutscenePlaybackManager is missing.");
                return;
            }

            if (manager.IsPlaying)
            {
                return;
            }

            CloseInventory();
            manager.PlayToasterCutscene();
            return;
        }

        if (IsCutsceneBombItem(item))
        {
            HandleBombInventoryClick(slotIndex);
        }
    }

    private void HandleBombInventoryClick(int slotIndex)
    {
        CutscenePlaybackManager manager = ResolveCutscenePlaybackManager();
        if (manager == null)
        {
            Debug.LogWarning("Cannot start bomb explosion ending because CutscenePlaybackManager is missing.");
            return;
        }

        if (manager.IsPlaying)
        {
            return;
        }

        float now = Time.unscaledTime;
        bool isDoubleClick = lastBombClickSlot == slotIndex &&
                             now - lastBombClickTime <= Mathf.Max(0.05f, bombDoubleClickInterval);
        lastBombClickSlot = slotIndex;
        lastBombClickTime = now;

        if (!isDoubleClick)
        {
            return;
        }

        CloseInventory();
        manager.PlayBombExplosionEnding();
    }

    private bool IsCutsceneToasterItem(InventoryItemDefinition item)
    {
        if (item == null)
        {
            return false;
        }

        if (cutsceneClickItem != null && item == cutsceneClickItem)
        {
            return true;
        }

        return MatchesToasterAlias(item.name) ||
               MatchesToasterAlias(item.displayName) ||
               MatchesToasterAlias(item.itemId);
    }

    private static bool MatchesToasterAlias(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = NormalizeName(value);
        return normalized == "toaster" ||
               normalized == "atomtoster" ||
               normalized == "atomtoaster" ||
               normalized == "acidtoaster" ||
               normalized == "атомныйтостер";
    }

    private bool IsCutsceneBombItem(InventoryItemDefinition item)
    {
        if (item == null)
        {
            return false;
        }

        if (bombCutsceneClickItem != null && item == bombCutsceneClickItem)
        {
            return true;
        }

        return MatchesBombAlias(item.name) ||
               MatchesBombAlias(item.displayName) ||
               MatchesBombAlias(item.itemId);
    }

    private static bool MatchesBombAlias(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = NormalizeName(value);
        return normalized == "bomb" || normalized == "explosive" || normalized == "boom";
    }

    private static string NormalizeName(string value)
    {
        return value.Trim()
            .ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty);
    }

    private void SetSlotPulse(int slotIndex, InventoryItemDefinition item)
    {
        bool toasterPulse = IsCutsceneToasterItem(item);
        bool bombPulse = IsCutsceneBombItem(item);
        CutscenePlaybackManager manager = ResolveCutscenePlaybackManager();
        bool canPulse = currentMode == InventoryPresentationMode.Standalone &&
                        (toasterPulse || bombPulse) &&
                        (manager == null || !manager.IsPlaying);
        Color pulseColor = bombPulse
            ? new Color(1f, 0.84f, 0.2f, 1f)
            : new Color(1f, 0.74f, 0.24f, 1f);
        float pulseSpeed = bombPulse ? 4.4f : 4.1f;
        float scaleAmount = bombPulse ? 0.045f : 0.05f;

        Component buttonTarget = terminalSlotButtons != null && slotIndex < terminalSlotButtons.Length
            ? terminalSlotButtons[slotIndex]
            : slotButtons != null && slotIndex < slotButtons.Length ? slotButtons[slotIndex] : null;
        Component iconTarget = terminalSlotIcons != null && slotIndex < terminalSlotIcons.Length
            ? terminalSlotIcons[slotIndex]
            : slotIcons != null && slotIndex < slotIcons.Length ? slotIcons[slotIndex] : null;

        SetPulse(buttonTarget, canPulse, pulseColor, pulseSpeed, scaleAmount, CutsceneHintPulse.PulseStyle.Glow);
        SetPulse(iconTarget, canPulse, pulseColor, pulseSpeed, scaleAmount * 0.7f, CutsceneHintPulse.PulseStyle.Sparkles);
    }

    private static void SetPulse(
        Component target,
        bool shouldPulse,
        Color pulseColor,
        float pulseSpeed,
        float scaleAmount,
        CutsceneHintPulse.PulseStyle pulseStyle)
    {
        if (target == null)
        {
            return;
        }

        CutsceneHintPulse pulse = target.GetComponent<CutsceneHintPulse>();
        if (shouldPulse)
        {
            if (pulse == null)
            {
                pulse = target.gameObject.AddComponent<CutsceneHintPulse>();
            }

            pulse.Configure(pulseColor, pulseSpeed, scaleAmount, pulseStyle);
            pulse.enabled = true;
            return;
        }

        if (pulse != null)
        {
            pulse.enabled = false;
        }
    }

    private CutscenePlaybackManager ResolveCutscenePlaybackManager()
    {
        return CutscenePlaybackManager.Instance != null
            ? CutscenePlaybackManager.Instance
            : FindFirstObjectByType<CutscenePlaybackManager>();
    }

    private void ConfigureCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (canvas == null)
        {
            return;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void EnsureTerminalVisual()
    {
        if (terminalRoot != null || inventoryRoot == null)
        {
            return;
        }

        Transform oldPanel = inventoryRoot.transform.Find("PanelHuman");
        if (oldPanel != null)
        {
            oldPanel.gameObject.SetActive(false);
        }

        Image backdrop = inventoryRoot.GetComponent<Image>();
        if (backdrop != null)
        {
            backdrop.color = new Color(0f, 0f, 0f, 0.48f);
            backdrop.raycastTarget = false;
        }

        terminalRoot = TechUiTheme.CreateRect("TechInventoryTerminal", inventoryRoot.transform);
        TechUiTheme.Stretch(terminalRoot, Vector2.zero, Vector2.zero);

        Image moduleImage = TechUiTheme.CreateImage("InventoryModule", terminalRoot, panelColor, true);
        inventoryModule = moduleImage.rectTransform;
        TechUiTheme.SetRect(inventoryModule, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), standaloneModulePosition, inventoryModuleSize);
        TechUiTheme.AddOutline(moduleImage.gameObject, new Color(dangerColor.r, dangerColor.g, dangerColor.b, 0.5f), new Vector2(1.5f, -1.5f));
        TechUiTheme.AddPanelChrome(inventoryModule, accentColor, dangerColor);
        moduleImage.gameObject.AddComponent<TechUiReveal>();

        Text title = TechUiTheme.CreateText("Title", inventoryModule, inventoryTitle, 22, accentColor, TextAnchor.MiddleLeft, FontStyle.Bold);
        TechUiTheme.SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(300f, 30f));

        Text channel = TechUiTheme.CreateText("Channel", inventoryModule, inventoryChannel, 10, TechUiTheme.Muted, TextAnchor.MiddleRight);
        TechUiTheme.SetRect(channel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -22f), new Vector2(180f, 22f));

        Text description = TechUiTheme.CreateText("Description", inventoryModule, inventoryDescription, 11, TechUiTheme.Muted);
        TechUiTheme.SetRect(description.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -55f), new Vector2(440f, 24f));

        int slotCount = GetVisibleSlotCount();
        terminalSlotButtons = new Button[slotCount];
        terminalSlotIcons = new Image[slotCount];
        terminalSlotNames = new Text[slotCount];
        terminalSlotCodes = new Text[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            int capturedIndex = i;
            Button slotButton = TechUiTheme.CreateButton(
                $"InventorySlot_{i + 1:00}",
                inventoryModule,
                TechUiTheme.Slot,
                new Color(0.22f, 0.055f, 0.035f, 1f),
                new Color(0.34f, 0.08f, 0.035f, 1f));
            TechUiTheme.SetRect(
                slotButton.transform as RectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -94f - i * 120f),
                new Vector2(452f, 102f));
            TechUiTheme.AddOutline(slotButton.gameObject, new Color(dangerColor.r, dangerColor.g, dangerColor.b, 0.36f), new Vector2(1f, -1f));
            slotButton.onClick.AddListener(() => OnInventoryButtonClicked(capturedIndex));

            Image accent = TechUiTheme.CreateImage("SelectionBar", slotButton.transform, accentColor);
            TechUiTheme.SetRect(accent.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(3f, 74f));

            Text code = TechUiTheme.CreateText("SlotCode", slotButton.transform, $"SLOT {i + 1:00}", 10, TechUiTheme.Muted, TextAnchor.UpperLeft, FontStyle.Bold);
            TechUiTheme.SetRect(code.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -10f), new Vector2(92f, 20f));

            Image icon = TechUiTheme.CreateImage("ItemIcon", slotButton.transform, Color.white);
            icon.preserveAspect = true;
            TechUiTheme.SetRect(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(70f, -10f), new Vector2(68f, 68f));

            Text name = TechUiTheme.CreateText("ItemName", slotButton.transform, emptySlotText, 17, TechUiTheme.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            TechUiTheme.SetRect(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, -4f), new Vector2(300f, 46f));

            terminalSlotButtons[i] = slotButton;
            terminalSlotIcons[i] = icon;
            terminalSlotNames[i] = name;
            terminalSlotCodes[i] = code;
        }

        Text closeHint = TechUiTheme.CreateText("CloseHint", inventoryModule, closeHintText, 10, TechUiTheme.Muted, TextAnchor.MiddleLeft);
        TechUiTheme.SetRect(closeHint.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 16f), new Vector2(440f, 24f));
    }

    private int GetVisibleSlotCount()
    {
        int slotCount = InventorySystem.Instance != null ? InventorySystem.Instance.SlotCount : 0;
        if (slotButtons != null)
        {
            slotCount = Mathf.Max(slotCount, slotButtons.Length);
        }

        if (slotIcons != null)
        {
            slotCount = Mathf.Max(slotCount, slotIcons.Length);
        }

        return Mathf.Max(1, slotCount);
    }

    private void RefreshSlotSelection()
    {
        if (terminalSlotButtons == null)
        {
            return;
        }

        for (int i = 0; i < terminalSlotButtons.Length; i++)
        {
            Button button = terminalSlotButtons[i];
            if (button == null)
            {
                continue;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = i == selectedSlotIndex ? selectedSlotColor : TechUiTheme.Slot;
            button.colors = colors;
        }
    }

    private void SetButtonsEnabled(bool isEnabled)
    {
        if (slotButtons != null)
        {
            foreach (Button button in slotButtons)
            {
                if (button != null)
                {
                    button.interactable = isEnabled;
                }
            }
        }

        if (terminalSlotButtons != null)
        {
            foreach (Button button in terminalSlotButtons)
            {
                if (button != null)
                {
                    button.interactable = isEnabled;
                }
            }
        }
    }

    private void Bind()
    {
        if (sceneButtonsBound || slotButtons == null)
        {
            return;
        }

        for (int index = 0; index < slotButtons.Length; index++)
        {
            Button slotButton = slotButtons[index];
            if (slotButton == null)
            {
                continue;
            }

            int capturedIndex = index;
            slotButton.onClick.AddListener(() => OnInventoryButtonClicked(capturedIndex));
        }

        sceneButtonsBound = true;
    }

    private void Expose()
    {
        if (!sceneButtonsBound || slotButtons == null)
        {
            return;
        }

        foreach (Button button in slotButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }

        sceneButtonsBound = false;
    }
}
