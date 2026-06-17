using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class FreeCameraMode : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera controlledCamera;

    [Header("Toggle")]
    [Tooltip("Ctrl + RightBracket. Russian hard sign key on RU layout.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.RightBracket;
    [SerializeField] private bool escapeExitsFreecam = true;

    [Header("Do NOT Disable Canvases")]
    [Tooltip("Keep false if world-space monitors use Canvas.")]
    [SerializeField] private bool disableAllCanvases = false;

    [Tooltip("Disables EventSystem. UI stays visible, but does not receive clicks.")]
    [SerializeField] private bool disableEventSystems = true;

    [Header("Hide Specific UI / Hint Text")]
    [SerializeField] private bool autoHideTextByContent = true;

    [SerializeField]
    private string[] textContentPatternsToHide =
    {
        "POWER PANEL",
        "Hand penalty",
        "Total mistakes",
        "mistakes",
        "LOCKED"
    };

    [SerializeField] private bool autoDisableHintLikeComponents = true;

    [SerializeField]
    private string[] componentNamePatternsToDisable =
    {
        "HUD",
        "Hint",
        "Prompt",
        "Tooltip",
        "PowerPanel",
        "InteractionHint",
        "InteractionPrompt",
        "DebugUI",
        "DebugHud"
    };

    [Header("Manual Hide / Disable")]
    [Tooltip("Objects to fully disable through SetActive(false). Do not put monitor Canvas here.")]
    [SerializeField] private GameObject[] objectsToDisableWhileFreecam;

    [Tooltip("Objects to hide without full GameObject disabling. Use for hands, arms, hint objects.")]
    [SerializeField] private GameObject[] objectsToHideWhileFreecam;

    [SerializeField] private Renderer[] renderersToHideWhileFreecam;

    [Tooltip("Specific scripts/components to disable during freecam.")]
    [SerializeField] private Behaviour[] componentsToDisableWhileFreecam;

    [SerializeField] private bool hideRenderers = true;
    [SerializeField] private bool disableCanvasesOnHiddenObjects = false;
    [SerializeField] private bool disableCollidersOnHiddenObjects = true;
    [SerializeField] private bool disableLightsOnHiddenObjects = false;

    [Tooltip("Keeps selected objects hidden/disabled every frame if other scripts turn them on.")]
    [SerializeField] private bool forceIsolationEveryFrame = true;

    [Header("Keep Alive")]
    [SerializeField] private GameObject[] keepAliveObjects;

    [Tooltip("If camera is inside disabled player object, it will be temporarily detached.")]
    [SerializeField] private bool detachKeepAliveObjectsFromDisabledParents = true;

    [Header("Movement")]
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private float minMoveSpeed = 0.15f;
    [SerializeField] private float maxMoveSpeed = 120f;
    [SerializeField] private float scrollSpeedMultiplier = 1.25f;
    [SerializeField] private float sprintMultiplier = 3f;
    [SerializeField] private float precisionMultiplier = 0.25f;
    [SerializeField] private float movementSmoothTime = 0.12f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2.1f;
    [SerializeField] private float lookSmoothTime = 0.045f;
    [SerializeField] private float minPitch = -89f;
    [SerializeField] private float maxPitch = 89f;

    [Header("Manual FOV Control")]
    [SerializeField] private bool ctrlMouseWheelChangesFov = true;
    [SerializeField] private float minFreecamFov = 25f;
    [SerializeField] private float maxFreecamFov = 110f;
    [SerializeField] private float fovScrollStep = 4f;

    [Header("Visual Feel")]
    [SerializeField] private bool useSpeedFovEffect = true;
    [SerializeField] private float maxExtraFov = 8f;
    [SerializeField] private float fovSmoothTime = 0.15f;

    [Header("Optional Freeze")]
    [SerializeField] private bool changeTimeScaleWhileFreecam = false;
    [SerializeField] private float freecamTimeScale = 0f;

    private bool isFreecam;
    private bool isRestoring;

    private Transform cameraTransform;

    private Vector3 savedCameraWorldPosition;
    private Quaternion savedCameraWorldRotation;
    private float savedCameraFov;

    private CursorLockMode savedCursorLockMode;
    private bool savedCursorVisible;

    private float savedTimeScale;

    private float targetMoveSpeed;
    private float currentMoveSpeed;
    private float moveSpeedSmoothRef;

    private Vector3 currentVelocity;
    private Vector3 velocitySmoothRef;

    private float targetYaw;
    private float targetPitch;
    private float currentYaw;
    private float currentPitch;
    private float yawSmoothRef;
    private float pitchSmoothRef;

    private float targetFreecamFov;
    private float fovSmoothRef;

    private readonly List<GameObjectState> savedGameObjectStates = new List<GameObjectState>();
    private readonly List<TransformState> savedTransformStates = new List<TransformState>();
    private readonly List<RendererState> savedRendererStates = new List<RendererState>();
    private readonly List<ColliderState> savedColliderStates = new List<ColliderState>();
    private readonly List<BehaviourState> savedBehaviourStates = new List<BehaviourState>();

    private readonly HashSet<GameObject> savedGameObjects = new HashSet<GameObject>();
    private readonly HashSet<Renderer> savedRenderers = new HashSet<Renderer>();
    private readonly HashSet<Collider> savedColliders = new HashSet<Collider>();
    private readonly HashSet<Behaviour> savedBehaviours = new HashSet<Behaviour>();

    private struct GameObjectState
    {
        public GameObject GameObject;
        public bool ActiveSelf;

        public GameObjectState(GameObject gameObject, bool activeSelf)
        {
            GameObject = gameObject;
            ActiveSelf = activeSelf;
        }
    }

    private struct TransformState
    {
        public Transform Transform;
        public Transform Parent;
        public int SiblingIndex;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;

        public TransformState(Transform transform)
        {
            Transform = transform;
            Parent = transform.parent;
            SiblingIndex = transform.GetSiblingIndex();
            LocalPosition = transform.localPosition;
            LocalRotation = transform.localRotation;
            LocalScale = transform.localScale;
        }
    }

    private struct RendererState
    {
        public Renderer Renderer;
        public bool Enabled;

        public RendererState(Renderer renderer, bool enabled)
        {
            Renderer = renderer;
            Enabled = enabled;
        }
    }

    private struct ColliderState
    {
        public Collider Collider;
        public bool Enabled;

        public ColliderState(Collider collider, bool enabled)
        {
            Collider = collider;
            Enabled = enabled;
        }
    }

    private struct BehaviourState
    {
        public Behaviour Behaviour;
        public bool Enabled;

        public BehaviourState(Behaviour behaviour, bool enabled)
        {
            Behaviour = behaviour;
            Enabled = enabled;
        }
    }

    private void Reset()
    {
        controlledCamera = GetComponent<Camera>();
    }

    private void Awake()
    {
        ResolveCamera();

        targetMoveSpeed = baseMoveSpeed;
        currentMoveSpeed = baseMoveSpeed;
    }

    private void Update()
    {
        if (WasTogglePressed())
        {
            if (isFreecam)
                DisableFreecam();
            else
                EnableFreecam();
        }

        if (!isFreecam)
            return;

        if (escapeExitsFreecam && Input.GetKeyDown(KeyCode.Escape))
        {
            DisableFreecam();
            return;
        }

        HandleSpeed();
        HandleLook();
        HandleMovement();
        HandleFov();
    }

    private void LateUpdate()
    {
        if (!isFreecam)
            return;

        if (!forceIsolationEveryFrame)
            return;

        ApplyTargetedUiIsolation();
        EnforceDisabledObjects();
        EnforceHiddenStates();
        ApplyHiddenTargets();
    }

    private void OnDisable()
    {
        if (isFreecam && !isRestoring)
            DisableFreecam();
    }

    private bool ResolveCamera()
    {
        if (controlledCamera == null)
            controlledCamera = GetComponent<Camera>();

        if (controlledCamera == null)
            controlledCamera = Camera.main;

        if (controlledCamera == null)
        {
            Debug.LogError("FreeCameraMode: camera is not assigned.");
            enabled = false;
            return false;
        }

        cameraTransform = controlledCamera.transform;
        return true;
    }

    private bool IsCtrlHeld()
    {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    private bool WasTogglePressed()
    {
        return IsCtrlHeld() && Input.GetKeyDown(toggleKey);
    }

    private void EnableFreecam()
    {
        if (!ResolveCamera())
            return;

        isFreecam = true;
        isRestoring = false;

        savedCameraWorldPosition = cameraTransform.position;
        savedCameraWorldRotation = cameraTransform.rotation;
        savedCameraFov = controlledCamera.fieldOfView;
        targetFreecamFov = savedCameraFov;

        savedCursorLockMode = Cursor.lockState;
        savedCursorVisible = Cursor.visible;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (changeTimeScaleWhileFreecam)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = freecamTimeScale;
        }

        Vector3 euler = cameraTransform.rotation.eulerAngles;

        targetYaw = currentYaw = euler.y;
        targetPitch = currentPitch = NormalizePitch(euler.x);

        currentVelocity = Vector3.zero;
        velocitySmoothRef = Vector3.zero;

        moveSpeedSmoothRef = 0f;
        yawSmoothRef = 0f;
        pitchSmoothRef = 0f;
        fovSmoothRef = 0f;

        PrepareIsolation();
    }

    private void DisableFreecam()
    {
        if (!isFreecam)
            return;

        isRestoring = true;
        isFreecam = false;

        RestoreDisabledObjects();
        RestoreKeptTransforms();
        RestoreHiddenTargets();

        cameraTransform.position = savedCameraWorldPosition;
        cameraTransform.rotation = savedCameraWorldRotation;
        controlledCamera.fieldOfView = savedCameraFov;

        currentVelocity = Vector3.zero;
        velocitySmoothRef = Vector3.zero;

        Cursor.lockState = savedCursorLockMode;
        Cursor.visible = savedCursorVisible;

        if (changeTimeScaleWhileFreecam)
            Time.timeScale = savedTimeScale;

        ClearSavedStates();

        isRestoring = false;
    }

    private void PrepareIsolation()
    {
        ClearSavedStates();

        List<Transform> protectedTransforms = CollectProtectedTransforms();

        SaveProtectedTransformStates(protectedTransforms);

        if (detachKeepAliveObjectsFromDisabledParents)
            DetachProtectedChildrenFromDisabledParents(protectedTransforms);

        ApplyTargetedUiIsolation();
        ApplyHiddenTargets();
        DisableSelectedObjects(protectedTransforms);
    }

    private void ApplyTargetedUiIsolation()
    {
        if (disableAllCanvases)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);

            for (int i = 0; i < canvases.Length; i++)
                SaveAndDisableBehaviour(canvases[i]);
        }

        if (disableEventSystems)
        {
            EventSystem[] eventSystems = FindObjectsOfType<EventSystem>(true);

            for (int i = 0; i < eventSystems.Length; i++)
                SaveAndDisableBehaviour(eventSystems[i]);
        }

        if (autoHideTextByContent)
            HideTextComponentsByContent();

        if (autoDisableHintLikeComponents)
            DisableHintLikeComponentsByName();
    }

    private void HideTextComponentsByContent()
    {
        Behaviour[] behaviours = FindObjectsOfType<Behaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            Behaviour behaviour = behaviours[i];

            if (behaviour == null)
                continue;

            if (behaviour == this || behaviour == controlledCamera)
                continue;

            string text = TryGetTextValue(behaviour);

            if (string.IsNullOrEmpty(text))
                continue;

            if (!ContainsAnyPattern(text, textContentPatternsToHide))
                continue;

            SaveAndDisableBehaviour(behaviour);
        }
    }

    private void DisableHintLikeComponentsByName()
    {
        Behaviour[] behaviours = FindObjectsOfType<Behaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            Behaviour behaviour = behaviours[i];

            if (behaviour == null)
                continue;

            if (behaviour == this || behaviour == controlledCamera)
                continue;

            string typeName = behaviour.GetType().Name;
            string objectName = behaviour.gameObject.name;

            bool matchesType = ContainsAnyPattern(typeName, componentNamePatternsToDisable);
            bool matchesObject = ContainsAnyPattern(objectName, componentNamePatternsToDisable);

            if (!matchesType && !matchesObject)
                continue;

            SaveAndDisableBehaviour(behaviour);
        }
    }

    private string TryGetTextValue(Behaviour behaviour)
    {
        Type type = behaviour.GetType();

        PropertyInfo property = type.GetProperty(
            "text",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (property == null || property.PropertyType != typeof(string))
            return null;

        try
        {
            object value = property.GetValue(behaviour, null);
            return value as string;
        }
        catch
        {
            return null;
        }
    }

    private bool ContainsAnyPattern(string value, string[] patterns)
    {
        if (string.IsNullOrEmpty(value) || patterns == null)
            return false;

        for (int i = 0; i < patterns.Length; i++)
        {
            string pattern = patterns[i];

            if (string.IsNullOrEmpty(pattern))
                continue;

            if (value.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private List<Transform> CollectProtectedTransforms()
    {
        List<Transform> result = new List<Transform>();

        AddUniqueTransform(result, cameraTransform);
        AddUniqueTransform(result, transform);

        if (keepAliveObjects != null)
        {
            for (int i = 0; i < keepAliveObjects.Length; i++)
            {
                if (keepAliveObjects[i] != null)
                    AddUniqueTransform(result, keepAliveObjects[i].transform);
            }
        }

        return result;
    }

    private void SaveProtectedTransformStates(List<Transform> protectedTransforms)
    {
        for (int i = 0; i < protectedTransforms.Count; i++)
        {
            Transform target = protectedTransforms[i];

            if (target == null)
                continue;

            savedTransformStates.Add(new TransformState(target));
        }
    }

    private void DetachProtectedChildrenFromDisabledParents(List<Transform> protectedTransforms)
    {
        if (objectsToDisableWhileFreecam == null)
            return;

        for (int i = 0; i < protectedTransforms.Count; i++)
        {
            Transform protectedTransform = protectedTransforms[i];

            if (protectedTransform == null)
                continue;

            for (int j = 0; j < objectsToDisableWhileFreecam.Length; j++)
            {
                GameObject objectToDisable = objectsToDisableWhileFreecam[j];

                if (objectToDisable == null)
                    continue;

                Transform disabledTransform = objectToDisable.transform;

                if (protectedTransform == disabledTransform)
                    continue;

                if (protectedTransform.IsChildOf(disabledTransform))
                {
                    protectedTransform.SetParent(null, true);
                    break;
                }
            }
        }
    }

    private void DisableSelectedObjects(List<Transform> protectedTransforms)
    {
        if (objectsToDisableWhileFreecam == null)
            return;

        for (int i = 0; i < objectsToDisableWhileFreecam.Length; i++)
        {
            GameObject target = objectsToDisableWhileFreecam[i];

            if (target == null)
                continue;

            if (ShouldSkipDisable(target.transform, protectedTransforms))
                continue;

            SaveAndDisableGameObject(target);
        }
    }

    private bool ShouldSkipDisable(Transform target, List<Transform> protectedTransforms)
    {
        for (int i = 0; i < protectedTransforms.Count; i++)
        {
            Transform protectedTransform = protectedTransforms[i];

            if (protectedTransform == null)
                continue;

            if (target == protectedTransform)
                return true;

            if (target.IsChildOf(protectedTransform))
                return true;
        }

        return false;
    }

    private void ApplyHiddenTargets()
    {
        if (objectsToHideWhileFreecam != null)
        {
            for (int i = 0; i < objectsToHideWhileFreecam.Length; i++)
            {
                GameObject target = objectsToHideWhileFreecam[i];

                if (target == null)
                    continue;

                HideObjectContent(target);
            }
        }

        if (renderersToHideWhileFreecam != null)
        {
            for (int i = 0; i < renderersToHideWhileFreecam.Length; i++)
                SaveAndDisableRenderer(renderersToHideWhileFreecam[i]);
        }

        if (componentsToDisableWhileFreecam != null)
        {
            for (int i = 0; i < componentsToDisableWhileFreecam.Length; i++)
                SaveAndDisableBehaviour(componentsToDisableWhileFreecam[i]);
        }
    }

    private void HideObjectContent(GameObject target)
    {
        if (target == null)
            return;

        if (hideRenderers)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
                SaveAndDisableRenderer(renderers[i]);
        }

        if (disableCanvasesOnHiddenObjects)
        {
            Canvas[] canvases = target.GetComponentsInChildren<Canvas>(true);

            for (int i = 0; i < canvases.Length; i++)
                SaveAndDisableBehaviour(canvases[i]);
        }

        if (disableCollidersOnHiddenObjects)
        {
            Collider[] colliders = target.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
                SaveAndDisableCollider(colliders[i]);
        }

        if (disableLightsOnHiddenObjects)
        {
            Light[] lights = target.GetComponentsInChildren<Light>(true);

            for (int i = 0; i < lights.Length; i++)
                SaveAndDisableBehaviour(lights[i]);
        }
    }

    private void SaveAndDisableGameObject(GameObject target)
    {
        if (target == null)
            return;

        if (!savedGameObjects.Contains(target))
        {
            savedGameObjects.Add(target);
            savedGameObjectStates.Add(new GameObjectState(target, target.activeSelf));
        }

        target.SetActive(false);
    }

    private void SaveAndDisableRenderer(Renderer target)
    {
        if (target == null)
            return;

        if (!savedRenderers.Contains(target))
        {
            savedRenderers.Add(target);
            savedRendererStates.Add(new RendererState(target, target.enabled));
        }

        target.enabled = false;
    }

    private void SaveAndDisableCollider(Collider target)
    {
        if (target == null)
            return;

        if (!savedColliders.Contains(target))
        {
            savedColliders.Add(target);
            savedColliderStates.Add(new ColliderState(target, target.enabled));
        }

        target.enabled = false;
    }

    private void SaveAndDisableBehaviour(Behaviour target)
    {
        if (target == null)
            return;

        if (target == this)
            return;

        if (controlledCamera != null && target == controlledCamera)
            return;

        if (!savedBehaviours.Contains(target))
        {
            savedBehaviours.Add(target);
            savedBehaviourStates.Add(new BehaviourState(target, target.enabled));
        }

        target.enabled = false;
    }

    private void EnforceDisabledObjects()
    {
        for (int i = 0; i < savedGameObjectStates.Count; i++)
        {
            GameObject target = savedGameObjectStates[i].GameObject;

            if (target == null)
                continue;

            if (target.activeSelf)
                target.SetActive(false);
        }
    }

    private void EnforceHiddenStates()
    {
        for (int i = 0; i < savedRendererStates.Count; i++)
        {
            Renderer target = savedRendererStates[i].Renderer;

            if (target != null && target.enabled)
                target.enabled = false;
        }

        for (int i = 0; i < savedColliderStates.Count; i++)
        {
            Collider target = savedColliderStates[i].Collider;

            if (target != null && target.enabled)
                target.enabled = false;
        }

        for (int i = 0; i < savedBehaviourStates.Count; i++)
        {
            Behaviour target = savedBehaviourStates[i].Behaviour;

            if (target != null && target.enabled)
                target.enabled = false;
        }
    }

    private void RestoreDisabledObjects()
    {
        for (int i = 0; i < savedGameObjectStates.Count; i++)
        {
            GameObjectState state = savedGameObjectStates[i];

            if (state.GameObject == null)
                continue;

            state.GameObject.SetActive(state.ActiveSelf);
        }
    }

    private void RestoreKeptTransforms()
    {
        for (int i = 0; i < savedTransformStates.Count; i++)
        {
            TransformState state = savedTransformStates[i];

            if (state.Transform == null)
                continue;

            state.Transform.SetParent(state.Parent, false);

            if (state.Parent != null)
            {
                int childCount = state.Parent.childCount;
                int safeIndex = Mathf.Clamp(state.SiblingIndex, 0, childCount - 1);
                state.Transform.SetSiblingIndex(safeIndex);
            }

            state.Transform.localPosition = state.LocalPosition;
            state.Transform.localRotation = state.LocalRotation;
            state.Transform.localScale = state.LocalScale;
        }
    }

    private void RestoreHiddenTargets()
    {
        for (int i = 0; i < savedRendererStates.Count; i++)
        {
            RendererState state = savedRendererStates[i];

            if (state.Renderer == null)
                continue;

            state.Renderer.enabled = state.Enabled;
        }

        for (int i = 0; i < savedColliderStates.Count; i++)
        {
            ColliderState state = savedColliderStates[i];

            if (state.Collider == null)
                continue;

            state.Collider.enabled = state.Enabled;
        }

        for (int i = 0; i < savedBehaviourStates.Count; i++)
        {
            BehaviourState state = savedBehaviourStates[i];

            if (state.Behaviour == null)
                continue;

            state.Behaviour.enabled = state.Enabled;
        }
    }

    private void ClearSavedStates()
    {
        savedGameObjectStates.Clear();
        savedTransformStates.Clear();
        savedRendererStates.Clear();
        savedColliderStates.Clear();
        savedBehaviourStates.Clear();

        savedGameObjects.Clear();
        savedRenderers.Clear();
        savedColliders.Clear();
        savedBehaviours.Clear();
    }

    private void HandleSpeed()
    {
        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            if (ctrlMouseWheelChangesFov && IsCtrlHeld())
            {
                targetFreecamFov -= scroll * fovScrollStep;
                targetFreecamFov = Mathf.Clamp(targetFreecamFov, minFreecamFov, maxFreecamFov);
            }
            else
            {
                targetMoveSpeed *= Mathf.Pow(scrollSpeedMultiplier, scroll);
                targetMoveSpeed = Mathf.Clamp(targetMoveSpeed, minMoveSpeed, maxMoveSpeed);
            }
        }

        currentMoveSpeed = Mathf.SmoothDamp(
            currentMoveSpeed,
            targetMoveSpeed,
            ref moveSpeedSmoothRef,
            0.08f,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        targetYaw += mouseX;
        targetPitch -= mouseY;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        float dt = Time.unscaledDeltaTime;

        currentYaw = Mathf.SmoothDampAngle(
            currentYaw,
            targetYaw,
            ref yawSmoothRef,
            lookSmoothTime,
            Mathf.Infinity,
            dt
        );

        currentPitch = Mathf.SmoothDampAngle(
            currentPitch,
            targetPitch,
            ref pitchSmoothRef,
            lookSmoothTime,
            Mathf.Infinity,
            dt
        );

        cameraTransform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
    }

    private void HandleMovement()
    {
        Vector3 input = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) input += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) input += Vector3.back;
        if (Input.GetKey(KeyCode.D)) input += Vector3.right;
        if (Input.GetKey(KeyCode.A)) input += Vector3.left;

        if (Input.GetKey(KeyCode.E)) input += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) input += Vector3.down;

        input = Vector3.ClampMagnitude(input, 1f);

        float multiplier = 1f;

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            multiplier *= sprintMultiplier;

        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            multiplier *= precisionMultiplier;

        Vector3 desiredVelocity =
            cameraTransform.forward * input.z +
            cameraTransform.right * input.x +
            Vector3.up * input.y;

        desiredVelocity *= currentMoveSpeed * multiplier;

        currentVelocity = Vector3.SmoothDamp(
            currentVelocity,
            desiredVelocity,
            ref velocitySmoothRef,
            movementSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );

        cameraTransform.position += currentVelocity * Time.unscaledDeltaTime;
    }

    private void HandleFov()
    {
        if (controlledCamera == null)
            return;

        float desiredFov = targetFreecamFov;

        if (useSpeedFovEffect)
        {
            float speed01 = Mathf.InverseLerp(minMoveSpeed, maxMoveSpeed, currentMoveSpeed);
            desiredFov += speed01 * maxExtraFov;
        }

        desiredFov = Mathf.Clamp(desiredFov, minFreecamFov, maxFreecamFov);

        controlledCamera.fieldOfView = Mathf.SmoothDamp(
            controlledCamera.fieldOfView,
            desiredFov,
            ref fovSmoothRef,
            fovSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );
    }

    private void AddUniqueTransform(List<Transform> list, Transform target)
    {
        if (target == null)
            return;

        if (!list.Contains(target))
            list.Add(target);
    }

    private float NormalizePitch(float pitch)
    {
        if (pitch > 180f)
            pitch -= 360f;

        return Mathf.Clamp(pitch, minPitch, maxPitch);
    }
}