using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TutorialHintIconType
{
    MouseLook,
    RotateAD,
    Tab,
    Eye,
    CloseE,
    StartButton,
    MistakeCounter,
    Punishment,
    Click,
    Wheel,
    UV,
    Scan,
    Sort,
    Generic
}

[DisallowMultipleComponent]
public class TutorialHintSystem : MonoBehaviour
{
    public static TutorialHintSystem Instance { get; private set; }

    private const string HintMouseLook = "HINT_MOUSE_LOOK";
    private const string HintRotateAd = "HINT_ROTATE_AD";
    private const string HintOpenInventory = "HINT_OPEN_INVENTORY";
    private const string HintCloseInventory = "HINT_CLOSE_INVENTORY";
    private const string HintTakeTablet = "HINT_TAKE_TABLET";
    private const string HintTabletMainTabs = "HINT_TABLET_MAIN_TABS";
    private const string HintVisitTabletTabs = "HINT_VISIT_TABLET_TABS";
    private const string HintStartShift = "HINT_START_SHIFT";
    private const string HintTakeFirstItem = "HINT_TAKE_FIRST_ITEM";
    private const string HintInspectionOverview = "HINT_INSPECTION_OVERVIEW";
    private const string HintInspectionZoom = "HINT_INSPECTION_ZOOM";
    private const string HintUseUv = "HINT_USE_UV";
    private const string HintUseScanner = "HINT_USE_SCANNER";
    private const string HintMarkersOverview = "HINT_MARKERS_OVERVIEW";
    private const string HintPressSortButton = "HINT_PRESS_SORT_BUTTON";
    private const string HintFirstMistake = "HINT_FIRST_MISTAKE";
    private const string HintPunishmentEnd = "HINT_PUNISHMENT_END";
    private const string PlayerPrefsPrefix = "Sorter.TutorialHint.";

    private static readonly string[] AllHintIds =
    {
        HintMouseLook,
        HintRotateAd,
        HintOpenInventory,
        HintCloseInventory,
        HintTakeTablet,
        HintTabletMainTabs,
        HintVisitTabletTabs,
        HintStartShift,
        HintTakeFirstItem,
        HintInspectionOverview,
        HintInspectionZoom,
        HintUseUv,
        HintUseScanner,
        HintMarkersOverview,
        HintPressSortButton,
        HintFirstMistake,
        HintPunishmentEnd
    };

    [Header("References")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform startShiftButtonTarget;
    [SerializeField] private RectTransform markerTabTarget;
    [SerializeField] private RectTransform infoTabTarget;
    [SerializeField] private RectTransform bestiaryTabTarget;
    [SerializeField] private RectTransform homeButtonTarget;
    [SerializeField] private Transform tabletWorldTarget;
    [SerializeField] private Transform tableUVFlashlightTarget;
    [SerializeField] private RectTransform uvButtonTarget;
    [SerializeField] private Transform tableScannerTarget;
    [SerializeField] private RectTransform scannerButtonTarget;
    [SerializeField] private RectTransform markerPanelTarget;
    [SerializeField] private Transform sortWorldButtonTarget;
    [SerializeField] private RectTransform sortUiButtonTarget;

    [Header("Timing")]
    [SerializeField] private bool showOnlyOnce = true;
    [SerializeField] private bool persistHintsInPlayerPrefs;
    [SerializeField] private float passiveHintDuration = 10f;
    [SerializeField] private float inspectionOverviewDuration = 10f;

    [Header("Visuals")]
    [SerializeField] private Vector2 screenOffset = new Vector2(28f, 34f);
    [SerializeField] private Vector2 inspectionScreenOffset = new Vector2(28f, 190f);
    [SerializeField] private Color panelColor = new Color(0.04f, 0.04f, 0.045f, 0.88f);
    [SerializeField] private Color accentColor = new Color(1f, 0.78f, 0.22f, 1f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.22f, 0.14f, 1f);
    [SerializeField] private float outlineWidth = 5f;
    [SerializeField] private bool enableDebugLogs;

    private readonly HashSet<string> shownHints = new HashSet<string>();
    private readonly HashSet<string> warnedMissingTargets = new HashSet<string>();

    private TutorialHintView view;
    private TutorialWorldHighlight highlighter;
    private GameManager gameManager;
    private PlayerView playerView;
    private InventoryUIController inventoryController;
    private TabletInteractable tabletInteractable;
    private CutscenePlaybackManager cutscenePlaybackManager;
    private ItemMarkerUI itemMarkerUI;
    private TableFlashlight tableFlashlight;
    private TableScaner tableScaner;

    private string currentHintId;
    private string currentHighlightToken;
    private bool tutorialSkipped;
    private bool referencesResolved;
    private bool pendingFirstMistakeHint;
    private bool pendingPunishmentHint;
    private bool visitedInfoTab;
    private bool visitedBestiaryTab;
    private bool visitedMarkersTab;
    private bool firstItemTutorialStarted;
    private bool firstItemTutorialCompleted;
    private bool firstItemTimerPaused;
    private bool firstItemInspectionStarted;
    private bool zoomWheelForwardUsed;
    private bool zoomWheelBackwardUsed;
    private bool uvActivatedOnce;
    private bool uvDeactivatedAfterUse;
    private bool scannerActivatedOnce;
    private bool scannerDeactivatedAfterUse;
    private bool markerClickedOnce;
    private bool sortPressedOnce;
    private int baselineMistakes;
    private int baselinePunishments;
    private int sequenceStep;
    private float mouseLookInputSeconds;
    private float currentHintEndTime = -1f;
    private GameObject firstTutorialItem;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        LoadShownHints();
    }

