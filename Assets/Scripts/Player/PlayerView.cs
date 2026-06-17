using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    public static PlayerView Instance;

    [Header("Rotation Settings")] public float rotationDuration = 0.3f;
    public float rotationAngle = 90f;
    [SerializeField] private bool useDiscreteRotationViews = true;
    [SerializeField] private float duplicateViewTolerance = 2f;

    [Header("Camera Look Settings")] public float cameraLookSpeed = 2f;
    public float maxCameraAngle = 15f;

    [Header("Camera")] public Transform cameraTransform;

    [HideInInspector] public bool canRotate = true;
    [HideInInspector] public bool canLook = true;
    public GameObject pauseMenuUI;

    private bool isRotating = false;
    private bool isPaused = false;
    private float rotationProgress = 0f;
    private Quaternion startRotation;
    private Quaternion targetRotation;
    private Quaternion cameraStartLocalRotation;
    private Vector2 currentCameraRotation;
    private Vector2 tutorialCameraHintOffset;
    private Coroutine tutorialCameraHintRoutine;
    private readonly List<float> rotationViewYaws = new List<float>();
    private float initialYaw;
    private int currentRotationViewIndex;
    private bool ventHandExtraViewsUnlocked;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        initialYaw = transform.eulerAngles.y;
        RebuildRotationViews();
        currentRotationViewIndex = FindClosestRotationViewIndex(transform.eulerAngles.y);

        if (cameraTransform != null)
        {
            cameraStartLocalRotation = cameraTransform.localRotation;
            currentCameraRotation = Vector2.zero;
        }
    }

    public void UnlockMovement()
    {
        canRotate = true;
        canLook = true;
    }

    public void BlockMovement()
    {
        canRotate = false;
        canLook = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) &&
            !PlayerInteraction.IsCloseContextActive &&
            !PlayerInteraction.WasCloseActionConsumedThisFrame)
        {
            TogglePause();
        }

        if (canLook) HandleCameraLook();
        if (canRotate) HandleRotation();
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        pauseMenuUI.SetActive(isPaused);
        if (isPaused == false)
        {
            GameManager.Instance.ResumeGame();
        }
        else
        {
            GameManager.Instance.isTimerWork = false;
        }

        AudioListener.pause = isPaused;
    }

    private void HandleCameraLook()
    {
        if (cameraTransform == null) return;

        Vector2 mouseScreenPos = new Vector2(
            Input.mousePosition.x / Screen.width * 2 - 1,
            Input.mousePosition.y / Screen.height * 2 - 1
        );

        Vector2 targetRotation = new Vector2(
            -mouseScreenPos.y * maxCameraAngle,
            mouseScreenPos.x * maxCameraAngle
        );

        currentCameraRotation = Vector2.Lerp(
            currentCameraRotation,
            targetRotation,
            cameraLookSpeed * Time.deltaTime
        );

        Quaternion newRotation = cameraStartLocalRotation *
                                 Quaternion.Euler(
                                     currentCameraRotation.x + tutorialCameraHintOffset.x,
                                     currentCameraRotation.y + tutorialCameraHintOffset.y,
                                     0);
        cameraTransform.localRotation = newRotation;
    }

    private void HandleRotation()
    {
        if (!isRotating)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                StartRotation(-1); //Left
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                StartRotation(1); //Right
            }
        }

        if (isRotating)
        {
            rotationProgress += Time.deltaTime / rotationDuration;

            float easedProgress = Mathf.SmoothStep(0f, 1f, rotationProgress);
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, easedProgress);

            if (rotationProgress >= 1f)
            {
                isRotating = false;
                rotationProgress = 0f;
                transform.rotation = targetRotation;
            }
        }
    }

    private void StartRotation(int direction)
    {
        if (isRotating) return;

        isRotating = true;
        startRotation = transform.rotation;
        if (useDiscreteRotationViews && rotationViewYaws.Count > 0)
        {
            currentRotationViewIndex = FindClosestRotationViewIndex(transform.eulerAngles.y);
            currentRotationViewIndex = RepeatIndex(currentRotationViewIndex + direction, rotationViewYaws.Count);
            targetRotation = Quaternion.Euler(0f, rotationViewYaws[currentRotationViewIndex], 0f);
        }
        else
        {
            targetRotation = startRotation * Quaternion.Euler(0, rotationAngle * direction, 0);
        }
        rotationProgress = 0f;
    }

    public void UnlockVentHandExtraRotationViews(Transform ventTarget, Transform electricPanelTarget, float ventYawOffset = 0f, float panelYawOffset = 0f)
    {
        if (ventHandExtraViewsUnlocked)
        {
            return;
        }

        ventHandExtraViewsUnlocked = true;
        RebuildRotationViews(ventTarget, electricPanelTarget, ventYawOffset, panelYawOffset);
        currentRotationViewIndex = FindClosestRotationViewIndex(transform.eulerAngles.y);
    }

    private void RebuildRotationViews(Transform ventTarget = null, Transform electricPanelTarget = null, float ventYawOffset = 0f, float panelYawOffset = 0f)
    {
        rotationViewYaws.Clear();

        for (int i = 0; i < 4; i++)
        {
            AddRotationViewYaw(initialYaw + rotationAngle * i);
        }

        if (ventHandExtraViewsUnlocked)
        {
            if (TryGetYawToTarget(ventTarget, out float ventYaw))
            {
                AddRotationViewYaw(ventYaw + ventYawOffset);
            }

            if (TryGetYawToTarget(electricPanelTarget, out float panelYaw))
            {
                AddRotationViewYaw(panelYaw + panelYawOffset);
            }
        }

        rotationViewYaws.Sort(CompareRotationViews);
    }

    private bool TryGetYawToTarget(Transform target, out float yaw)
    {
        yaw = 0f;
        if (target == null)
        {
            return false;
        }

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return false;
        }

        yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        return true;
    }

    private void AddRotationViewYaw(float yaw)
    {
        yaw = NormalizeYaw(yaw);
        for (int i = 0; i < rotationViewYaws.Count; i++)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(rotationViewYaws[i], yaw)) <= duplicateViewTolerance)
            {
                return;
            }
        }

        rotationViewYaws.Add(yaw);
    }

    private int FindClosestRotationViewIndex(float yaw)
    {
        if (rotationViewYaws.Count == 0)
        {
            return 0;
        }

        int closestIndex = 0;
        float closestDelta = float.MaxValue;
        for (int i = 0; i < rotationViewYaws.Count; i++)
        {
            float delta = Mathf.Abs(Mathf.DeltaAngle(yaw, rotationViewYaws[i]));
            if (delta < closestDelta)
            {
                closestDelta = delta;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private int CompareRotationViews(float first, float second)
    {
        float firstRelative = Mathf.Repeat(first - initialYaw, 360f);
        float secondRelative = Mathf.Repeat(second - initialYaw, 360f);
        return firstRelative.CompareTo(secondRelative);
    }

    private static int RepeatIndex(int index, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        return (index % count + count) % count;
    }

    private static float NormalizeYaw(float yaw)
    {
        return Mathf.Repeat(yaw, 360f);
    }

    public void ResetCameraLook()
    {
        currentCameraRotation = Vector2.zero;
        tutorialCameraHintOffset = Vector2.zero;
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = cameraStartLocalRotation;
        }
    }

    public void PlayTutorialMouseLookHint()
    {
        StartTutorialCameraHint(true);
    }

    public void PlayTutorialRotateHint()
    {
        StartTutorialCameraHint(false);
    }

    private void StartTutorialCameraHint(bool circularMotion)
    {
        if (cameraTransform == null || !canLook)
        {
            return;
        }

        if (tutorialCameraHintRoutine != null)
        {
            StopCoroutine(tutorialCameraHintRoutine);
        }

        tutorialCameraHintRoutine = StartCoroutine(TutorialCameraHintRoutine(circularMotion));
    }

    private IEnumerator TutorialCameraHintRoutine(bool circularMotion)
    {
        float duration = 1.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if ((circularMotion && Mathf.Abs(Input.GetAxisRaw("Mouse X")) + Mathf.Abs(Input.GetAxisRaw("Mouse Y")) > 0.15f) ||
                (!circularMotion && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))))
            {
                break;
            }

            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float easeOut = 1f - normalized;

            if (circularMotion)
            {
                float phase = normalized * Mathf.PI * 2f;
                tutorialCameraHintOffset = new Vector2(Mathf.Sin(phase) * 0.55f, Mathf.Cos(phase) * 0.75f) * easeOut;
            }
            else
            {
                float sway = Mathf.Sin(normalized * Mathf.PI * 4f) * 1.1f * easeOut;
                tutorialCameraHintOffset = new Vector2(0f, sway);
            }

            yield return null;
        }

        tutorialCameraHintOffset = Vector2.zero;
        tutorialCameraHintRoutine = null;
    }
}
