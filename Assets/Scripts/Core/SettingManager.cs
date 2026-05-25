using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
    private const string DifficultyResourcesPath = "Difficulties";
    private const float DefaultVolumeValue = 1f;

    public static SettingManager Instance;

    [Header("Available Difficulties")]
    public List<Difficult> availableDifficulties;

    [Header("Current Difficulty")]
    public Difficult currentDifficulty;

    [Header("Default Settings")]
    public string defaultDifficultyName = "NORMAL";

    [Header("Audio")]
    public float volumeValue = DefaultVolumeValue;

    [Header("Timer")]
    public bool timer = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureInitialized();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        EnsureInitialized();
    }

    private void Update()
    {
        ApplyVolume();
    }

    public static SettingManager EnsureInstance()
    {
        if (Instance != null)
        {
            Instance.EnsureInitialized();
            return Instance;
        }

        SettingManager sceneInstance = FindFirstObjectByType<SettingManager>();
        if (sceneInstance != null)
        {
            Instance = sceneInstance;
            DontDestroyOnLoad(Instance.gameObject);
            Instance.EnsureInitialized();
            return Instance;
        }

        GameObject settingsObject = new GameObject("SettingManager_AutoBootstrap");
        SettingManager createdInstance = settingsObject.AddComponent<SettingManager>();
        Debug.LogWarning("SettingManager was created automatically because no instance was found. This usually happens when Main is started directly.");
        createdInstance.EnsureInitialized();
        return createdInstance;
    }

    public void SetDifficulty(string diffName)
    {
        EnsureDifficultiesLoaded();

        if (availableDifficulties == null || availableDifficulties.Count == 0)
        {
            Debug.LogError("Cannot set difficulty because no Difficult assets are available.");
            currentDifficulty = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(diffName))
        {
            Debug.LogWarning("Cannot set difficulty because difficulty name is empty.");
            return;
        }

        var difficulty = availableDifficulties.Find(d => MatchesDifficultyName(d, diffName));

        if (difficulty != null)
        {
            currentDifficulty = difficulty;
            Debug.Log($"Difficulty changed to: {currentDifficulty.difficultyName}");
        }
        else
        {
            Debug.LogWarning($"Difficulty {diffName} was not found.");
        }
    }

    private void EnsureInitialized()
    {
        EnsureDifficultiesLoaded();
        EnsureCurrentDifficulty();
        ApplyVolume();
    }

    private void EnsureDifficultiesLoaded()
    {
        if (availableDifficulties == null)
        {
            availableDifficulties = new List<Difficult>();
        }

        availableDifficulties.RemoveAll(difficulty => difficulty == null);
        if (availableDifficulties.Count > 0)
        {
            return;
        }

        Difficult[] loadedDifficulties = Resources.LoadAll<Difficult>(DifficultyResourcesPath);
        if (loadedDifficulties == null || loadedDifficulties.Length == 0)
        {
            Debug.LogError("No Difficult assets were found in Resources/Difficulties. SettingManager cannot choose a difficulty.");
            return;
        }

        availableDifficulties = loadedDifficulties.ToList();
        Debug.Log($"Loaded {availableDifficulties.Count} difficulties from Resources/Difficulties.");
    }

    private void EnsureCurrentDifficulty()
    {
        if (currentDifficulty != null)
        {
            return;
        }

        if (availableDifficulties == null || availableDifficulties.Count == 0)
        {
            Debug.LogError("No difficulty is available. Gameplay systems should stop instead of using null difficulty.");
            return;
        }

        Difficult normalDifficulty = availableDifficulties.Find(difficulty =>
            MatchesDifficultyName(difficulty, defaultDifficultyName));

        currentDifficulty = normalDifficulty != null ? normalDifficulty : availableDifficulties[0];

        if (normalDifficulty != null)
        {
            Debug.Log($"Fallback difficulty selected: {currentDifficulty.difficultyName}.");
        }
        else
        {
            Debug.LogWarning($"Default difficulty {defaultDifficultyName} was not found. Fallback difficulty selected: {currentDifficulty.difficultyName}.");
        }
    }

    private static bool MatchesDifficultyName(Difficult difficulty, string difficultyName)
    {
        return difficulty != null &&
               !string.IsNullOrWhiteSpace(difficulty.difficultyName) &&
               string.Equals(difficulty.difficultyName, difficultyName, System.StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyVolume()
    {
        volumeValue = Mathf.Clamp01(volumeValue);
        AudioListener.volume = volumeValue;
    }
}
