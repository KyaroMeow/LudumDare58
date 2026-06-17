using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class ConveyorItemInteractable : MonoBehaviour, IInteractable
{
    public ToolType toolTypeForDisassemble = ToolType.None;

    [SerializeField] private string bookItemName;
    [SerializeField] private InventoryItemDefinition detailReward;
    [SerializeField] private InventoryItemDefinition trashReward;
    [SerializeField] private InventoryItemDefinition stealReward;
    [Header("Dropped Reward Pickup")]
    [SerializeField] private GameObject detailDropPrefab;
    [SerializeField] private GameObject stealDropPrefab;
    [SerializeField] private Vector3 rewardDropOffset = new Vector3(0f, 0.08f, 0f);
    [SerializeField] private bool alignRewardDropToItemRotation = true;
    [SerializeField] private bool canBeStolen = true;
    [SerializeField] private string itemName;
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue pickupSfx;
    [SerializeField] private SfxCue wrenchDisassembleSfx;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    private Collider[] cachedColliders;
    private bool isBeingInspected;
    private bool warnedStealLocked;
    private GameObject stealButtonRoot;
    private Button stealButton;
    private CutsceneHintPulse stealButtonPulse;
    private static readonly Color BookStealPulseColor = new Color(1f, 0.82f, 0.28f, 1f);
    private static readonly Color AnomalyFinalStealPulseColor = new Color(0.42f, 0.9f, 1f, 1f);

    private void Awake()
    {
        cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    public void Interact(Transform holdPosition)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsStoryInteractionLocked)
        {
            Debug.Log("Item inspection blocked because vent hand intro is running.");
            PlayerInteraction.Instance?.ClearCurrentInteractable(this);
            return;
        }

        AnomallyController anomalyController = ResolveAnomalyController();
        if (anomalyController != null && anomalyController.IsChaseActive && anomalyController.IsStoryAnomalyCube(gameObject))
        {
            Debug.Log("Anomaly cube inspection is blocked while the sphere chase is active.");
            PlayerInteraction.Instance?.ClearCurrentInteractable(this);
            return;
        }

        TabletInteractable.Instance?.OpenBestiaryItem(itemName);

        PlayerView.Instance?.BlockMovement();

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;

        transform.SetParent(holdPosition);
        transform.localPosition = Vector3.zero;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        SetCollidersEnabled(false);

        Item item = GetComponentInChildren<Item>(true);
        PlayerHeldItem.Instance?.TrySelectItem(item);
        HUDManager.Instance?.showItemScanHUD(item);
        PlayerItemInspection.Instance?.BeginInspection(gameObject);
        isBeingInspected = true;
        TutorialHintSystem.Instance?.NotifyItemInspectionStarted(gameObject);
        PlaySfx(pickupSfx);
        RefreshStealButton();
    }

    private void Update()
    {
        if (isBeingInspected)
        {
            RefreshStealButton();
        }
    }

    public void StopInteract()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        SetCollidersEnabled(true);

        PlayerView.Instance?.UnlockMovement();
        transform.SetParent(originalParent);
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;

        GameManager.Instance?.ToggleScanerOff();
        HUDManager.Instance?.hideItemScanHUD();
        PlayerHeldItem.Instance?.ClearItem();
        PlayerItemInspection.Instance?.EndInspection();
        isBeingInspected = false;
        DestroyStealButton();
    }

    public bool TryDisassemble(ToolType toolType)
    {
        return TryUseTool(toolType);
    }

    public bool TryStealFromInspection()
    {
        if (!IsStealUnlocked())
        {
            if (!warnedStealLocked)
            {
                warnedStealLocked = true;
                Debug.Log("Steal is locked until vent hand intro is completed.");
            }

            return false;
        }

        if (TryHandleStoryAnomalySteal(out bool anomalyResult))
        {
            return anomalyResult;
        }
        
        bool shouldPlayBookCutscene = IsCurrentHeldBookItem();
        bool stealSucceeded = TryUseTool(ToolType.Steal);

        if (stealSucceeded)
        {
            DestroyStealButton();
        }

        if (stealSucceeded && shouldPlayBookCutscene)
        {
            if (CutscenePlaybackManager.Instance != null)
            {
                CutscenePlaybackManager.Instance.PlayBookTheftCutscene();
            }
            else
            {
                Debug.LogWarning("Cannot start book theft cutscene because CutscenePlaybackManager is missing.");
            }
        }

        return stealSucceeded;
    }

    private bool TryHandleStoryAnomalySteal(out bool result)
    {
        result = false;

        AnomallyController anomalyController = ResolveAnomalyController();
        if (anomalyController == null || !anomalyController.IsStoryAnomalyCube(gameObject))
        {
            return false;
        }

        if (anomalyController.CanFinalStealEnergySphere(gameObject))
        {
            result = anomalyController.TryFinalStealEnergySphere(gameObject);
            if (result)
            {
                DestroyStealButton();
            }

            return true;
        }

        if (anomalyController.CanStartAnomalySteal(gameObject))
        {
            result = anomalyController.TryStartAnomalySteal(gameObject);
            if (result)
            {
                DestroyStealButton();
            }

            return true;
        }

        return true;
    }

    private bool TryUseTool(ToolType toolType)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsStoryInteractionLocked)
        {
            Debug.Log($"Tool action {toolType} blocked because vent hand intro is running.");
            return false;
        }

        if (InventorySystem.Instance == null)
        {
            Debug.LogWarning("InventorySystem is not present in scene.");
            return false;
        }

        InventoryItemDefinition reward = null;
        string actionName = $"Use tool {toolType}";
        bool isDisassemblyReward = false;
        bool isStealReward = false;

        if (toolType != ToolType.Steal && TryResolveTargetedDisassembly(out ToolType requiredTool, out string disassemblyTargetName))
        {
            if (toolType != requiredTool)
            {
                Debug.LogWarning($"Cannot disassemble '{gameObject.name}' with {toolType}. Required tool: {requiredTool}. Item remains in place.");
                return false;
            }

            reward = detailReward;
            actionName = $"Disassemble {disassemblyTargetName}";
            isDisassemblyReward = true;
        }
        else if (toolType == ToolType.Steal)
        {
            if (!canBeStolen)
            {
                Debug.LogWarning($"Cannot steal '{gameObject.name}' because this item is marked as not stealable.");
                return false;
            }
            
            reward = stealReward;
            actionName = "Steal item";
            isStealReward = true;
        }
        else if (toolTypeForDisassemble == toolType)
        {
            reward = detailReward;
            isDisassemblyReward = true;
        }
        else
        {
            reward = trashReward;
        }

        if (reward == null)
        {
            Debug.LogWarning($"Cannot use tool {toolType} on '{gameObject.name}' because no reward is configured. Item remains in place.");
            return false;
        }

        GameObject rewardDropPrefab = ResolveRewardDropPrefab(isDisassemblyReward, isStealReward);
        if (rewardDropPrefab != null)
        {
            if (!SpawnRewardDrop(rewardDropPrefab, reward, toolType))
            {
                return false;
            }

            PlayToolSuccessSfx(toolType);
            GameManager.Instance?.securitySystem?.ReportViolation(actionName);
            CompleteToolAction();
            return true;
        }

        if (!InventorySystem.Instance.TryAddItem(reward))
        {
            Debug.LogWarning($"Inventory is full. Cannot add reward '{reward.displayName}' from item '{gameObject.name}' using tool {toolType}.");
            return false;
        }

        PlayToolSuccessSfx(toolType);
        GameManager.Instance?.securitySystem?.ReportViolation(actionName);
        CompleteToolAction();
        return true;
    }

    private GameObject ResolveRewardDropPrefab(bool isDisassemblyReward, bool isStealReward)
    {
        if (isStealReward)
        {
            return stealDropPrefab;
        }

        if (isDisassemblyReward)
        {
            return detailDropPrefab;
        }

        return null;
    }

    private bool SpawnRewardDrop(GameObject dropPrefab, InventoryItemDefinition reward, ToolType toolType)
    {
        if (dropPrefab == null || reward == null)
        {
            return false;
        }

        Quaternion spawnRotation = alignRewardDropToItemRotation ? transform.rotation : dropPrefab.transform.rotation;
        GameObject droppedItem = Instantiate(dropPrefab, transform.position + rewardDropOffset, spawnRotation);
        if (droppedItem == null)
        {
            Debug.LogWarning($"Cannot spawn dropped reward '{reward.displayName}' from item '{gameObject.name}' using tool {toolType}.");
            return false;
        }

        DroppedInventoryItemInteractable pickup = droppedItem.GetComponentInChildren<DroppedInventoryItemInteractable>(true);
        if (pickup == null)
        {
            pickup = droppedItem.AddComponent<DroppedInventoryItemInteractable>();
        }

        pickup.Configure(reward, pickupSfx);
        return true;
    }

    private void PlayToolSuccessSfx(ToolType toolType)
    {
        if (toolType == ToolType.Wrench && toolTypeForDisassemble == toolType)
        {
            PlaySfx(wrenchDisassembleSfx);
        }
    }

    private bool TryResolveTargetedDisassembly(out ToolType requiredTool, out string targetName)
    {
        string identity = $"{itemName} {gameObject.name}".ToLowerInvariant();

        if (ContainsAny(identity, "cassette", "cassete", "casette", "кассета"))
        {
            requiredTool = toolTypeForDisassemble;
            targetName = "cassette";
            return true;
        }

        if (ContainsAny(identity, "toaster", "toster", "тостер") &&
            !ContainsAny(identity, "atomic", "atom", "acid", "атом", "финальный"))
        {
            requiredTool = toolTypeForDisassemble;
            targetName = "toaster";
            return true;
        }

        requiredTool = ToolType.None;
        targetName = string.Empty;
        return false;
    }

    private static bool ContainsAny(string value, params string[] tokens)
    {
        if (string.IsNullOrEmpty(value) || tokens == null)
        {
            return false;
        }

        for (int i = 0; i < tokens.Length; i++)
        {
            if (!string.IsNullOrEmpty(tokens[i]) &&
                value.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private void CompleteToolAction()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteCurrentItemAfterToolAction(gameObject);
            return;
        }

        Debug.LogWarning($"Cannot complete tool action for '{gameObject.name}' because GameManager.Instance is missing.");
    }

    private void RefreshStealButton()
    {
        bool shouldShow = ShouldShowStealButton();
        if (!shouldShow)
        {
            if (stealButtonRoot != null)
            {
                stealButtonRoot.SetActive(false);
            }

            return;
        }

        EnsureStealButton();

        if (stealButtonRoot != null && !stealButtonRoot.activeSelf)
        {
            stealButtonRoot.SetActive(true);
        }

        bool managerPlaying = CutscenePlaybackManager.Instance != null && CutscenePlaybackManager.Instance.IsPlaying;
        if (stealButton != null)
        {
            stealButton.interactable = !managerPlaying;
        }

        if (stealButtonPulse != null)
        {
            stealButtonPulse.enabled = ShouldPulseStealButton();
        }
    }

    private bool ShouldShowStealButton()
    {
        if (!isBeingInspected)
        {
            return false;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsStoryInteractionLocked)
        {
            return false;
        }

        return IsStealUnlocked();
    }

    private bool ShouldPulseStealButton()
    {
        if (!ShouldShowStealButton() || !canBeStolen)
        {
            return false;
        }

        if (CutscenePlaybackManager.Instance != null && CutscenePlaybackManager.Instance.IsPlaying)
        {
            return false;
        }

        AnomallyController anomalyController = ResolveAnomalyController();
        if (anomalyController != null &&
            anomalyController.CanFinalStealEnergySphere(gameObject))
        {
            stealButtonPulse?.Configure(AnomalyFinalStealPulseColor, 4.5f, 0.08f, CutsceneHintPulse.PulseStyle.Sparkles);
            return true;
        }

        if (IsCurrentHeldBookItem())
        {
            stealButtonPulse?.Configure(BookStealPulseColor, 3.6f, 0.06f, CutsceneHintPulse.PulseStyle.Sparkles);
            return true;
        }

        return false;
    }

    private void EnsureStealButton()
    {
        if (stealButtonRoot != null)
        {
            return;
        }

        stealButtonRoot = new GameObject("Steal Inspection Button", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = stealButtonRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 30000;

        CanvasScaler scaler = stealButtonRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject buttonObject = new GameObject("Steal Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CutsceneHintPulse));
        buttonObject.transform.SetParent(stealButtonRoot.transform, false);

        RectTransform buttonTransform = buttonObject.GetComponent<RectTransform>();
        buttonTransform.anchorMin = new Vector2(0f, 1f);
        buttonTransform.anchorMax = new Vector2(0f, 1f);
        buttonTransform.pivot = new Vector2(0f, 1f);
        buttonTransform.anchoredPosition = new Vector2(16f, -104f);
        buttonTransform.sizeDelta = new Vector2(160f, 40f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.12f, 0.1f, 0.16f, 0.92f);

        stealButton = buttonObject.GetComponent<Button>();
        stealButton.targetGraphic = buttonImage;
        stealButton.onClick.AddListener(HandleStealButtonClicked);

        stealButtonPulse = buttonObject.GetComponent<CutsceneHintPulse>();
        stealButtonPulse.enabled = false;

        GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelTransform = labelObject.GetComponent<RectTransform>();
        labelTransform.anchorMin = Vector2.zero;
        labelTransform.anchorMax = Vector2.one;
        labelTransform.offsetMin = Vector2.zero;
        labelTransform.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        label.text = "Украсть";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.black;
        label.fontSize = 18;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void HandleStealButtonClicked()
    {
        if (CutscenePlaybackManager.Instance != null && CutscenePlaybackManager.Instance.IsPlaying)
        {
            return;
        }

        TryStealFromInspection();
    }

    private void DestroyStealButton()
    {
        if (stealButton != null)
        {
            stealButton.onClick.RemoveListener(HandleStealButtonClicked);
            stealButton = null;
        }

        stealButtonPulse = null;

        if (stealButtonRoot != null)
        {
            Destroy(stealButtonRoot);
            stealButtonRoot = null;
        }
    }

    private bool IsStealUnlocked()
    {
        VentHandIntroController introController = VentHandIntroController.Instance;
        return introController != null && introController.IsStealUnlocked;
    }

    private AnomallyController ResolveAnomalyController()
    {
        return GameManager.Instance != null ? GameManager.Instance.anomallyController : null;
    }

    private bool IsCurrentHeldBookItem()
    {
        if (string.IsNullOrEmpty(bookItemName))
        {
            return false;
        }

        ConveyorItemInteractable currentInteractable = ResolveCurrentHeldInteractable();
        return currentInteractable != null && currentInteractable.itemName == bookItemName;
    }

    private ConveyorItemInteractable ResolveCurrentHeldInteractable()
    {
        PlayerHeldItem heldItem = PlayerInteraction.Instance != null ? PlayerInteraction.Instance.CurrentHeldItem : null;
        Item currentItem = heldItem != null ? heldItem.CurrentItem : null;

        if (currentItem != null)
        {
            ConveyorItemInteractable interactable = currentItem.GetComponentInParent<ConveyorItemInteractable>();
            if (interactable != null)
            {
                return interactable;
            }

            interactable = currentItem.GetComponentInChildren<ConveyorItemInteractable>(true);
            if (interactable != null)
            {
                return interactable;
            }
        }

        return this;
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        if (cachedColliders == null)
        {
            return;
        }

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            if (cachedColliders[i] != null)
            {
                if (cachedColliders[i].CompareTag("Code"))
                {
                    continue;
                }

                cachedColliders[i].enabled = isEnabled;
            }
        }
    }

    private void PlaySfx(SfxCue cue)
    {
        if (cue == null)
        {
            return;
        }

        if (sfxEmitter == null)
        {
            sfxEmitter = GetComponent<SfxEmitter>();
            if (sfxEmitter == null)
            {
                sfxEmitter = gameObject.AddComponent<SfxEmitter>();
            }
        }

        sfxEmitter.Play(cue);
    }
}