    private void Start()
    {
        ResolveReferences();
        EnsureView();

        if (gameManager != null)
        {
            baselineMistakes = gameManager.currentMistakes;
            baselinePunishments = gameManager.HandPunishmentsApplied;
        }
    }

    private void Update()
    {
        if (tutorialSkipped || !referencesResolved)
        {
            UpdateTutorialItemTimerPause();
            return;
        }

        TrackTabletProgress();
        TrackReactiveHints();
        UpdateTutorialItemTimerPause();

        if (ShouldSuppressHints())
        {
            return;
        }

        RefreshDynamicHighlight();
        UpdateCurrentHintCompletion();

        if (!string.IsNullOrEmpty(currentHintId))
        {
            return;
        }

        TryShowPendingReactiveHint();
        if (!string.IsNullOrEmpty(currentHintId))
        {
            return;
        }

        TryAdvancePrimarySequence();
    }

    public void NotifyItemInspectionStarted(GameObject inspectedItem)
    {
        if (!firstItemTutorialStarted || firstItemTutorialCompleted)
        {
            return;
        }

        if (firstTutorialItem == null && inspectedItem != null)
        {
            firstTutorialItem = inspectedItem;
        }

        if (IsFirstTutorialItem(inspectedItem))
        {
            firstItemInspectionStarted = true;
        }
    }

    public void NotifyUVActiveChanged(bool isActive)
    {
        if (!firstItemTutorialStarted || firstItemTutorialCompleted)
        {
            return;
        }

        if (isActive)
        {
            uvActivatedOnce = true;
            return;
        }

        if (uvActivatedOnce)
        {
            uvDeactivatedAfterUse = true;
        }
    }

    public void NotifyInspectionZoomInput(float scrollValue)
    {
        if (!firstItemTutorialStarted || firstItemTutorialCompleted || Mathf.Abs(scrollValue) <= 0.001f)
        {
            return;
        }

        if (scrollValue > 0f)
        {
            zoomWheelForwardUsed = true;
        }
        else
        {
            zoomWheelBackwardUsed = true;
        }
    }

    public void NotifyScannerActiveChanged(bool isActive)
    {
        if (!firstItemTutorialStarted || firstItemTutorialCompleted)
        {
            return;
        }

        if (isActive)
        {
            scannerActivatedOnce = true;
            return;
        }

        if (scannerActivatedOnce)
        {
            scannerDeactivatedAfterUse = true;
        }
    }

    public void NotifyMarkerClicked()
    {
        if (!firstItemTutorialStarted || firstItemTutorialCompleted)
        {
            return;
        }

        markerClickedOnce = true;
    }

    public void NotifySortButtonPressed()
    {
        if (!firstItemTutorialStarted || firstItemTutorialCompleted)
        {
            return;
        }

        sortPressedOnce = true;
        firstItemTutorialCompleted = true;
        ReleaseTutorialItemTimerPause();
    }

    public void ShowHint(
        string id,
        string text,
        TutorialHintIconType iconType,
        float duration = -1f,
        Transform worldTarget = null,
        Color? highlightColor = null)
    {
        if (string.IsNullOrWhiteSpace(id) || tutorialSkipped)
        {
            return;
        }

        if (showOnlyOnce && HasShown(id))
        {
            return;
        }

        EnsureView();
        if (view == null)
        {
            WarnOnce("view", "Tutorial hint skipped because no suitable Canvas was found.");
            return;
        }

        ClearCurrentHighlight();
        currentHintId = id;
        currentHighlightToken = null;
        duration = ResolveHintDuration(id, duration);
        currentHintEndTime = duration > 0f ? Time.unscaledTime + duration : -1f;
        Color color = highlightColor ?? accentColor;

        view.SetScreenOffset(IsInspectionHint(id) ? inspectionScreenOffset : screenOffset);
        view.Show(text, GetIconLabel(iconType), color);
        TriggerHintMotion(id);

        if (worldTarget != null && highlighter != null)
        {
            highlighter.HighlightWorldTarget(worldTarget, color, outlineWidth);
        }

        Log($"Show {id}");
    }

