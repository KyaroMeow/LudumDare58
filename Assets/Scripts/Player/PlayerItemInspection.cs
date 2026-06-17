using UnityEngine;

public class PlayerItemInspection : MonoBehaviour
{
    public static PlayerItemInspection Instance;
    
    public Camera playerCamera;
    public float inspectRotationSpeed;
    public UVLighter uvLighter;
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue rotateSfx;
    [SerializeField] private float rotateSfxCooldown = 0.18f;
    [SerializeField] private bool enableInspectionZoom = true;
    [SerializeField] private float zoomSpeed = 0.35f;
    [SerializeField] private float minZoomOffset = -0.35f;
    [SerializeField] private float maxZoomOffset = 0.45f;
    [SerializeField] private float zoomSmoothSpeed = 12f;
    [SerializeField] private bool useUnscaledTimeForZoom = false;
    
    private GameObject _currentHeldItem;
    private float nextRotateSfxTime;
    private Vector3 baseInspectionPosition;
    private float currentZoomOffset;
    private float targetZoomOffset;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void Update()
    {
        HandleInspection();
    }

    public void BeginInspection(GameObject currentHeldItem)
    {
        _currentHeldItem = currentHeldItem;
        if (_currentHeldItem != null)
        {
            baseInspectionPosition = _currentHeldItem.transform.position;
        }

        currentZoomOffset = 0f;
        targetZoomOffset = 0f;
    }

    public void EndInspection()
    {
        if (_currentHeldItem != null)
        {
            Item item = _currentHeldItem.GetComponent<Item>();
            if (item == null)
            {
                item = _currentHeldItem.GetComponentInChildren<Item>(true);
            }

            item?.HideAllUVStains();
            _currentHeldItem.transform.position = baseInspectionPosition;
        }

        _currentHeldItem = null;
        currentZoomOffset = 0f;
        targetZoomOffset = 0f;
        uvLighter?.ToggleLighterOff();
    }
    
    private void HandleInspection()
    {
        if (_currentHeldItem == null)
            return;

        HandleInspectionZoom();
        
        if (Input.GetMouseButton(0))
        {
            Vector2 mouseDelta = Input.mousePositionDelta;
            if (mouseDelta.sqrMagnitude > 0.01f && Time.time >= nextRotateSfxTime)
            {
                PlaySfx(rotateSfx);
                nextRotateSfxTime = Time.time + Mathf.Max(0.01f, rotateSfxCooldown);
            }

            _currentHeldItem.transform.Rotate(playerCamera.transform.up, -mouseDelta.x * inspectRotationSpeed * Time.deltaTime, Space.World);
            _currentHeldItem.transform.Rotate(playerCamera.transform.right, mouseDelta.y * inspectRotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void HandleInspectionZoom()
    {
        if (!enableInspectionZoom || _currentHeldItem == null || playerCamera == null)
        {
            return;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetZoomOffset = Mathf.Clamp(
                targetZoomOffset + scroll * zoomSpeed,
                minZoomOffset,
                maxZoomOffset);
            TutorialHintSystem.Instance?.NotifyInspectionZoomInput(scroll);
        }

        float deltaTime = useUnscaledTimeForZoom ? Time.unscaledDeltaTime : Time.deltaTime;
        float lerpFactor = zoomSmoothSpeed <= 0f ? 1f : 1f - Mathf.Exp(-zoomSmoothSpeed * deltaTime);
        currentZoomOffset = Mathf.Lerp(currentZoomOffset, targetZoomOffset, lerpFactor);
        _currentHeldItem.transform.position = baseInspectionPosition + playerCamera.transform.forward * currentZoomOffset;
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
