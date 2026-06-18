using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class VentHandDialogueLine
{
    [TextArea(2, 4)] public string text;
    public float holdTime = 1.6f;
    public AudioClip[] voiceBlips;
    public float blipInterval = 0.055f;
    public bool giveKeyAfterThisLine;
}

public class VentHandIntroController : MonoBehaviour
{
    public static VentHandIntroController Instance { get; private set; }
    private const float VentRotationViewYawOffset = 0f;
    private const float PanelRotationViewYawOffset = 180f;

    [Header("Scheduling")]
    [SerializeField] private float minIntroDelay = 15f;
    [SerializeField] private float maxIntroDelay = 30f;
    [SerializeField] private bool triggerOnlyAfterGameStarted = true;

    [Header("Scene References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ElectricPanelController electricPanelController;
    [SerializeField] private Conveyor conveyor;
    [SerializeField] private ConveyorExitController conveyorExitController;
    [SerializeField] private ItemSpawner itemSpawner;
    [SerializeField] private ToolCaseLock toolCaseLock;
    [SerializeField] private GameObject handObject;
    [SerializeField] private Collider[] handInteractionColliders;
    [SerializeField] private Transform hiddenPose;
    [SerializeField] private Transform introPose;
    [SerializeField] private Transform idlePose;
    [SerializeField] private Transform keyDropPoint;

    [Header("Vent")]
    [SerializeField] private Animator ventAnimator;
    [SerializeField] private string ventOpenStateName = "open";
    [SerializeField] private string ventCloseStateName = "close";
    [SerializeField] private string ventOpenTrigger;
    [SerializeField] private string ventCloseTrigger;
    [SerializeField] private float ventCrossFadeDuration = 0.2f;
    [SerializeField] private float ventOpenStartNormalizedTime = 0f;
    [SerializeField] private float ventCloseStartNormalizedTime = 0f;
    [SerializeField] private bool resetVentAnimatorBeforePlay = false;

    [Header("Hand Animation")]
    [SerializeField] private Animator handAnimator;
    [SerializeField] private string handAppearTrigger;
    [SerializeField] private string handIdleTrigger;
    [SerializeField] private string handGiveKeyTrigger;
    [SerializeField] private string handDisappearTrigger;
    [SerializeField] private string handCraftSuccessTrigger;
    [SerializeField] private string handCraftFailTrigger;
    [SerializeField] private float appearDuration = 1.25f;
    [SerializeField] private float moveToIdleDuration = 0.75f;
    [SerializeField] private float disappearDuration = 1f;
    [SerializeField] private AnimationCurve handMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool hideHandBeforeIntro = true;
    [SerializeField] private bool hideHandAfterIntro = true;

    [Header("Dialogue")]
    [SerializeField] private VentHandDialogueLine[] introLines =
    {
        new VentHandDialogueLine { text = "Тише. Я не охрана.", blipInterval = 0.05f },
        new VentHandDialogueLine { text = "Камеры ослепли ненадолго. Так что слушай.", blipInterval = 0.05f },
        new VentHandDialogueLine { text = "Хочешь выбраться — собирай детали. Я сделаю из них кое-что полезное.", blipInterval = 0.045f },
        new VentHandDialogueLine { text = "Но кради и разбирай только в темноте. При свете система бьёт без предупреждения.", blipInterval = 0.045f },
        new VentHandDialogueLine { text = "Щиток перегреется — дёрни рычаг. Пока питание лежит, камера ничего не увидит.", blipInterval = 0.045f },
        new VentHandDialogueLine { text = "Они ускоряют таймер с каждым предметом. Выжимают рабочих, пока те не сломаются.", blipInterval = 0.045f },
        new VentHandDialogueLine { text = "Держи ключ. Инструменты в кейсе. И не заставляй меня жалеть об этом.", blipInterval = 0.045f, giveKeyAfterThisLine = true }
    };
    [SerializeField] private string dialogueContactLabel = "НЕИЗВЕСТНЫЙ КОНТАКТ";
    [SerializeField] private string dialogueChannelLabel = "КАНАЛ 03 // ЗАШИФРОВАНО";
    [SerializeField] private string dialogueAdvanceHint = "CLICK / E / SPACE  //  ДАЛЕЕ";
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private bool dialogueStartsOnlyByHandClick = true;
    [SerializeField] private float handAutoTalkDelay = 0f;
    [SerializeField] private float characterDelay = 0.035f;
    [SerializeField] private AudioClip[] defaultVoiceBlips;
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private bool useFallbackOnGuiDialogue = true;
    [SerializeField, Range(0.35f, 0.95f)] private float fallbackDialogueWidth01 = 0.7f;
    [SerializeField] private float fallbackDialogueHeight = 150f;
    [SerializeField] private float fallbackDialogueBottomOffset = 55f;
    [SerializeField] private bool createRuntimeDialoguePanel = true;
    [SerializeField] private Color dialoguePanelColor = new Color(0.012f, 0.014f, 0.02f, 0.94f);
    [SerializeField] private Color dialogueAccentColor = new Color(1f, 0.78f, 0.22f, 1f);
    [SerializeField] private Color dialogueDangerColor = new Color(1f, 0.12f, 0.08f, 1f);

    [Header("Key")]
    [SerializeField] private GameObject keyPrefab;
    [SerializeField] private InventoryItemDefinition keyInventoryItem;
    [SerializeField] private Vector3 placeholderKeyScale = new Vector3(0.08f, 0.035f, 0.08f);
    [SerializeField] private float keyPickupFallbackRadius = 0.12f;
    [SerializeField] private bool addOutlineToPlaceholderKey = true;

    [Header("Future Craft Hook")]
    [SerializeField] private bool appearDuringRegularBlackout = true;
    [SerializeField] private bool enableCraftInteractionAfterIntro;

    [Header("Feedback Lights")]
    [SerializeField] private Light[] handArrivalLights;
    [SerializeField] private Light[] ventHandLights;
    [SerializeField] private Light[] handHighlightLights;
    [SerializeField] private Light[] handFeedbackLights;
    [SerializeField] private float feedbackLightFadeDuration = 0.4f;
    [SerializeField] private float feedbackLightIntensity = 2f;
    [SerializeField] private Color feedbackLightColor = Color.green;
    [SerializeField] private bool turnHandLightsOffBeforeIntro = true;
    [SerializeField] private bool restoreInitialLightStateAfterIntro = true;

    [Header("Input Lock")]
    [SerializeField] private bool createRuntimeUiInputBlocker = true;
    [SerializeField] private string runtimeUiInputBlockerName = "VentHandDialogueInputBlocker";
    [SerializeField] private int runtimeUiInputBlockerSortingOrder = 5000;

    [Header("SFX")]
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue ventOpenSfx;
    [SerializeField] private SfxCue ventCloseSfx;
    [SerializeField] private SfxCue handAppearSfx;
    [SerializeField] private SfxCue handMoveSfx;
    [SerializeField] private SfxCue handIdleSfx;
    [SerializeField] private SfxCue handGiveKeySfx;
    [SerializeField] private SfxCue handClickSfx;
    [SerializeField] private SfxCue dialogueStartSfx;
    [SerializeField] private SfxCue keyDropSfx;
    [SerializeField] private SfxCue keyPickupSfx;
    [SerializeField] private SfxCue storyBlackoutStartSfx;
    [SerializeField] private SfxCue storyBlackoutEndSfx;
    [SerializeField] private AudioClip ventOpenAudioClip;
    [SerializeField] private AudioClip ventCloseAudioClip;
    [SerializeField] private AudioClip handAppearAudioClip;
    [SerializeField] private AudioClip handMoveAudioClip;
    [SerializeField] private AudioClip handIdleAudioClip;
    [SerializeField] private AudioClip handGiveKeyAudioClip;
    [SerializeField] private AudioClip handClickAudioClip;
    [SerializeField] private AudioClip dialogueStartAudioClip;
    [SerializeField] private AudioClip keyDropAudioClip;
    [SerializeField] private AudioClip keyPickupAudioClip;
    [SerializeField] private AudioClip storyBlackoutStartAudioClip;
    [SerializeField] private AudioClip storyBlackoutEndAudioClip;

    private readonly List<LightSnapshot> feedbackLightSnapshots = new List<LightSnapshot>();
    private readonly HashSet<string> missingSfxWarnings = new HashSet<string>();
    private Coroutine scheduledIntroRoutine;
    private Coroutine activeIntroRoutine;
    private GameObject spawnedKey;
    private string fallbackDialogueText;
    private AudioSource fallbackAudioSource;
    private bool hasVentHandIntroStarted;
    private bool hasVentHandIntroCompleted;
    private bool hasKeyBeenDropped;
    private bool hasKeyBeenPickedUp;
    private bool isToolCaseUnlocked;
    private bool storyInteractionLockActive;
    private bool warnedMissingVentAnimator;
    private bool dialogueCanBeStarted;
    private bool dialogueRunning;
    private bool dialogueStartRequested;
    private bool dialogueAdvanceRequested;
    private int suppressDialogueInputUntilFrame;
    private GameObject runtimeUiInputBlocker;
    private GameObject runtimeDialogueCanvas;
    private Text runtimeDialogueText;
    private ElectricPanelController subscribedPanel;
    private Coroutine regularBlackoutHandRoutine;
    private bool isWaitingForKeyInventorySpace;

    private struct LightSnapshot
    {
        public Light Light;
        public bool Enabled;
        public float Intensity;
        public Color Color;
    }

    public bool IsIntroRunning => activeIntroRoutine != null;
    public bool IsIntroScheduled => scheduledIntroRoutine != null;
    public bool HasVentHandIntroStarted => hasVentHandIntroStarted;
    public bool HasVentHandIntroCompleted => hasVentHandIntroCompleted;
    public bool HasKeyBeenDropped => hasKeyBeenDropped;
    public bool HasKeyBeenPickedUp => hasKeyBeenPickedUp;
    public bool IsToolCaseUnlocked => isToolCaseUnlocked;
    public bool IsStoryInteractionLocked => storyInteractionLockActive;
    public bool IsIntroCompleted => hasVentHandIntroCompleted;
    public bool IsStealUnlocked => hasVentHandIntroCompleted;
    public bool CanStartIntroDialogue => dialogueCanBeStarted && !dialogueRunning;
    public bool IsIntroDialogueRunning => dialogueRunning;
    public bool AppearDuringRegularBlackout => appearDuringRegularBlackout;
    public bool EnableCraftInteractionAfterIntro => hasVentHandIntroCompleted;
    public bool IsWaitingForKeyInventorySpace => isWaitingForKeyInventorySpace;
    public Transform HandTransform => handObject != null ? handObject.transform : null;
    public Transform SpawnedKeyTransform => spawnedKey != null ? spawnedKey.transform : null;

    public bool HandleHandInteractionClick()
    {
        if (!hasVentHandIntroStarted || hasVentHandIntroCompleted)
        {
            return false;
        }

        if (hasKeyBeenDropped && !hasKeyBeenPickedUp)
        {
            return false;
        }

        PlaySfx(handClickSfx, handClickAudioClip, nameof(handClickSfx));

        if (dialogueRunning)
        {
            dialogueAdvanceRequested = true;
            return true;
        }

        if (dialogueCanBeStarted)
        {
            dialogueStartRequested = true;
            return true;
        }

        return false;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        ResolveReferences();
        ResolveHandInteractionColliders();
        DisableKeyDropPointInteraction();
        SubscribeElectricPanelEvents();
        CacheFeedbackLights();
        EnsureRuntimeDialoguePanel();
        if (turnHandLightsOffBeforeIntro)
        {
            SetFeedbackLightsImmediate(false);
        }
        SetDialogueVisible(false);

        if (hideHandBeforeIntro)
        {
            MoveHandToPose(hiddenPose);
            SetHandActive(false);
            SetHandInteractableEnabled(false);
            SetHandInteractionCollidersEnabled(false);
        }
    }

    private void Start()
    {
        ResolveReferences();
        ResolveHandInteractionColliders();
        SubscribeElectricPanelEvents();
        electricPanelController?.SetStoryIntroLockActive(true);
        toolCaseLock?.SetLocked(true);
    }

    public void NotifyFirstSortingMistake()
    {
        NotifyFirstHandPunishment();
    }

    public void NotifyFirstHandPunishment()
    {
        ResolveReferences();

        if (hasVentHandIntroStarted || hasVentHandIntroCompleted || scheduledIntroRoutine != null)
        {
            return;
        }

        if (triggerOnlyAfterGameStarted && gameManager != null && !gameManager.isGameStarted)
        {
            return;
        }

        float minDelay = Mathf.Max(0f, Mathf.Min(minIntroDelay, maxIntroDelay));
        float maxDelay = Mathf.Max(minDelay, Mathf.Max(minIntroDelay, maxIntroDelay));
        float delay = Random.Range(minDelay, maxDelay);
        scheduledIntroRoutine = StartCoroutine(ScheduleIntroRoutine(delay));
        Debug.Log($"Vent hand intro scheduled after first hand punishment in {delay:F1} seconds.");
    }

    public bool NotifyKeyPickedUp(VentHandKeyPickup pickup, InventoryItemDefinition pickupItem)
    {
        if (!hasKeyBeenDropped || hasKeyBeenPickedUp)
        {
            return false;
        }

        InventoryItemDefinition itemToAdd = pickupItem != null ? pickupItem : keyInventoryItem;
        if (itemToAdd != null &&
            (InventorySystem.Instance == null || !InventorySystem.Instance.TryAddItem(itemToAdd)))
        {
            isWaitingForKeyInventorySpace = true;
            SetRuntimeUiInputBlockerActive(false);
            Debug.LogWarning("The tool case key remains in the world until the player frees an inventory slot.", this);
            return false;
        }

        isWaitingForKeyInventorySpace = false;
        hasKeyBeenPickedUp = true;
        PlaySfx(keyPickupSfx, keyPickupAudioClip, nameof(keyPickupSfx));

        if (spawnedKey != null)
        {
            Destroy(spawnedKey);
            spawnedKey = null;
        }

        toolCaseLock?.UnlockCase(itemToAdd);
        isToolCaseUnlocked = toolCaseLock == null || toolCaseLock.IsUnlocked;
        SetHandInteractableEnabled(false);
        SetAllHandCollidersEnabled(false);
        SetHandOutlineEnabled(false);
        Debug.Log("Vent hand key picked up. Tool case unlocked.");

        if (activeIntroRoutine == null)
        {
            activeIntroRoutine = StartCoroutine(CompleteIntroRoutine());
        }

        return true;
    }

    [ContextMenu("Debug Trigger Vent Hand Intro")]
    public void DebugTriggerVentHandIntro()
    {
        if (scheduledIntroRoutine != null)
        {
            StopCoroutine(scheduledIntroRoutine);
            scheduledIntroRoutine = null;
        }

        if (activeIntroRoutine != null)
        {
            return;
        }

        activeIntroRoutine = StartCoroutine(RunIntroWhenConveyorReadyRoutine());
    }

    [ContextMenu("Debug Complete Vent Hand Intro")]
    public void DebugCompleteVentHandIntro()
    {
        if (activeIntroRoutine != null)
        {
            StopCoroutine(activeIntroRoutine);
            activeIntroRoutine = null;
        }

        if (scheduledIntroRoutine != null)
        {
            StopCoroutine(scheduledIntroRoutine);
            scheduledIntroRoutine = null;
        }

        hasKeyBeenDropped = true;
        hasKeyBeenPickedUp = true;
        StartCoroutine(CompleteIntroRoutine());
    }

    [ContextMenu("Debug Reset Vent Hand Intro Runtime State")]
    public void DebugResetRuntimeState()
    {
        if (scheduledIntroRoutine != null)
        {
            StopCoroutine(scheduledIntroRoutine);
            scheduledIntroRoutine = null;
        }

        if (activeIntroRoutine != null)
        {
            StopCoroutine(activeIntroRoutine);
            activeIntroRoutine = null;
        }

        hasVentHandIntroStarted = false;
        hasVentHandIntroCompleted = false;
        hasKeyBeenDropped = false;
        hasKeyBeenPickedUp = false;
        isWaitingForKeyInventorySpace = false;
        isToolCaseUnlocked = false;
        storyInteractionLockActive = false;
        dialogueCanBeStarted = false;
        dialogueRunning = false;
        dialogueStartRequested = false;
        dialogueAdvanceRequested = false;
        fallbackDialogueText = string.Empty;
        SetDialogueVisible(false);
        SetRuntimeUiInputBlockerActive(false);
        SetHandActive(false);
        SetHandInteractableEnabled(false);
        SetHandInteractionCollidersEnabled(false);
        MoveHandToPose(hiddenPose);
        toolCaseLock?.SetLocked(true);
        gameManager?.SetStoryInteractionLocked(false);
        gameManager?.SetTimerPausedForStory(false);
        itemSpawner?.SetStoryPaused(false);
        conveyor?.SetStoryPaused(false);
        electricPanelController?.SetStoryIntroLockActive(true);
        Debug.Log("Vent hand intro runtime state reset.");
    }

    [ContextMenu("Debug Drop Key")]
    public void DebugDropKey()
    {
        DropKey();
    }

    [ContextMenu("Debug Unlock Tool Case")]
    public void DebugUnlockToolCase()
    {
        toolCaseLock?.UnlockCase();
        isToolCaseUnlocked = true;
    }

    private IEnumerator ScheduleIntroRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        scheduledIntroRoutine = null;

        if (hasVentHandIntroStarted || hasVentHandIntroCompleted)
        {
            yield break;
        }

        if (gameManager != null && (!gameManager.isGameStarted || gameManager.IsGameOverStarted))
        {
            Debug.Log("Vent hand intro skipped because the shift is not active.");
            yield break;
        }

        activeIntroRoutine = StartCoroutine(RunIntroWhenConveyorReadyRoutine());
    }

    private IEnumerator RunIntroWhenConveyorReadyRoutine()
    {
        yield return WaitForConveyorExitToFinishRoutine();

        if (hasVentHandIntroStarted || hasVentHandIntroCompleted)
        {
            activeIntroRoutine = null;
            yield break;
        }

        if (gameManager != null && (!gameManager.isGameStarted || gameManager.IsGameOverStarted))
        {
            Debug.Log("Vent hand intro skipped because the shift is not active.");
            activeIntroRoutine = null;
            yield break;
        }

        yield return RunIntroRoutine();
    }

    private IEnumerator WaitForConveyorExitToFinishRoutine()
    {
        ResolveReferences();
        bool loggedWait = false;

        while (IsConveyorExitActive())
        {
            if (!loggedWait)
            {
                loggedWait = true;
                Debug.Log("Vent hand intro waiting for conveyor exit to finish.");
            }

            yield return null;
            ResolveReferences();
        }
    }

    private bool IsConveyorExitActive()
    {
        return (gameManager != null && gameManager.IsCompletingCurrentItem) ||
               (conveyorExitController != null && conveyorExitController.IsRunning);
    }

    private IEnumerator RunIntroRoutine()
    {
        ResolveReferences();
        hasVentHandIntroStarted = true;
        storyInteractionLockActive = true;
        hasKeyBeenDropped = false;
        hasKeyBeenPickedUp = false;
        Debug.Log("Vent hand intro started.");

        gameManager?.SetStoryInteractionLocked(true);
        gameManager?.SetTimerPausedForStory(true);
        SetRuntimeUiInputBlockerActive(true);
        PlayerInteraction.Instance?.HandleStopInteraction();
        itemSpawner?.SetStoryPaused(true);
        conveyor?.SetStoryPaused(true);

        PlaySfx(storyBlackoutStartSfx, storyBlackoutStartAudioClip, nameof(storyBlackoutStartSfx));
        electricPanelController?.BeginStoryBlackout();

        yield return FadeFeedbackLights(true);
        PlayVentOpen();
        SetHandActive(true);
        SetHandInteractableEnabled(true);
        SetHandInteractionCollidersEnabled(true);
        TriggerHand(handAppearTrigger);
        PlaySfx(handAppearSfx, handAppearAudioClip, nameof(handAppearSfx));
        yield return MoveHandRoutine(hiddenPose, introPose, appearDuration);
        UnlockPlayerExtraRotationViews();

        TriggerHand(handIdleTrigger);
        PlaySfx(handIdleSfx, handIdleAudioClip, nameof(handIdleSfx));
        yield return MoveHandRoutine(introPose, idlePose, moveToIdleDuration);

        yield return WaitForDialogueStartRoutine();
        yield return RunDialogueRoutine();

        if (!hasKeyBeenDropped)
        {
            DropKey();
        }

        while (!hasKeyBeenPickedUp)
        {
            yield return null;
        }

        yield return CompleteIntroRoutine();
    }

    private IEnumerator CompleteIntroRoutine()
    {
        TriggerHand(handDisappearTrigger);
        PlaySfx(handMoveSfx, handMoveAudioClip, nameof(handMoveSfx));
        yield return MoveHandRoutine(idlePose, hiddenPose, disappearDuration);

        if (hideHandAfterIntro)
        {
            SetHandActive(false);
        }

        SetHandInteractableEnabled(false);
        SetAllHandCollidersEnabled(false);
        SetHandOutlineEnabled(false);

        PlayVentClose();
        yield return FadeFeedbackLights(false);
        SetDialogueVisible(false);
        fallbackDialogueText = string.Empty;

        PlaySfx(storyBlackoutEndSfx, storyBlackoutEndAudioClip, nameof(storyBlackoutEndSfx));
        if (electricPanelController != null)
        {
            yield return electricPanelController.EndStoryBlackoutRoutine(true);
            electricPanelController.SetStoryIntroLockActive(false);
        }

        itemSpawner?.SetStoryPaused(false);
        conveyor?.SetStoryPaused(false);
        gameManager?.SetTimerPausedForStory(false);
        gameManager?.SetStoryInteractionLocked(false);
        SetRuntimeUiInputBlockerActive(false);

        storyInteractionLockActive = false;
        dialogueCanBeStarted = false;
        dialogueRunning = false;
        hasVentHandIntroCompleted = true;
        UnlockPlayerExtraRotationViews();
        activeIntroRoutine = null;
        Debug.Log("Vent hand intro completed. Electric panel unlocked.");
    }

    private void UnlockPlayerExtraRotationViews()
    {
        Transform ventTarget = ResolveVentRotationTarget();
        Transform panelTarget = electricPanelController != null ? electricPanelController.transform : null;
        PlayerView.Instance?.UnlockVentHandExtraRotationViews(ventTarget, panelTarget, VentRotationViewYawOffset, PanelRotationViewYawOffset);
    }

    private Transform ResolveVentRotationTarget()
    {
        GameObject ceilingVent = GameObject.Find("CeilingVent");
        if (ceilingVent != null)
        {
            return ceilingVent.transform;
        }

        if (ventAnimator != null)
        {
            return ventAnimator.transform;
        }

        return introPose != null ? introPose : idlePose;
    }

    private IEnumerator RunDialogueRoutine()
    {
        dialogueCanBeStarted = false;
        dialogueRunning = true;
        dialogueAdvanceRequested = false;
        PlaySfx(dialogueStartSfx, dialogueStartAudioClip, nameof(dialogueStartSfx));
        SetDialogueVisible(true);

        if (introLines == null || introLines.Length == 0)
        {
            dialogueRunning = false;
            yield break;
        }

        for (int i = 0; i < introLines.Length; i++)
        {
            VentHandDialogueLine line = introLines[i];
            if (line == null)
            {
                continue;
            }

            yield return TypeLineRoutine(line);
            yield return WaitForDialogueAdvanceRoutine();
            if (line.giveKeyAfterThisLine && !hasKeyBeenDropped)
            {
                TriggerHand(handGiveKeyTrigger);
                PlaySfx(handGiveKeySfx, handGiveKeyAudioClip, nameof(handGiveKeySfx));
                DropKey();
            }
        }

        dialogueRunning = false;
    }

    private IEnumerator TypeLineRoutine(VentHandDialogueLine line)
    {
        string fullText = line.text ?? string.Empty;
        SetDialogueText(string.Empty);
        float nextBlipTime = 0f;

        for (int i = 0; i < fullText.Length; i++)
        {
            if (ConsumeDialogueAdvancePressed())
            {
                SetDialogueText(fullText);
                yield break;
            }

            SetDialogueText(fullText.Substring(0, i + 1));
            if (!char.IsWhiteSpace(fullText[i]) && Time.unscaledTime >= nextBlipTime)
            {
                PlayVoiceBlip(line);
                nextBlipTime = Time.unscaledTime + Mathf.Max(0.01f, line.blipInterval);
            }

            yield return new WaitForSeconds(Mathf.Max(0.001f, characterDelay));
        }
    }

    private IEnumerator WaitForDialogueStartRoutine()
    {
        dialogueCanBeStarted = true;
        dialogueStartRequested = false;
        suppressDialogueInputUntilFrame = Time.frameCount + 2;

        if (dialogueStartsOnlyByHandClick)
        {
            while (!dialogueStartRequested)
            {
                yield return null;
            }
        }
        else
        {
            float elapsed = 0f;
            float delay = Mathf.Max(0f, handAutoTalkDelay);
            while (elapsed < delay && !dialogueStartRequested)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        dialogueCanBeStarted = false;
        dialogueStartRequested = false;
        suppressDialogueInputUntilFrame = Time.frameCount + 2;
    }

    private IEnumerator WaitForDialogueAdvanceRoutine()
    {
        suppressDialogueInputUntilFrame = Time.frameCount + 1;
        while (!ConsumeDialogueAdvancePressed())
        {
            yield return null;
        }
    }

    private void DropKey()
    {
        if (hasKeyBeenDropped)
        {
            return;
        }

        hasKeyBeenDropped = true;
        Transform dropPoint = keyDropPoint != null ? keyDropPoint : transform;
        spawnedKey = keyPrefab != null
            ? Instantiate(keyPrefab, dropPoint.position, dropPoint.rotation)
            : CreatePlaceholderKey(dropPoint);

        if (spawnedKey != null)
        {
            VentHandKeyPickup pickup = spawnedKey.GetComponent<VentHandKeyPickup>();
            if (pickup == null)
            {
                pickup = spawnedKey.AddComponent<VentHandKeyPickup>();
            }

            pickup.Configure(this, toolCaseLock, keyInventoryItem, keyPickupSfx, keyPickupAudioClip);
            EnsureKeyPickupReady(spawnedKey, pickup);
        }

        PlayerInteraction.Instance?.HandleStopInteraction();
        DisableHandInteractionForKeyPickup();
        PlaySfx(keyDropSfx, keyDropAudioClip, nameof(keyDropSfx));
        Debug.Log("Vent hand dropped the tool case key.");
    }

    private void EnsureKeyPickupReady(GameObject keyObject, VentHandKeyPickup pickup)
    {
        if (keyObject == null)
        {
            return;
        }

        if (pickup != null)
        {
            pickup.enabled = true;
        }

        Collider rootCollider = keyObject.GetComponent<Collider>();
        if (rootCollider == null)
        {
            SphereCollider fallbackCollider = keyObject.AddComponent<SphereCollider>();
            fallbackCollider.radius = Mathf.Max(0.01f, keyPickupFallbackRadius);
            fallbackCollider.enabled = true;
        }
        else
        {
            rootCollider.enabled = true;
        }

        Collider[] keyColliders = keyObject.GetComponentsInChildren<Collider>(true);
        if (keyColliders.Length == 0)
        {
            return;
        }

        for (int i = 0; i < keyColliders.Length; i++)
        {
            if (keyColliders[i] != null)
            {
                keyColliders[i].enabled = true;
            }
        }
    }

    private void DisableKeyDropPointInteraction()
    {
        if (keyDropPoint == null)
        {
            return;
        }

        VentHandKeyPickup helperPickup = keyDropPoint.GetComponent<VentHandKeyPickup>();
        if (helperPickup != null)
        {
            helperPickup.enabled = false;
        }

        Collider helperCollider = keyDropPoint.GetComponent<Collider>();
        if (helperCollider != null)
        {
            helperCollider.enabled = false;
        }
    }

    private GameObject CreatePlaceholderKey(Transform dropPoint)
    {
        GameObject keyObject = new GameObject("VentHand_ToolCaseKey");
        keyObject.transform.position = dropPoint.position;
        keyObject.transform.rotation = dropPoint.rotation;

        float scale = Mathf.Max(0.25f, placeholderKeyScale.x / 0.08f);
        Color metalColor = new Color(0.64f, 0.43f, 0.13f, 1f);
        Color edgeColor = new Color(0.92f, 0.72f, 0.28f, 1f);
        Color ribbonColor = new Color(0.58f, 0.025f, 0.035f, 1f);

        const int ringSegments = 12;
        Vector3 ringCenter = new Vector3(-0.045f, 0f, 0f) * scale;
        float ringRadius = 0.035f * scale;
        float segmentLength = 2f * Mathf.PI * ringRadius / ringSegments * 1.08f;
        for (int i = 0; i < ringSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / ringSegments;
            Vector3 position = ringCenter + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * ringRadius;
            CreateKeyPart(
                keyObject.transform,
                $"Ring_{i:00}",
                PrimitiveType.Cylinder,
                position,
                new Vector3(0.007f * scale, segmentLength * 0.5f, 0.007f * scale),
                new Vector3(0f, 0f, angle * Mathf.Rad2Deg - 90f),
                edgeColor,
                true);
        }

        CreateKeyPart(
            keyObject.transform,
            "Shaft",
            PrimitiveType.Cylinder,
            new Vector3(0.035f, 0f, 0f) * scale,
            new Vector3(0.009f, 0.055f, 0.009f) * scale,
            new Vector3(0f, 0f, 90f),
            metalColor,
            true);
        CreateKeyPart(
            keyObject.transform,
            "Bit",
            PrimitiveType.Cube,
            new Vector3(0.09f, -0.006f, 0f) * scale,
            new Vector3(0.032f, 0.018f, 0.018f) * scale,
            Vector3.zero,
            edgeColor,
            true);
        CreateKeyPart(
            keyObject.transform,
            "Tooth_A",
            PrimitiveType.Cube,
            new Vector3(0.087f, -0.025f, 0f) * scale,
            new Vector3(0.012f, 0.024f, 0.018f) * scale,
            Vector3.zero,
            metalColor,
            true);
        CreateKeyPart(
            keyObject.transform,
            "Tooth_B",
            PrimitiveType.Cube,
            new Vector3(0.105f, -0.019f, 0f) * scale,
            new Vector3(0.01f, 0.018f, 0.018f) * scale,
            Vector3.zero,
            metalColor,
            true);

        CreateKeyPart(
            keyObject.transform,
            "Ribbon_Left",
            PrimitiveType.Cube,
            new Vector3(-0.068f, 0.045f, 0.006f) * scale,
            new Vector3(0.018f, 0.055f, 0.005f) * scale,
            new Vector3(0f, 0f, 24f),
            ribbonColor,
            false);
        CreateKeyPart(
            keyObject.transform,
            "Ribbon_Right",
            PrimitiveType.Cube,
            new Vector3(-0.037f, 0.046f, 0.004f) * scale,
            new Vector3(0.017f, 0.052f, 0.005f) * scale,
            new Vector3(0f, 0f, -18f),
            ribbonColor,
            false);

        Rigidbody rb = keyObject.AddComponent<Rigidbody>();
        rb.mass = 0.05f;
        rb.isKinematic = true;

        SphereCollider pickupCollider = keyObject.AddComponent<SphereCollider>();
        pickupCollider.center = new Vector3(0.02f, 0f, 0f) * scale;
        pickupCollider.radius = 0.105f * scale;

        if (addOutlineToPlaceholderKey && keyObject.GetComponent<OutlineEffect>() == null)
        {
            keyObject.AddComponent<OutlineEffect>();
        }

        return keyObject;
    }

    private static GameObject CreateKeyPart(
        Transform parent,
        string objectName,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localScale,
        Vector3 localEulerAngles,
        Color color,
        bool metallic)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = objectName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localEulerAngles = localEulerAngles;
        part.transform.localScale = localScale;

        Collider partCollider = part.GetComponent<Collider>();
        if (partCollider != null)
        {
            partCollider.enabled = false;
            Destroy(partCollider);
        }

        Renderer partRenderer = part.GetComponent<Renderer>();
        if (partRenderer != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            block.SetColor("_BaseColor", color);
            block.SetFloat("_Metallic", metallic ? 0.72f : 0.05f);
            block.SetFloat("_Smoothness", metallic ? 0.68f : 0.28f);
            partRenderer.SetPropertyBlock(block);
        }

        return part;
    }

    private IEnumerator MoveHandRoutine(Transform fromPose, Transform toPose, float duration)
    {
        if (handObject == null || toPose == null)
        {
            yield break;
        }

        Transform handTransform = handObject.transform;
        Vector3 startPosition = fromPose != null ? fromPose.position : handTransform.position;
        Quaternion startRotation = fromPose != null ? fromPose.rotation : handTransform.rotation;
        Vector3 targetPosition = toPose.position;
        Quaternion targetRotation = toPose.rotation;
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float eased = handMoveCurve != null ? handMoveCurve.Evaluate(t) : t;
            handTransform.position = Vector3.Lerp(startPosition, targetPosition, eased);
            handTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, eased);
            yield return null;
        }

