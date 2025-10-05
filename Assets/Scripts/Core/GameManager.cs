using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public float timePerItem = 20f;
    public int maxMistakes = 5;
    public int itemsToWin = 10;

    [Header("Current Game State")]
    public GameObject currentItem;
    public int currentMistakes = 0;
    public int itemsSorted = 0;
    public int totalItemsProcessed = 0;
    public float currentTime;
    public bool isGameActive = true;

    public ItemSpawner itemSpawner;
    public Conveyor conveyor;
    public PlayerInteract playerInteract;
    public ScanUI scanUI;
    public GameObject scaner;
    public Lights lights;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        SpawnItem();
    }
    void Update()
    {
        UpdateTimer();
    }
    private void UpdateTimer()
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
    public void ShowScanResult(bool result)
    {
        scanUI.ShowResult(result);
    }
    private void CorrectSort()
    {
        lights.ChangeColorGreen();
        itemsSorted++;
        Destroy(currentItem);
        SpawnItem();
    }
    public void WrongSort()
    {
        lights.ChangeColorRed();
        playerInteract.DropItem();
        currentMistakes++;
        Destroy(currentItem);
        SpawnItem();
    }
    public void SpawnItem()
    {
        itemSpawner.SpawnItem();
        currentTime = timePerItem;
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
    public void StartConveyor()
    {
        conveyor.canMove = true;
    }
}
