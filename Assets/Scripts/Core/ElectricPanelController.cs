using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricPanelController : MonoBehaviour
{
    public enum PanelState
    {
        Locked,
        Charging,
        Ready,
        BlackoutActive,
        RestoreWarning,
        Restoring,
        Cooldown
    }

    private sealed class LightState
    {
        public Light Light;
        public bool Enabled;
        public float Intensity;
        public Color Color;
        public bool IsEmergency;
    }

    private sealed class CameraLightState
    {
        public Light Light;
        public bool Enabled;
        public float Intensity;
        public Color Color;
    }

    private sealed class GaugeNeedleState
    {
        public Transform Needle;
        public Quaternion InitialLocalRotation;
    }

    public static ElectricPanelController Instance { get; private set; }
    public event System.Action BlackoutStarted;
    public event System.Action BlackoutEnded;

    [Header("Debug and Timing")]
    [SerializeField] private bool debugAllowPanelBeforeHandIntro = true;
    [SerializeField] private float chargeDuration = 45f;
    [SerializeField] private float blackoutDuration = 20f;
    [SerializeField] private float cameraRestoreWarningTime = 5f;
    [SerializeField] private float cooldownDuration = 10f;
    [SerializeField] private float leverAnimationDuration = 0.35f;
    [SerializeField] private float blackoutFadeDuration = 0.75f;
    [SerializeField] private float restoreFadeDuration = 1.5f;
    [SerializeField] private float lightFlickerDuration = 0.8f;
    [SerializeField] private float mainLightIntensityMultiplier = 0.15f;
    [SerializeField] private float emergencyLightIntensityMultiplier = 1f;
    [SerializeField] private bool affectAllSceneLights = true;
    [SerializeField] private bool autoFindSceneLights = true;
    [SerializeField] private bool autoFindSecurityCamera = true;
    [SerializeField] private bool autoFindIndicatorObjects = true;
    [SerializeField] private bool showDebugGui = true;

    [Header("Story Intro Lock")]
    [SerializeField] private float preIntroVoltageMin = 0.2f;
    [SerializeField] private float preIntroVoltageMax = 0.3f;
    [SerializeField] private float preIntroVoltageNoiseSpeed = 0.65f;

    [Header("Lever")]
    [SerializeField] private Transform leverVisual;
    [SerializeField] private Transform leverRestPose;
    [SerializeField] private Transform leverPulledPose;
    [SerializeField] private bool usePoseBasedLeverAnimation = true;
    [SerializeField] private bool createLeverPoseHelpersIfMissing = true;
    [SerializeField] private bool animateLeverPosition;
    [SerializeField] private bool animateLeverRotation = true;
    [SerializeField] private Vector3 leverLocalRotationAxis = Vector3.right;
    [SerializeField] private float leverUpAngle = 0f;
    [SerializeField] private float leverDownAngle = -65f;
    [SerializeField] private AnimationCurve leverAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool useSafeLeverAnimation = true;
    [SerializeField] private bool useExplicitLeverPoses = true;
    [SerializeField] private Vector3 leverRestLocalEuler;
    [SerializeField] private Vector3 leverPulledLocalEuler;
    [SerializeField] private bool captureCurrentPoseAsRestOnAwake = true;
    [SerializeField] private Vector3 leverPulledLocalEulerOffset = new Vector3(0f, 0f, -65f);
    [SerializeField] private bool useLeverLocalPositionOffset;
    [SerializeField] private Vector3 leverPulledLocalPositionOffset;

    [Header("Indicator")]
    [SerializeField] private ElectricPanelVoltageSegmentIndicator voltageSegmentIndicator;
    [SerializeField] private bool useSegmentedVoltageIndicator = true;
    [SerializeField] private Renderer chargeBarRenderer;
    [SerializeField] private Renderer chargeBarBackgroundRenderer;
    [SerializeField] private Transform chargeBarTransform;
    [SerializeField] private Transform chargeFillVisual;
    [SerializeField] private Renderer chargeFillRenderer;
    [SerializeField] private bool useGeneratedChargeFillIfNeeded;
    [SerializeField] private bool useGeneratedChargeFill;
    [SerializeField] private Vector3 generatedFillLocalPosition;
    [SerializeField] private Vector3 generatedFillLocalRotation;
    [SerializeField] private Vector3 generatedFillFullScale = Vector3.one;
    [SerializeField] private Vector3 chargeFillAxis = Vector3.right;
    [SerializeField] private bool chargeFillAnchoredFromLeft = true;
    [SerializeField] private Vector3 chargeBarFillAxis = Vector3.right;
    [SerializeField] private bool useScaleBasedChargeBar;
    [SerializeField] private float minChargeScale = 0.03f;
    [SerializeField] private float maxChargeScale = 1f;
    [SerializeField] private bool chargeBarAnchoredFromLeft = true;
    [SerializeField] private bool anchorChargeBarFromStart = true;
    [SerializeField] private bool enableVoltageJitter = true;
    [SerializeField] private float voltageJitterLowAmplitude = 0.015f;
    [SerializeField] private float voltageJitterMidAmplitude = 0.035f;
    [SerializeField] private float voltageJitterHighAmplitude = 0.07f;
    [SerializeField] private float voltageJitterLowSpeed = 5f;
    [SerializeField] private float voltageJitterMidSpeed = 10f;
    [SerializeField] private float voltageJitterHighSpeed = 18f;
    [SerializeField] private float voltageNoiseBlend = 0.5f;
    [SerializeField] private bool smoothVoltageValue = true;
    [SerializeField] private float voltageSmoothSpeed = 8f;
    [SerializeField] private Gradient chargeColorGradient = CreateDefaultChargeGradient();
    [SerializeField] private Renderer[] decorativeGaugeRenderers;
    [SerializeField] private Transform[] decorativeGaugeNeedles;
    [SerializeField] private bool animateGaugeNeedles = true;
    [SerializeField] private float gaugeNeedleLowShakeAngle = 1f;
    [SerializeField] private float gaugeNeedleMidShakeAngle = 3f;
    [SerializeField] private float gaugeNeedleHighShakeAngle = 7f;
    [SerializeField] private float gaugeNeedleLowSpeed = 4f;
    [SerializeField] private float gaugeNeedleMidSpeed = 9f;
    [SerializeField] private float gaugeNeedleHighSpeed = 16f;
    [SerializeField] private Vector3 gaugeNeedleLocalAxis = Vector3.forward;
    [SerializeField] private Color chargeLowColor = Color.green;
    [SerializeField] private Color chargeMediumColor = new Color(1f, 0.65f, 0f, 1f);
    [SerializeField] private Color chargeHighColor = Color.red;
    [SerializeField] private Color chargeWarningColor = Color.red;
    [SerializeField] private float mediumChargeThreshold = 0.5f;
    [SerializeField] private float highChargeThreshold = 0.8f;
    [SerializeField] private Renderer[] indicatorSegments;
    [SerializeField] private Renderer[] indicatorExtraRenderers;
    [SerializeField] private Color indicatorLockedColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color indicatorChargingColor = new Color(0.1f, 0.65f, 1f, 1f);
    [SerializeField] private Color indicatorReadyColor = Color.red;
    [SerializeField] private Color indicatorBlackoutColor = new Color(1f, 0.2f, 0.05f, 1f);
    [SerializeField] private Color indicatorWarningColor = Color.yellow;
    [SerializeField] private Color indicatorOffColor = Color.black;
    [SerializeField] private string[] indicatorAutoFindKeywords = { "indicator", "indicator_plane", "indicator_circle", "indicator_line" };

    [Header("Lights")]
    [SerializeField] private Light[] sceneLights;
    [SerializeField] private Light[] emergencyLights;
    [SerializeField] private string[] emergencyLightNameKeywords = { "Emergency", "Red", "Reserve", "Backup", "Alarm" };

    [Header("Security Camera")]
    [SerializeField] private GameObject securityCameraRoot;
    [SerializeField] private Transform securityCameraVisual;
    [SerializeField] private Light[] cameraLights;
    [SerializeField] private Renderer[] cameraIndicatorRenderers;
    [SerializeField] private Animator[] cameraAnimators;
    [SerializeField] private Color cameraOnlineColor = Color.green;
    [SerializeField] private Color cameraOfflineColor = Color.black;
    [SerializeField] private Color cameraWarningColor = Color.red;
    [SerializeField] private string securityCameraRootName = "SecurityCam";
    [SerializeField] private string securityCameraVisualName = "cam_low";
    [SerializeField] private string cameraLightName = "LightOfCam";

    [Header("SFX")]
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue leverClickSfx;
    [SerializeField] private SfxCue powerDownSfx;
    [SerializeField] private SfxCue powerRestoreSfx;
    [SerializeField] private SfxCue cameraWarningSfx;
    [SerializeField] private SfxCue leverDeniedSfx;
    [SerializeField] private AudioClip leverClickAudioClip;
    [SerializeField] private AudioClip powerDownAudioClip;
    [SerializeField] private AudioClip powerRestoreAudioClip;
    [SerializeField] private AudioClip cameraWarningAudioClip;
    [SerializeField] private AudioClip leverDeniedAudioClip;
    [SerializeField] private float deniedFeedbackCooldown = 0.25f;

    private readonly List<LightState> lightStates = new List<LightState>();
    private readonly List<CameraLightState> cameraLightStates = new List<CameraLightState>();
    private readonly List<GaugeNeedleState> gaugeNeedleStates = new List<GaugeNeedleState>();
    private readonly Dictionary<Animator, float> cameraAnimatorSpeeds = new Dictionary<Animator, float>();
    private readonly HashSet<string> missingSfxWarnings = new HashSet<string>();

    private PanelState state = PanelState.Locked;
    private MaterialPropertyBlock propertyBlock;
    private Quaternion leverInitialLocalRotation;
    private Vector3 leverInitialLocalPosition;
    private Quaternion leverRestLocalRotation;
    private Quaternion leverPulledLocalRotation;
    private Vector3 leverRestLocalPosition;
    private Vector3 leverPulledLocalPosition;
    private Vector3 chargeBarInitialLocalScale;
    private Vector3 chargeBarInitialLocalPosition;
    private Bounds chargeBarInitialLocalBounds;
    private Transform cachedChargeFillTransform;
    private float charge01;
    private float smoothedVisualCharge;
    private float blackoutRemaining;
    private float cooldownRemaining;
    private bool initialized;
    private bool warningStarted;
    private bool warnedMissingCamera;
    private Coroutine leverRoutine;
    private Coroutine lightRoutine;
    private GameAudioManager gameAudioManager;
    private AudioSource fallbackAudioSource;
    private bool chargeBarPoseCached;
    private bool leverPoseCached;
    private bool voltageValueInitialized;
    private bool gaugeNeedlePoseCached;
    private bool warnedMissingGaugeNeedles;
    private bool storyIntroLockActive;
    private bool storyBlackoutActive;
    private bool storyBlackoutRestoring;
    private float lastDeniedFeedbackTime = -999f;

    public bool IsBlackoutActive => storyBlackoutActive || state == PanelState.BlackoutActive || state == PanelState.RestoreWarning;
    public bool IsReady => state == PanelState.Ready;
    public float Charge01 => charge01;
    public float BlackoutRemaining => Mathf.Max(0f, blackoutRemaining);
    public float CooldownRemaining => Mathf.Max(0f, cooldownRemaining);
    public PanelState CurrentState => state;
    public bool IsStoryIntroLockActive => storyIntroLockActive;
    public bool IsStoryBlackoutActive => storyBlackoutActive || storyBlackoutRestoring;
    public Transform LeverTransform => leverVisual;

    private static Gradient CreateDefaultChargeGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.green, 0f),
                new GradientColorKey(Color.green, 0.45f),
                new GradientColorKey(new Color(1f, 0.65f, 0f, 1f), 0.75f),
                new GradientColorKey(Color.red, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });

        return gradient;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        propertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        EnsureInitialized();
    }

    private void Update()
    {
        EnsureInitialized();

        if (storyBlackoutActive || storyBlackoutRestoring)
        {
            UpdateIndicatorVisuals();
            UpdateCameraWarningVisuals();
            return;
        }

        if (storyIntroLockActive && state == PanelState.Locked)
        {
            UpdatePreIntroVoltageVisual();
        }

        if (!storyIntroLockActive && state == PanelState.Locked && debugAllowPanelBeforeHandIntro && IsShiftReadyForDebugCharge())
        {
            StartCharging();
        }

        if (state == PanelState.Charging)
        {
            charge01 = Mathf.Clamp01(charge01 + Time.deltaTime / Mathf.Max(0.01f, chargeDuration));
            if (charge01 >= 1f)
            {
                state = PanelState.Ready;
                Debug.Log("Electric panel ready.");
            }
        }
        else if (state == PanelState.BlackoutActive || state == PanelState.RestoreWarning)
        {
            UpdateBlackoutTimer();
        }
        else if (state == PanelState.Cooldown)
        {
            cooldownRemaining -= Time.deltaTime;
            if (cooldownRemaining <= 0f)
            {
                charge01 = 0f;
                StartCharging();
            }
        }

        UpdateIndicatorVisuals();
        UpdateCameraWarningVisuals();
    }

    public void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        gameAudioManager = FindFirstObjectByType<GameAudioManager>();
        ResolveSfxEmitter();
        ResolveLeverVisual();
        ResolveLeverPoseHelpers();
        ResolveChargeBar();
        ResolveVoltageSegmentIndicator();
        ResolveIndicators();
        ResolveSecurityCamera();
        ResolveSceneLights();
        CacheLightStates();
        CacheCameraStates();
        CacheChargeBarPose();
        CacheLeverPose();
        CacheGaugeNeedleStates();
        ApplyCameraOnline();
        UpdateIndicatorVisuals();

        if (!storyIntroLockActive && debugAllowPanelBeforeHandIntro && IsShiftReadyForDebugCharge())
        {
            StartCharging();
        }
        else
        {
            state = PanelState.Locked;
        }
    }

    public void RegisterLeverVisual(Transform visual)
    {
        if (leverVisual == null && visual != null)
        {
            leverVisual = visual;
            CacheLeverPose();
        }
    }

    public void UnlockAfterVentHandIntro()
    {
        storyIntroLockActive = false;
        if (state != PanelState.Locked)
        {
            return;
        }

        StartCharging();
    }

    public bool TryActivateBlackout()
    {
        EnsureInitialized();

        if (storyIntroLockActive || storyBlackoutActive || storyBlackoutRestoring)
        {
            Debug.Log("Electric panel lever ignored. Vent hand intro has not unlocked the panel yet.");
            PlayDeniedFeedback();
            return false;
        }

        if (state != PanelState.Ready)
        {
            Debug.Log($"Electric panel lever ignored. Current state: {state}.");
            PlayDeniedFeedback();
            return false;
        }

        StartBlackout();
        return true;
    }

    public void RestorePower()
    {
        if (storyBlackoutActive || storyBlackoutRestoring)
        {
            Debug.Log("RestorePower ignored because story blackout controls restoration.");
            return;
        }

        if (state == PanelState.Restoring || state == PanelState.Cooldown)
        {
            return;
        }

        StartCoroutine(RestorePowerRoutine());
    }

    public void SetStoryIntroLockActive(bool active, float minVisual = 0.2f, float maxVisual = 0.3f)
    {
        EnsureInitialized();
        storyIntroLockActive = active;
        preIntroVoltageMin = Mathf.Clamp01(Mathf.Min(minVisual, maxVisual));
        preIntroVoltageMax = Mathf.Clamp01(Mathf.Max(minVisual, maxVisual));

        if (active && !storyBlackoutActive && !storyBlackoutRestoring)
        {
            state = PanelState.Locked;
            warningStarted = false;
            cooldownRemaining = 0f;
            blackoutRemaining = 0f;
            UpdatePreIntroVoltageVisual();
            Debug.Log("Electric panel story intro lock enabled.");
        }
        else if (!active)
        {
            Debug.Log("Electric panel story intro lock disabled.");
        }
    }

    public void BeginStoryBlackout()
    {
        EnsureInitialized();

        if (storyBlackoutActive)
        {
            return;
        }

        storyBlackoutActive = true;
        storyBlackoutRestoring = false;
        state = PanelState.BlackoutActive;
        charge01 = 0f;
        blackoutRemaining = 0f;
        warningStarted = true;

        Debug.Log("Electric panel story blackout started.");
        PlaySfx(powerDownSfx, powerDownAudioClip, nameof(powerDownSfx));
        gameAudioManager?.OnBlackoutStarted();

        SecuritySystem securitySystem = ResolveSecuritySystem();
        securitySystem?.SetSecurityEnabled(false);

        ApplyCameraOffline();
        StartLightRoutine(ApplyBlackoutLightsRoutine());
        UpdateIndicatorVisuals();
    }

    public void EndStoryBlackout()
    {
        StartCoroutine(EndStoryBlackoutRoutine(false));
    }

    public IEnumerator EndStoryBlackoutRoutine(bool unlockAfterRestore)
    {
        EnsureInitialized();

        if (!storyBlackoutActive && !storyBlackoutRestoring)
        {
            if (unlockAfterRestore)
            {
                UnlockAfterVentHandIntro();
            }

            yield break;
        }

        storyBlackoutActive = false;
        storyBlackoutRestoring = true;
        state = PanelState.Restoring;
        blackoutRemaining = 0f;
        charge01 = 0f;

        Debug.Log("Electric panel ending story blackout.");
        PlaySfx(powerRestoreSfx, powerRestoreAudioClip, nameof(powerRestoreSfx));
        gameAudioManager?.OnBlackoutEnded();

        SecuritySystem securitySystem = ResolveSecuritySystem();
        securitySystem?.SetSecurityEnabled(true);

        ApplyCameraOnline();
        yield return RestoreLightsRoutine();

        storyBlackoutRestoring = false;
        state = PanelState.Locked;
        if (unlockAfterRestore)
        {
            UnlockAfterVentHandIntro();
        }
    }

    private void StartCharging()
    {
        state = PanelState.Charging;
        warningStarted = false;
        blackoutRemaining = 0f;
        cooldownRemaining = 0f;
        Debug.Log("Electric panel charging started.");
    }

    private void UpdatePreIntroVoltageVisual()
    {
        float min = Mathf.Clamp01(Mathf.Min(preIntroVoltageMin, preIntroVoltageMax));
        float max = Mathf.Clamp01(Mathf.Max(preIntroVoltageMin, preIntroVoltageMax));
        float noise = Mathf.PerlinNoise(Time.time * Mathf.Max(0.01f, preIntroVoltageNoiseSpeed), 24.7f);
        charge01 = Mathf.Lerp(min, max, noise);
    }

    private void PlayDeniedFeedback()
    {
        if (Time.unscaledTime - lastDeniedFeedbackTime < deniedFeedbackCooldown)
        {
            return;
        }

        lastDeniedFeedbackTime = Time.unscaledTime;
        PlaySfx(leverDeniedSfx, leverDeniedAudioClip, nameof(leverDeniedSfx));
    }

    private void StartBlackout()
    {
        state = PanelState.BlackoutActive;
        charge01 = 1f;
        blackoutRemaining = Mathf.Max(0.01f, blackoutDuration);
        warningStarted = false;

        Debug.Log("Electric panel blackout started.");
        PlaySfx(leverClickSfx, leverClickAudioClip, nameof(leverClickSfx));
        PlaySfx(powerDownSfx, powerDownAudioClip, nameof(powerDownSfx));
        gameAudioManager?.OnBlackoutStarted();

        SecuritySystem securitySystem = ResolveSecuritySystem();
        securitySystem?.SetSecurityEnabled(false);

        ApplyCameraOffline();
        AnimateLeverPulled();
        StartLightRoutine(ApplyBlackoutLightsRoutine());
        BlackoutStarted?.Invoke();
    }

    private void UpdateBlackoutTimer()
    {
        blackoutRemaining -= Time.deltaTime;
        charge01 = Mathf.Clamp01(blackoutRemaining / Mathf.Max(0.01f, blackoutDuration));

        if (!warningStarted && blackoutRemaining <= Mathf.Max(0f, cameraRestoreWarningTime))
        {
            EnterRestoreWarning();
        }

        if (blackoutRemaining <= 0f)
        {
            RestorePower();
        }
    }

    private void EnterRestoreWarning()
    {
        warningStarted = true;
        state = PanelState.RestoreWarning;
        Debug.Log("Security camera restoring soon.");
        PlaySfx(cameraWarningSfx, cameraWarningAudioClip, nameof(cameraWarningSfx));
        gameAudioManager?.OnBlackoutRestoreWarning();
    }

    private IEnumerator RestorePowerRoutine()
    {
        state = PanelState.Restoring;
        blackoutRemaining = 0f;
        charge01 = 0f;

        Debug.Log("Electric panel restoring power.");
        PlaySfx(powerRestoreSfx, powerRestoreAudioClip, nameof(powerRestoreSfx));
        gameAudioManager?.OnBlackoutEnded();
        BlackoutEnded?.Invoke();

        SecuritySystem securitySystem = ResolveSecuritySystem();
        securitySystem?.SetSecurityEnabled(true);

        ApplyCameraOnline();
        AnimateLeverRest();
        yield return RestoreLightsRoutine();

        cooldownRemaining = Mathf.Max(0f, cooldownDuration);
        state = PanelState.Cooldown;
        Debug.Log("Electric panel cooldown started.");
    }

    private void StartLightRoutine(IEnumerator routine)
    {
        if (lightRoutine != null)
        {
            StopCoroutine(lightRoutine);
        }

        lightRoutine = StartCoroutine(routine);
    }

    private IEnumerator ApplyBlackoutLightsRoutine()
    {
        float elapsed = 0f;
        float safeFlicker = Mathf.Max(0.01f, lightFlickerDuration);
        while (elapsed < safeFlicker)
        {
            elapsed += Time.deltaTime;
            float flicker = Random.Range(0.55f, 1.15f);
            ApplyLightMultiplier(mainLightIntensityMultiplier * flicker, emergencyLightIntensityMultiplier);
            yield return null;
        }

        elapsed = 0f;
        float safeFade = Mathf.Max(0.01f, blackoutFadeDuration);
        while (elapsed < safeFade)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeFade);
            ApplyLightMultiplier(Mathf.Lerp(1f, mainLightIntensityMultiplier, t), emergencyLightIntensityMultiplier);
            yield return null;
        }

        ApplyLightMultiplier(mainLightIntensityMultiplier, emergencyLightIntensityMultiplier);
        lightRoutine = null;
    }

    private IEnumerator RestoreLightsRoutine()
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, restoreFadeDuration);
        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            RestoreLightStateLerp(t);
            yield return null;
        }

        RestoreLightStateLerp(1f);
        lightRoutine = null;
    }

    private void ApplyLightMultiplier(float mainMultiplier, float emergencyMultiplier)
    {
        for (int i = 0; i < lightStates.Count; i++)
        {
            LightState stateItem = lightStates[i];
            if (stateItem.Light == null)
            {
                continue;
            }

            float multiplier = stateItem.IsEmergency ? emergencyMultiplier : mainMultiplier;
            stateItem.Light.enabled = stateItem.Enabled || stateItem.IsEmergency;
            stateItem.Light.intensity = stateItem.Intensity * Mathf.Max(0f, multiplier);
        }
    }

    private void RestoreLightStateLerp(float t)
    {
        for (int i = 0; i < lightStates.Count; i++)
        {
            LightState stateItem = lightStates[i];
            if (stateItem.Light == null)
            {
                continue;
            }

            stateItem.Light.enabled = stateItem.Enabled;
            stateItem.Light.intensity = Mathf.Lerp(stateItem.Light.intensity, stateItem.Intensity, t);
            stateItem.Light.color = Color.Lerp(stateItem.Light.color, stateItem.Color, t);
        }
    }

    private void AnimateLeverPulled()
    {
        AnimateLever(true);
    }

    private void AnimateLeverRest()
    {
        AnimateLever(false);
    }

    private void AnimateLever(bool pulled)
    {
        if (!useSafeLeverAnimation || leverVisual == null)
        {
            return;
        }

        CacheLeverPose();

        if (leverRoutine != null)
        {
            StopCoroutine(leverRoutine);
        }

        if (usePoseBasedLeverAnimation && leverRestPose != null && leverPulledPose != null)
        {
            leverRoutine = StartCoroutine(AnimateLeverPoseRoutine(
                pulled ? leverPulledPose.localRotation : leverRestPose.localRotation,
                pulled ? leverPulledPose.localPosition : leverRestPose.localPosition));
            return;
        }

        if (useExplicitLeverPoses)
        {
            leverRoutine = StartCoroutine(AnimateLeverPoseRoutine(
                pulled ? leverPulledLocalRotation : leverRestLocalRotation,
                pulled ? leverPulledLocalPosition : leverRestLocalPosition));
            return;
        }

        leverRoutine = StartCoroutine(AnimateLeverAxisRoutine(pulled ? leverDownAngle : leverUpAngle));
    }

    private IEnumerator AnimateLeverAxisRoutine(float targetAngle)
    {
        Vector3 axis = leverLocalRotationAxis.sqrMagnitude > 0.001f ? leverLocalRotationAxis.normalized : Vector3.right;
        Quaternion startRotation = leverVisual.localRotation;
        Quaternion targetRotation = leverInitialLocalRotation * Quaternion.AngleAxis(targetAngle, axis);
        Vector3 startPosition = leverVisual.localPosition;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, leverAnimationDuration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float easedT = leverAnimationCurve != null ? leverAnimationCurve.Evaluate(t) : t;
            if (animateLeverRotation)
            {
                leverVisual.localRotation = Quaternion.Slerp(startRotation, targetRotation, easedT);
            }

            if (animateLeverPosition)
            {
                leverVisual.localPosition = Vector3.Lerp(startPosition, leverInitialLocalPosition, easedT);
            }
            yield return null;
        }

        if (animateLeverRotation)
        {
            leverVisual.localRotation = targetRotation;
        }

        if (animateLeverPosition)
        {
            leverVisual.localPosition = leverInitialLocalPosition;
        }
        leverRoutine = null;
    }

    private IEnumerator AnimateLeverPoseRoutine(Quaternion targetRotation, Vector3 targetPosition)
    {
        Quaternion startRotation = leverVisual.localRotation;
        Vector3 startPosition = leverVisual.localPosition;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, leverAnimationDuration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float easedT = leverAnimationCurve != null ? leverAnimationCurve.Evaluate(t) : t;
            if (animateLeverRotation)
            {
                leverVisual.localRotation = Quaternion.Slerp(startRotation, targetRotation, easedT);
            }

            if (animateLeverPosition)
            {
                leverVisual.localPosition = Vector3.Lerp(startPosition, targetPosition, easedT);
            }
            yield return null;
        }

        if (animateLeverRotation)
        {
            leverVisual.localRotation = targetRotation;
        }

        if (animateLeverPosition)
        {
            leverVisual.localPosition = targetPosition;
        }
        leverRoutine = null;
    }

    private void UpdateIndicatorVisuals()
    {
        Color activeColor = GetIndicatorColor();
        UpdateChargeBarVisual(activeColor);

        if (chargeBarRenderer != null)
        {
            return;
        }

        int segmentCount = indicatorSegments != null ? indicatorSegments.Length : 0;
        int filledSegments = segmentCount > 0 ? Mathf.CeilToInt(charge01 * segmentCount) : 0;

        if (state == PanelState.Ready)
        {
            filledSegments = segmentCount;
        }

        for (int i = 0; i < segmentCount; i++)
        {
            Renderer rendererItem = indicatorSegments[i];
            if (rendererItem == null)
            {
                continue;
            }

            SetRendererColor(rendererItem, i < filledSegments ? activeColor : indicatorOffColor);
        }

        ApplyColorToRenderers(indicatorExtraRenderers, activeColor);
    }

    private Color GetIndicatorColor()
    {
        if (state == PanelState.RestoreWarning)
        {
            return IsBlinkOn() ? indicatorWarningColor : indicatorOffColor;
        }

        switch (state)
        {
            case PanelState.Locked:
                return indicatorLockedColor;
            case PanelState.Charging:
                return indicatorChargingColor;
            case PanelState.Ready:
                return indicatorReadyColor;
            case PanelState.BlackoutActive:
                return indicatorBlackoutColor;
            case PanelState.Cooldown:
            case PanelState.Restoring:
                return indicatorLockedColor;
            default:
                return indicatorOffColor;
        }
    }

    private void UpdateChargeBarVisual(Color fallbackColor)
    {
        float gameplayCharge = GetGameplayCharge01();
        if (useSegmentedVoltageIndicator && voltageSegmentIndicator != null)
        {
            voltageSegmentIndicator.SetVoltage(
                gameplayCharge,
                state == PanelState.Ready,
                state == PanelState.BlackoutActive,
                state == PanelState.RestoreWarning,
                (state == PanelState.Locked && !storyIntroLockActive) || state == PanelState.Cooldown || state == PanelState.Restoring);
            UpdateGaugeNeedleVisuals(gameplayCharge);
            return;
        }

        Renderer activeRenderer = GetActiveChargeFillRenderer();
        if (activeRenderer == null)
        {
            return;
        }

        CacheChargeBarPose();

        float visualCharge = GetVisualCharge01(gameplayCharge);
        Color chargeColor = GetChargeBarColor(visualCharge, fallbackColor);

        if (state == PanelState.RestoreWarning && !IsBlinkOn())
        {
            chargeColor = indicatorOffColor;
        }

        SetRendererColor(activeRenderer, chargeColor);

        if (chargeBarBackgroundRenderer != null)
        {
            SetRendererColor(chargeBarBackgroundRenderer, indicatorLockedColor);
        }

        UpdateGaugeNeedleVisuals(visualCharge);

        Transform activeTransform = GetActiveChargeFillTransform();
        if (!useScaleBasedChargeBar || activeTransform == null)
        {
            return;
        }

        float normalizedScale = Mathf.Lerp(minChargeScale, maxChargeScale, Mathf.Clamp01(visualCharge));
        ApplyChargeBarScale(normalizedScale);
    }

    private float GetGameplayCharge01()
    {
        if (state == PanelState.Ready)
        {
            return 1f;
        }

        if (storyIntroLockActive && state == PanelState.Locked)
        {
            return Mathf.Clamp01(charge01);
        }

        if (state == PanelState.Locked || state == PanelState.Cooldown || state == PanelState.Restoring)
        {
            return 0f;
        }

        return Mathf.Clamp01(charge01);
    }

    private float GetVisualCharge01(float gameplayCharge)
    {
        float targetCharge = Mathf.Clamp01(gameplayCharge);
        if (enableVoltageJitter && (state == PanelState.Charging || state == PanelState.Ready || state == PanelState.BlackoutActive || state == PanelState.RestoreWarning))
        {
            float amplitude = ResolveByCharge(targetCharge, voltageJitterLowAmplitude, voltageJitterMidAmplitude, voltageJitterHighAmplitude);
            float speed = ResolveByCharge(targetCharge, voltageJitterLowSpeed, voltageJitterMidSpeed, voltageJitterHighSpeed);
            float noise = Mathf.PerlinNoise(Time.time * speed, 13.71f) - 0.5f;
            float wave = Mathf.Sin(Time.time * speed * 1.37f) * 0.5f;
            float jitter = Mathf.Lerp(wave, noise, Mathf.Clamp01(voltageNoiseBlend)) * amplitude;
            targetCharge = Mathf.Clamp01(targetCharge + jitter);
        }

        if (!voltageValueInitialized)
        {
            smoothedVisualCharge = targetCharge;
            voltageValueInitialized = true;
        }
        else if (smoothVoltageValue)
        {
            float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, voltageSmoothSpeed) * Time.deltaTime);
            smoothedVisualCharge = Mathf.Lerp(smoothedVisualCharge, targetCharge, t);
        }
        else
        {
            smoothedVisualCharge = targetCharge;
        }

        return Mathf.Clamp01(smoothedVisualCharge);
    }

    private Color GetChargeBarColor(float normalizedCharge, Color fallbackColor)
    {
        if (state == PanelState.RestoreWarning)
        {
            return chargeWarningColor;
        }

        if (state == PanelState.Ready)
        {
            return chargeHighColor;
        }

        if (state == PanelState.BlackoutActive)
        {
            return Color.Lerp(chargeMediumColor, chargeHighColor, Mathf.Clamp01(normalizedCharge));
        }

        if (state == PanelState.Locked || state == PanelState.Cooldown || state == PanelState.Restoring)
        {
            return fallbackColor;
        }

        if (chargeColorGradient != null)
        {
            return chargeColorGradient.Evaluate(Mathf.Clamp01(normalizedCharge));
        }

        if (normalizedCharge >= highChargeThreshold)
        {
            return chargeHighColor;
        }

        if (normalizedCharge >= mediumChargeThreshold)
        {
            float t = Mathf.InverseLerp(mediumChargeThreshold, highChargeThreshold, normalizedCharge);
            return Color.Lerp(chargeMediumColor, chargeHighColor, t);
        }

        float mediumT = Mathf.InverseLerp(0f, Mathf.Max(0.01f, mediumChargeThreshold), normalizedCharge);
        return Color.Lerp(chargeLowColor, chargeMediumColor, mediumT);
    }

    private void ApplyChargeBarScale(float normalizedScale)
    {
        Transform activeTransform = GetActiveChargeFillTransform();
        if (activeTransform == null)
        {
            return;
        }

        Vector3 configuredAxis = useGeneratedChargeFill && chargeFillVisual != null ? chargeFillAxis : chargeBarFillAxis;
        bool anchorFromStart = useGeneratedChargeFill && chargeFillVisual != null
            ? chargeFillAnchoredFromLeft
            : chargeBarAnchoredFromLeft || anchorChargeBarFromStart;
        Vector3 axis = ResolveDominantAxis(configuredAxis);
        Vector3 newScale = chargeBarInitialLocalScale;
        float initialLength = 0f;
        float newLength = 0f;

        if (Mathf.Abs(axis.x) > 0.5f)
        {
            newScale.x = chargeBarInitialLocalScale.x * normalizedScale;
            initialLength = Mathf.Abs(chargeBarInitialLocalBounds.size.x * chargeBarInitialLocalScale.x);
            newLength = Mathf.Abs(chargeBarInitialLocalBounds.size.x * newScale.x);
        }
        else if (Mathf.Abs(axis.y) > 0.5f)
        {
            newScale.y = chargeBarInitialLocalScale.y * normalizedScale;
            initialLength = Mathf.Abs(chargeBarInitialLocalBounds.size.y * chargeBarInitialLocalScale.y);
            newLength = Mathf.Abs(chargeBarInitialLocalBounds.size.y * newScale.y);
        }
        else
        {
            newScale.z = chargeBarInitialLocalScale.z * normalizedScale;
            initialLength = Mathf.Abs(chargeBarInitialLocalBounds.size.z * chargeBarInitialLocalScale.z);
            newLength = Mathf.Abs(chargeBarInitialLocalBounds.size.z * newScale.z);
        }

        activeTransform.localScale = newScale;

        if (anchorFromStart)
        {
            float offset = (initialLength - newLength) * 0.5f;
            activeTransform.localPosition = chargeBarInitialLocalPosition - axis * offset;
        }
        else
        {
            activeTransform.localPosition = chargeBarInitialLocalPosition;
        }
    }

    private Vector3 ResolveDominantAxis(Vector3 axis)
    {
        if (axis.sqrMagnitude < 0.001f)
        {
            return Vector3.right;
        }

        Vector3 normalized = axis.normalized;
        float absX = Mathf.Abs(normalized.x);
        float absY = Mathf.Abs(normalized.y);
        float absZ = Mathf.Abs(normalized.z);

        if (absX >= absY && absX >= absZ)
        {
            return new Vector3(Mathf.Sign(normalized.x), 0f, 0f);
        }

        if (absY >= absX && absY >= absZ)
        {
            return new Vector3(0f, Mathf.Sign(normalized.y), 0f);
        }

        return new Vector3(0f, 0f, Mathf.Sign(normalized.z));
    }

    private float ResolveByCharge(float charge, float lowValue, float midValue, float highValue)
    {
        if (charge >= highChargeThreshold)
        {
            return highValue;
        }

        if (charge >= mediumChargeThreshold)
        {
            float t = Mathf.InverseLerp(mediumChargeThreshold, highChargeThreshold, charge);
            return Mathf.Lerp(midValue, highValue, t);
        }

        float midT = Mathf.InverseLerp(0f, Mathf.Max(0.01f, mediumChargeThreshold), charge);
        return Mathf.Lerp(lowValue, midValue, midT);
    }

    private Renderer GetActiveChargeFillRenderer()
    {
        if (useGeneratedChargeFill && chargeFillRenderer != null)
        {
            return chargeFillRenderer;
        }

        if (useGeneratedChargeFillIfNeeded && chargeFillRenderer != null && chargeBarRenderer == null)
        {
            return chargeFillRenderer;
        }

        return chargeBarRenderer;
    }

    private Transform GetActiveChargeFillTransform()
    {
        if (useGeneratedChargeFill && chargeFillVisual != null)
        {
            return chargeFillVisual;
        }

        if (useGeneratedChargeFillIfNeeded && chargeFillVisual != null && chargeBarRenderer == null)
        {
            return chargeFillVisual;
        }

        return chargeBarTransform;
    }

    private void UpdateGaugeNeedleVisuals(float visualCharge)
    {
        if (!animateGaugeNeedles)
        {
            return;
        }

        CacheGaugeNeedleStates();
        if (gaugeNeedleStates.Count == 0)
        {
            if (!warnedMissingGaugeNeedles)
            {
                warnedMissingGaugeNeedles = true;
                Debug.LogWarning("Electric panel gauge needles were not found. Assign decorativeGaugeNeedles manually if gauge shake is required.", this);
            }

            return;
        }

        Vector3 axis = ResolveDominantAxis(gaugeNeedleLocalAxis);
        float amplitude = ResolveByCharge(visualCharge, gaugeNeedleLowShakeAngle, gaugeNeedleMidShakeAngle, gaugeNeedleHighShakeAngle);
        float speed = ResolveByCharge(visualCharge, gaugeNeedleLowSpeed, gaugeNeedleMidSpeed, gaugeNeedleHighSpeed);

        if (state == PanelState.Locked || state == PanelState.Cooldown || state == PanelState.Restoring)
        {
            amplitude = 0f;
        }

        for (int i = 0; i < gaugeNeedleStates.Count; i++)
        {
            GaugeNeedleState needleState = gaugeNeedleStates[i];
            if (needleState.Needle == null)
            {
                continue;
            }

            float noise = Mathf.PerlinNoise(Time.time * speed, i * 3.17f + 0.42f) - 0.5f;
            float wave = Mathf.Sin(Time.time * speed + i * 1.9f) * 0.5f;
            float loadBias = Mathf.Lerp(-amplitude * 0.35f, amplitude * 0.65f, visualCharge);
            float angle = loadBias + (noise + wave) * amplitude;
            needleState.Needle.localRotation = needleState.InitialLocalRotation * Quaternion.AngleAxis(angle, axis);
        }
    }

    private void ApplyCameraOffline()
    {
        for (int i = 0; i < cameraLightStates.Count; i++)
        {
            CameraLightState stateItem = cameraLightStates[i];
            if (stateItem.Light != null)
            {
                stateItem.Light.enabled = false;
                stateItem.Light.intensity = 0f;
            }
        }

        ApplyColorToRenderers(cameraIndicatorRenderers, cameraOfflineColor);
        SetCameraAnimatorSpeed(0f);
    }

    private void ApplyCameraOnline()
    {
        for (int i = 0; i < cameraLightStates.Count; i++)
        {
            CameraLightState stateItem = cameraLightStates[i];
            if (stateItem.Light != null)
            {
                stateItem.Light.enabled = stateItem.Enabled;
                stateItem.Light.intensity = stateItem.Intensity;
                stateItem.Light.color = stateItem.Color;
            }
        }

        ApplyColorToRenderers(cameraIndicatorRenderers, cameraOnlineColor);
        RestoreCameraAnimatorSpeed();
    }

    private void UpdateCameraWarningVisuals()
    {
        if (state != PanelState.RestoreWarning)
        {
            return;
        }

        Color warningColor = IsBlinkOn() ? cameraWarningColor : cameraOfflineColor;
        ApplyColorToRenderers(cameraIndicatorRenderers, warningColor);

        for (int i = 0; i < cameraLightStates.Count; i++)
        {
            CameraLightState stateItem = cameraLightStates[i];
            if (stateItem.Light != null)
            {
                stateItem.Light.enabled = IsBlinkOn();
                stateItem.Light.intensity = IsBlinkOn() ? Mathf.Max(0.1f, stateItem.Intensity * 0.5f) : 0f;
            }
        }
    }

    private bool IsBlinkOn()
    {
        return Mathf.FloorToInt(Time.time * 5f) % 2 == 0;
    }

    private void ApplyColorToRenderers(Renderer[] renderers, Color color)
    {
        if (renderers == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                SetRendererColor(renderers[i], color);
            }
        }
    }

    private void SetRendererColor(Renderer rendererItem, Color color)
    {
        if (rendererItem == null)
        {
            return;
        }

        rendererItem.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_Color", color);
        propertyBlock.SetColor("_BaseColor", color);
        rendererItem.SetPropertyBlock(propertyBlock);
    }

    private void ResolveLeverVisual()
    {
        if (leverVisual == null)
        {
            GameObject leverObject = GameObject.Find("Electro_block_handle");
            if (leverObject != null)
            {
                leverVisual = leverObject.transform;
            }
        }

        if (leverVisual != null)
        {
            CacheLeverPose();
        }
    }

    private void ResolveLeverPoseHelpers()
    {
        if (!createLeverPoseHelpersIfMissing)
        {
            return;
        }

        if (leverRestPose == null)
        {
            leverRestPose = FindSceneTransformByPath("Управление/Electro_block_handle_RestPose");
        }

        if (leverPulledPose == null)
        {
            leverPulledPose = FindSceneTransformByPath("Управление/Electro_block_handle_PulledPose");
        }
    }

    private void ResolveChargeBar()
    {
        if (chargeBarRenderer == null)
        {
            Transform chargeTransform = FindSceneTransformByPath("Управление/indicator_line/Osnova_light");
            if (chargeTransform == null)
            {
                chargeTransform = FindSceneTransformByName("Osnova_light");
            }

            if (chargeTransform != null)
            {
                chargeBarRenderer = chargeTransform.GetComponent<Renderer>();
            }
        }

        if (chargeBarTransform == null && chargeBarRenderer != null)
        {
            chargeBarTransform = chargeBarRenderer.transform;
        }

        if (chargeBarBackgroundRenderer == null)
        {
            Transform backgroundTransform = FindSceneTransformByPath("Управление/indicator_line/osnova");
            if (backgroundTransform == null)
            {
                backgroundTransform = FindSceneTransformByName("osnova");
            }

            if (backgroundTransform != null)
            {
                chargeBarBackgroundRenderer = backgroundTransform.GetComponent<Renderer>();
            }
        }

        if (chargeFillVisual == null)
        {
            return;
        }

        if (chargeFillRenderer == null && chargeFillVisual != null)
        {
            chargeFillRenderer = chargeFillVisual.GetComponent<Renderer>();
        }
    }

    private void ResolveVoltageSegmentIndicator()
    {
        if (voltageSegmentIndicator != null)
        {
            return;
        }

        GameObject indicatorObject = GameObject.Find("ElectricPanel_VoltageIndicator");
        if (indicatorObject != null)
        {
            voltageSegmentIndicator = indicatorObject.GetComponent<ElectricPanelVoltageSegmentIndicator>();
        }
    }

    private void ResolveIndicators()
    {
        if (!autoFindIndicatorObjects || chargeBarRenderer != null || (indicatorSegments != null && indicatorSegments.Length > 0))
        {
            return;
        }

        List<Renderer> foundRenderers = new List<Renderer>();
        Renderer[] sceneRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneRenderers.Length; i++)
        {
            if (sceneRenderers[i] != null && NameContainsKeyword(sceneRenderers[i].name, indicatorAutoFindKeywords))
            {
                foundRenderers.Add(sceneRenderers[i]);
            }
        }

        if (foundRenderers.Count > 0)
        {
            indicatorSegments = foundRenderers.ToArray();
            indicatorExtraRenderers = indicatorSegments;
        }
    }

    private void CacheChargeBarPose()
    {
        Renderer activeRenderer = GetActiveChargeFillRenderer();
        Transform activeTransform = GetActiveChargeFillTransform();
        if (activeRenderer == null || activeTransform == null)
        {
            return;
        }

        if (chargeBarPoseCached && cachedChargeFillTransform == activeTransform)
        {
            return;
        }

        cachedChargeFillTransform = activeTransform;
        chargeBarInitialLocalScale = activeTransform.localScale;
        chargeBarInitialLocalPosition = activeTransform.localPosition;
        chargeBarInitialLocalBounds = activeRenderer.localBounds;
        chargeBarPoseCached = true;
    }

    private void CacheLeverPose()
    {
        if (leverVisual == null || leverPoseCached)
        {
            return;
        }

        leverInitialLocalRotation = leverVisual.localRotation;
        leverInitialLocalPosition = leverVisual.localPosition;

        if (captureCurrentPoseAsRestOnAwake)
        {
            leverRestLocalEuler = leverVisual.localEulerAngles;
        }

        if (leverRestPose != null)
        {
            leverRestLocalEuler = leverRestPose.localEulerAngles;
            leverRestLocalPosition = leverRestPose.localPosition;
            leverRestLocalRotation = leverRestPose.localRotation;
        }
        else
        {
            leverRestLocalRotation = Quaternion.Euler(leverRestLocalEuler);
            leverRestLocalPosition = leverInitialLocalPosition;
        }

        if (leverPulledPose != null)
        {
            leverPulledLocalEuler = leverPulledPose.localEulerAngles;
            leverPulledLocalPosition = leverPulledPose.localPosition;
            leverPulledLocalRotation = leverPulledPose.localRotation;
        }
        else
        {
            leverPulledLocalEuler = leverRestLocalEuler + leverPulledLocalEulerOffset;
            leverPulledLocalRotation = Quaternion.Euler(leverPulledLocalEuler);
            leverPulledLocalPosition = useLeverLocalPositionOffset
                ? leverInitialLocalPosition + leverPulledLocalPositionOffset
                : leverInitialLocalPosition;
        }

        leverPoseCached = true;
    }

    private void CacheGaugeNeedleStates()
    {
        if (gaugeNeedlePoseCached)
        {
            return;
        }

        gaugeNeedleStates.Clear();
        if (decorativeGaugeNeedles != null)
        {
            for (int i = 0; i < decorativeGaugeNeedles.Length; i++)
            {
                Transform needle = decorativeGaugeNeedles[i];
                if (needle != null)
                {
                    gaugeNeedleStates.Add(new GaugeNeedleState
                    {
                        Needle = needle,
                        InitialLocalRotation = needle.localRotation
                    });
                }
            }
        }

        gaugeNeedlePoseCached = true;
    }

    private void ResolveSceneLights()
    {
        if (!autoFindSceneLights || !affectAllSceneLights || (sceneLights != null && sceneLights.Length > 0))
        {
            return;
        }

        GameObject lightsRoot = GameObject.Find("Lights_Root");
        if (lightsRoot != null)
        {
            sceneLights = FilterOutCameraLights(lightsRoot.GetComponentsInChildren<Light>(true));
            return;
        }

        sceneLights = FilterOutCameraLights(FindObjectsByType<Light>(FindObjectsSortMode.None));
    }

    private Light[] FilterOutCameraLights(Light[] lights)
    {
        if (lights == null || lights.Length == 0)
        {
            return lights;
        }

        List<Light> filteredLights = new List<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null && !IsCameraLight(lights[i]))
            {
                filteredLights.Add(lights[i]);
            }
        }

        return filteredLights.ToArray();
    }

    private bool IsCameraLight(Light lightItem)
    {
        if (lightItem == null || cameraLights == null)
        {
            return false;
        }

        for (int i = 0; i < cameraLights.Length; i++)
        {
            if (cameraLights[i] == lightItem)
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveSecurityCamera()
    {
        if (!autoFindSecurityCamera)
        {
            return;
        }

        if (securityCameraRoot == null)
        {
            securityCameraRoot = GameObject.Find(securityCameraRootName);
        }

        if (securityCameraRoot == null)
        {
            WarnMissingCameraOnce();
            return;
        }

        if (securityCameraVisual == null)
        {
            Transform visual = FindChildRecursive(securityCameraRoot.transform, securityCameraVisualName);
            securityCameraVisual = visual;
        }

        if (cameraLights == null || cameraLights.Length == 0)
        {
            Light namedLight = null;
            Transform namedLightTransform = FindChildRecursive(securityCameraRoot.transform, cameraLightName);
            if (namedLightTransform != null)
            {
                namedLight = namedLightTransform.GetComponent<Light>();
            }

            cameraLights = namedLight != null
                ? new[] { namedLight }
                : securityCameraRoot.GetComponentsInChildren<Light>(true);
        }

        if (cameraAnimators == null || cameraAnimators.Length == 0)
        {
            cameraAnimators = securityCameraRoot.GetComponentsInChildren<Animator>(true);
        }

        if (cameraIndicatorRenderers == null || cameraIndicatorRenderers.Length == 0)
        {
            List<Renderer> indicatorRenderers = new List<Renderer>();
            Renderer[] renderers = securityCameraRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (NameContainsKeyword(renderers[i].name, new[] { "indicator", "led", "status", "light" }))
                {
                    indicatorRenderers.Add(renderers[i]);
                }
            }

            cameraIndicatorRenderers = indicatorRenderers.ToArray();
        }
    }

    private void CacheLightStates()
    {
        lightStates.Clear();
        AddLightStates(sceneLights, false);
        AddLightStates(emergencyLights, true);
    }

    private void AddLightStates(Light[] lights, bool forceEmergency)
    {
        if (lights == null)
        {
            return;
        }

        for (int i = 0; i < lights.Length; i++)
        {
            Light lightItem = lights[i];
            if (lightItem == null || ContainsLightState(lightItem))
            {
                continue;
            }

            lightStates.Add(new LightState
            {
                Light = lightItem,
                Enabled = lightItem.enabled,
                Intensity = lightItem.intensity,
                Color = lightItem.color,
                IsEmergency = forceEmergency || IsEmergencyLight(lightItem)
            });
        }
    }

    private void CacheCameraStates()
    {
        cameraLightStates.Clear();
        if (cameraLights != null)
        {
            for (int i = 0; i < cameraLights.Length; i++)
            {
                Light lightItem = cameraLights[i];
                if (lightItem != null)
                {
                    cameraLightStates.Add(new CameraLightState
                    {
                        Light = lightItem,
                        Enabled = lightItem.enabled,
                        Intensity = lightItem.intensity,
                        Color = lightItem.color
                    });
                }
            }
        }

        cameraAnimatorSpeeds.Clear();
        if (cameraAnimators != null)
        {
            for (int i = 0; i < cameraAnimators.Length; i++)
            {
                if (cameraAnimators[i] != null && !cameraAnimatorSpeeds.ContainsKey(cameraAnimators[i]))
                {
                    cameraAnimatorSpeeds.Add(cameraAnimators[i], cameraAnimators[i].speed);
                }
            }
        }
    }

    private bool ContainsLightState(Light lightItem)
    {
        for (int i = 0; i < lightStates.Count; i++)
        {
            if (lightStates[i].Light == lightItem)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsEmergencyLight(Light lightItem)
    {
        if (lightItem == null)
        {
            return false;
        }

        if (NameContainsKeyword(lightItem.name, emergencyLightNameKeywords))
        {
            return true;
        }

        return lightItem.color.r > 0.65f && lightItem.color.g < 0.35f && lightItem.color.b < 0.35f;
    }

    private void SetCameraAnimatorSpeed(float speed)
    {
        if (cameraAnimators == null)
        {
            return;
        }

        for (int i = 0; i < cameraAnimators.Length; i++)
        {
            if (cameraAnimators[i] != null)
            {
                cameraAnimators[i].speed = speed;
            }
        }
    }

    private void RestoreCameraAnimatorSpeed()
    {
        foreach (KeyValuePair<Animator, float> pair in cameraAnimatorSpeeds)
        {
            if (pair.Key != null)
            {
                pair.Key.speed = pair.Value;
            }
        }
    }

    private SecuritySystem ResolveSecuritySystem()
    {
        if (GameManager.Instance != null && GameManager.Instance.securitySystem != null)
        {
            return GameManager.Instance.securitySystem;
        }

        return FindFirstObjectByType<SecuritySystem>();
    }

    private void ResolveSfxEmitter()
    {
        if (sfxEmitter == null)
        {
            sfxEmitter = GetComponent<SfxEmitter>();
            if (sfxEmitter == null)
            {
                sfxEmitter = gameObject.AddComponent<SfxEmitter>();
            }
        }
    }

    private void PlaySfx(SfxCue cue, AudioClip fallbackClip, string fieldName)
    {
        if (cue != null)
        {
            ResolveSfxEmitter();
            sfxEmitter.Play(cue);
            return;
        }

        if (fallbackClip != null)
        {
            ResolveFallbackAudioSource();
            fallbackAudioSource.PlayOneShot(fallbackClip);
            return;
        }

        if (missingSfxWarnings.Add(fieldName))
        {
            Debug.LogWarning($"Electric panel SFX '{fieldName}' is not assigned.");
        }
    }

    private void ResolveFallbackAudioSource()
    {
        if (fallbackAudioSource != null)
        {
            return;
        }

        fallbackAudioSource = GetComponent<AudioSource>();
        if (fallbackAudioSource == null)
        {
            fallbackAudioSource = gameObject.AddComponent<AudioSource>();
        }

        fallbackAudioSource.playOnAwake = false;
    }

    private bool IsShiftReadyForDebugCharge()
    {
        return GameManager.Instance == null || GameManager.Instance.isGameStarted;
    }

    private bool NameContainsKeyword(string objectName, string[] keywords)
    {
        if (string.IsNullOrEmpty(objectName) || keywords == null)
        {
            return false;
        }

        for (int i = 0; i < keywords.Length; i++)
        {
            if (!string.IsNullOrEmpty(keywords[i]) && objectName.IndexOf(keywords[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private Transform FindSceneTransformByPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        string[] parts = path.Split('/');
        if (parts.Length == 0)
        {
            return null;
        }

        GameObject rootObject = GameObject.Find(parts[0]);
        Transform current = rootObject != null ? rootObject.transform : null;
        for (int i = 1; i < parts.Length && current != null; i++)
        {
            current = FindDirectChild(current, parts[i]);
        }

        return current;
    }

    private Transform FindDirectChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private Transform FindSceneTransformByName(string objectName)
    {
        GameObject gameObject = GameObject.Find(objectName);
        return gameObject != null ? gameObject.transform : null;
    }

    private void WarnMissingCameraOnce()
    {
        if (warnedMissingCamera)
        {
            return;
        }

        warnedMissingCamera = true;
        Debug.LogWarning($"Security camera root '{securityCameraRootName}' was not found. Electric panel will still control security logic.");
    }

    private void OnGUI()
    {
        if (!showDebugGui)
        {
            return;
        }

        string message = $"POWER PANEL: {state}";
        if (storyBlackoutActive)
        {
            message = "POWER PANEL: STORY BLACKOUT";
        }
        else if (storyIntroLockActive && state == PanelState.Locked)
        {
            message = $"POWER PANEL: LOCKED {Mathf.RoundToInt(charge01 * 100f)}%";
        }
        else if (state == PanelState.Charging)
        {
            message = $"POWER PANEL: CHARGING {Mathf.RoundToInt(charge01 * 100f)}%";
        }
        else if (state == PanelState.BlackoutActive)
        {
            message = $"POWER PANEL: BLACKOUT {BlackoutRemaining:F1}s";
        }
        else if (state == PanelState.RestoreWarning)
        {
            message = "POWER PANEL: CAMERA RESTORE WARNING";
        }
        else if (state == PanelState.Cooldown)
        {
            message = $"POWER PANEL: COOLDOWN {CooldownRemaining:F1}s";
        }

        GUI.Label(new Rect(16f, 144f, 360f, 24f), message);
    }
}
