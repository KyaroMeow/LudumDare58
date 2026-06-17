using UnityEngine;

public class UVLighter : MonoBehaviour
{
    public static UVLighter Instance { get; private set; }

    [Header("Scene References")]
    [SerializeField] private GameObject lighter;
    [SerializeField] private GameObject uVOnTable;

    [Header("Follow")]
    [SerializeField] private bool followMouse = true;
    [SerializeField, Min(0f)] private float followSpeed;
    [SerializeField] private bool lockWorldX = true;
    [SerializeField] private float fixedWorldX;
    [SerializeField] private Vector3 cursorOffset = new Vector3(0f, -0.55f, 0f);
    [SerializeField] private bool followCursorByModelCenter = true;

    [Header("Held Visual")]
    [SerializeField] private bool useTableFlashlightAsHeldModel = true;
    [SerializeField] private Vector3 heldVisualLocalPosition = new Vector3(0f, -0.08f, 0f);
    [SerializeField] private Vector3 heldVisualLocalEuler = new Vector3(0f, -90f, -12f);
    [SerializeField] private Vector3 heldModelLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 heldModelLocalEuler = Vector3.zero;
    [SerializeField] private float heldModelScale = 1f;
    [SerializeField] private bool centerHeldModelPivot = true;
    [SerializeField] private Vector3 centeredModelOffset = Vector3.zero;
    [SerializeField] private bool aimTowardCurrentItem = true;
    [SerializeField, Range(0f, 1f)] private float itemAimStrength = 0.85f;
    [SerializeField, Min(1f)] private float itemAimMaxAngle = 68f;
    [SerializeField, Min(0f)] private float itemAimSpeed = 11f;
    [SerializeField] private Vector3 itemAimOffset = new Vector3(0f, 0.05f, 0f);
    [SerializeField, Min(0.01f)] private float itemAimAssistRadius = 2.35f;
    [SerializeField, Range(0f, 1f)] private float itemAimMinimumAssist = 0.45f;
    [SerializeField] private bool alignVisualModelToBeam = true;
    [SerializeField] private bool useModelBoundsForVisualAim = true;
    [SerializeField] private bool flipVisualModelAim = true;
    [SerializeField] private Vector3 visualAimLocalDirection = Vector3.left;

    [Header("Beam")]
    [SerializeField, Min(0.05f)] private float beamLength = 1.35f;
    [SerializeField, Min(0.01f)] private float beamRadius = 0.035f;
    [SerializeField, Min(0.01f)] private float beamHorizontalRadius = 0.06f;
    [SerializeField, Min(0.01f)] private float beamVerticalRadius = 0.08f;
    [SerializeField] private Vector3 beamLocalOffset = new Vector3(0f, 0f, 0.18f);
    [SerializeField] private Vector3 beamLocalEuler = Vector3.zero;
    [SerializeField] private Color beamColor = new Color(0.42f, 0.08f, 1f, 1f);
    [SerializeField] private bool beamVisible = false;
    [SerializeField] private bool beamColliderIsTrigger = true;
    [SerializeField] private bool revealOnStay = true;
    [SerializeField] private bool stopBeamAtObjects = true;
    [SerializeField, Min(0.02f)] private float minBeamLength = 0.12f;
    [SerializeField, Min(0f)] private float beamStopPadding = 0.025f;
    [SerializeField] private LayerMask beamBlockerMask = -1;
    [SerializeField] private QueryTriggerInteraction beamBlockerTriggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Beam Aim")]
    [SerializeField] private bool forceBeamToCurrentItem = true;
    [SerializeField] private bool aimBeamAtMouseProjectedPoint = true;
    [SerializeField, Min(0f)] private float beamTargetSmoothing = 16f;
    [SerializeField, Range(0f, 1f)] private float itemTargetFallbackCenterBias = 0.35f;
    [SerializeField, Range(0f, 1f)] private float nearItemTargetCenterBias = 0.06f;
    [SerializeField, Range(0f, 1f)] private float farItemTargetCenterBias = 0.48f;
    [SerializeField, Min(0.01f)] private float centerBiasStartDistance = 0.85f;
    [SerializeField, Min(0.01f)] private float centerBiasFullDistance = 2.45f;
    [SerializeField, Min(0f)] private float itemSurfacePadding = 0.02f;