    public void CompleteHint(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        MarkShown(id);

        if (currentHintId == id)
        {
            currentHintId = null;
            currentHighlightToken = null;
            currentHintEndTime = -1f;
            ClearCurrentHighlight();
            if (view != null)
            {
                view.Hide();
            }
        }
    }

    public bool HasShown(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return shownHints.Contains(id) ||
               (persistHintsInPlayerPrefs && PlayerPrefs.GetInt(PlayerPrefsPrefix + id, 0) == 1);
    }

    public void SuppressCurrent()
    {
        if (!string.IsNullOrEmpty(currentHintId))
        {
            CompleteHint(currentHintId);
        }
    }

    private void SkipTutorial()
    {
        tutorialSkipped = true;
        for (int i = 0; i < AllHintIds.Length; i++)
        {
            MarkShown(AllHintIds[i]);
        }

        firstItemTutorialCompleted = true;
        ReleaseTutorialItemTimerPause();
        currentHintId = null;
        currentHighlightToken = null;
        currentHintEndTime = -1f;
        ClearCurrentHighlight();
        if (view != null)
        {
            view.Hide();
        }

        Log("Tutorial skipped");
    }

    private void TryAdvancePrimarySequence()
    {
        if (gameManager != null && gameManager.isGameStarted && sequenceStep < 8)
        {
            sequenceStep = 8;
        }

        if (gameManager == null || !gameManager.isGameStarted)
        {
            TryAdvancePreShiftSequence();
            return;
        }

        TryAdvanceFirstItemSequence();
    }

    private void TryAdvancePreShiftSequence()
    {
        switch (sequenceStep)
        {
            case 0:
                if (CanShowControlHint())
                {
                    ShowHint(HintMouseLook, "Осмотритесь с помощью мыши", TutorialHintIconType.MouseLook);
                    sequenceStep = 1;
                }
                break;
            case 1:
                ShowHint(HintRotateAd, "Повернитесь в другую сторону клавишами A и D", TutorialHintIconType.RotateAD);
                sequenceStep = 2;
                break;
            case 2:
                ShowHint(HintOpenInventory, "Откройте инвентарь клавишей TAB", TutorialHintIconType.Tab);
                sequenceStep = 3;
                break;
            case 3:
                ShowHint(HintCloseInventory, "Закройте инвентарь клавишей TAB", TutorialHintIconType.Tab);
                sequenceStep = 4;
                break;
            case 4:
                ShowHint(HintTakeTablet, "Возьмите планшет со стола", TutorialHintIconType.MouseLook);
                HighlightWorldTarget(tabletWorldTarget, HintTakeTablet);
                sequenceStep = 5;
                break;
            case 5:
                if (!IsTabletOpen())
                {
                    break;
                }

                ShowHint(HintTabletMainTabs, "Изучите вкладки планшета", TutorialHintIconType.Eye);
                HighlightMainTabletTabs();
                sequenceStep = 6;
                break;
            case 6:
                if (!IsTabletOpen())
                {
                    break;
                }

                ShowHint(HintVisitTabletTabs, "Откройте каждую вкладку и изучите информацию", TutorialHintIconType.Eye);
                RefreshVisitTabsHighlight(true);
                sequenceStep = 7;
                break;
            case 7:
                if (!IsTabletOpen() || !HaveVisitedAllTabletTabs() || !IsTabletHomeOpen())
                {
                    break;
                }

                ShowHint(HintStartShift, "Теперь можно начать смену", TutorialHintIconType.StartButton);
                HighlightUiTarget(startShiftButtonTarget, HintStartShift);
                sequenceStep = 8;
                break;
        }
    }

