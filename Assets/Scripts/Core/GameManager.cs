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
        }
    }

    public void StartTimer()
    {
        isTimerWork = true;
    }

    public void StartGame()
    {
        isGameStarted = true;
        isTimerWork = true;
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
        if (!isTimerWork || !SettingManager.Instance.timer)
        {
            return;
        }

        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            gameAudioManager?.UpdateTimerMusicIntensity(currentTime, SettingManager.Instance.currentDifficulty.timePerItem);
        }
        else
        {
            WrongSort();
        }
    }

    public void SetVolume()
    {
        SettingManager.Instance.volumeValue = volumeSlider.value;
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
            Debug.LogWarning("Item submission blocked: select either Accept/Not Defective or Reject/Defective before submitting.");
            return;
        }

        if (hasAcceptMarker && hasRejectMarker)
        {
            Debug.LogWarning("Item submission blocked: conflicting final markers selected. Choose either Accept/Not Defective or Reject/Defective, not both.");
            return;
        }

        hands?.PlayPressButton();
        ProcessSortSubmission(item, hasRejectMarker);
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
        ProcessSortSubmission(item, selectedVariant);
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
        PlaySfx(sortingSuccessSfx);
        lights.ChangeColorGreen();
        totalItemsProcessed++;
        securitySystem?.NotifySortingAction();

        if (additionalMistakes > 0)
        {
            AddMistakes(additionalMistakes);
            totalMarkerPenalty += additionalMistakes;
        }

        CompleteCurrentItem();
        StartTimer();
        SpawnItem();
    }

    public void WrongSort(int mistakesToAdd = 1)
    {
        PlaySfx(sortingFailSfx);
        lights.ChangeColorRed();
        totalItemsProcessed++;
        securitySystem?.NotifySortingAction();
        AddMistakes(mistakesToAdd);
        CompleteCurrentItem();
        SpawnItem();
    }

    private void CheckForDamage(int previousMistakeCount)
    {
        int mistakesPerDamage = GetMistakesPerDamage();

        for (int mistakeIndex = previousMistakeCount + 1; mistakeIndex <= currentMistakes; mistakeIndex++)
        {
            if (mistakeIndex % mistakesPerDamage == 0)
            {
                PlaySfx(punishmentSfx);
                hands.PlayTakeDamage();
            }
        }
    }

    private int GetMistakesPerDamage()
    {
        switch (SettingManager.Instance.currentDifficulty.difficultyName)
        {
            case "EASY":
                return 3;
            case "NORMAL":
                return 2;
            case "HARD":
                return 1;
            default:
                return 3;
        }
    }

    public void BadEnd()
    {
        CutsceneManager.Instance.PlayLooseCutscene(() => SceneManager.LoadScene(0));
    }

    public void SpawnItem()
    {
        if (totalItemsProcessed == SettingManager.Instance.currentDifficulty.anomalyItemNum)
        {
            itemSpawner.SpawnAnomalyItem();
        }
        else if (totalItemsProcessed == SettingManager.Instance.currentDifficulty.bombNum)   
        {
            itemSpawner.SpawnBomb();
        }
        else
        {
            itemSpawner.SpawnItem();
            currentTime = SettingManager.Instance.currentDifficulty.timePerItem;
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
        isTimerWork = SettingManager.Instance.timer;
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
        SceneManager.LoadScene(0);
    }

    private void AddMistakes(int mistakesToAdd)
    {
        if (mistakesToAdd <= 0)
        {
            return;
        }

        int previousMistakeCount = currentMistakes;
        currentMistakes += mistakesToAdd;
        CheckForDamage(previousMistakeCount);

        if (currentMistakes > SettingManager.Instance.currentDifficulty.maxMistakes)
        {
            GameOver();
        }
    }

    private int GetMarkerPenalty(int missedMarkers)
    {
        if (missedMarkers <= 0)
        {
            return 0;
        }

        string difficultyName = SettingManager.Instance.currentDifficulty.difficultyName.ToUpperInvariant();
        int penaltyPerMarker = difficultyName == "HARD" ? 2 : 1;
        return missedMarkers * penaltyPerMarker;
    }

    private void ProcessSortSubmission(Item item, bool selectedVariant)
    {
        if (item == null)
        {
            return;
        }

        int missedMarkers = GetMissedMarkerCountForSubmission(item, selectedVariant);
        int markerPenalty = GetMarkerPenalty(missedMarkers);
        lastMissedMarkers = missedMarkers;

        if (selectedVariant == item.isDefective)
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

    private int GetMissedMarkerCountForSubmission(Item item, bool selectedVariant)
    {
        List<ItemMarkerType> submittedMarkers = new List<ItemMarkerType>(item.GetPlayerMarkedMarkers());
        ItemMarkerType finalMarker = selectedVariant ? ItemMarkerType.Defective : ItemMarkerType.Ideal;

        if (!submittedMarkers.Contains(finalMarker))
        {
            submittedMarkers.Add(finalMarker);
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
}