    [Header("Light")]
    [SerializeField] private bool syncLightToBeam = true;
    [SerializeField, Min(0.1f)] private float decorativeLightRange = 5f;
    [SerializeField, Min(0f)] private float decorativeLightIntensity = 4f;
    [SerializeField, Min(1f)] private float lightSpotAngle = 38f;
    [SerializeField, Range(0.1f, 1.25f)] private float revealConeAngleScale = 0.78f;
    [SerializeField, Range(0f, 1f)] private float revealConeSoftness = 0.45f;
    [SerializeField] private LightShadows uvLightShadows = LightShadows.None;
    [SerializeField, Range(0f, 1f)] private float uvLightShadowStrength = 0f;

    [Header("SFX")]
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue toggleOnSfx;
    [SerializeField] private SfxCue toggleOffSfx;

    private Camera mainCamera;
    private UVBeamTrigger beamTrigger;
    private GameObject beamObject;
    private Renderer beamRenderer;
    private Light uvSpotLight;
    private GameObject heldVisualRoot;
    private GameObject heldModelInstance;
    private float currentBeamLength;
    private Renderer[] toolRenderers;
    private bool warnedLockedWithoutItem;
    private bool hasFixedWorldX;
    private bool heldModelCentered;
    private Quaternion baseHeldLocalRotation;
    private bool hasBeamAim;
    private Vector3 currentBeamTargetPoint;
    private Vector3 currentBeamWorldDirection = Vector3.forward;

    public bool IsLighterActive { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;
        CacheToolRenderers();
        CaptureFixedWorldX();
        EnsureHeldVisual();
        SetBeamActive(false);
        if (lighter != null)
        {
            lighter.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsLighterActive)
        {
            return;
        }

        if (!TryGetCurrentItem(out _))
        {
            ToggleLighterOff();
            return;
        }

        FollowCursorYZ();
        UpdateSmartAim();
        UpdateBeamAimAndShape();
    }

    public void ToggleLighter()
    {
        if (IsLighterActive)
        {
            ToggleLighterOff();
            return;
        }

        if (!TryGetCurrentItem(out Item item))
        {
            if (!warnedLockedWithoutItem)
            {
                warnedLockedWithoutItem = true;
                Debug.Log("UV flashlight ignored because no item is currently being inspected.");
            }

            return;
        }

        warnedLockedWithoutItem = false;
        GameManager.Instance?.ToggleScanerOff();
        item.HideAllUVStains();
        EnsureHeldVisual();
        SetActiveState(true);
        FollowCursorYZ(immediate: true);
        SetBeamActive(true);
        UpdateSmartAim(immediate: true);
        UpdateBeamAimAndShape(immediate: true);
        TutorialHintSystem.Instance?.NotifyUVActiveChanged(true);
        PlaySfx(toggleOnSfx);
    }

    public void ToggleLighterOff()
    {
        bool wasActive = IsLighterActive;
        if (GameManager.Instance != null && GameManager.Instance.TryResolveCurrentItem(out Item item))
        {
            item.HideAllUVStains();
        }

        SetBeamActive(false);
        SetActiveState(false);
        TutorialHintSystem.Instance?.NotifyUVActiveChanged(false);
        if (wasActive)
        {
            PlaySfx(toggleOffSfx);
        }
    }

    public bool TryGetRevealStrength(UVRevealable revealable, out float strength)
    {
        strength = 0f;
        if (!IsLighterActive || revealable == null)
        {
            return false;
        }

        Renderer targetRenderer = revealable.TargetRenderer;
        if (targetRenderer == null)
        {
            return false;
        }

        Vector3 origin = GetBeamWorldOrigin();
        Vector3 direction = GetBeamWorldDirection();
        Bounds bounds = targetRenderer.bounds;
        Vector3 stainPoint = bounds.center;
        Vector3 toStain = stainPoint - origin;
        float distance = toStain.magnitude;
        float revealRange = Mathf.Max(decorativeLightRange, currentBeamLength + itemSurfacePadding);
        if (distance <= 0.001f || distance > revealRange)
        {
            return false;
        }

        Vector3 toStainDirection = toStain / distance;
        float angle = Vector3.Angle(direction, toStainDirection);
        float halfAngle = Mathf.Max(0.1f, lightSpotAngle * 0.5f * revealConeAngleScale);
        if (angle > halfAngle)
        {
            return false;
        }

        float softStart = halfAngle * Mathf.Clamp01(1f - revealConeSoftness);
        float angleStrength = angle <= softStart
            ? 1f
            : 1f - Mathf.InverseLerp(softStart, halfAngle, angle);
        float distanceStrength = 1f - Mathf.Clamp01(distance / revealRange);
        strength = Mathf.Clamp01(Mathf.Max(0.25f, angleStrength) * Mathf.Lerp(0.75f, 1f, distanceStrength));
        return strength > 0f;
    }