        handTransform.position = targetPosition;
        handTransform.rotation = targetRotation;
    }

    private IEnumerator FadeFeedbackLights(bool enable)
    {
        Light[] lights = GetActiveFeedbackLights();
        if (lights.Length == 0)
        {
            yield break;
        }

        float safeDuration = Mathf.Max(0.01f, feedbackLightFadeDuration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            for (int i = 0; i < lights.Length; i++)
            {
                Light lightItem = lights[i];
                if (lightItem == null)
                {
                    continue;
                }

                LightSnapshot snapshot = GetSnapshot(lightItem);
                lightItem.enabled = enable || snapshot.Enabled;
                lightItem.color = Color.Lerp(snapshot.Color, feedbackLightColor, enable ? t : 1f - t);
                lightItem.intensity = Mathf.Lerp(
                    snapshot.Enabled ? snapshot.Intensity : 0f,
                    feedbackLightIntensity,
                    enable ? t : 1f - t);
            }

            yield return null;
        }

        if (!enable && restoreInitialLightStateAfterIntro && !turnHandLightsOffBeforeIntro)
        {
            RestoreFeedbackLights();
        }
        else if (!enable)
        {
            SetFeedbackLightsImmediate(false);
        }
    }

    private LightSnapshot GetSnapshot(Light lightItem)
    {
        for (int i = 0; i < feedbackLightSnapshots.Count; i++)
        {
            if (feedbackLightSnapshots[i].Light == lightItem)
            {
                return feedbackLightSnapshots[i];
            }
        }

        return new LightSnapshot
        {
            Light = lightItem,
            Enabled = lightItem != null && lightItem.enabled,
            Intensity = lightItem != null ? lightItem.intensity : 0f,
            Color = lightItem != null ? lightItem.color : Color.white
        };
    }

    private void RestoreFeedbackLights()
    {
        for (int i = 0; i < feedbackLightSnapshots.Count; i++)
        {
            LightSnapshot snapshot = feedbackLightSnapshots[i];
            if (snapshot.Light == null)
            {
                continue;
            }

            snapshot.Light.enabled = snapshot.Enabled;
            snapshot.Light.intensity = snapshot.Intensity;
            snapshot.Light.color = snapshot.Color;
        }
    }

    private void CacheFeedbackLights()
    {
        feedbackLightSnapshots.Clear();
        Light[] lights = GetActiveFeedbackLights();
        for (int i = 0; i < lights.Length; i++)
        {
            Light lightItem = lights[i];
            if (lightItem == null)
            {
                continue;
            }

            feedbackLightSnapshots.Add(new LightSnapshot
            {
                Light = lightItem,
                Enabled = lightItem.enabled,
                Intensity = lightItem.intensity,
                Color = lightItem.color
            });
        }
    }

    private Light[] GetActiveFeedbackLights()
    {
        List<Light> lights = new List<Light>();

        // Важно: свет руки назначается только вручную.
        // Автоподбор и старые массивы не используем, чтобы не включать лампы конвейера.
        AddLights(lights, handArrivalLights);

        return lights.ToArray();
    }

    private static void AddLights(List<Light> lights, Light[] source)
    {
        if (source == null)
        {
            return;
        }

        for (int i = 0; i < source.Length; i++)
        {
            Light lightItem = source[i];
            if (lightItem != null && !lights.Contains(lightItem))
            {
                lights.Add(lightItem);
            }
        }
    }

    private void SetFeedbackLightsImmediate(bool enabled)
    {
        Light[] lights = GetActiveFeedbackLights();
        for (int i = 0; i < lights.Length; i++)
        {
            Light lightItem = lights[i];
            if (lightItem == null)
            {
                continue;
            }

            if (enabled)
            {
                lightItem.enabled = true;
                lightItem.color = feedbackLightColor;
                lightItem.intensity = feedbackLightIntensity;
            }
            else
            {
                lightItem.enabled = false;
                lightItem.intensity = 0f;
            }
        }
    }

    private void PlayVentOpen()
    {
        PlaySfx(ventOpenSfx, ventOpenAudioClip, nameof(ventOpenSfx));
        PlayAnimator(
            ventAnimator,
            ventOpenTrigger,
            ventCloseTrigger,
            ventOpenStateName,
            "vent open",
            ventOpenStartNormalizedTime);
    }

    private void PlayVentClose()
    {
        PlaySfx(ventCloseSfx, ventCloseAudioClip, nameof(ventCloseSfx));
        PlayAnimator(
            ventAnimator,
            ventCloseTrigger,
            ventOpenTrigger,
            ventCloseStateName,
            "vent close",
            ventCloseStartNormalizedTime);
    }

    private void TriggerHand(string trigger)
    {
        if (handAnimator == null || string.IsNullOrWhiteSpace(trigger))
        {
            return;
        }

        handAnimator.SetTrigger(trigger);
    }

    private void PlayAnimator(
        Animator animator,
        string triggerName,
        string oppositeTriggerName,
        string stateName,
        string actionName,
        float startNormalizedTime)
    {
        if (animator == null)
        {
            if (!warnedMissingVentAnimator)
            {
                warnedMissingVentAnimator = true;
                Debug.LogWarning($"Vent hand intro skipped {actionName} animation because animator is not assigned.", this);
            }

            return;
        }

        if (resetVentAnimatorBeforePlay)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        if (!string.IsNullOrWhiteSpace(triggerName))
        {
            if (!string.IsNullOrWhiteSpace(oppositeTriggerName))
            {
                animator.ResetTrigger(oppositeTriggerName);
            }

            animator.SetTrigger(triggerName);
            return;
        }

        if (!string.IsNullOrWhiteSpace(stateName))
        {
            float normalizedTime = Mathf.Clamp01(startNormalizedTime);
            float crossFadeDuration = Mathf.Max(0f, ventCrossFadeDuration);
            if (crossFadeDuration <= 0f)
            {
                animator.Play(stateName, 0, normalizedTime);
            }
            else
            {
                animator.CrossFade(stateName, crossFadeDuration, 0, normalizedTime);
            }
        }
    }

    private void PlayVoiceBlip(VentHandDialogueLine line)
    {
        AudioClip[] clips = line.voiceBlips != null && line.voiceBlips.Length > 0
            ? line.voiceBlips
            : defaultVoiceBlips;

        if (clips == null || clips.Length == 0)
        {
            return;
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
        {
            return;
        }

        AudioSource source = voiceAudioSource != null ? voiceAudioSource : ResolveFallbackAudioSource();
        source.PlayOneShot(clip);
    }

    private void PlaySfx(SfxCue cue, AudioClip fallbackClip, string fieldName)
    {
        if (cue != null)
        {
            ResolveSfxEmitter();
            sfxEmitter.Play(cue);
            return;
        }

        if (fallbackClip != null)
        {
            ResolveFallbackAudioSource().PlayOneShot(fallbackClip);
            return;
        }

        if (missingSfxWarnings.Add(fieldName))
        {
            Debug.LogWarning($"Vent hand intro SFX '{fieldName}' is not assigned.", this);
        }
    }

    private void ResolveSfxEmitter()
    {
        if (sfxEmitter != null)
        {
            return;
        }

        sfxEmitter = GetComponent<SfxEmitter>();
        if (sfxEmitter == null)
        {
            sfxEmitter = gameObject.AddComponent<SfxEmitter>();
        }
    }

    private AudioSource ResolveFallbackAudioSource()
    {
        if (fallbackAudioSource != null)
        {
            return fallbackAudioSource;
        }

        fallbackAudioSource = GetComponent<AudioSource>();
        if (fallbackAudioSource == null)
        {
            fallbackAudioSource = gameObject.AddComponent<AudioSource>();
        }

        fallbackAudioSource.playOnAwake = false;
        return fallbackAudioSource;
    }

    private bool ConsumeDialogueAdvancePressed()
    {
        if (Time.frameCount <= suppressDialogueInputUntilFrame)
        {
            return false;
        }

        if (dialogueAdvanceRequested)
        {
            dialogueAdvanceRequested = false;
            return true;
        }

        if (PlayerInteraction.GetCloseActionKeyDown())
        {
            PlayerInteraction.MarkCloseActionConsumed();
            return true;
        }

        return Input.GetMouseButtonDown(0);
    }

    private void SetDialogueVisible(bool visible)
    {
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = visible ? 1f : 0f;
            dialogueCanvasGroup.interactable = visible;
            dialogueCanvasGroup.blocksRaycasts = visible;
        }

        if (!visible)
        {
            SetDialogueText(string.Empty);
        }
    }

    private void SetDialogueText(string text)
    {
        fallbackDialogueText = text ?? string.Empty;
        if (dialogueText != null)
        {
            dialogueText.text = fallbackDialogueText;
        }

        if (runtimeDialogueText != null)
        {
            runtimeDialogueText.text = fallbackDialogueText;
        }
    }

    [ContextMenu("Restore Default Dialogue Script")]
    private void RestoreDefaultDialogueScript()
    {
        introLines = new[]
        {
            new VentHandDialogueLine { text = "Тише. Я не охрана.", blipInterval = 0.05f },
            new VentHandDialogueLine { text = "Камеры ослепли ненадолго. Так что слушай.", blipInterval = 0.05f },
            new VentHandDialogueLine { text = "Хочешь выбраться — собирай детали. Я сделаю из них кое-что полезное.", blipInterval = 0.045f },
            new VentHandDialogueLine { text = "Но кради и разбирай только в темноте. При свете система бьёт без предупреждения.", blipInterval = 0.045f },
            new VentHandDialogueLine { text = "Щиток перегреется — дёрни рычаг. Пока питание лежит, камера ничего не увидит.", blipInterval = 0.045f },
            new VentHandDialogueLine { text = "Они ускоряют таймер с каждым предметом. Выжимают рабочих, пока те не сломаются.", blipInterval = 0.045f },
            new VentHandDialogueLine
            {
                text = "Держи ключ. Инструменты в кейсе. И не заставляй меня жалеть об этом.",
                blipInterval = 0.045f,
                giveKeyAfterThisLine = true
            }
        };
    }

    private void MoveHandToPose(Transform pose)
    {
        if (handObject == null || pose == null)
        {
            return;
        }

        handObject.transform.position = pose.position;
        handObject.transform.rotation = pose.rotation;
    }

    private void SetHandActive(bool active)
    {
        if (handObject != null)
        {
            handObject.SetActive(active);
        }
    }

    private void SetHandInteractableEnabled(bool enabled)
    {
        if (handObject == null)
        {
            return;
        }

        VentHandInteractable[] interactables = handObject.GetComponentsInChildren<VentHandInteractable>(true);
        for (int i = 0; i < interactables.Length; i++)
        {
            if (interactables[i] != null)
            {
                interactables[i].enabled = enabled;
            }
        }
    }

    private void ResolveHandInteractionColliders()
    {
        if (handObject == null || (handInteractionColliders != null && handInteractionColliders.Length > 0))
        {
            return;
        }

        handInteractionColliders = handObject.GetComponentsInChildren<Collider>(true);
    }

    private void SetHandInteractionCollidersEnabled(bool enabled)
    {
        ResolveHandInteractionColliders();
        if (handInteractionColliders == null)
        {
            return;
        }

        for (int i = 0; i < handInteractionColliders.Length; i++)
        {
            Collider handCollider = handInteractionColliders[i];
            if (handCollider != null && !IsPartOfSpawnedKey(handCollider.transform))
            {
                handCollider.enabled = enabled;
            }
        }
    }

    private void DisableHandInteractionForKeyPickup()
    {
        SetHandInteractableEnabled(false);
        SetAllHandCollidersEnabled(false);
        SetHandOutlineEnabled(false);
    }

    private void SetAllHandCollidersEnabled(bool enabled)
    {
        if (handObject == null)
        {
            return;
        }

        Collider[] colliders = handObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider handCollider = colliders[i];
            if (handCollider == null || IsPartOfSpawnedKey(handCollider.transform))
            {
                continue;
            }

            handCollider.enabled = enabled;
        }

        handInteractionColliders = colliders;
    }

    private void SetHandOutlineEnabled(bool enabled)
    {
        if (handObject == null)
        {
            return;
        }

        OutlineEffect[] outlines = handObject.GetComponentsInChildren<OutlineEffect>(true);
        for (int i = 0; i < outlines.Length; i++)
        {
            OutlineEffect outline = outlines[i];
            if (outline == null || IsPartOfSpawnedKey(outline.transform))
            {
                continue;
            }

            outline.enabled = enabled;
        }
    }

    private bool IsPartOfSpawnedKey(Transform target)
    {
        return spawnedKey != null &&
               target != null &&
               (target == spawnedKey.transform || target.IsChildOf(spawnedKey.transform));
    }

    private void SubscribeElectricPanelEvents()
    {
        if (electricPanelController == null || subscribedPanel == electricPanelController)
        {
            return;
        }

        if (subscribedPanel != null)
        {
            subscribedPanel.BlackoutStarted -= HandleRegularBlackoutStarted;
            subscribedPanel.BlackoutEnded -= HandleRegularBlackoutEnded;
        }

        subscribedPanel = electricPanelController;
        subscribedPanel.BlackoutStarted += HandleRegularBlackoutStarted;
        subscribedPanel.BlackoutEnded += HandleRegularBlackoutEnded;
    }

    private void UnsubscribeElectricPanelEvents()
    {
        if (subscribedPanel == null)
        {
            return;
        }

        subscribedPanel.BlackoutStarted -= HandleRegularBlackoutStarted;
        subscribedPanel.BlackoutEnded -= HandleRegularBlackoutEnded;
        subscribedPanel = null;
    }

    private void HandleRegularBlackoutStarted()
    {
        if (!hasVentHandIntroCompleted || !appearDuringRegularBlackout || activeIntroRoutine != null)
        {
            return;
        }

        if (regularBlackoutHandRoutine != null)
        {
            StopCoroutine(regularBlackoutHandRoutine);
        }

        regularBlackoutHandRoutine = StartCoroutine(RegularBlackoutHandAppearRoutine());
    }

    private void HandleRegularBlackoutEnded()
    {
        if (!hasVentHandIntroCompleted || !appearDuringRegularBlackout)
        {
            return;
        }

        if (regularBlackoutHandRoutine != null)
        {
            StopCoroutine(regularBlackoutHandRoutine);
        }

        regularBlackoutHandRoutine = StartCoroutine(RegularBlackoutHandDisappearRoutine());
    }

    private IEnumerator RegularBlackoutHandAppearRoutine()
    {
        ResolveReferences();
        ResolveHandInteractionColliders();
        yield return FadeFeedbackLights(true);
        PlayVentOpen();
        SetHandActive(true);
        SetHandInteractableEnabled(enableCraftInteractionAfterIntro);
        SetHandInteractionCollidersEnabled(enableCraftInteractionAfterIntro);
        TriggerHand(handAppearTrigger);
        PlaySfx(handAppearSfx, handAppearAudioClip, nameof(handAppearSfx));
        yield return MoveHandRoutine(hiddenPose, introPose, appearDuration);
        UnlockPlayerExtraRotationViews();
        TriggerHand(handIdleTrigger);
        yield return MoveHandRoutine(introPose, idlePose, moveToIdleDuration);
        regularBlackoutHandRoutine = null;
    }

    private IEnumerator RegularBlackoutHandDisappearRoutine()
    {
        TriggerHand(handDisappearTrigger);
        PlaySfx(handMoveSfx, handMoveAudioClip, nameof(handMoveSfx));
        yield return MoveHandRoutine(idlePose, hiddenPose, disappearDuration);
        SetHandInteractableEnabled(false);
        SetHandInteractionCollidersEnabled(false);
        if (hideHandAfterIntro)
        {
            SetHandActive(false);
        }

        PlayVentClose();
        yield return FadeFeedbackLights(false);
        regularBlackoutHandRoutine = null;
    }

    private void SetRuntimeUiInputBlockerActive(bool active)
    {
        if (!createRuntimeUiInputBlocker)
        {
            return;
        }

        if (active)
        {
            EnsureRuntimeUiInputBlocker();
        }

        if (runtimeUiInputBlocker != null)
        {
            runtimeUiInputBlocker.SetActive(active);
        }
    }

    private void EnsureRuntimeUiInputBlocker()
    {
        if (runtimeUiInputBlocker != null)
        {
            return;
        }

        GameObject root = GameObject.Find("UI_Root");
        runtimeUiInputBlocker = new GameObject(runtimeUiInputBlockerName);
        if (root != null)
        {
            runtimeUiInputBlocker.transform.SetParent(root.transform, false);
        }

        Canvas canvas = runtimeUiInputBlocker.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = runtimeUiInputBlockerSortingOrder;
        runtimeUiInputBlocker.AddComponent<GraphicRaycaster>();

        GameObject imageObject = new GameObject("Blocker");
        imageObject.transform.SetParent(runtimeUiInputBlocker.transform, false);
        RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = true;
    }

    private void EnsureRuntimeDialoguePanel()
    {
        if (!createRuntimeDialoguePanel || dialogueText != null || runtimeDialogueText != null || dialogueCanvasGroup != null)
        {
            return;
        }

        runtimeDialogueCanvas = new GameObject(
            "VentHandDialogueCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        runtimeDialogueCanvas.transform.SetParent(transform, false);

        Canvas canvas = runtimeDialogueCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = runtimeUiInputBlockerSortingOrder + 2;

        CanvasScaler scaler = runtimeDialogueCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelObject = new GameObject(
            "EncryptedContactPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));
        panelObject.transform.SetParent(runtimeDialogueCanvas.transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 24f);
        panelRect.sizeDelta = new Vector2(940f, 184f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = dialoguePanelColor;
        panelImage.raycastTarget = false;

        Outline panelOutline = panelObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(dialogueDangerColor.r, dialogueDangerColor.g, dialogueDangerColor.b, 0.48f);
        panelOutline.effectDistance = new Vector2(1.5f, -1.5f);

        dialogueCanvasGroup = panelObject.GetComponent<CanvasGroup>();
        CreateDialogueCorners(panelRect);
        CreateDialogueImage(
            "AccentLine",
            panelRect,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -10f),
            new Vector2(-30f, 2f),
            dialogueAccentColor);

        Text contactLabel = CreateDialogueText(
            "ContactLabel",
            panelRect,
            new Vector2(24f, -18f),
            new Vector2(570f, 28f),
            dialogueContactLabel,
            20,
            FontStyle.Bold,
            TextAnchor.MiddleLeft);
        contactLabel.color = dialogueAccentColor;

        Text channelLabel = CreateDialogueText(
            "ChannelLabel",
            panelRect,
            new Vector2(650f, -21f),
            new Vector2(264f, 22f),
            dialogueChannelLabel,
            10,
            FontStyle.Normal,
            TextAnchor.MiddleRight);
        channelLabel.color = new Color(0.78f, 0.82f, 0.86f, 0.72f);

        runtimeDialogueText = CreateDialogueText(
            "DialogueText",
            panelRect,
            new Vector2(26f, -58f),
            new Vector2(888f, 102f),
            string.Empty,
            24,
            FontStyle.Normal,
            TextAnchor.UpperLeft);
        runtimeDialogueText.color = new Color(0.94f, 0.96f, 0.98f, 1f);
        runtimeDialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        runtimeDialogueText.verticalOverflow = VerticalWrapMode.Truncate;

        Text advanceLabel = CreateDialogueText(
            "AdvanceHint",
            panelRect,
            new Vector2(650f, -156f),
            new Vector2(264f, 18f),
            dialogueAdvanceHint,
            9,
            FontStyle.Normal,
            TextAnchor.MiddleRight);
        advanceLabel.color = new Color(0.78f, 0.82f, 0.86f, 0.58f);
    }

    private void CreateDialogueCorners(RectTransform parent)
    {
        Color cornerColor = new Color(dialogueDangerColor.r, dialogueDangerColor.g, dialogueDangerColor.b, 0.92f);
        CreateDialogueImage("CornerTopLeftH", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(72f, 3f), cornerColor);
        CreateDialogueImage("CornerTopLeftV", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(3f, 34f), cornerColor);
        CreateDialogueImage("CornerBottomRightH", parent, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(72f, 3f), cornerColor);
        CreateDialogueImage("CornerBottomRightV", parent, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(3f, 34f), cornerColor);
    }

    private static Image CreateDialogueImage(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateDialogueText(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size,
        string text,
        int fontSize,
        FontStyle fontStyle,
        TextAnchor alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text label = textObject.GetComponent<Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = alignment;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.raycastTarget = false;
        return label;
    }

    private void ResolveReferences()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
        }

        if (electricPanelController == null)
        {
            electricPanelController = ElectricPanelController.Instance != null
                ? ElectricPanelController.Instance
                : FindFirstObjectByType<ElectricPanelController>();
        }

        if (itemSpawner == null && gameManager != null)
        {
            itemSpawner = gameManager.itemSpawner;
        }

        if (itemSpawner == null)
        {
            itemSpawner = FindFirstObjectByType<ItemSpawner>();
        }

        if (conveyor == null)
        {
            conveyor = FindFirstObjectByType<Conveyor>();
        }

        if (conveyorExitController == null)
        {
            conveyorExitController = FindFirstObjectByType<ConveyorExitController>();
        }

        if (toolCaseLock == null)
        {
            toolCaseLock = FindFirstObjectByType<ToolCaseLock>();
        }

        if (handObject != null && handAnimator == null)
        {
            handAnimator = handObject.GetComponentInChildren<Animator>(true);
        }

        if (ventAnimator == null)
        {
            GameObject ventObject = GameObject.Find("CeilingVent");
            if (ventObject != null)
            {
                ventAnimator = ventObject.GetComponent<Animator>();
            }
        }

        SubscribeElectricPanelEvents();
    }

    private void OnDisable()
    {
        UnsubscribeElectricPanelEvents();
        SetRuntimeUiInputBlockerActive(false);
        if (regularBlackoutHandRoutine != null)
        {
            StopCoroutine(regularBlackoutHandRoutine);
            regularBlackoutHandRoutine = null;
        }

        if (!storyInteractionLockActive)
        {
            return;
        }

        gameManager?.SetStoryInteractionLocked(false);
        gameManager?.SetTimerPausedForStory(false);
        itemSpawner?.SetStoryPaused(false);
        conveyor?.SetStoryPaused(false);
        electricPanelController?.SetStoryIntroLockActive(false);
        if (turnHandLightsOffBeforeIntro)
        {
            SetFeedbackLightsImmediate(false);
        }
        else
        {
            RestoreFeedbackLights();
        }
        SetHandInteractionCollidersEnabled(false);
        dialogueCanBeStarted = false;
        dialogueRunning = false;
        dialogueStartRequested = false;
        dialogueAdvanceRequested = false;
    }

    private void OnGUI()
    {
        if (!useFallbackOnGuiDialogue || string.IsNullOrEmpty(fallbackDialogueText) || dialogueText != null || runtimeDialogueText != null)
        {
            return;
        }

        float width = Mathf.Clamp(Screen.width * fallbackDialogueWidth01, 320f, Screen.width - 40f);
        float height = Mathf.Clamp(fallbackDialogueHeight, 120f, 180f);
        float x = (Screen.width - width) * 0.5f;
        float y = Screen.height - height - Mathf.Clamp(fallbackDialogueBottomOffset, 40f, 70f);
        Rect rect = new Rect(x, y, width, height);

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.024f), 16, 28)
        };

        GUI.Box(rect, fallbackDialogueText, style);
    }
}
