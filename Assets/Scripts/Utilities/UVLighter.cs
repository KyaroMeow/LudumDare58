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

    [Header("Beam")]
    [SerializeField, Min(0.05f)] private float beamLength = 1.35f;
    [SerializeField, Min(0.01f)] private float beamRadius = 0.035f;
    [SerializeField, Min(0.01f)] private float beamHorizontalRadius = 0.035f;
    [SerializeField, Min(0.01f)] private float beamVerticalRadius = 0.035f;
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

    [Header("Light")]
    [SerializeField] private bool syncLightToBeam = true;
    [SerializeField, Min(0.1f)] private float decorativeLightRange = 4.2f;
    [SerializeField, Min(0f)] private float decorativeLightIntensity = 4.25f;
    [SerializeField, Min(1f)] private float lightSpotAngle = 36f;
    [SerializeField, Range(0.1f, 1.25f)] private float revealConeAngleScale = 0.7f;
    [SerializeField, Range(0f, 1f)] private float revealConeSoftness = 0.35f;
    [SerializeField] private LightShadows uvLightShadows = LightShadows.Soft;
    [SerializeField, Range(0f, 1f)] private float uvLightShadowStrength = 0.65f;

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
    private Quaternion baseHeldLocalRotation;

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
        UpdateBeamShape();
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
        UpdateBeamShape();
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
        if (distance <= 0.001f || distance > decorativeLightRange)
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
        float distanceStrength = 1f - Mathf.Clamp01(distance / decorativeLightRange);
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

        transform.position = immediate || followSpeed <= 0f
            ? worldPos
            : Vector3.Lerp(transform.position, worldPos, followSpeed * Time.deltaTime);
    }

    private void UpdateSmartAim(bool immediate = false)
    {
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
            CenterHeldModelOnPivot();
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
    }

    private void UpdateBeamShape()
    {
        if (!IsLighterActive || beamObject == null || heldVisualRoot == null)
        {
            return;
        }

        float targetLength = CalculateVisibleBeamLength();
        if (Mathf.Abs(targetLength - currentBeamLength) < 0.005f)
        {
            return;
        }

        currentBeamLength = targetLength;
        ApplyBeamTransform(currentBeamLength);
    }

    private float CalculateVisibleBeamLength()
    {
        float maxLength = Mathf.Max(minBeamLength, beamLength);
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

        if (hitCollider.GetComponent<UVRevealable>() != null ||
            hitCollider.GetComponentInParent<UVRevealable>() != null)
        {
            return true;
        }

        return false;
    }

    private Vector3 GetBeamWorldOrigin()
    {
        return heldVisualRoot != null
            ? heldVisualRoot.transform.TransformPoint(beamLocalOffset)
            : transform.TransformPoint(beamLocalOffset);
    }

    private Vector3 GetBeamWorldDirection()
    {
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
        beamObject.transform.localPosition = beamLocalOffset + (Quaternion.Euler(beamLocalEuler) * Vector3.forward) * (clampedLength * 0.5f);
        beamObject.transform.localRotation = Quaternion.Euler(beamLocalEuler) * Quaternion.Euler(90f, 0f, 0f);
        float horizontalRadius = Mathf.Max(0.01f, beamHorizontalRadius > 0f ? beamHorizontalRadius : beamRadius);
        float verticalRadius = Mathf.Max(0.01f, beamVerticalRadius > 0f ? beamVerticalRadius : beamRadius);
        beamObject.transform.localScale = new Vector3(horizontalRadius * 2f, clampedLength * 0.5f, verticalRadius * 2f);
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

    private void CenterHeldModelOnPivot()
    {
        if (heldModelInstance == null || heldVisualRoot == null)
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