    private void TryAdvanceFirstItemSequence()
    {
        if (firstItemTutorialCompleted || HasShown(HintPressSortButton))
        {
            firstItemTutorialCompleted = true;
            ReleaseTutorialItemTimerPause();
            return;
        }

        if (gameManager.currentItem == null && !HasInspectedItem())
        {
            return;
        }

        if (!firstItemTutorialStarted)
        {
            firstItemTutorialStarted = true;
            firstTutorialItem = gameManager.currentItem;
            PauseTutorialItemTimer();
        }

        switch (sequenceStep)
        {
            case 8:
                if (!HasInspectedItem())
                {
                    ShowHint(HintTakeFirstItem, "Возьмите предмет: нажмите по нему ЛКМ", TutorialHintIconType.Click);
                    HighlightCurrentItem();
                    sequenceStep = 9;
                    break;
                }

                sequenceStep = 9;
                break;
            case 9:
                if (!HasInspectedItem())
                {
                    sequenceStep = 8;
                    break;
                }

                ShowHint(HintInspectionOverview, "Это режим осмотра. Проверьте предмет инструментами и отметьте найденные признаки", TutorialHintIconType.Eye, inspectionOverviewDuration);
                sequenceStep = 10;
                break;
            case 10:
                if (!HasInspectedItem())
                {
                    sequenceStep = 8;
                    break;
                }

                if (!HasShown(HintInspectionZoom))
                {
                    ShowHint(HintInspectionZoom, "Приближайте и отдаляйте предмет колесиком мыши", TutorialHintIconType.Wheel);
                    break;
                }

                ShowHint(HintUseUv, "Проверьте предмет ультрафиолетом. Возьмите фонарик на столе или нажмите кнопку справа", TutorialHintIconType.UV);
                HighlightUvTargets(true);
                sequenceStep = 11;
                break;
            case 11:
                if (!HasInspectedItem())
                {
                    sequenceStep = 8;
                    break;
                }

                ShowHint(HintUseScanner, "Теперь проверьте предмет сканером. Возьмите сканер на столе или нажмите кнопку слева", TutorialHintIconType.Scan);
                HighlightScannerTargets(true);
                sequenceStep = 12;
                break;
            case 12:
                if (!HasInspectedItem())
                {
                    sequenceStep = 8;
                    break;
                }

                ShowHint(HintMarkersOverview, "Снизу находится список маркеров. Отмечайте признаки, которые нашли при осмотре", TutorialHintIconType.MistakeCounter);
                HighlightMarkerPanel();
                sequenceStep = 13;
                break;
            case 13:
                if (!HasInspectedItem() && !markerClickedOnce)
                {
                    sequenceStep = 8;
                    break;
                }

                ShowHint(HintPressSortButton, "Когда закончите проверку, нажмите кнопку сортировки", TutorialHintIconType.Sort);
                HighlightSortTargets(true);
                sequenceStep = 14;
                break;
            case 14:
                if (sortPressedOnce || gameManager.currentItem == null)
                {
                    firstItemTutorialCompleted = true;
                    ReleaseTutorialItemTimerPause();
                    CompleteHint(HintPressSortButton);
                    sequenceStep = 15;
                }
                break;
        }
    }

    private void TrackReactiveHints()
    {
        if (gameManager == null)
        {
            return;
        }

        if (!HasShown(HintFirstMistake) && gameManager.currentMistakes > baselineMistakes)
        {
            pendingFirstMistakeHint = true;
        }

        if (!HasShown(HintPunishmentEnd) && gameManager.HandPunishmentsApplied > baselinePunishments)
        {
            pendingPunishmentHint = true;
        }
    }

    private void TryShowPendingReactiveHint()
    {
        if (pendingPunishmentHint && !HasShown(HintPunishmentEnd))
        {
            pendingPunishmentHint = false;
            ShowHint(HintPunishmentEnd, "Ничто не прощается. Будьте внимательнее", TutorialHintIconType.Punishment, -1f, null, dangerColor);
            return;
        }

        if (pendingFirstMistakeHint && !HasShown(HintFirstMistake))
        {
            pendingFirstMistakeHint = false;
            ShowHint(HintFirstMistake, "Следите за счетчиком ошибок. Если он заполнится, ничего хорошего не будет", TutorialHintIconType.MistakeCounter);
        }
    }

    private void UpdateCurrentHintCompletion()
    {
        if (string.IsNullOrEmpty(currentHintId))
        {
            return;
        }

        if (IsCurrentHintActionCompleted())
        {
            CompleteHint(currentHintId);
        }
    }

