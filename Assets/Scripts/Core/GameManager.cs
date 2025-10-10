using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Current Game State")]
    public GameObject currentItem;
    public int currentMistakes = 0;
    public int itemsSorted = 0;
    public int totalItemsProcessed = 0;
    public float currentTime = 0;
    public bool isGameActive = true;
    public bool isGameStarted = false;
    public bool isTimerWork = false;

    public ItemSpawner itemSpawner;
    public Hands hands;
    public Conveyor conveyor;
    public PlayerInteract playerInteract;
    public ScanUI scanUI;
    public GameObject scaner;
    public GameObject scanerOnTable;
    public Lights lights;
    public Slider volumeSlider;
    public AnomallyController anomallyController;

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
    void Start()
    {
        CutSceneManager.Instance.StartInitialCutScene();
    }
    void Update()
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
        if (isTimerWork && SettingManager.Instance.timer)
        {
            if (currentTime > 0)
            {
                currentTime -= Time.deltaTime;
            }
            else
            {
                WrongSort();
            }
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
    public void SortItem(bool selectedVariant)
    {
        if (currentItem == null) return;
        hands.PlayPressButton();
        totalItemsProcessed++;
        bool itemVariant = currentItem.GetComponent<Item>().isDefective;
        if (selectedVariant == itemVariant)
        {
            CorrectSort();
        }
        else
        {
            WrongSort();
        }
    }
    public void ShowScanResult()
    {
        scanUI.ShowResult(currentItem.GetComponent<Item>().barcodeShowsGood);
    }
    public void CorrectSort()
    {
        AudioManager.Instance.PlayAgree();
        lights.ChangeColorGreen();
        itemsSorted++;
        if (currentItem!= null)Destroy(currentItem);
        SpawnItem();
    }
    public void WrongSort()
    {
        AudioManager.Instance.PlayDisAgree();
        lights.ChangeColorRed();
        playerInteract.DropItem();
        currentMistakes++;
        CheckForDamage();
        if (currentMistakes > SettingManager.Instance.maxMistakes)
        {
            GameOver();
        }
        Destroy(currentItem);
        SpawnItem();
    }
    private void CheckForDamage()
    {
        int mistakesPerDamage = GetMistakesPerDamage();
        if (currentMistakes % mistakesPerDamage == 0)
        {
            hands.PlayTakeDamage();
        }
    }
    private int GetMistakesPerDamage()
    {
        switch (SettingManager.Instance.currentDifficulty)
        {
            case "Easy":
                return 3;
            case "Normal":
                return 2;
            case "Hard":
                return 1;
            default:
                return 3;
        }
    }
    public void BadEnd()
    {
        CutSceneManager.Instance.StartLooseCutScene();
    }
    public void SpawnItem()
    {
        if (totalItemsProcessed == SettingManager.Instance.anomalyItemNum)
        {
            itemSpawner.SpawnAnomalyItem();
        }
        else if (totalItemsProcessed == SettingManager.Instance.BombNum)
        {
            itemSpawner.SpawnBomb();
        }
        else
        {
            itemSpawner.SpawnItem();
            currentTime = SettingManager.Instance.timePerItem;
        }
    }
    public void PauseGame()
    {
        isGameActive = false;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isGameActive = true;
        Time.timeScale = 1f;
    }
    public void StopConveyor()
    {
        conveyor.canMove = false;
    }
    private void GameOver()
    {
        SceneManager.LoadScene(0);
    }
    public void StartConveyor()
    {
        conveyor.canMove = true;
    }
}
