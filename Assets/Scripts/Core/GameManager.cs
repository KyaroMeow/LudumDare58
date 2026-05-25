using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Current Game State")]
    public GameObject currentItem;
    public int currentMistakes = 0;
    public int totalItemsProcessed = 0;
    public float currentTime = 0;
    public bool isGameStarted = false;
    public bool isTimerWork = false;
    public bool isCameraWorking = true;
    public int totalMarkerPenalty = 0;
    public int lastMissedMarkers = 0;

    public ItemSpawner itemSpawner;
    public Hands hands;
    public ScanUI scanUI;
    public GameObject scaner;
    public GameObject scanerOnTable;
    public Lights lights;
    public Slider volumeSlider;
    public AnomallyController anomallyController;
    public ItemMarkerUI itemMarkerUI;
    public SecuritySystem securitySystem;
    [SerializeField] private GameAudioManager gameAudioManager;
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue scannerStartSfx;
    [SerializeField] private SfxCue scannerErrorSfx;
    [SerializeField] private SfxCue sortingSuccessSfx;
    [SerializeField] private SfxCue sortingFailSfx;
    [SerializeField] private SfxCue punishmentSfx;

    [Header("Hand Damage Counter")]
    [SerializeField] private int currentHandDamageCounter = 0;
    [SerializeField] private int handCounterLimit = 10;
    [SerializeField] private int easyHandPenaltyPoints = 1;
    [SerializeField] private int normalHandPenaltyPoints = 2;
    [SerializeField] private int hardHandPenaltyPoints = 5;
    [SerializeField] private bool enableHandCounterDecay = true;
    [SerializeField] private float handCounterDecayDelay = 5f;
    [SerializeField] private float handCounterDecayInterval = 3f;
    [SerializeField] private int handCounterDecayAmount = 1;
    [SerializeField] private bool showHandPenaltyDebugCounter = true;
    [SerializeField] private Vector2 handPenaltyDebugPosition = new Vector2(16f, 16f);
    [SerializeField] private Vector2 handPenaltyDebugSize = new Vector2(220f, 48f);

    private bool isGameOverStarted;
    private bool warnedMissingHandsForDamage;
    private float nextHandCounterDecayTime;

    public int CurrentHandDamageCounter => currentHandDamageCounter;
    public int CurrentHandDamageThreshold => GetHandCounterLimit();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SettingManager.EnsureInstance();
        ResolveGameAudioManager();

        if(CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.PlayStartCutscene();
        }
    }

    private void Update()
    {
        if (isGameStarted)
        {
            UpdateTimer();
            UpdateHandCounterDecay();
        }
    }

    public void StartTimer()
    {
        isTimerWork = true;
    }

    public void StartGame()
    {
        Difficult difficulty = GetCurrentDifficulty("start the game");
        if (difficulty == null)
        {
            return;
        }

        isGameStarted = true;
        isTimerWork = true;
        currentHandDamageCounter = 0;
        ResetHandCounterDecayTimer();
        ResolveGameAudioManager();
        gameAudioManager?.StartShiftMusic();
        SpawnItem();
    }

    public void StartAnomally()
    {
        anomallyController.StartAnomally();
    }

    private void UpdateTimer()
    {
        SettingManager settings = SettingManager.EnsureInstance();
        if (!isTimerWork || settings == null || !settings.timer)
        {
            return;
        }

        Difficult difficulty = GetCurrentDifficulty("update the timer");
        if (difficulty == null)
        {
            isTimerWork = false;
            return;
        }

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            gameAudioManager?.UpdateTimerMusicIntensity(currentTime, difficulty.timePerItem);
        }
        else
        {
            WrongSort();
        }
    }

    public void SetVolume()
    {
        SettingManager settings = SettingManager.EnsureInstance();
        if (settings == null)
        {
            return;
        }

        if (volumeSlider == null)
        {
            Debug.LogWarning("Cannot set volume because volumeSlider is not assigned on GameManager.");
            return;
        }

        settings.volumeValue = volumeSlider.value;
    }

    public void ToggleScaner()
    {
        scaner.SetActive(!scaner.activeSelf);
        scanerOnTable.SetActive(!scaner.activeSelf);

        if (scaner.activeSelf)
        {
            PlaySfx(scannerStartSfx);
        }
    }

    public void ToggleScanerOff()
    {
        scaner.SetActive(false);
        scanerOnTable.SetActive(true);
    }

    public void SubmitCurrentItem()
    {
        if (currentItem == null)
        {
            return;
        }

        Item item = currentItem.GetComponent<Item>();
        if (item == null)
        {
            Debug.LogWarning("Cannot submit current item because it has no Item component.");
            return;
        }

        bool hasAcceptMarker = item.IsMarkerSelected(ItemMarkerType.Ideal);
        bool hasRejectMarker = item.IsMarkerSelected(ItemMarkerType.Defective);

        if (!hasAcceptMarker && !hasRejectMarker)
        {
            Debug.Log("Item submitted without final marker. Treated as wrong sort.");
            hands?.PlayPressButton();
            ProcessSortSubmission(item, false, false);
            return;
        }

        if (hasAcceptMarker && hasRejectMarker)
        {
            Debug.LogWarning("Item submission blocked: conflicting final markers selected. Choose either Accept/Not Defective or Reject/Defective, not both.");
            return;
        }

        hands?.PlayPressButton();
        ProcessSortSubmission(item, hasRejectMarker, true);
    }

    public void SortItem(bool selectedVariant)
    {
        if (currentItem == null)
        {
            return;
        }

        Item item = currentItem.GetComponent<Item>();
        if (item == null)
        {
            Debug.LogWarning("Cannot sort current item because it has no Item component.");
            return;
        }

        if (HasConflictingFinalMarkers(item))
        {
            Debug.LogWarning("Item sorting blocked: conflicting final markers selected. Choose either Accept/Not Defective or Reject/Defective, not both.");
            return;
        }

        if (HasOppositeFinalMarker(item, selectedVariant))
        {
            Debug.LogWarning("Item sorting blocked: selected category conflicts with the final marker on the item.");
            return;
        }

        hands?.PlayPressButton();
        ProcessSortSubmission(item, selectedVariant, true);
    }

    public void ShowScanResult()
    {
        Item item = currentItem.GetComponent<Item>();
        if (item == null)
        {
            return;
        }

        scanUI.ShowResult(item.barcodeShowsGood);

        if (!item.barcodeShowsGood)
        {
            PlaySfx(scannerErrorSfx);
        }
    }

    public void CorrectSort(int additionalMistakes = 0)
    {
        if (isGameOverStarted)
        {
            Debug.LogWarning("Correct sort ignored because game over has already started.");
            return;
        }

        PlaySfx(sortingSuccessSfx);
        lights.ChangeColorGreen();
        totalItemsProcessed++;
        securitySystem?.NotifySortingAction();

        bool canContinue = true;
        if (additionalMistakes > 0)
        {
            canContinue = AddMistakes(additionalMistakes);
            totalMarkerPenalty += additionalMistakes;
        }

        CompleteCurrentItem();

        if (canContinue)
        {
            StartTimer();
            SpawnItem();
        }
    }

    public void WrongSort(int mistakesToAdd = 1)
    {
        if (isGameOverStarted)
        {
            Debug.LogWarning("Wrong sort ignored because game over has already started.");
            return;
        }

        PlaySfx(sortingFailSfx);
        lights.ChangeColorRed();
        totalItemsProcessed++;
        securitySystem?.NotifySortingAction();
        bool canContinue = AddMistakes(mistakesToAdd);
        CompleteCurrentItem();

        if (canContinue)
        {
            SpawnItem();
        }
    }

    private bool AccumulateHandDamageCounter()
    {
        if (isGameOverStarted)
        {
            Debug.LogWarning("Hand damage counter update ignored because game over has already started.");
            return false;
        }

        int handCounterLimitValue = GetHandCounterLimit();
        if (handCounterLimitValue <= 0)
        {
            Debug.LogWarning($"Hand damage counter skipped: invalid limit value {handCounterLimitValue}.");
            return true;
        }

        int handPenaltyPoints = GetHandPenaltyPointsPerMistake();
        if (handPenaltyPoints <= 0)
        {
            Debug.LogWarning($"Hand damage counter skipped: invalid penalty points value {handPenaltyPoints}.");
            return true;
        }

        currentHandDamageCounter = Mathf.Min(currentHandDamageCounter + handPenaltyPoints, handCounterLimitValue);
        ResetHandCounterDecayTimer();
        Debug.Log($"Hand damage counter increased by {handPenaltyPoints}: {currentHandDamageCounter}/{handCounterLimitValue}.");

        if (currentHandDamageCounter < handCounterLimitValue)
        {
            return true;
        }

        Debug.Log($"Hand damage counter filled: {currentHandDamageCounter}/{handCounterLimitValue}. Applying one hand punishment.");
        currentHandDamageCounter = 0;
        ResetHandCounterDecayTimer();
        return ApplyHandPunishment();
    }

    private bool ApplyHandPunishment()
    {
        if (isGameOverStarted)
        {
            Debug.LogWarning("Hand punishment ignored because game over has already started.");
            return false;
        }

        if (hands == null)
        {
            WarnMissingHandsForDamage();
            Debug.Log($"Hand damage counter reset after skipped punishment: {currentHandDamageCounter}/{GetHandCounterLimit()}.");
            return true;
        }

        if (hands.IsFullyDamaged)
        {
            Debug.LogWarning("Hand punishment reached critical state: all fingers are already damaged. Starting game over.");
            GameOver();
            return false;
        }

        PlaySfx(punishmentSfx);
        if (hands.TryPlayTakeDamage())
        {
            Debug.Log($"Hand damage applied. Counter reset to {currentHandDamageCounter}/{GetHandCounterLimit()}.");
            return true;
        }

        if (hands.IsFullyDamaged)
        {
            Debug.LogWarning("Hand punishment failed because the hand is fully damaged. Starting game over.");
            GameOver();
            return false;
        }

        Debug.LogWarning($"Hand damage logic continued, but visual damage was skipped. Counter reset to {currentHandDamageCounter}/{GetHandCounterLimit()}.");
        return true;
    }

    private void UpdateHandCounterDecay()
    {
        if (!enableHandCounterDecay || isGameOverStarted || currentHandDamageCounter <= 0)
        {
            return;
        }

        if (handCounterDecayInterval <= 0f || handCounterDecayAmount <= 0)
        {
            return;
        }

        if (Time.time < nextHandCounterDecayTime)
        {
            return;
        }

        int previousCounter = currentHandDamageCounter;
        currentHandDamageCounter = Mathf.Max(0, currentHandDamageCounter - handCounterDecayAmount);
        nextHandCounterDecayTime = Time.time + handCounterDecayInterval;
        Debug.Log($"Hand damage counter decayed: {previousCounter} -> {currentHandDamageCounter}/{GetHandCounterLimit()}.");
    }

    private void ResetHandCounterDecayTimer()
    {
        nextHandCounterDecayTime = Time.time + Mathf.Max(0f, handCounterDecayDelay);
    }

    private int GetHandCounterLimit()
    {
        return Mathf.Max(1, handCounterLimit);
    }

    private int GetHandPenaltyPointsPerMistake()
    {
        Difficult difficulty = GetCurrentDifficulty("calculate hand penalty points");
        if (difficulty == null)
        {
            return normalHandPenaltyPoints;
        }

        switch (difficulty.difficultyName)
        {
            case "EASY":
                return easyHandPenaltyPoints;
            case "NORMAL":
                return normalHandPenaltyPoints;
            case "HARD":
                return hardHandPenaltyPoints;
            default:
                return normalHandPenaltyPoints;
        }
    }

    public void BadEnd()
    {
        CutsceneManager cutsceneManager = CutsceneManager.Instance;
        if (cutsceneManager == null)
        {
            Debug.LogWarning("CutsceneManager is missing. Loading menu without loose cutscene.");
            SceneManager.LoadScene(0);
            return;
        }

        cutsceneManager.PlayLooseCutscene(() => SceneManager.LoadScene(0));
    }

    public void SpawnItem()
    {
        Difficult difficulty = GetCurrentDifficulty("spawn an item");
        if (difficulty == null)
        {
            return;
        }

        if (totalItemsProcessed == difficulty.anomalyItemNum)
        {
            itemSpawner.SpawnAnomalyItem();
        }
        else if (totalItemsProcessed == difficulty.bombNum)
        {
            itemSpawner.SpawnBomb();
        }
        else
        {
            itemSpawner.SpawnItem();
            currentTime = difficulty.timePerItem;
            gameAudioManager?.ResetTimerMusicIntensity();
        }
    }

    private void ResolveGameAudioManager()
    {
        if (gameAudioManager != null)
        {
            return;
        }

        gameAudioManager = FindFirstObjectByType<GameAudioManager>();
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

    public void ResumeGame()
    {
        SettingManager settings = SettingManager.EnsureInstance();
        isTimerWork = settings != null && settings.timer;
        AudioListener.pause = false;
    }

    public void SpawnNextItemAfterBypass()
    {
        if (isGameStarted)
        {
            SpawnItem();
        }
    }

    public void ApplyPenalty(int mistakesToAdd)
    {
        AddMistakes(mistakesToAdd);
    }

    public void SetCameraState(bool isEnabled)
    {
        isCameraWorking = isEnabled;
    }

    private void GameOver()
    {
        if (isGameOverStarted)
        {
            Debug.LogWarning("Game over request ignored because game over has already started.");
            return;
        }

        isGameOverStarted = true;
        isTimerWork = false;
        isGameStarted = false;
        Debug.Log("Game over started.");

        CutsceneManager cutsceneManager = CutsceneManager.Instance;
        if (cutsceneManager == null)
        {
            Debug.LogWarning("CutsceneManager is missing. Loading menu without loose cutscene.");
            SceneManager.LoadScene(0);
            return;
        }

        cutsceneManager.PlayLooseCutscene(() => SceneManager.LoadScene(0));
    }

    private bool AddMistakes(int mistakesToAdd)
    {
        if (isGameOverStarted)
        {
            Debug.LogWarning($"Mistake penalty {mistakesToAdd} ignored because game over has already started.");
            return false;
        }

        if (mistakesToAdd <= 0)
        {
            return true;
        }

        currentMistakes += mistakesToAdd;
        Debug.Log($"Mistakes added: +{mistakesToAdd}. Total mistakes: {currentMistakes}.");
        return AccumulateHandDamageCounter();
    }

    private int GetMarkerPenalty(int missedMarkers)
    {
        if (missedMarkers <= 0)
        {
            return 0;
        }

        Difficult difficulty = GetCurrentDifficulty("calculate marker penalty");
        if (difficulty == null)
        {
            return missedMarkers;
        }

        string difficultyName = difficulty.difficultyName.ToUpperInvariant();
        int penaltyPerMarker = difficultyName == "HARD" ? 2 : 1;
        return missedMarkers * penaltyPerMarker;
    }

    private Difficult GetCurrentDifficulty(string actionName)
    {
        SettingManager settings = SettingManager.EnsureInstance();
        if (settings == null)
        {
            Debug.LogError($"Cannot {actionName}: SettingManager is missing.");
            return null;
        }

        if (settings.currentDifficulty == null)
        {
            Debug.LogError($"Cannot {actionName}: current difficulty is not assigned.");
            return null;
        }

        return settings.currentDifficulty;
    }

    private void ProcessSortSubmission(Item item, bool selectedVariant, bool hasFinalChoice)
    {
        if (item == null)
        {
            return;
        }

        int missedMarkers = GetMissedMarkerCountForSubmission(item, selectedVariant, hasFinalChoice);
        int markerPenalty = GetMarkerPenalty(missedMarkers);
        lastMissedMarkers = missedMarkers;

        if (!hasFinalChoice)
        {
            WrongSort(1 + markerPenalty);
        }
        else if (selectedVariant == item.isDefective)
        {
            CorrectSort(markerPenalty);
        }
        else
        {
            WrongSort(1 + markerPenalty);
        }

        if (missedMarkers > 0)
        {
            Debug.Log($"Missed markers: {missedMarkers}. Expected: {item.BuildExpectedMarkersDebugText()}. Selected: {item.BuildPlayerMarkersDebugText()}");
        }
    }

    private int GetMissedMarkerCountForSubmission(Item item, bool selectedVariant, bool hasFinalChoice)
    {
        List<ItemMarkerType> submittedMarkers = new List<ItemMarkerType>(item.GetPlayerMarkedMarkers());

        if (hasFinalChoice)
        {
            ItemMarkerType finalMarker = selectedVariant ? ItemMarkerType.Defective : ItemMarkerType.Ideal;

            if (!submittedMarkers.Contains(finalMarker))
            {
                submittedMarkers.Add(finalMarker);
            }
        }

        return item.GetMissedMarkerCount(submittedMarkers);
    }

    private bool HasConflictingFinalMarkers(Item item)
    {
        return item.IsMarkerSelected(ItemMarkerType.Ideal) &&
               item.IsMarkerSelected(ItemMarkerType.Defective);
    }

    private bool HasOppositeFinalMarker(Item item, bool selectedVariant)
    {
        return selectedVariant
            ? item.IsMarkerSelected(ItemMarkerType.Ideal)
            : item.IsMarkerSelected(ItemMarkerType.Defective);
    }

    private void CompleteCurrentItem()
    {
        ReleaseHeldCurrentItem();

        if (currentItem != null)
        {
            Destroy(currentItem);
            currentItem = null;
        }
    }

    private void ReleaseHeldCurrentItem()
    {
        if (currentItem == null || PlayerInteraction.Instance == null)
        {
            return;
        }

        ConveyorItemInteractable interactable = currentItem.GetComponent<ConveyorItemInteractable>();
        if (interactable != null && PlayerInteraction.Instance.IsCurrentInteractable(interactable))
        {
            PlayerInteraction.Instance.HandleStopInteraction();
        }
    }

    private void WarnMissingHandsForDamage()
    {
        if (warnedMissingHandsForDamage)
        {
            return;
        }

        warnedMissingHandsForDamage = true;
        Debug.LogWarning("Hand damage skipped: GameManager hands reference is not assigned.");
    }

    private void OnGUI()
    {
        if (!showHandPenaltyDebugCounter || !isGameStarted)
        {
            return;
        }

        int handCounterLimitValue = GetHandCounterLimit();
        GUI.Label(
            new Rect(handPenaltyDebugPosition.x, handPenaltyDebugPosition.y, handPenaltyDebugSize.x, handPenaltyDebugSize.y),
            $"Hand penalty: {currentHandDamageCounter} / {handCounterLimitValue}\nTotal mistakes: {currentMistakes}");
    }
}
