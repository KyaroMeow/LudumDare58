using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolCaseLock : MonoBehaviour, IInteractable, IOneShotInteractable
{
    [Header("State")]
    [SerializeField] private bool startsLocked = true;
    [SerializeField] private bool isUnlocked;
    [SerializeField] private bool isOpen;

    [Header("Visuals")]
    [SerializeField] private bool keepCaseBaseAlwaysVisible = true;
    [SerializeField] private Transform lidTransform;
    [SerializeField] private Transform closedPose;
    [SerializeField] private Transform openPose;
    [SerializeField] private float openDuration = 0.55f;
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openVisual;
    [SerializeField] private Animator caseAnimator;
    [SerializeField] private string openTrigger;
    [SerializeField] private string closeTrigger;
    [SerializeField] private bool disableCaseColliderWhenOpen = true;
    [SerializeField] private Collider caseClosedCollider;
    [SerializeField] private Collider interactCollider;

    [Header("Controlled Tools")]
    [SerializeField] private bool disableControlledToolsWhileClosed = true;
    [SerializeField] private Instrument[] controlledInstruments;
    [SerializeField] private Collider[] controlledToolColliders;
    [SerializeField] private InventoryItemDefinition keyToConsume;

    [Header("Security")]
    [SerializeField] private SecuritySystem securitySystem;

    [Header("SFX")]
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue lockedSfx;
    [SerializeField] private SfxCue unlockSfx;
    [SerializeField] private SfxCue openSfx;
    [SerializeField] private SfxCue closeSfx;
    [SerializeField] private AudioClip lockedAudioClip;
    [SerializeField] private AudioClip unlockAudioClip;
    [SerializeField] private AudioClip openAudioClip;
    [SerializeField] private AudioClip closeAudioClip;

    private AudioSource fallbackAudioSource;
    private readonly HashSet<string> missingSfxWarnings = new HashSet<string>();
    private Coroutine lidAnimationRoutine;
    private Vector3 fallbackClosedLocalPosition;
    private Quaternion fallbackClosedLocalRotation;
    private SecuritySystem subscribedSecurity;
    private bool keyConsumed;
    private bool exposureReportedForCurrentOpen;
    private int exposureSequence;

    public bool IsUnlocked => isUnlocked;
    public bool IsOpen => isOpen;
    public bool KeepCaseBaseAlwaysVisible => keepCaseBaseAlwaysVisible;
    public Instrument[] ControlledInstruments => controlledInstruments;

    private void Awake()
    {
        if (interactCollider == null)
        {
            interactCollider = GetComponent<Collider>();
        }

        if (caseClosedCollider == null)
        {
            caseClosedCollider = interactCollider;
        }

        CollectControlledToolsIfNeeded();
        ResolveSecuritySystem();
        SubscribeSecurityEvents();
        CaptureFallbackClosedPose();
        if (startsLocked && !isUnlocked)
        {
            SetLocked(true);
        }
        else
        {
            SetLocked(false);
        }

        ApplyOpenState(isOpen, true);
        ApplyInteractionColliderState();
    }

    public void Interact(Transform holdPosition)
    {
        if (!isUnlocked)
        {
            Debug.Log("Tool case is locked. Find the key first.");
            PlaySfx(lockedSfx, lockedAudioClip, nameof(lockedSfx));
            return;
        }

        if (isOpen)
        {
            CloseCase();
            return;
        }

        OpenCase();
    }

    public void StopInteract()
    {
    }

    public void SetLocked(bool locked)
    {
        startsLocked = locked;
        isUnlocked = !locked;
        if (locked)
        {
            isOpen = false;
            keyConsumed = false;
            exposureReportedForCurrentOpen = false;
        }

        ApplyOpenState(isOpen, true);
        SetControlledToolsEnabled(isUnlocked && isOpen);
        ApplyInteractionColliderState();
    }

    public void UnlockCase()
    {
        UnlockCase(null);
    }

    public void UnlockCase(InventoryItemDefinition inventoryKey)
    {
        if (inventoryKey != null)
        {
            keyToConsume = inventoryKey;
        }

        if (isUnlocked)
        {
            return;
        }

        isUnlocked = true;
        PlaySfx(unlockSfx, unlockAudioClip, nameof(unlockSfx));
        Debug.Log("Tool case unlocked.");
        SetControlledToolsEnabled(isOpen);
        ApplyInteractionColliderState();
    }

    public void OpenCase()
    {
        if (!isUnlocked)
        {
            Debug.Log("Tool case cannot open because it is locked.");
            PlaySfx(lockedSfx, lockedAudioClip, nameof(lockedSfx));
            return;
        }

        if (isOpen)
        {
            Debug.Log("Tool case is already open.");
            return;
        }

        if (!ConsumeKeyForFirstOpen())
        {
            Debug.LogWarning("Tool case cannot open because its key is not in the inventory.", this);
            PlaySfx(lockedSfx, lockedAudioClip, nameof(lockedSfx));
            return;
        }

        isOpen = true;
        exposureReportedForCurrentOpen = false;
        exposureSequence++;
        ApplyOpenState(true, false);
        SetControlledToolsEnabled(true);
        ApplyInteractionColliderState();
        if (caseAnimator != null && !string.IsNullOrWhiteSpace(openTrigger))
        {
            caseAnimator.SetTrigger(openTrigger);
        }

        PlaySfx(openSfx, openAudioClip, nameof(openSfx));
        ReportCaseExposureIfNeeded();
        Debug.Log("Tool case opened.");
    }

    public void CloseCase()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        exposureReportedForCurrentOpen = false;
        ClearHeldCaseTool();
        ApplyOpenState(false, false);
        SetControlledToolsEnabled(false);
        ApplyInteractionColliderState();
        if (caseAnimator != null && !string.IsNullOrWhiteSpace(closeTrigger))
        {
            caseAnimator.SetTrigger(closeTrigger);
        }

        PlaySfx(closeSfx, closeAudioClip, nameof(closeSfx));
        Debug.Log("Tool case closed.");
    }

    public void SetControlledInstruments(Instrument[] instruments)
    {
        controlledInstruments = instruments;
        CollectControlledToolColliders();
        SetControlledToolsEnabled(isUnlocked && isOpen);
    }

    private void ApplyOpenState(bool open, bool instant)
    {
        AnimateLidToState(open, instant);
    }

    private void ApplyInteractionColliderState()
    {
        if (interactCollider != null)
        {
            interactCollider.enabled = true;
        }

        if (!disableCaseColliderWhenOpen)
        {
            return;
        }

        if (caseClosedCollider != null && caseClosedCollider != interactCollider)
        {
            caseClosedCollider.enabled = !isOpen;
        }
    }

    private void CaptureFallbackClosedPose()
    {
        if (lidTransform == null)
        {
            fallbackClosedLocalPosition = transform.localPosition;
            fallbackClosedLocalRotation = transform.localRotation;
            return;
        }

        fallbackClosedLocalPosition = lidTransform.localPosition;
        fallbackClosedLocalRotation = lidTransform.localRotation;
    }

    private void AnimateLidToState(bool open, bool instant)
    {
        if (lidTransform == null)
        {
            return;
        }

        Vector3 targetPosition;
        Quaternion targetRotation;
        GetTargetLidPose(open, out targetPosition, out targetRotation);

        if (lidAnimationRoutine != null)
        {
            StopCoroutine(lidAnimationRoutine);
            lidAnimationRoutine = null;
        }

        if (instant || !Application.isPlaying)
        {
            lidTransform.localPosition = targetPosition;
            lidTransform.localRotation = targetRotation;
            return;
        }

        lidAnimationRoutine = StartCoroutine(AnimateLidRoutine(targetPosition, targetRotation));
    }

    private IEnumerator AnimateLidRoutine(Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 startPosition = lidTransform.localPosition;
        Quaternion startRotation = lidTransform.localRotation;
        float duration = Mathf.Max(0.01f, openDuration);
        float elapsed = 0f;

        while (elapsed < duration && lidTransform != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = openCurve != null ? openCurve.Evaluate(t) : t;
            lidTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, eased);
            lidTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, eased);
            yield return null;
        }

        if (lidTransform != null)
        {
            lidTransform.localPosition = targetPosition;
            lidTransform.localRotation = targetRotation;
        }

        lidAnimationRoutine = null;
    }

    private void GetTargetLidPose(bool open, out Vector3 localPosition, out Quaternion localRotation)
    {
        Transform targetPose = open ? openPose : closedPose;
        if (targetPose != null)
        {
            Transform parent = lidTransform != null ? lidTransform.parent : null;
            if (parent != null)
            {
                localPosition = parent.InverseTransformPoint(targetPose.position);
                localRotation = Quaternion.Inverse(parent.rotation) * targetPose.rotation;
            }
            else
            {
                localPosition = targetPose.position;
                localRotation = targetPose.rotation;
            }

            return;
        }

        localPosition = fallbackClosedLocalPosition;
        localRotation = open
            ? fallbackClosedLocalRotation * Quaternion.Euler(-75f, 0f, 0f)
            : fallbackClosedLocalRotation;
    }

    private void SetControlledToolsEnabled(bool enabled)
    {
        if (!disableControlledToolsWhileClosed)
        {
            return;
        }

        if (controlledInstruments != null)
        {
            for (int i = 0; i < controlledInstruments.Length; i++)
            {
                if (controlledInstruments[i] != null)
                {
                    bool instrumentEnabled = enabled && controlledInstruments[i].toolType != ToolType.Steal;
                    controlledInstruments[i].enabled = instrumentEnabled;
                    if (!instrumentEnabled)
                    {
                        controlledInstruments[i].StopInteract();
                    }
                }
            }
        }

        if (controlledToolColliders != null)
        {
            for (int i = 0; i < controlledToolColliders.Length; i++)
            {
                if (controlledToolColliders[i] != null)
                {
                    Instrument owner = controlledToolColliders[i].GetComponentInParent<Instrument>();
                    controlledToolColliders[i].enabled = enabled && (owner == null || owner.toolType != ToolType.Steal);
                }
            }
        }
    }

    private void CollectControlledToolsIfNeeded()
    {
        if (controlledInstruments == null || controlledInstruments.Length == 0)
        {
            controlledInstruments = GetComponentsInChildren<Instrument>(true);
        }

        CollectControlledToolColliders();
    }

    private void CollectControlledToolColliders()
    {
        if (controlledInstruments == null || controlledInstruments.Length == 0)
        {
            controlledToolColliders = new Collider[0];
            return;
        }

        System.Collections.Generic.List<Collider> colliders = new System.Collections.Generic.List<Collider>();
        for (int i = 0; i < controlledInstruments.Length; i++)
        {
            Instrument instrument = controlledInstruments[i];
            if (instrument == null)
            {
                continue;
            }

            Collider[] instrumentColliders = instrument.GetComponentsInChildren<Collider>(true);
            for (int j = 0; j < instrumentColliders.Length; j++)
            {
                if (instrumentColliders[j] != null && !colliders.Contains(instrumentColliders[j]))
                {
                    colliders.Add(instrumentColliders[j]);
                }
            }
        }

        controlledToolColliders = colliders.ToArray();
    }

    private bool ConsumeKeyForFirstOpen()
    {
        if (keyConsumed || keyToConsume == null)
        {
            return true;
        }

        if (InventorySystem.Instance == null || !InventorySystem.Instance.TryRemoveItem(keyToConsume))
        {
            return false;
        }

        keyConsumed = true;
        return true;
    }

    private void ResolveSecuritySystem()
    {
        if (securitySystem == null)
        {
            securitySystem = GameManager.Instance != null
                ? GameManager.Instance.securitySystem
                : FindFirstObjectByType<SecuritySystem>();
        }
    }

    private void SubscribeSecurityEvents()
    {
        ResolveSecuritySystem();
        if (subscribedSecurity == securitySystem)
        {
            return;
        }

        if (subscribedSecurity != null)
        {
            subscribedSecurity.CameraStateChanged -= HandleCameraStateChanged;
        }

        subscribedSecurity = securitySystem;
        if (subscribedSecurity != null)
        {
            subscribedSecurity.CameraStateChanged += HandleCameraStateChanged;
        }
    }

    private void HandleCameraStateChanged(bool cameraActive)
    {
        if (cameraActive)
        {
            ReportCaseExposureIfNeeded();
        }
    }

    private void ReportCaseExposureIfNeeded()
    {
        ResolveSecuritySystem();
        if (!isOpen || exposureReportedForCurrentOpen || securitySystem == null || !securitySystem.IsCameraActive)
        {
            return;
        }

        exposureReportedForCurrentOpen = true;
        securitySystem.ReportViolation($"Open tool case #{exposureSequence}");
    }

    private void ClearHeldCaseTool()
    {
        PlayerHeldItem heldItem = PlayerHeldItem.Instance;
        Instrument heldTool = heldItem != null ? heldItem.CurrentTool : null;
        if (heldTool == null || controlledInstruments == null)
        {
            return;
        }

        for (int i = 0; i < controlledInstruments.Length; i++)
        {
            if (controlledInstruments[i] == heldTool)
            {
                heldItem.ClearTool();
                return;
            }
        }
    }

    private void OnEnable()
    {
        SubscribeSecurityEvents();
    }

    private void OnDisable()
    {
        if (subscribedSecurity != null)
        {
            subscribedSecurity.CameraStateChanged -= HandleCameraStateChanged;
            subscribedSecurity = null;
        }
    }

    private void PlaySfx(SfxCue cue, AudioClip fallbackClip, string fieldName)
    {
        if (cue != null)
        {
            if (sfxEmitter == null)
            {
                sfxEmitter = GetComponent<SfxEmitter>();
                if (sfxEmitter == null)
                {
                    sfxEmitter = gameObject.AddComponent<SfxEmitter>();
                }
            }

            sfxEmitter.Play(cue);
            return;
        }

        if (fallbackClip == null)
        {
            if (missingSfxWarnings.Add(fieldName))
            {
                Debug.LogWarning($"Tool case SFX '{fieldName}' is not assigned.", this);
            }

            return;
        }

        if (fallbackAudioSource == null)
        {
            fallbackAudioSource = GetComponent<AudioSource>();
            if (fallbackAudioSource == null)
            {
                fallbackAudioSource = gameObject.AddComponent<AudioSource>();
            }

            fallbackAudioSource.playOnAwake = false;
        }

        fallbackAudioSource.PlayOneShot(fallbackClip);
    }
}
