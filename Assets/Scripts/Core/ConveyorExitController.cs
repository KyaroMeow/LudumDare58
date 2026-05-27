using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorExitController : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private Animator exitDoorAnimator;
    [SerializeField] private string openTrigger = "open";
    [SerializeField] private string closeTrigger = "close";
    [SerializeField] private string openState;
    [SerializeField] private string closeState;
    [SerializeField] private float doorOpenDelay = 0.2f;
    [SerializeField] private bool closeDoorAfterDestroy = true;
    [SerializeField] private bool useAnimatorTriggers = true;
    [SerializeField] private bool disableExitDoorAnimatorUntilTriggered = true;

    [Header("Trigger Exit")]
    [SerializeField] private ConveyorExitTrigger exitTrigger;
    [SerializeField] private ConveyorCenterStopTrigger centerStopTrigger;
    [SerializeField] private float destroyDelayAfterTrigger = 3f;
    [SerializeField] private float maxExitWaitTime = 12f;
    [SerializeField] private bool destroyAfterExit = true;
    [SerializeField] private bool completeWhenTriggerReached = false;

    [Header("Conveyor")]
    [SerializeField] private Conveyor conveyor;
    [SerializeField] private GameManager gameManager;

    [Header("SFX")]
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue doorOpenSfx;
    [SerializeField] private SfxCue doorCloseSfx;
    [SerializeField] private AudioClip doorOpenAudioClip;
    [SerializeField] private AudioClip doorCloseAudioClip;

    private readonly HashSet<string> missingSfxWarnings = new HashSet<string>();
    private AudioSource fallbackAudioSource;
    private GameObject currentExitingItem;
    private Action currentOnComplete;
    private Coroutine exitRoutine;
    private bool isRunning;
    private bool exitTriggerReached;
    private bool doorAnimatorArmed;
    private bool warnedMissingTrigger;
    private bool warnedTimeout;
    private bool warnedAlreadyRunning;

    public bool IsRunning => isRunning;
    public bool HasExitTarget => exitTrigger != null;

    private void Awake()
    {
        ResolveConveyorReferences();
        ResolveCenterStopTrigger();
        ResolveExitDoorAnimator();
        ResolveTrigger();
        DisarmExitDoorAnimator();
    }

    public bool TryRunExit(GameObject itemObject, Action onComplete)
    {
        if (itemObject == null)
        {
            onComplete?.Invoke();
            return true;
        }

        if (isRunning)
        {
            if (!warnedAlreadyRunning)
            {
                warnedAlreadyRunning = true;
                Debug.LogWarning("Conveyor exit sequence is already running. Ignoring duplicate exit request.", this);
            }

            return false;
        }

        ResolveTrigger();
        if (exitTrigger == null)
        {
            if (!warnedMissingTrigger)
            {
                warnedMissingTrigger = true;
                Debug.LogWarning("ConveyorExitController has no ConveyorExitTrigger assigned. Using old instant destroy/spawn flow.", this);
            }

            return false;
        }

        currentExitingItem = itemObject;
        currentOnComplete = onComplete;
        isRunning = true;
        exitTriggerReached = false;
        warnedAlreadyRunning = false;
        ReleaseCenterStop(itemObject);
        ResolveExitDoorAnimator();
        PrepareItemForExit(itemObject);
        exitTrigger.Configure(this);
        exitRoutine = StartCoroutine(WaitForExitTriggerRoutine());
        return true;
    }

    public void NotifyExitTriggerReached(GameObject itemObject)
    {
        if (!isRunning || exitTriggerReached || !IsCurrentExitingItem(itemObject))
        {
            return;
        }

        exitTriggerReached = true;
        if (exitRoutine != null)
        {
            StopCoroutine(exitRoutine);
        }

        exitRoutine = StartCoroutine(CompleteExitAfterTriggerRoutine());
    }

    public bool IsCurrentExitingItem(GameObject candidate)
    {
        if (candidate == null || currentExitingItem == null)
        {
            return false;
        }

        Transform current = currentExitingItem.transform;
        Transform candidateTransform = candidate.transform;
        return candidate == currentExitingItem ||
               candidateTransform.IsChildOf(current) ||
               current.IsChildOf(candidateTransform);
    }

    public GameObject ResolveExitingItemFromCollider(Collider other)
    {
        if (other == null || currentExitingItem == null)
        {
            return null;
        }

        ConveyorExitingItemMarker marker = other.GetComponentInParent<ConveyorExitingItemMarker>();
        if (marker != null && IsCurrentExitingItem(marker.gameObject))
        {
            return currentExitingItem;
        }

        if (other.transform.IsChildOf(currentExitingItem.transform))
        {
            return currentExitingItem;
        }

        Item item = other.GetComponentInParent<Item>();
        if (item != null && IsCurrentExitingItem(item.gameObject))
        {
            return currentExitingItem;
        }

        return null;
    }

    private IEnumerator WaitForExitTriggerRoutine()
    {
        float elapsed = 0f;
        float timeout = Mathf.Max(0.1f, maxExitWaitTime);
        while (isRunning && currentExitingItem != null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!isRunning)
        {
            yield break;
        }

        if (!warnedTimeout)
        {
            warnedTimeout = true;
            Debug.LogWarning("Conveyor exiting item did not reach ItemTrigger before timeout. Destroying it to keep the game flow moving.", this);
        }

        FinishExit(destroyCurrentItem: true, closeDoor: false);
    }

    private IEnumerator CompleteExitAfterTriggerRoutine()
    {
        PlayDoor(open: true);

        if (doorOpenDelay > 0f)
        {
            yield return new WaitForSeconds(doorOpenDelay);
        }

        if (completeWhenTriggerReached)
        {
            InvokeCompletionCallback();
        }

        if (destroyDelayAfterTrigger > 0f)
        {
            yield return new WaitForSeconds(destroyDelayAfterTrigger);
        }

        FinishExit(destroyCurrentItem: destroyAfterExit, closeDoor: closeDoorAfterDestroy, invokeCallback: !completeWhenTriggerReached);
    }

    private void FinishExit(bool destroyCurrentItem, bool closeDoor, bool invokeCallback = true)
    {
        GameObject itemToDestroy = currentExitingItem;
        Action onComplete = invokeCallback ? currentOnComplete : null;

        if (destroyCurrentItem && itemToDestroy != null)
        {
            Destroy(itemToDestroy);
        }

        if (closeDoor)
        {
            PlayDoor(open: false);
        }

        currentExitingItem = null;
        currentOnComplete = null;
        exitRoutine = null;
        isRunning = false;
        exitTriggerReached = false;
        onComplete?.Invoke();
    }

    private void InvokeCompletionCallback()
    {
        Action onComplete = currentOnComplete;
        currentOnComplete = null;
        onComplete?.Invoke();
    }

    private void PrepareItemForExit(GameObject itemObject)
    {
        if (itemObject == null)
        {
            return;
        }

        ConveyorItemInteractable interactable = itemObject.GetComponent<ConveyorItemInteractable>();
        if (interactable != null)
        {
            interactable.enabled = false;
        }

        ConveyorExitingItemMarker marker = itemObject.GetComponent<ConveyorExitingItemMarker>();
        if (marker == null)
        {
            marker = itemObject.AddComponent<ConveyorExitingItemMarker>();
        }

        marker.Configure(this);

        Rigidbody rb = itemObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }

    private void ResolveTrigger()
    {
        if (exitTrigger != null)
        {
            exitTrigger.Configure(this);
            return;
        }

        exitTrigger = FindFirstObjectByType<ConveyorExitTrigger>();
        if (exitTrigger != null)
        {
            exitTrigger.Configure(this);
            return;
        }

        GameObject triggerObject = GameObject.Find("ItemTrigger");
        if (triggerObject == null)
        {
            triggerObject = GameObject.Find("ItemTriger");
        }
        if (triggerObject == null)
        {
            return;
        }

        exitTrigger = triggerObject.GetComponent<ConveyorExitTrigger>();
        if (exitTrigger == null)
        {
            exitTrigger = triggerObject.AddComponent<ConveyorExitTrigger>();
        }

        exitTrigger.Configure(this);
    }

    private void ResolveCenterStopTrigger()
    {
        ResolveConveyorReferences();

        if (centerStopTrigger == null)
        {
            centerStopTrigger = FindFirstObjectByType<ConveyorCenterStopTrigger>();
        }

        if (centerStopTrigger == null)
        {
            GameObject centerObject = GameObject.Find("Conveyor/ItemTriggerCenter");
            if (centerObject == null)
            {
                centerObject = GameObject.Find("ItemTriggerCenter");
            }
            if (centerObject == null)
            {
                centerObject = GameObject.Find("Conveyor/ItemTrigerCenter");
            }
            if (centerObject == null)
            {
                centerObject = GameObject.Find("ItemTrigerCenter");
            }

            if (centerObject != null)
            {
                centerStopTrigger = centerObject.GetComponent<ConveyorCenterStopTrigger>();
                if (centerStopTrigger == null)
                {
                    centerStopTrigger = centerObject.AddComponent<ConveyorCenterStopTrigger>();
                }
            }
        }

        if (centerStopTrigger != null)
        {
            centerStopTrigger.Configure(conveyor, gameManager);
        }
    }

    private void ReleaseCenterStop(GameObject itemObject)
    {
        ResolveCenterStopTrigger();
        centerStopTrigger?.ReleaseStoppedItem(itemObject);
    }

    private void ResolveConveyorReferences()
    {
        if (conveyor == null)
        {
            conveyor = FindFirstObjectByType<Conveyor>();
        }

        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
        }
    }

    private void ResolveExitDoorAnimator()
    {
        if (exitDoorAnimator != null && exitDoorAnimator.name == "door_conveyo_function (2)")
        {
            return;
        }

        GameObject conveyor = GameObject.Find("Conveyor");
        Transform exactDoor = conveyor != null ? conveyor.transform.Find("door_conveyo_function (2)") : null;
        Animator exactAnimator = exactDoor != null ? exactDoor.GetComponent<Animator>() : null;
        if (exactAnimator != null)
        {
            exitDoorAnimator = exactAnimator;
        }
    }

    private void PlayDoor(bool open)
    {
        if (exitDoorAnimator != null)
        {
            ArmExitDoorAnimator();
            string trigger = open ? openTrigger : closeTrigger;
            string state = open ? openState : closeState;
            if (useAnimatorTriggers && !string.IsNullOrWhiteSpace(trigger))
            {
                exitDoorAnimator.SetTrigger(trigger);
            }
            else if (!string.IsNullOrWhiteSpace(state))
            {
                exitDoorAnimator.CrossFade(state, 0.08f);
            }
        }

        if (open)
        {
            PlaySfx(doorOpenSfx, doorOpenAudioClip, nameof(doorOpenSfx), warnIfMissing: true);
        }
        else if (doorCloseSfx != null || doorCloseAudioClip != null)
        {
            PlaySfx(doorCloseSfx, doorCloseAudioClip, nameof(doorCloseSfx), warnIfMissing: false);
        }
    }

    private void ArmExitDoorAnimator()
    {
        if (exitDoorAnimator == null || doorAnimatorArmed)
        {
            return;
        }

        exitDoorAnimator.enabled = true;
        doorAnimatorArmed = true;
    }

    private void DisarmExitDoorAnimator()
    {
        if (!disableExitDoorAnimatorUntilTriggered || exitDoorAnimator == null)
        {
            doorAnimatorArmed = exitDoorAnimator != null && exitDoorAnimator.enabled;
            return;
        }

        exitDoorAnimator.enabled = false;
        doorAnimatorArmed = false;
    }

    private void PlaySfx(SfxCue cue, AudioClip fallbackClip, string fieldName, bool warnIfMissing)
    {
        if (cue != null)
        {
            ResolveSfxEmitter().PlayOneShot(cue);
            return;
        }

        if (fallbackClip != null)
        {
            ResolveFallbackAudioSource().PlayOneShot(fallbackClip);
            return;
        }

        if (warnIfMissing && missingSfxWarnings.Add(fieldName))
        {
            Debug.LogWarning($"Conveyor exit SFX '{fieldName}' is not assigned.", this);
        }
    }

    private SfxEmitter ResolveSfxEmitter()
    {
        if (sfxEmitter != null)
        {
            return sfxEmitter;
        }

        sfxEmitter = GetComponent<SfxEmitter>();
        if (sfxEmitter == null)
        {
            sfxEmitter = gameObject.AddComponent<SfxEmitter>();
        }

        return sfxEmitter;
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
}

public sealed class ConveyorExitingItemMarker : MonoBehaviour
{
    public ConveyorExitController Controller { get; private set; }

    public void Configure(ConveyorExitController controller)
    {
        Controller = controller;
    }
}
