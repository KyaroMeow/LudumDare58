using System.Collections;
using UnityEngine;

public class SecuritySystem : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private bool cameraStartsEnabled = true;
    [SerializeField] private float currentLoadNormalized;

    [Header("Generator Load")]
    [SerializeField] private float passiveLoadPerSecond = 0.01f;
    [SerializeField] private float loadPerSortingAction = 0.1f;

    [Header("Auto Shutdown")]
    [SerializeField] private float minAutoShutdownDelay = 60f;
    [SerializeField] private float maxAutoShutdownDelay = 120f;
    [SerializeField] private float autoShutdownDuration = 12f;

    [Header("Manual Shutdown")]
    [SerializeField] private float manualShutdownDuration = 15f;

    [Header("Optional Visuals")]
    [SerializeField] private GameObject cameraEnabledIndicator;
    [SerializeField] private GameObject cameraDisabledIndicator;
    [SerializeField] private Animator generatorAnimator;
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue cameraCaptureSfx;
    [SerializeField] private float duplicateViolationSuppressSeconds = 0.75f;

    private Coroutine autoShutdownRoutine;
    private Coroutine shutdownRoutine;
    private string lastViolationActionName;
    private float lastViolationTime = -999f;

    public bool IsCameraActive { get; private set; }
    public bool IsSecurityEnabled { get; private set; } = true;
    public bool IsGeneratorReady => currentLoadNormalized >= 1f;
    public float CurrentLoadNormalized => currentLoadNormalized;

    private void Start()
    {
        SetCameraState(cameraStartsEnabled);
        ScheduleAutoShutdown();
    }

    private void Update()
    {
        if (IsCameraActive)
        {
            AddLoad(passiveLoadPerSecond * Time.deltaTime);
        }
    }

    public void NotifySortingAction()
    {
        AddLoad(loadPerSortingAction);
    }

    public void ReportViolation(string actionName)
    {
        if (string.IsNullOrWhiteSpace(actionName))
        {
            Debug.LogWarning("Protocol violation ignored: action name is empty.");
            return;
        }

        if (!IsSecurityEnabled)
        {
            Debug.Log($"Protocol violation ignored because security is offline: {actionName}");
            return;
        }

        if (!IsCameraActive)
        {
            return;
        }

        if (IsDuplicateViolation(actionName))
        {
            Debug.Log($"Duplicate protocol violation suppressed: {actionName}");
            return;
        }

        int penalty = GetViolationPenalty();
        Debug.Log($"Protocol violation detected: {actionName}. Penalty: {penalty}");
        lastViolationActionName = actionName;
        lastViolationTime = Time.time;

        PlaySfx(cameraCaptureSfx);

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning($"Protocol violation '{actionName}' could not apply penalty because GameManager is missing.");
            return;
        }

        gameManager.ApplyPenalty(penalty);
    }

    public bool TryManualShutdown()
    {
        if (!IsGeneratorReady || shutdownRoutine != null)
        {
            return false;
        }

        StartShutdown(manualShutdownDuration);
        currentLoadNormalized = 0f;
        return true;
    }

    public void ForceTemporaryShutdown(float duration)
    {
        StartShutdown(duration);
    }

    public void SetSecurityEnabled(bool enabled)
    {
        if (IsSecurityEnabled == enabled)
        {
            return;
        }

        IsSecurityEnabled = enabled;
        SetCameraState(enabled);
        Debug.Log(enabled ? "Security system online." : "Security system offline.");
    }

    private void AddLoad(float amount)
    {
        currentLoadNormalized = Mathf.Clamp01(currentLoadNormalized + amount);
    }

    private void ScheduleAutoShutdown()
    {
        if (autoShutdownRoutine != null)
        {
            StopCoroutine(autoShutdownRoutine);
        }

        autoShutdownRoutine = StartCoroutine(AutoShutdownLoop());
    }

    private IEnumerator AutoShutdownLoop()
    {
        while (true)
        {
            float delay = Random.Range(minAutoShutdownDelay, maxAutoShutdownDelay);
            yield return new WaitForSeconds(delay);

            if (shutdownRoutine == null)
            {
                StartShutdown(autoShutdownDuration);
            }
        }
    }

    private void StartShutdown(float duration)
    {
        if (shutdownRoutine != null)
        {
            StopCoroutine(shutdownRoutine);
        }

        shutdownRoutine = StartCoroutine(ShutdownRoutine(duration));
    }

    private IEnumerator ShutdownRoutine(float duration)
    {
        SetCameraState(false);

        if (generatorAnimator != null)
        {
            generatorAnimator.SetBool("IsShutdown", true);
        }

        yield return new WaitForSeconds(duration);

        if (generatorAnimator != null)
        {
            generatorAnimator.SetBool("IsShutdown", false);
        }

        SetCameraState(true);
        shutdownRoutine = null;
    }

    private void SetCameraState(bool isEnabled)
    {
        IsCameraActive = isEnabled;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCameraState(isEnabled);
        }

        if (cameraEnabledIndicator != null)
        {
            cameraEnabledIndicator.SetActive(isEnabled);
        }

        if (cameraDisabledIndicator != null)
        {
            cameraDisabledIndicator.SetActive(!isEnabled);
        }
    }

    private int GetViolationPenalty()
    {
        SettingManager settings = SettingManager.EnsureInstance();
        Difficult difficulty = settings != null ? settings.currentDifficulty : null;
        if (difficulty == null)
        {
            Debug.LogWarning("Using default protocol violation penalty because current difficulty is not assigned.");
            return 5;
        }

        return difficulty.difficultyName.ToUpperInvariant() == "HARD"
            ? difficulty.protocolViolationPenaltyHard
            : difficulty.protocolViolationPenaltyDefault;
    }

    private bool IsDuplicateViolation(string actionName)
    {
        return duplicateViolationSuppressSeconds > 0f &&
               actionName == lastViolationActionName &&
               Time.time - lastViolationTime <= duplicateViolationSuppressSeconds;
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
