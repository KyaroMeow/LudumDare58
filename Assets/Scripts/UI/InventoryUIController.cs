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

            SetSlotPulse(i, IsCutsceneToasterItem(item));
        }
    }

    private void OnInventoryButtonClicked(int slotIndex)
    {
        var item = InventorySystem.Instance.GetItemInSlot(slotIndex);
        if(!item)
            return;

        if (IsCutsceneToasterItem(item))
        {
            CutscenePlaybackManager manager = CutscenePlaybackManager.Instance;
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
        }
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

    private static string NormalizeToasterName(string value)
    {
        return value.Trim()
            .ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty);
    }

    private void SetSlotPulse(int slotIndex, bool shouldPulse)
    {
        bool canPulse = shouldPulse && (CutscenePlaybackManager.Instance == null || !CutscenePlaybackManager.Instance.IsPlaying);

        SetPulse(slotButtons != null && slotIndex < slotButtons.Length ? slotButtons[slotIndex] : null, canPulse);
        SetPulse(slotIcons != null && slotIndex < slotIcons.Length ? slotIcons[slotIndex] : null, canPulse);
        SetPulse(slotLabels != null && slotIndex < slotLabels.Length ? slotLabels[slotIndex] : null, canPulse);
    }

    private void SetPulse(Component target, bool shouldPulse)
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

            pulse.enabled = true;
            return;
        }

        if (pulse != null)
        {
            pulse.enabled = false;
        }
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
