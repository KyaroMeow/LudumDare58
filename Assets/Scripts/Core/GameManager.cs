using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        AudioManager.Instance.Play("DroneSound");
        if(CutsceneManager.Instance != null)
        {
        CutsceneManager.Instance.PlayStartCutscene(() =>
        {
            AudioManager.Instance.Play("Wake up");
            AudioManager.Instance.Stop("DroneSound");
            AudioManager.Instance.Play("Conveyor");
        });
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

        hands.PlayPressButton();

        Item item = currentItem.GetComponent<Item>();
        if (item == null)
        {
            return;
        }

        int missedMarkers = item.GetMissedPlayerMarkerCount();
        int markerPenalty = GetMarkerPenalty(missedMarkers);
        bool hasNoMarkers = !item.HasAnyPlayerMarkers();
        ItemMarkerUI.MarkerVerdict verdict = ResolveMarkerVerdict(item);

        lastMissedMarkers = missedMarkers;

        bool selectedVariant = ResolveSelectedVariant(verdict, item);
        int totalPenalty = markerPenalty + (hasNoMarkers ? 1 : 0);

        if (selectedVariant == item.isDefective)
        {
            CorrectSort(totalPenalty);
        }
        else
        {
            WrongSort(1 + totalPenalty);
        }

        if (hasNoMarkers || missedMarkers > 0)
        {
            Debug.Log($"Submit item. No markers: {hasNoMarkers}. Missed markers: {missedMarkers}. Expected: {item.BuildExpectedMarkersDebugText()}. Selected: {item.BuildPlayerMarkersDebugText()}");
        }
    }

    public void SortItem(bool selectedVariant)
    {
        if (currentItem == null)
        {
            return;
        }

        hands.PlayPressButton();
        Item item = currentItem.GetComponent<Item>();
        if (item == null)
        {
            return;
        }

        int missedMarkers = item.GetMissedPlayerMarkerCount();
        int markerPenalty = GetMarkerPenalty(missedMarkers);
        lastMissedMarkers = missedMarkers;

        bool itemVariant = item.isDefective;
        if (selectedVariant == itemVariant)
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

    public void ShowScanResult()
    {
        scanUI.ShowResult(currentItem.GetComponent<Item>().barcodeShowsGood);
    }

    public void CorrectSort(int additionalMistakes = 0)
    {
        AudioManager.Instance.Play("CorrectSort");
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
        AudioManager.Instance.Play("IncorrectSort");
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
        }
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

    private bool ResolveSelectedVariant(ItemMarkerUI.MarkerVerdict verdict, Item item)
    {
        switch (verdict)
        {
            case ItemMarkerUI.MarkerVerdict.Accept:
                return false;
            case ItemMarkerUI.MarkerVerdict.Reject:
                return true;
            default:
                return item.isDefective;
        }
    }

    private ItemMarkerUI.MarkerVerdict ResolveMarkerVerdict(Item item)
    {
        if (item == null || !item.HasAnyPlayerMarkers())
        {
            return ItemMarkerUI.MarkerVerdict.None;
        }

        if (item.IsMarkerSelected(ItemMarkerType.Ideal))
        {
            return ItemMarkerUI.MarkerVerdict.Accept;
        }

        if (item.IsMarkerSelected(ItemMarkerType.Defective) ||
            item.IsMarkerSelected(ItemMarkerType.Scratch) ||
            item.IsMarkerSelected(ItemMarkerType.Stain) ||
            item.IsMarkerSelected(ItemMarkerType.LegitimacyNegative) ||
            item.IsMarkerSelected(ItemMarkerType.Anomaly) ||
            item.IsMarkerSelected(ItemMarkerType.MassProduct))
        {
            return ItemMarkerUI.MarkerVerdict.Reject;
        }

        if (item.IsMarkerSelected(ItemMarkerType.LegitimacyPositive))
        {
            return ItemMarkerUI.MarkerVerdict.Accept;
        }

        return ItemMarkerUI.MarkerVerdict.None;
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
