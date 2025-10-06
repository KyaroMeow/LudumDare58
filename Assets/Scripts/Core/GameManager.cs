using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]


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
    public Conveyor conveyor;
    public PlayerInteract playerInteract;
    public ScanUI scanUI;
    public GameObject scaner;
    public Lights lights;
    public Slider volumeSlider;

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
       
    }
    void Update()
    {
        if (isGameStarted)
        {
            UpdateTimer();
        }
    }
    public void StartGame()
    {
        isGameStarted = true;
        SpawnItem();
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
    }
    public void SortItem(bool selectedVariant)
    {
        if (currentItem == null) return;
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
    private void CorrectSort()
    {
        AudioManager.Instance.PlayAgree();
        lights.ChangeColorGreen();
        itemsSorted++;
        Destroy(currentItem);
        SpawnItem();
    }
    public void WrongSort()
    {
        AudioManager.Instance.PlayDisAgree();
        lights.ChangeColorRed();
        playerInteract.DropItem();
        currentMistakes++;
        if (currentMistakes >= SettingManager.Instance.maxMistakes) {
            GameOver();
        }
        Destroy(currentItem);
        SpawnItem();
    }
    public void SpawnItem()
    {
        itemSpawner.SpawnItem();
        currentTime = SettingManager.Instance.timePerItem;
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