    private bool IsCurrentHintActionCompleted()
    {
        switch (currentHintId)
        {
            case HintMouseLook:
                if (Mathf.Abs(Input.GetAxisRaw("Mouse X")) + Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.15f)
                {
                    mouseLookInputSeconds += Time.unscaledDeltaTime;
                }
                return mouseLookInputSeconds >= 0.2f;
            case HintRotateAd:
                return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D);
            case HintOpenInventory:
                return inventoryController != null && inventoryController.IsInventoryOpen;
            case HintCloseInventory:
                return inventoryController != null && !inventoryController.IsInventoryOpen;
            case HintTakeTablet:
                return IsTabletOpen();
            case HintTabletMainTabs:
                return visitedInfoTab || visitedBestiaryTab || visitedMarkersTab;
            case HintVisitTabletTabs:
                return HaveVisitedAllTabletTabs() && IsTabletHomeOpen();
            case HintStartShift:
                return gameManager != null && gameManager.isGameStarted;
            case HintTakeFirstItem:
                return HasInspectedItem();
            case HintInspectionOverview:
                return currentHintEndTime > 0f && Time.unscaledTime >= currentHintEndTime;
            case HintInspectionZoom:
                return (zoomWheelForwardUsed && zoomWheelBackwardUsed) ||
                       (currentHintEndTime > 0f && Time.unscaledTime >= currentHintEndTime);
            case HintUseUv:
                return uvActivatedOnce && uvDeactivatedAfterUse;
            case HintUseScanner:
                return scannerActivatedOnce && scannerDeactivatedAfterUse;
            case HintMarkersOverview:
                return markerClickedOnce;
            case HintPressSortButton:
                return sortPressedOnce || gameManager == null || gameManager.currentItem == null;
            default:
                return IsPassiveHint(currentHintId) && currentHintEndTime > 0f && Time.unscaledTime >= currentHintEndTime;
        }
    }

    private float ResolveHintDuration(string id, float requestedDuration)
    {
        if (requestedDuration > 0f)
        {
            return requestedDuration;
        }

        return IsPassiveHint(id) ? Mathf.Max(0.1f, passiveHintDuration) : requestedDuration;
    }

    private bool IsPassiveHint(string id)
    {
        return id == HintInspectionOverview ||
               id == HintInspectionZoom ||
               id == HintFirstMistake ||
               id == HintPunishmentEnd;
    }

    private bool IsInspectionHint(string id)
    {
        return id == HintInspectionOverview ||
               id == HintInspectionZoom ||
               id == HintUseUv ||
               id == HintUseScanner ||
               id == HintMarkersOverview ||
               id == HintPressSortButton;
    }

    private void TrackTabletProgress()
    {
        if (!IsTabletOpen())
        {
            return;
        }

        string header = tabletInteractable.CurrentHeader;
        if (string.IsNullOrWhiteSpace(header))
        {
            return;
        }

        string normalized = header.Trim().ToLowerInvariant();
        if (normalized.Contains("info"))
        {
            visitedInfoTab = true;
        }
        else if (normalized.Contains("bestiary"))
        {
            visitedBestiaryTab = true;
        }
        else if (normalized.Contains("marks") || normalized.Contains("marker"))
        {
            visitedMarkersTab = true;
        }
    }

    private bool CanShowControlHint()
    {
        return playerView == null || (playerView.canLook && playerView.canRotate);
    }

    private bool IsTabletOpen()
    {
        return tabletInteractable != null && tabletInteractable.IsOpen;
    }

    private bool IsTabletHomeOpen()
    {
        return tabletInteractable != null && tabletInteractable.IsHomePageActive;
    }

    private bool HaveVisitedAllTabletTabs()
    {
        return visitedInfoTab && visitedBestiaryTab && visitedMarkersTab;
    }

    private bool HasInspectedItem()
    {
        return firstItemInspectionStarted ||
               (PlayerHeldItem.Instance != null && PlayerHeldItem.Instance.HasItem);
    }

    private bool IsFirstTutorialItem(GameObject inspectedItem)
    {
        if (inspectedItem == null)
        {
            return false;
        }

        return firstTutorialItem == null ||
               inspectedItem == firstTutorialItem ||
               inspectedItem.transform.IsChildOf(firstTutorialItem.transform) ||
               firstTutorialItem.transform.IsChildOf(inspectedItem.transform);
    }

    private bool ShouldSuppressHints()
    {
        if (TrashBinInteractable.IsTrashUiOpen || VentHandInteractable.IsCraftUiOpen)
        {
            return true;
        }

        if (gameManager != null && (gameManager.IsStoryInteractionLocked || gameManager.IsGameOverStarted))
        {
            return true;
        }

        if (cutscenePlaybackManager != null && cutscenePlaybackManager.IsPlaying)
        {
            return true;
        }

        return playerView != null && playerView.pauseMenuUI != null && playerView.pauseMenuUI.activeInHierarchy;
    }

    private void TriggerHintMotion(string id)
    {
        if (playerView == null)
        {
            return;
        }

        if (id == HintMouseLook)
        {
            mouseLookInputSeconds = 0f;
            playerView.PlayTutorialMouseLookHint();
        }
        else if (id == HintRotateAd)
        {
            playerView.PlayTutorialRotateHint();
        }
    }

    private void RefreshDynamicHighlight()
    {
        if (currentHintId == HintVisitTabletTabs)
        {
            RefreshVisitTabsHighlight(false);
        }
        else if (currentHintId == HintTakeFirstItem)
        {
            HighlightCurrentItem(false);
        }
        else if (currentHintId == HintUseUv)
        {
            HighlightUvTargets(false);
        }
        else if (currentHintId == HintUseScanner)
        {
            HighlightScannerTargets(false);
        }
        else if (currentHintId == HintPressSortButton)
        {
            HighlightSortTargets(false);
        }
    }

    private void HighlightMainTabletTabs()
    {
        Component[] targets = FilterValidTargets(infoTabTarget, bestiaryTabTarget, markerTabTarget);
        if (targets.Length == 0)
        {
            WarnOnce(HintTabletMainTabs, "Tablet main tab targets were not found. Showing hint without highlight.");
            return;
        }

        HighlightUiTargets(targets, HintTabletMainTabs, "main-tabs");
    }

    private void RefreshVisitTabsHighlight(bool force)
    {
        if (!IsTabletOpen())
        {
            return;
        }

        if (!IsTabletHomeOpen())
        {
            HighlightUiTargets(FilterValidTargets(homeButtonTarget), HintVisitTabletTabs, "home", force);
            return;
        }

        List<Component> targets = new List<Component>(3);
        if (!visitedInfoTab && infoTabTarget != null)
        {
            targets.Add(infoTabTarget);
        }

        if (!visitedBestiaryTab && bestiaryTabTarget != null)
        {
            targets.Add(bestiaryTabTarget);
        }

        if (!visitedMarkersTab && markerTabTarget != null)
        {
            targets.Add(markerTabTarget);
        }

        if (targets.Count == 0)
        {
            HighlightUiTargets(FilterValidTargets(homeButtonTarget), HintVisitTabletTabs, "home-after-tabs", force);
            return;
        }

        HighlightUiTargets(targets.ToArray(), HintVisitTabletTabs, "unvisited-tabs", force);
    }

    private void HighlightCurrentItem(bool force = true)
    {
        if (!force && currentHighlightToken == "current-item")
        {
            return;
        }

        Transform target = gameManager != null && gameManager.currentItem != null
            ? gameManager.currentItem.transform
            : null;

        if (target == null)
        {
            WarnOnce(HintTakeFirstItem, "Tutorial current item target was not found yet. Waiting without highlight.");
            return;
        }

        if (highlighter != null)
        {
            highlighter.HighlightWorldTarget(target, accentColor, outlineWidth);
            currentHighlightToken = "current-item";
        }
    }

    private void HighlightUvTargets(bool force)
    {
        if (!force && currentHighlightToken == "uv")
        {
            return;
        }

        Transform[] worldTargets = FilterValidTransforms(tableUVFlashlightTarget);
        Component[] uiTargets = FilterValidTargets(uvButtonTarget);
        WarnMissingMixedTargets(HintUseUv, worldTargets.Length, uiTargets.Length, "UV flashlight");

        if (highlighter != null)
        {
            highlighter.HighlightMixedTargets(worldTargets, uiTargets, accentColor, outlineWidth);
            currentHighlightToken = "uv";
        }
    }

    private void HighlightScannerTargets(bool force)
    {
        if (!force && currentHighlightToken == "scanner")
        {
            return;
        }

        Transform[] worldTargets = FilterValidTransforms(tableScannerTarget);
        Component[] uiTargets = FilterValidTargets(scannerButtonTarget);
        WarnMissingMixedTargets(HintUseScanner, worldTargets.Length, uiTargets.Length, "scanner");

        if (highlighter != null)
        {
            highlighter.HighlightMixedTargets(worldTargets, uiTargets, accentColor, outlineWidth);
            currentHighlightToken = "scanner";
        }
    }

    private void HighlightMarkerPanel()
    {
        Component[] targets = FilterValidTargets(markerPanelTarget);
        if (targets.Length == 0)
        {
            WarnOnce(HintMarkersOverview, "Marker panel target was not found. Showing hint without highlight.");
            return;
        }

        HighlightUiTargets(targets, HintMarkersOverview, "marker-panel");
    }

    private void HighlightSortTargets(bool force)
    {
        if (!force && currentHighlightToken == "sort")
        {
            return;
        }

        Transform[] worldTargets = FilterValidTransforms(sortWorldButtonTarget);
        Component[] uiTargets = FilterValidTargets(sortUiButtonTarget);
        WarnMissingMixedTargets(HintPressSortButton, worldTargets.Length, uiTargets.Length, "sort button");

        if (highlighter != null)
        {
            highlighter.HighlightMixedTargets(worldTargets, uiTargets, accentColor, outlineWidth);
            currentHighlightToken = "sort";
        }
    }

    private void WarnMissingMixedTargets(string warningKey, int worldCount, int uiCount, string label)
    {
        if (worldCount > 0 || uiCount > 0)
        {
            return;
        }

        WarnOnce(warningKey, $"Tutorial {label} target was not found. Showing hint without highlight.");
    }

    private void HighlightUiTarget(Component target, string warningKey)
    {
        if (target == null)
        {
            WarnOnce(warningKey, $"Tutorial hint target '{warningKey}' was not found. Showing hint without highlight.");
            return;
        }

        HighlightUiTargets(new[] { target }, warningKey, warningKey);
    }

    private void HighlightUiTargets(Component[] targets, string warningKey, string token, bool force = true)
    {
        if (!force && currentHighlightToken == token)
        {
            return;
        }

        Component[] validTargets = FilterValidTargets(targets);
        if (validTargets.Length == 0)
        {
            WarnOnce(warningKey, $"Tutorial hint target '{warningKey}' was not found. Showing hint without highlight.");
            ClearCurrentHighlight();
            currentHighlightToken = token;
            return;
        }

        if (highlighter != null)
        {
            highlighter.HighlightUiTargets(validTargets, accentColor);
            currentHighlightToken = token;
        }
    }

    private Component[] FilterValidTargets(params Component[] targets)
    {
        List<Component> result = new List<Component>();
        if (targets == null)
        {
            return result.ToArray();
        }

        for (int i = 0; i < targets.Length; i++)
        {
            Component target = targets[i];
            if (target != null && target.transform is RectTransform)
            {
                result.Add(target);
            }
        }

        return result.ToArray();
    }

    private Transform[] FilterValidTransforms(params Transform[] targets)
    {
        List<Transform> result = new List<Transform>();
        if (targets == null)
        {
            return result.ToArray();
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                result.Add(targets[i]);
            }
        }

        return result.ToArray();
    }

    private void HighlightWorldTarget(Transform target, string warningKey)
    {
        if (target == null)
        {
            WarnOnce(warningKey, $"Tutorial hint world target '{warningKey}' was not found. Showing hint without highlight.");
            return;
        }

        if (highlighter != null)
        {
            highlighter.HighlightWorldTarget(target, accentColor, outlineWidth);
        }
    }

    private void ClearCurrentHighlight()
    {
        if (highlighter != null)
        {
            highlighter.Clear();
        }
    }

    private void PauseTutorialItemTimer()
    {
        if (firstItemTimerPaused || gameManager == null)
        {
            return;
        }

        gameManager.SetTimerPausedForStory(true);
        firstItemTimerPaused = true;
    }

    private void ReleaseTutorialItemTimerPause()
    {
        if (!firstItemTimerPaused || gameManager == null)
        {
            return;
        }

        gameManager.SetTimerPausedForStory(false);
        firstItemTimerPaused = false;
    }

    private void UpdateTutorialItemTimerPause()
    {
        if (!firstItemTimerPaused)
        {
            return;
        }

        if (tutorialSkipped || firstItemTutorialCompleted || gameManager == null || gameManager.currentItem == null)
        {
            ReleaseTutorialItemTimerPause();
        }
    }

    private void ResolveReferences()
    {
        if (referencesResolved)
        {
            return;
        }

        gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
        playerView = PlayerView.Instance != null ? PlayerView.Instance : FindFirstObjectByType<PlayerView>();
        inventoryController = FindFirstObjectByType<InventoryUIController>();
        tabletInteractable = TabletInteractable.Instance != null ? TabletInteractable.Instance : FindFirstObjectByType<TabletInteractable>();
        cutscenePlaybackManager = CutscenePlaybackManager.Instance != null ? CutscenePlaybackManager.Instance : FindFirstObjectByType<CutscenePlaybackManager>();
        itemMarkerUI = FindFirstObjectByType<ItemMarkerUI>();
        tableFlashlight = FindFirstObjectByType<TableFlashlight>();
        tableScaner = FindFirstObjectByType<TableScaner>();

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (targetCanvas == null)
        {
            targetCanvas = ResolveCanvas(transforms);
        }

        if (startShiftButtonTarget == null)
        {
            startShiftButtonTarget = ResolveRectTransformByName(transforms, "StartBtn");
        }

        if (infoTabTarget == null)
        {
            infoTabTarget = ResolveRectTransformByName(transforms, "ButtonInfo");
        }

        if (bestiaryTabTarget == null)
        {
            bestiaryTabTarget = ResolveRectTransformByName(transforms, "ButtonBisteary");
        }

        if (markerTabTarget == null || markerTabTarget.name == "MarkerPage")
        {
            markerTabTarget = ResolveSiblingRectTransformByName(infoTabTarget, "ButtonMark") ??
                              ResolveSiblingRectTransformByName(bestiaryTabTarget, "ButtonMark") ??
                              ResolveRectTransformByName(transforms, "ButtonMark");
        }

        if (homeButtonTarget == null)
        {
            homeButtonTarget = ResolveRectTransformByName(transforms, "homeBtn");
        }

        if (tabletWorldTarget == null && tabletInteractable != null)
        {
            tabletWorldTarget = tabletInteractable.transform;
        }

        if (tableUVFlashlightTarget == null && tableFlashlight != null)
        {
            tableUVFlashlightTarget = tableFlashlight.transform;
        }

        if (tableScannerTarget == null && tableScaner != null)
        {
            tableScannerTarget = tableScaner.transform;
        }

        if (markerPanelTarget == null && itemMarkerUI != null)
        {
            markerPanelTarget = itemMarkerUI.transform as RectTransform;
        }

        if (sortUiButtonTarget == null)
        {
            sortUiButtonTarget = ResolveRectTransformByName(transforms, "SortButton");
        }

        if (sortWorldButtonTarget == null)
        {
            sortWorldButtonTarget = ResolveSubmitButtonTransform();
        }

        referencesResolved = true;
    }

    private Canvas ResolveCanvas(Transform[] transforms)
    {
        Canvas fallback = null;

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null)
            {
                continue;
            }

            Canvas canvas = candidate.GetComponent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                continue;
            }

            if (candidate.name == "InventoryCanvas")
            {
                return canvas;
            }

            if (fallback == null)
            {
                fallback = canvas;
            }
        }

        return fallback;
    }

    private RectTransform ResolveRectTransformByName(Transform[] transforms, string targetName)
    {
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate != null && candidate.name == targetName)
            {
                return candidate as RectTransform;
            }
        }

        return null;
    }

    private RectTransform ResolveSiblingRectTransformByName(RectTransform sibling, string targetName)
    {
        if (sibling == null || sibling.parent == null)
        {
            return null;
        }

        for (int i = 0; i < sibling.parent.childCount; i++)
        {
            Transform child = sibling.parent.GetChild(i);
            if (child != null && child.name == targetName)
            {
                return child as RectTransform;
            }
        }

        return null;
    }

    private Transform ResolveSubmitButtonTransform()
    {
        SubmitItemInteractable[] submitButtons = FindObjectsByType<SubmitItemInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        SubmitItemInteractable fallback = null;

        for (int i = 0; i < submitButtons.Length; i++)
        {
            SubmitItemInteractable submitButton = submitButtons[i];
            if (submitButton == null)
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = submitButton;
            }
            string objectName = submitButton.gameObject.name;
            if (objectName.IndexOf("red_buttom", StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("red_button", StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("sort", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return submitButton.transform;
            }
        }

        return fallback != null ? fallback.transform : null;
    }

    private void EnsureView()
    {
        if (view != null)
        {
            return;
        }

        if (targetCanvas == null)
        {
            ResolveReferences();
        }

        if (targetCanvas == null)
        {
            return;
        }

        highlighter = gameObject.GetComponent<TutorialWorldHighlight>();
        if (highlighter == null)
        {
            highlighter = gameObject.AddComponent<TutorialWorldHighlight>();
        }

        GameObject viewObject = new GameObject("TutorialHintView", typeof(RectTransform));
        view = viewObject.AddComponent<TutorialHintView>();
        view.Initialize(targetCanvas, panelColor, accentColor, screenOffset, SkipTutorial);
    }

    private void LoadShownHints()
    {
        shownHints.Clear();

        if (!persistHintsInPlayerPrefs)
        {
            return;
        }

        for (int i = 0; i < AllHintIds.Length; i++)
        {
            string id = AllHintIds[i];
            if (PlayerPrefs.GetInt(PlayerPrefsPrefix + id, 0) == 1)
            {
                shownHints.Add(id);
            }
        }
    }

    private void MarkShown(string id)
    {
        if (shownHints.Add(id) && persistHintsInPlayerPrefs)
        {
            PlayerPrefs.SetInt(PlayerPrefsPrefix + id, 1);
        }
    }

    private string GetIconLabel(TutorialHintIconType iconType)
    {
        switch (iconType)
        {
            case TutorialHintIconType.MouseLook:
                return "MOUSE";
            case TutorialHintIconType.RotateAD:
                return "A  D";
            case TutorialHintIconType.Tab:
                return "TAB";
            case TutorialHintIconType.Eye:
                return "EYE";
            case TutorialHintIconType.CloseE:
                return "E";
            case TutorialHintIconType.StartButton:
                return "START";
            case TutorialHintIconType.MistakeCounter:
                return "0/10";
            case TutorialHintIconType.Punishment:
                return "!";
            case TutorialHintIconType.Click:
                return "CLICK";
            case TutorialHintIconType.Wheel:
                return "WHEEL";
            case TutorialHintIconType.UV:
                return "UV";
            case TutorialHintIconType.Scan:
                return "SCAN";
            case TutorialHintIconType.Sort:
                return "SORT";
            default:
                return "?";
        }
    }

    private void WarnOnce(string key, string message)
    {
        if (!warnedMissingTargets.Add(key))
        {
            return;
        }

        Debug.LogWarning(message, this);
    }

    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"TutorialHintSystem: {message}", this);
        }
    }
}
