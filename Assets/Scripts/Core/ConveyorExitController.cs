using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorExitController : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private Animator exitDoorAnimator;
    [SerializeField] private string openTrigger = "open";
    [SerializeField] private string openState;
    [SerializeField] private string closeTrigger = "close";
    [SerializeField] private string closeState;
    [SerializeField] private float doorOpenDelay = 0.2f;

    [Header("Movement")]
    [SerializeField] private Transform exitMoveTarget;
    [SerializeField] private Transform despawnPoint;
    [SerializeField] private float itemExitSpeed = 1.5f;
    [SerializeField] private bool destroyAfterExit = true;

    [Header("SFX")]
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue doorOpenSfx;
    [SerializeField] private SfxCue doorCloseSfx;
    [SerializeField] private AudioClip doorOpenAudioClip;
    [SerializeField] private AudioClip doorCloseAudioClip;

    private readonly HashSet<string> missingSfxWarnings = new HashSet<string>();
    private AudioSource fallbackAudioSource;
    private bool isRunning;
    private bool warnedMissingTarget;

    public bool IsRunning => isRunning;
    public bool HasExitTarget => exitMoveTarget != null || despawnPoint != null;

    public bool TryRunExit(GameObject itemObject, Action onComplete)
    {
        if (itemObject == null)
        {
            onComplete?.Invoke();
            return true;
        }

        if (isRunning)
        {
            Debug.LogWarning("Conveyor exit sequence is already running. Falling back to instant item completion.", this);
            return false;
        }

        if (!HasExitTarget)
        {
            if (!warnedMissingTarget)
            {
                warnedMissingTarget = true;
                Debug.LogWarning("ConveyorExitController has no exitMoveTarget/despawnPoint assigned. Using old instant destroy/spawn flow.", this);
            }

            return false;
        }

        StartCoroutine(RunExitRoutine(itemObject, onComplete));
        return true;
    }

    private IEnumerator RunExitRoutine(GameObject itemObject, Action onComplete)
    {
        isRunning = true;
        PrepareItemForExit(itemObject);
        PlayDoor(open: true);

        if (doorOpenDelay > 0f)
        {
            yield return new WaitForSeconds(doorOpenDelay);
        }

        if (itemObject != null && exitMoveTarget != null)
        {
            yield return MoveItemToTarget(itemObject.transform, exitMoveTarget.position);
        }

        if (itemObject != null && despawnPoint != null)
        {
            yield return MoveItemToTarget(itemObject.transform, despawnPoint.position);
        }

        if (destroyAfterExit && itemObject != null)
        {
            Destroy(itemObject);
        }

        PlayDoor(open: false);
        isRunning = false;
        onComplete?.Invoke();
    }

    private IEnumerator MoveItemToTarget(Transform itemTransform, Vector3 targetPosition)
    {
        float speed = Mathf.Max(0.01f, itemExitSpeed);
        while (itemTransform != null && Vector3.Distance(itemTransform.position, targetPosition) > 0.025f)
        {
            itemTransform.position = Vector3.MoveTowards(itemTransform.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }

        if (itemTransform != null)
        {
            itemTransform.position = targetPosition;
        }
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

        Collider[] colliders = itemObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        Rigidbody rb = itemObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        itemObject.transform.SetParent(null, true);
    }

    private void PlayDoor(bool open)
    {
        if (exitDoorAnimator != null)
        {
            string trigger = open ? openTrigger : closeTrigger;
            string state = open ? openState : closeState;
            if (!string.IsNullOrWhiteSpace(trigger))
            {
                exitDoorAnimator.SetTrigger(trigger);
            }
            else if (!string.IsNullOrWhiteSpace(state))
            {
                exitDoorAnimator.CrossFade(state, 0.08f);
            }
        }

        PlaySfx(open ? doorOpenSfx : doorCloseSfx, open ? doorOpenAudioClip : doorCloseAudioClip, open ? nameof(doorOpenSfx) : nameof(doorCloseSfx));
    }

    private void PlaySfx(SfxCue cue, AudioClip fallbackClip, string fieldName)
    {
        if (cue != null)
        {
            ResolveSfxEmitter().Play(cue);
            return;
        }

        if (fallbackClip != null)
        {
            ResolveFallbackAudioSource().PlayOneShot(fallbackClip);
            return;
        }

        if (missingSfxWarnings.Add(fieldName))
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