    private void SetActiveState(bool isActive)
    {
        IsLighterActive = isActive;

        if (lighter != null)
        {
            lighter.SetActive(isActive);
            if (isActive)
            {
                PlaceUVLight();
                ConfigureUVLight();
            }
        }

        if (heldVisualRoot != null)
        {
            heldVisualRoot.SetActive(isActive);
        }

        if (uVOnTable != null)
        {
            uVOnTable.SetActive(!isActive);
        }
    }

    private bool TryGetCurrentItem(out Item item)
    {
        item = null;
        return GameManager.Instance != null && GameManager.Instance.TryResolveCurrentItem(out item) && item != null;
    }

    private void CaptureFixedWorldX()
    {
        if (hasFixedWorldX)
        {
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.scaner != null)
        {
            fixedWorldX = GameManager.Instance.scaner.transform.position.x;
        }
        else
        {
            fixedWorldX = transform.position.x;
        }

        hasFixedWorldX = true;
    }

    private void FollowCursorYZ(bool immediate = false)
    {
        if (!followMouse)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        CaptureFixedWorldX();

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = mainCamera.WorldToScreenPoint(transform.position).z;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos) + cursorOffset;
        if (lockWorldX)
        {
            worldPos.x = fixedWorldX;
        }

        Vector3 targetPosition = worldPos;
        if (followCursorByModelCenter && TryGetHeldModelCenter(out Vector3 modelCenter))
        {
            targetPosition = transform.position + (worldPos - modelCenter);
            if (lockWorldX)
            {
                targetPosition.x = fixedWorldX;
            }
        }

