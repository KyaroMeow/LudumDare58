using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FreeCameraMode : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera controlledCamera;

    [Header("Toggle")]
    [Tooltip("Ctrl + RightBracket. На русской раскладке это клавиша твердого знака.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.RightBracket;

    [SerializeField] private KeyCode secondaryToggleKey = KeyCode.None;
    [SerializeField] private bool escapeExitsFreecam = true;

    [Header("Objects Isolation")]
    [Tooltip("Сюда можно кинуть Player, PlayerController, UI, руки, риг и любые объекты, которые надо выключить во время фрикама.")]
    [SerializeField] private GameObject[] objectsToDisableWhileFreecam;

    [Tooltip("Эти объекты останутся активными. Камера и объект с этим скриптом защищаются автоматически.")]
    [SerializeField] private GameObject[] keepAliveObjects;

    [Tooltip("Если камера лежит внутри объекта игрока, она будет временно отцеплена перед выключением игрока.")]
    [SerializeField] private bool detachKeepAliveObjectsFromDisabledParents = true;

    [Header("Movement")]
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private float minMoveSpeed = 0.15f;
    [SerializeField] private float maxMoveSpeed = 120f;

    [Tooltip("Множитель изменения скорости колесиком. 1.25 = плавное увеличение/уменьшение.")]
    [SerializeField] private float scrollSpeedMultiplier = 1.25f;

    [SerializeField] private float sprintMultiplier = 3f;
    [SerializeField] private float precisionMultiplier = 0.25f;

    [Tooltip("Чем меньше значение, тем резче старт движения.")]
    [SerializeField] private float movementSmoothTime = 0.12f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2.1f;

    [Tooltip("Чем меньше значение, тем резче поворот камеры.")]
    [SerializeField] private float lookSmoothTime = 0.045f;

    [SerializeField] private float minPitch = -89f;
    [SerializeField] private float maxPitch = 89f;

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
    private float fovSmoothRef;

    private readonly List<GameObjectState> savedObjectStates = new List<GameObjectState>();
    private readonly List<TransformState> savedTransformStates = new List<TransformState>();

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

    private bool WasTogglePressed()
    {
        bool ctrlHeld =
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);

        if (!ctrlHeld)
            return false;

        bool mainKeyPressed = Input.GetKeyDown(toggleKey);

        bool secondaryKeyPressed =
            secondaryToggleKey != KeyCode.None &&
            Input.GetKeyDown(secondaryToggleKey);

        return mainKeyPressed || secondaryKeyPressed;
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

        cameraTransform.position = savedCameraWorldPosition;
        cameraTransform.rotation = savedCameraWorldRotation;
        controlledCamera.fieldOfView = savedCameraFov;

        currentVelocity = Vector3.zero;
        velocitySmoothRef = Vector3.zero;

        Cursor.lockState = savedCursorLockMode;
        Cursor.visible = savedCursorVisible;

        if (changeTimeScaleWhileFreecam)
            Time.timeScale = savedTimeScale;

        savedObjectStates.Clear();
        savedTransformStates.Clear();

        isRestoring = false;
    }

    private void PrepareIsolation()
    {
        savedObjectStates.Clear();
        savedTransformStates.Clear();

        List<Transform> protectedTransforms = CollectProtectedTransforms();

        SaveProtectedTransformStates(protectedTransforms);

        if (detachKeepAliveObjectsFromDisabledParents)
            DetachProtectedChildrenFromDisabledParents(protectedTransforms);

        DisableSelectedObjects(protectedTransforms);
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

        HashSet<GameObject> alreadyProcessed = new HashSet<GameObject>();

        for (int i = 0; i < objectsToDisableWhileFreecam.Length; i++)
        {
            GameObject target = objectsToDisableWhileFreecam[i];

            if (target == null)
                continue;

            if (alreadyProcessed.Contains(target))
                continue;

            alreadyProcessed.Add(target);

            if (ShouldSkipDisable(target.transform, protectedTransforms))
                continue;

            savedObjectStates.Add(new GameObjectState(target, target.activeSelf));
            target.SetActive(false);
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

    private void RestoreDisabledObjects()
    {
        for (int i = 0; i < savedObjectStates.Count; i++)
        {
            GameObjectState state = savedObjectStates[i];

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

    private void HandleSpeed()
    {
        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetMoveSpeed *= Mathf.Pow(scrollSpeedMultiplier, scroll);
            targetMoveSpeed = Mathf.Clamp(targetMoveSpeed, minMoveSpeed, maxMoveSpeed);
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
        if (!useSpeedFovEffect || controlledCamera == null)
            return;

        float speed01 = Mathf.InverseLerp(minMoveSpeed, maxMoveSpeed, currentMoveSpeed);
        float targetFov = savedCameraFov + speed01 * maxExtraFov;

        controlledCamera.fieldOfView = Mathf.SmoothDamp(
            controlledCamera.fieldOfView,
            targetFov,
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