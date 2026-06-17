using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIController : MonoBehaviour
{
    [SerializeField] private GameObject inventoryRoot;
    [SerializeField] private Image[] slotIcons;
    [SerializeField] private Button[] slotButtons;
    [SerializeField] private TextMeshProUGUI[] slotLabels;
    [SerializeField] private InventoryItemDefinition cutsceneClickItem;
    [SerializeField] private InventoryItemDefinition bombCutsceneClickItem;
    [SerializeField] private float bombDoubleClickInterval = 1.5f;

    private int lastBombClickSlot = -1;
    private float lastBombClickTime = -100f;
    
    public bool IsInventoryOpen => inventoryRoot != null && inventoryRoot.activeSelf;

    private void Start()
    {
        Bind();
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

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
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

    private void SetButtonsEnabled(bool isEnabled)
    {
        if (slotButtons == null)
        {
            return;
        }

        foreach (Button button in slotButtons)
        {
            if (button != null)
            {
                button.enabled = isEnabled;
            }
        }
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
            OpenInventory();
        }
    }

    public void OpenInventory(bool enableSlotButtons = true)
    {
        if (inventoryRoot == null)
        {
            return;
        }

        inventoryRoot.SetActive(true);
        SetButtonsEnabled(enableSlotButtons);
        Refresh();
    }

    public void CloseInventory()
    {
        if (inventoryRoot == null)
        {
            return;
        }

        inventoryRoot.SetActive(false);
        SetButtonsEnabled(true);
        Refresh();
    }

    public void Refresh()
    {
        if (InventorySystem.Instance == null)
        {
            return;
        }

        for (int i = 0; i < slotIcons.Length; i++)
        {
            InventoryItemDefinition item = InventorySystem.Instance.GetItemInSlot(i);

            if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
            {
                slotLabels[i].text = item != null ? item.displayName : "Empty";
            }

            if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
            {
                slotIcons[i].enabled = item != null && item.icon != null;
                slotIcons[i].sprite = item != null ? item.icon : null;
            }

            SetSlotPulse(i, item);
        }
    }

    private void OnInventoryButtonClicked(int slotIndex)
    {
        var item = InventorySystem.Instance.GetItemInSlot(slotIndex);
        if(!item)
            return;

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
        bool isDoubleClick = lastBombClickSlot == slotIndex && now - lastBombClickTime <= Mathf.Max(0.05f, bombDoubleClickInterval);
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

    private bool MatchesToasterAlias(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = NormalizeToasterName(value);
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

    private bool MatchesBombAlias(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim()
            .ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty);

        return normalized == "bomb" ||
               normalized == "explosive" ||
               normalized == "boom";
    }

    private static string NormalizeToasterName(string value)
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
        bool canPulse = (toasterPulse || bombPulse) && (manager == null || !manager.IsPlaying);
        Color pulseColor = bombPulse
            ? new Color(1f, 0.84f, 0.2f, 1f)
            : new Color(1f, 0.74f, 0.24f, 1f);
        float pulseSpeed = bombPulse ? 4.4f : 4.1f;
        float scaleAmount = bombPulse ? 0.045f : 0.05f;

        SetPulse(slotButtons != null && slotIndex < slotButtons.Length ? slotButtons[slotIndex] : null, canPulse, pulseColor, pulseSpeed, scaleAmount, CutsceneHintPulse.PulseStyle.Glow);
        SetPulse(slotIcons != null && slotIndex < slotIcons.Length ? slotIcons[slotIndex] : null, canPulse, pulseColor, pulseSpeed, scaleAmount * 0.7f, CutsceneHintPulse.PulseStyle.Sparkles);
        SetPulse(slotLabels != null && slotIndex < slotLabels.Length ? slotLabels[slotIndex] : null, canPulse, pulseColor, pulseSpeed, scaleAmount * 0.25f, CutsceneHintPulse.PulseStyle.Glow);
    }

    private void SetPulse(Component target, bool shouldPulse, Color pulseColor, float pulseSpeed, float scaleAmount, CutsceneHintPulse.PulseStyle pulseStyle)
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
    
    private void Bind()
    {
        for (var index = 0; index < slotButtons.Length; index++)
        {
            var slotButton = slotButtons[index];
            var i = index;
            slotButton.onClick.AddListener(() => OnInventoryButtonClicked(i));
        }
    }

    private void Expose()
    {
        foreach (var b in slotButtons)
            b.onClick.RemoveAllListeners();
    }
}