        transform.position = immediate || followSpeed <= 0f
            ? targetPosition
            : Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }

    private void UpdateSmartAim(bool immediate = false)
    {
        if (alignVisualModelToBeam)
        {
            return;
        }

        if (heldVisualRoot == null)
        {
            return;
        }

        Quaternion baseWorldRotation = transform.rotation * baseHeldLocalRotation;
        Quaternion targetWorldRotation = baseWorldRotation;

        if (aimTowardCurrentItem &&
            itemAimStrength > 0f &&
            TryGetCurrentItem(out Item item) &&
            TryGetCurrentItemCenter(item, out Vector3 itemCenter))
        {
            Vector3 origin = GetBeamWorldOrigin();
            Vector3 directionToItem = itemCenter + itemAimOffset - origin;
            if (directionToItem.sqrMagnitude > 0.0001f)
            {
                float assist = CalculateAimAssist(origin, itemCenter);
                Quaternion lookRotation = Quaternion.LookRotation(directionToItem.normalized, baseWorldRotation * Vector3.up);
                Quaternion limitedLookRotation = Quaternion.RotateTowards(baseWorldRotation, lookRotation, itemAimMaxAngle);
                targetWorldRotation = Quaternion.Slerp(baseWorldRotation, limitedLookRotation, itemAimStrength * assist);
            }
        }

        heldVisualRoot.transform.rotation = immediate || itemAimSpeed <= 0f
            ? targetWorldRotation
            : Quaternion.Slerp(heldVisualRoot.transform.rotation, targetWorldRotation, itemAimSpeed * Time.deltaTime);
    }

    private float CalculateAimAssist(Vector3 origin, Vector3 itemCenter)
    {
        Vector2 originYZ = new Vector2(origin.y, origin.z);
        Vector2 itemYZ = new Vector2(itemCenter.y, itemCenter.z);
        float distance = Vector2.Distance(originYZ, itemYZ);
        float radius = Mathf.Max(0.01f, itemAimAssistRadius);
        float assist = 1f - Mathf.Clamp01(distance / radius);
        return Mathf.Max(itemAimMinimumAssist, assist);
    }

    private static bool TryGetCurrentItemCenter(Item item, out Vector3 center)
    {
        center = Vector3.zero;
        if (item == null)
        {
            return false;
        }

        Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer itemRenderer = renderers[i];
            if (itemRenderer == null || !itemRenderer.enabled)
            {
                continue;
            }

            if (itemRenderer.GetComponent<UVRevealable>() != null ||
                itemRenderer.GetComponentInParent<UVRevealable>() != null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = itemRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(itemRenderer.bounds);
            }
        }

        if (hasBounds)
        {
            center = bounds.center;
            return true;
        }

        Collider[] colliders = item.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider itemCollider = colliders[i];
            if (itemCollider == null || !itemCollider.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = itemCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(itemCollider.bounds);
            }
        }

        if (hasBounds)
        {
            center = bounds.center;
            return true;
        }

        center = item.transform.position;
        return true;
    }

    private void EnsureHeldVisual()
    {
        if (heldVisualRoot == null)
        {
            heldVisualRoot = new GameObject("UVHeldFlashlight");
            heldVisualRoot.transform.SetParent(transform, false);
        }

        baseHeldLocalRotation = Quaternion.Euler(heldVisualLocalEuler);
        heldVisualRoot.transform.localPosition = heldVisualLocalPosition;
        heldVisualRoot.transform.localRotation = baseHeldLocalRotation;
        heldVisualRoot.transform.localScale = Vector3.one;

        EnsureHeldModel();
        EnsureBeam();
        PlaceUVLight();
        heldVisualRoot.SetActive(IsLighterActive);
    }

    private void EnsureHeldModel()
    {
        if (!useTableFlashlightAsHeldModel || uVOnTable == null)
        {
            return;
        }

        EnsureHeldVisualRootOnly();
        if (heldModelInstance == null)
        {
            heldModelInstance = Instantiate(uVOnTable, heldVisualRoot.transform);
            heldModelInstance.name = "UVHeldFlashlightModel";
            StripHeldModelInteraction(heldModelInstance);
        }

        heldModelInstance.SetActive(true);
        heldModelInstance.transform.localPosition = heldModelLocalPosition;
        heldModelInstance.transform.localRotation = Quaternion.Euler(heldModelLocalEuler);
        heldModelInstance.transform.localScale = uVOnTable.transform.lossyScale * Mathf.Max(0.01f, heldModelScale);

        if (centerHeldModelPivot)
        {
            CenterHeldModelOnPivotOnce();
        }
    }

    private void EnsureBeam()
    {
        if (beamObject == null)
        {
            EnsureHeldVisualRootOnly();
            Transform existingBeam = heldVisualRoot.transform.Find("UVBeam");
            if (existingBeam != null)
            {
                beamObject = existingBeam.gameObject;
            }
            else
            {
                beamObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                beamObject.name = "UVBeam";
                beamObject.transform.SetParent(heldVisualRoot.transform, false);
            }
        }

        beamObject.transform.localPosition = beamLocalOffset;
        beamObject.transform.localRotation = Quaternion.Euler(beamLocalEuler) * Quaternion.Euler(90f, 0f, 0f);
        currentBeamLength = Mathf.Max(minBeamLength, beamLength);
        ApplyBeamTransform(currentBeamLength);

        beamRenderer = beamObject.GetComponent<Renderer>();
        if (beamRenderer != null)
        {
            beamRenderer.enabled = false;
            beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beamRenderer.receiveShadows = false;
        }

        Collider beamCollider = beamObject.GetComponent<Collider>();
        if (beamCollider != null)
        {
            beamCollider.isTrigger = beamColliderIsTrigger;
        }

        Rigidbody beamRigidbody = beamObject.GetComponent<Rigidbody>();
        if (beamRigidbody == null)
        {
            beamRigidbody = beamObject.AddComponent<Rigidbody>();
        }

        beamRigidbody.isKinematic = true;
        beamRigidbody.useGravity = false;

        beamTrigger = beamObject.GetComponent<UVBeamTrigger>();
        if (beamTrigger == null)
        {
            beamTrigger = beamObject.AddComponent<UVBeamTrigger>();
        }

        beamTrigger.Initialize(this, revealOnStay);
    }

    private void EnsureHeldVisualRootOnly()
    {
        if (heldVisualRoot != null)
        {
            return;
        }

        heldVisualRoot = new GameObject("UVHeldFlashlight");
        heldVisualRoot.transform.SetParent(transform, false);
        heldVisualRoot.transform.localPosition = heldVisualLocalPosition;
        baseHeldLocalRotation = Quaternion.Euler(heldVisualLocalEuler);
        heldVisualRoot.transform.localRotation = baseHeldLocalRotation;
    }

    private void StripHeldModelInteraction(GameObject model)
    {
        if (model == null)
        {
            return;
        }

        Collider[] colliders = model.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        TableFlashlight[] tableFlashlights = model.GetComponentsInChildren<TableFlashlight>(true);
        for (int i = 0; i < tableFlashlights.Length; i++)
        {
            tableFlashlights[i].enabled = false;
        }

        OutlineEffect[] outlines = model.GetComponentsInChildren<OutlineEffect>(true);
        for (int i = 0; i < outlines.Length; i++)
        {
            outlines[i].enabled = false;
        }
    }

    private void SetBeamActive(bool active)
    {
        EnsureHeldVisual();
        if (heldVisualRoot != null)
        {
            heldVisualRoot.SetActive(active);
        }

        if (beamRenderer != null)
        {
            beamRenderer.enabled = active && beamVisible;
        }

        if (beamObject != null)
        {
            beamObject.SetActive(active);
        }

        if (beamTrigger != null)
        {
            beamTrigger.SetBeamActive(active);
            if (!active)
            {
                beamTrigger.ClearTrackedRevealables();
            }
        }

        if (!active)
        {
            hasBeamAim = false;
        }
    }

    private void UpdateBeamAimAndShape(bool immediate = false)
    {
        if (!IsLighterActive || beamObject == null || heldVisualRoot == null)
        {
            return;
        }

        if (forceBeamToCurrentItem && TryGetCurrentItem(out Item item) && TryCalculateBeamTarget(item, out Vector3 targetPoint))
        {
            if (!hasBeamAim || immediate || beamTargetSmoothing <= 0f)
            {
                currentBeamTargetPoint = targetPoint;
            }
            else
            {
                float t = 1f - Mathf.Exp(-beamTargetSmoothing * Time.deltaTime);
                currentBeamTargetPoint = Vector3.Lerp(currentBeamTargetPoint, targetPoint, t);
            }

            Vector3 origin = GetBeamWorldOrigin();
            Vector3 toTarget = currentBeamTargetPoint - origin;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                currentBeamWorldDirection = toTarget.normalized;
                hasBeamAim = true;
                AlignVisualToBeamDirection(immediate);
            }
        }
        else
        {
            hasBeamAim = false;
        }

        UpdateBeamShape();
    }

    private void AlignVisualToBeamDirection(bool immediate)
    {
        if (!alignVisualModelToBeam || currentBeamWorldDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Transform visualTransform = heldModelInstance != null ? heldModelInstance.transform : heldVisualRoot != null ? heldVisualRoot.transform : null;
        if (visualTransform == null)
        {
            return;
        }

        Quaternion targetRotation;
        Vector3 targetVisualDirection = flipVisualModelAim
            ? -currentBeamWorldDirection.normalized
            : currentBeamWorldDirection.normalized;

        if (useModelBoundsForVisualAim && TryGetHeldModelCenter(out Vector3 modelCenter))
        {
            Vector3 lensDirection = GetBeamWorldOrigin() - modelCenter;
            if (lensDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion correction = Quaternion.FromToRotation(lensDirection.normalized, targetVisualDirection);
            targetRotation = correction * visualTransform.rotation;
        }
        else
        {
            Vector3 localAimDirection = visualAimLocalDirection.sqrMagnitude > 0.0001f
                ? visualAimLocalDirection.normalized
                : (Quaternion.Euler(beamLocalEuler) * Vector3.forward).normalized;
            Quaternion localAimToForward = Quaternion.Inverse(Quaternion.LookRotation(localAimDirection, Vector3.up));
            targetRotation = Quaternion.LookRotation(targetVisualDirection, Vector3.up) * localAimToForward;
        }

        visualTransform.rotation = immediate || itemAimSpeed <= 0f
            ? targetRotation
            : Quaternion.Slerp(visualTransform.rotation, targetRotation, itemAimSpeed * Time.deltaTime);
    }

    private void UpdateBeamShape()
    {
        if (!IsLighterActive || beamObject == null || heldVisualRoot == null)
        {
            return;
        }

        float targetLength = CalculateVisibleBeamLength();
        if (Mathf.Abs(targetLength - currentBeamLength) < 0.005f && !hasBeamAim)
        {
            return;
        }

        currentBeamLength = targetLength;
        ApplyBeamTransform(currentBeamLength);
    }

    private float CalculateVisibleBeamLength()
    {
        float maxLength = Mathf.Max(minBeamLength, beamLength);
        if (forceBeamToCurrentItem && hasBeamAim)
        {
            float targetDistance = Vector3.Distance(GetBeamWorldOrigin(), currentBeamTargetPoint) + itemSurfacePadding;
            return Mathf.Clamp(targetDistance, minBeamLength, Mathf.Max(maxLength, targetDistance));
        }

        if (!stopBeamAtObjects || heldVisualRoot == null)
        {
            return maxLength;
        }

        Vector3 origin = GetBeamWorldOrigin();
        Vector3 direction = GetBeamWorldDirection();
        float castRadius = Mathf.Max(0.01f, Mathf.Max(beamRadius, Mathf.Max(beamHorizontalRadius, beamVerticalRadius)) * 0.85f);

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            castRadius,
            direction,
            maxLength,
            beamBlockerMask,
            beamBlockerTriggerInteraction);

        float closest = maxLength;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || ShouldIgnoreBeamHit(hitCollider))
            {
                continue;
            }

            closest = Mathf.Min(closest, Mathf.Max(minBeamLength, hits[i].distance - beamStopPadding));
        }

        closest = Mathf.Min(closest, CalculateCurrentItemHitLength(origin, direction, maxLength));
        return Mathf.Clamp(closest, minBeamLength, maxLength);
    }

    private float CalculateCurrentItemHitLength(Vector3 origin, Vector3 direction, float maxLength)
    {
        if (!TryGetCurrentItem(out Item item) || item == null)
        {
            return maxLength;
        }

        Ray beamRay = new Ray(origin, direction);
        float closest = maxLength;

        Collider[] itemColliders = item.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < itemColliders.Length; i++)
        {
            Collider itemCollider = itemColliders[i];
            if (itemCollider == null || !itemCollider.enabled || ShouldIgnoreBeamHit(itemCollider))
            {
                continue;
            }

            if (itemCollider.Raycast(beamRay, out RaycastHit hit, maxLength))
            {
                closest = Mathf.Min(closest, Mathf.Max(minBeamLength, hit.distance - beamStopPadding));
            }
        }

        if (closest < maxLength)
        {
            return closest;
        }

        Renderer[] itemRenderers = item.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < itemRenderers.Length; i++)
        {
            Renderer itemRenderer = itemRenderers[i];
            if (itemRenderer == null || !itemRenderer.enabled)
            {
                continue;
            }

            if (itemRenderer.GetComponent<UVRevealable>() != null ||
                itemRenderer.GetComponentInParent<UVRevealable>() != null)
            {
                continue;
            }

            if (itemRenderer.bounds.IntersectRay(beamRay, out float distance) && distance <= maxLength)
            {
                closest = Mathf.Min(closest, Mathf.Max(minBeamLength, distance - beamStopPadding));
            }
        }

        return closest;
    }

    private bool TryCalculateBeamTarget(Item item, out Vector3 targetPoint)
    {
        targetPoint = Vector3.zero;
        if (item == null)
        {
            return false;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null && aimBeamAtMouseProjectedPoint)
        {
            Ray mouseRay = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (TryRaycastItem(mouseRay, item, out RaycastHit hit))
            {
                targetPoint = ApplyDistanceCenterBias(item, hit.point);
                return true;
            }

            if (TryGetCurrentItemBounds(item, out Bounds bounds))
            {
                float projectedDistance = Mathf.Max(0f, Vector3.Dot(bounds.center - mouseRay.origin, mouseRay.direction));
                Vector3 projectedPoint = mouseRay.GetPoint(projectedDistance);
                Vector3 closestPoint = bounds.ClosestPoint(projectedPoint);
                float fallbackBias = Mathf.Max(itemTargetFallbackCenterBias, CalculateDistanceCenterBias(bounds.center));
                targetPoint = Vector3.Lerp(closestPoint, bounds.center, fallbackBias);
                return true;
            }
        }

        return TryGetCurrentItemCenter(item, out targetPoint);
    }

    private Vector3 ApplyDistanceCenterBias(Item item, Vector3 rawTargetPoint)
    {
        if (item == null || !TryGetCurrentItemBounds(item, out Bounds bounds))
        {
            return rawTargetPoint;
        }

        Vector3 assistedCenter = bounds.center + itemAimOffset;
        float centerBias = CalculateDistanceCenterBias(bounds.center);
        return Vector3.Lerp(rawTargetPoint, assistedCenter, centerBias);
    }

    private float CalculateDistanceCenterBias(Vector3 itemCenter)
    {
        Vector3 origin = GetBeamWorldOrigin();
        Vector2 originYZ = new Vector2(origin.y, origin.z);
        Vector2 itemYZ = new Vector2(itemCenter.y, itemCenter.z);
        float distance = Vector2.Distance(originYZ, itemYZ);
        float startDistance = Mathf.Max(0.01f, centerBiasStartDistance);
        float fullDistance = Mathf.Max(startDistance + 0.01f, centerBiasFullDistance);
        float t = Mathf.InverseLerp(startDistance, fullDistance, distance);
        return Mathf.Lerp(nearItemTargetCenterBias, farItemTargetCenterBias, t);
    }

    private bool TryRaycastItem(Ray ray, Item item, out RaycastHit closestHit)
    {
        closestHit = default;
        if (item == null)
        {
            return false;
        }

        bool hasHit = false;
        float closestDistance = float.MaxValue;
        Collider[] colliders = item.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider itemCollider = colliders[i];
            if (itemCollider == null || !itemCollider.enabled || IsUVRevealableCollider(itemCollider))
            {
                continue;
            }

            if (itemCollider.Raycast(ray, out RaycastHit hit, decorativeLightRange * 2f) && hit.distance < closestDistance)
            {
                closestHit = hit;
                closestDistance = hit.distance;
                hasHit = true;
            }
        }

        return hasHit;
    }

    private static bool TryGetCurrentItemBounds(Item item, out Bounds bounds)
    {
        bounds = default;
        if (item == null)
        {
            return false;
        }

        bool hasBounds = false;
        Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer itemRenderer = renderers[i];
            if (itemRenderer == null || !itemRenderer.enabled || IsUVRevealableRenderer(itemRenderer))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = itemRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(itemRenderer.bounds);
            }
        }

        Collider[] colliders = item.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider itemCollider = colliders[i];
            if (itemCollider == null || !itemCollider.enabled || IsUVRevealableCollider(itemCollider))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = itemCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(itemCollider.bounds);
            }
        }

        return hasBounds;
    }

    private bool ShouldIgnoreBeamHit(Collider hitCollider)
    {
        if (hitCollider.transform.IsChildOf(transform))
        {
            return true;
        }

        if (uVOnTable != null && hitCollider.transform.IsChildOf(uVOnTable.transform))
        {
            return true;
        }

        if (IsUVRevealableCollider(hitCollider))
        {
            return true;
        }

        return false;
    }

    private static bool IsUVRevealableCollider(Collider itemCollider)
    {
        return itemCollider != null &&
               (itemCollider.GetComponent<UVRevealable>() != null ||
                itemCollider.GetComponentInParent<UVRevealable>() != null);
    }

    private static bool IsUVRevealableRenderer(Renderer itemRenderer)
    {
        return itemRenderer != null &&
               (itemRenderer.GetComponent<UVRevealable>() != null ||
                itemRenderer.GetComponentInParent<UVRevealable>() != null);
    }

    private Vector3 GetBeamWorldOrigin()
    {
        return heldVisualRoot != null
            ? heldVisualRoot.transform.TransformPoint(beamLocalOffset)
            : transform.TransformPoint(beamLocalOffset);
    }

    private Vector3 GetBeamWorldDirection()
    {
        if (hasBeamAim && currentBeamWorldDirection.sqrMagnitude > 0.0001f)
        {
            return currentBeamWorldDirection.normalized;
        }

        Quaternion localRotation = Quaternion.Euler(beamLocalEuler);
        return heldVisualRoot != null
            ? heldVisualRoot.transform.TransformDirection(localRotation * Vector3.forward).normalized
            : transform.TransformDirection(localRotation * Vector3.forward).normalized;
    }

    private void ApplyBeamTransform(float visibleLength)
    {
        if (beamObject == null)
        {
            return;
        }

        float clampedLength = Mathf.Max(minBeamLength, visibleLength);
        float horizontalRadius = Mathf.Max(0.01f, beamHorizontalRadius > 0f ? beamHorizontalRadius : beamRadius);
        float verticalRadius = Mathf.Max(0.01f, beamVerticalRadius > 0f ? beamVerticalRadius : beamRadius);

        if (forceBeamToCurrentItem && hasBeamAim)
        {
            Vector3 origin = GetBeamWorldOrigin();
            Vector3 direction = GetBeamWorldDirection();
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
            beamObject.transform.SetPositionAndRotation(origin + direction * (clampedLength * 0.5f), rotation);
            beamObject.transform.localScale = new Vector3(horizontalRadius * 2f, clampedLength * 0.5f, verticalRadius * 2f);
            UpdateWorldSpaceLight(origin, direction, clampedLength);
            return;
        }

        beamObject.transform.localPosition = beamLocalOffset + (Quaternion.Euler(beamLocalEuler) * Vector3.forward) * (clampedLength * 0.5f);
        beamObject.transform.localRotation = Quaternion.Euler(beamLocalEuler) * Quaternion.Euler(90f, 0f, 0f);
        beamObject.transform.localScale = new Vector3(horizontalRadius * 2f, clampedLength * 0.5f, verticalRadius * 2f);
    }

    private void UpdateWorldSpaceLight(Vector3 origin, Vector3 direction, float visibleLength)
    {
        if (!syncLightToBeam || lighter == null)
        {
            return;
        }

        lighter.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction, Vector3.up));
        if (uvSpotLight != null)
        {
            uvSpotLight.range = Mathf.Max(decorativeLightRange, visibleLength + itemSurfacePadding);
        }
    }

    private void PlaceUVLight()
    {
        if (lighter == null)
        {
            return;
        }

        EnsureHeldVisualRootOnly();
        lighter.transform.SetParent(heldVisualRoot.transform, false);
        lighter.transform.localPosition = beamLocalOffset;
        lighter.transform.localRotation = Quaternion.Euler(beamLocalEuler);
        lighter.transform.localScale = Vector3.one;
    }

    private void ConfigureUVLight()
    {
        if (!syncLightToBeam || lighter == null)
        {
            return;
        }

        Light uvLight = lighter.GetComponent<Light>();
        if (uvLight == null)
        {
            return;
        }

        uvSpotLight = uvLight;
        uvLight.type = LightType.Spot;
        uvLight.range = decorativeLightRange;
        uvLight.intensity = decorativeLightIntensity;
        uvLight.spotAngle = lightSpotAngle;
        uvLight.innerSpotAngle = Mathf.Min(lightSpotAngle, Mathf.Max(1f, lightSpotAngle * 0.75f));
        uvLight.color = new Color(beamColor.r, beamColor.g, beamColor.b, 1f);
        uvLight.shadows = uvLightShadows;
        uvLight.shadowStrength = uvLightShadowStrength;
    }

    private void CacheToolRenderers()
    {
        toolRenderers = GetComponentsInChildren<Renderer>(true);
    }

    private void CenterHeldModelOnPivotOnce()
    {
        if (heldModelCentered || heldModelInstance == null || heldVisualRoot == null)
        {
            return;
        }

        Renderer[] renderers = heldModelInstance.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }
        }

        Vector3 localCenter = heldVisualRoot.transform.InverseTransformPoint(worldBounds.center);
        heldModelInstance.transform.localPosition += heldModelLocalPosition + centeredModelOffset - localCenter;
        heldModelCentered = true;
    }

    private bool TryGetHeldModelCenter(out Vector3 center)
    {
        center = Vector3.zero;
        GameObject modelRoot = heldModelInstance != null ? heldModelInstance : heldVisualRoot;
        if (modelRoot == null)
        {
            return false;
        }

        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer == beamRenderer || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
        {
            center = modelRoot.transform.position;
            return true;
        }

        center = bounds.center;
        return true;
    }

    private void SetToolRenderersActive(bool active)
    {
        if (toolRenderers == null)
        {
            CacheToolRenderers();
        }

        for (int i = 0; i < toolRenderers.Length; i++)
        {
            Renderer renderer = toolRenderers[i];
            if (renderer == null || renderer == beamRenderer)
            {
                continue;
            }

            if (uVOnTable != null && renderer.transform.IsChildOf(uVOnTable.transform))
            {
                continue;
            }

            renderer.enabled = active;
        }
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
