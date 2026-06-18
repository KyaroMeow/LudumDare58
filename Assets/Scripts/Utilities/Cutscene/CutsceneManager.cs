using System;
using System.Collections;
using UnityEngine;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance;

    [Header("Cutscenes")]
    [SerializeField] private Cutscene startCutscene;
    [SerializeField] private Cutscene beglecCutscene;
    [SerializeField] private Cutscene looseCutscene;

    [Header("Tutorial Delay")]
    [SerializeField] private bool delayTutorialUntilStartCutsceneEnds = true;
    [SerializeField] private float tutorialDelayAfterStartCutscene = 2f;
    [SerializeField] private bool autoFindTutorialSystems = true;
    [SerializeField] private TutorialHintSystem[] tutorialSystemsToDelay;

    public bool HasStartCutsceneBeenRequested { get; private set; }
    public bool IsStartCutscenePlaying { get; private set; }
    public bool HasStartCutsceneFinished { get; private set; }
    public float LastStartCutsceneFinishedRealtime { get; private set; } = -1f;

    private Coroutine enableTutorialRoutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ResolveTutorialSystems();

        if (delayTutorialUntilStartCutsceneEnds)
        {
            SetTutorialSystemsEnabled(false);
        }

        if (startCutscene == null)
        {
            MarkStartCutsceneFinished();

            if (delayTutorialUntilStartCutsceneEnds)
            {
                StartEnableTutorialAfterDelay();
            }
        }
    }

    public void PlayStartCutscene(Action callback = null)
    {
        HasStartCutsceneBeenRequested = true;

        ResolveTutorialSystems();

        if (delayTutorialUntilStartCutsceneEnds)
        {
            SetTutorialSystemsEnabled(false);
        }

        if (startCutscene == null)
        {
            MarkStartCutsceneFinished();
            StartEnableTutorialAfterDelay();
            callback?.Invoke();
            return;
        }

        IsStartCutscenePlaying = true;
        HasStartCutsceneFinished = false;
        LastStartCutsceneFinishedRealtime = -1f;

        startCutscene.Play(() =>
        {
            MarkStartCutsceneFinished();
            StartEnableTutorialAfterDelay();
            callback?.Invoke();
        });
    }

    public void PlayBeglecCutscene(Action callback = null)
    {
        if (beglecCutscene == null)
        {
            Debug.LogWarning("Beglec cutscene is not assigned. Running fallback callback.");
            callback?.Invoke();
            return;
        }

        beglecCutscene.Play(callback);
    }

    public void PlayLooseCutscene(Action callback = null)
    {
        if (looseCutscene == null)
        {
            Debug.LogWarning("Loose cutscene is not assigned. Running fallback callback.");
            callback?.Invoke();
            return;
        }

        looseCutscene.Play(callback);
    }

    public bool CanStartTutorialAfterStartCutscene(float delaySeconds)
    {
        if (!HasStartCutsceneFinished)
        {
            return false;
        }

        if (LastStartCutsceneFinishedRealtime < 0f)
        {
            return true;
        }

        return Time.unscaledTime >= LastStartCutsceneFinishedRealtime + Mathf.Max(0f, delaySeconds);
    }

    private void MarkStartCutsceneFinished()
    {
        IsStartCutscenePlaying = false;
        HasStartCutsceneFinished = true;
        LastStartCutsceneFinishedRealtime = Time.unscaledTime;
    }

    private void StartEnableTutorialAfterDelay()
    {
        if (!delayTutorialUntilStartCutsceneEnds)
        {
            SetTutorialSystemsEnabled(true);
            return;
        }

        if (enableTutorialRoutine != null)
        {
            StopCoroutine(enableTutorialRoutine);
        }

        enableTutorialRoutine = StartCoroutine(EnableTutorialAfterDelayRoutine());
    }

    private IEnumerator EnableTutorialAfterDelayRoutine()
    {
        float delay = Mathf.Max(0f, tutorialDelayAfterStartCutscene);

        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        SetTutorialSystemsEnabled(true);
        enableTutorialRoutine = null;
    }

    private void ResolveTutorialSystems()
    {
        if (!autoFindTutorialSystems)
        {
            return;
        }

        if (tutorialSystemsToDelay != null && tutorialSystemsToDelay.Length > 0)
        {
            return;
        }

        tutorialSystemsToDelay = FindObjectsByType<TutorialHintSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
    }

    private void SetTutorialSystemsEnabled(bool enabled)
    {
        ResolveTutorialSystems();

        if (tutorialSystemsToDelay == null)
        {
            return;
        }

        for (int i = 0; i < tutorialSystemsToDelay.Length; i++)
        {
            TutorialHintSystem tutorialSystem = tutorialSystemsToDelay[i];

            if (tutorialSystem == null)
            {
                continue;
            }

            tutorialSystem.enabled = enabled;
        }
    }
}