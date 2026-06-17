using System.Collections;
using UnityEngine;

public class Scaner : MonoBehaviour
{
    [Header("Scanner Settings")]
    public Vector3 scanSize = new Vector3(0.5f, 0.1f, 0.1f);
    public float scanDistance = 3f;
    public LayerMask scanLayerMask = -1;

    [Header("Scanning")]
    [SerializeField, Min(0.05f)] private float scanDuration = 2.35f;
    [SerializeField, Min(0f)] private float scanProgressDecaySpeed = 1.2f;
    [SerializeField, Min(0f)] private float scanGraceTime = 0.25f;
    [SerializeField, Min(0f)] private float scanCompleteAutoDisableDelay = 0.25f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float followSpeed = 14f;
    [SerializeField, Range(0f, 1f)] private float aimAssistStrength = 0.65f;
    [SerializeField, Min(0.01f)] private float barcodeAimAssistRadius = 1.2f;

    [Header("Beam Visual")]
    [SerializeField] private bool showBeam = false;
    [SerializeField, Min(0.001f)] private float beamWidth = 0.025f;
    [SerializeField] private Color idleBeamColor = new Color(0.15f, 0.65f, 1f, 0.75f);
    [SerializeField] private Color scanningBeamColor = new Color(1f, 0.12f, 0.04f, 0.95f);
    [SerializeField, Min(0f)] private float beamPulseSpeed = 8f;
    [SerializeField, Min(0f)] private float beamHitPadding = 0.02f;

    [Header("SFX")]
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue scanningLoopSfx;
    [SerializeField] private SfxCue scanCompleteSfx;
    [SerializeField] private SfxCue scanLostSfx;

    private Camera mainCamera;
    private float fixedX;
    private bool isScanning;
    private Collider currentBarcodeCollider;
    private float scanProgress;
    private float lastBarcodeHitTime;
    private bool scanCompleted;
    private LineRenderer beamRenderer;
    private Material beamMaterial;
    private ScannerBarcodeGlow currentBarcodeGlow;
    private Coroutine completeDisableCoroutine;
    private bool loopPlaying;

    private void Start()
    {
        mainCamera = Camera.main;
        fixedX = transform.position.x;
        EnsureBeamRenderer();
        SetBeamVisible(false);
    }

    private void OnEnable()
    {
        mainCamera = Camera.main;
        fixedX = transform.position.x;
        scanCompleted = false;
        scanProgress = 0f;
        lastBarcodeHitTime = -999f;
        EnsureBeamRenderer();
        GameManager.Instance?.HideScanProgress();
    }

    private void OnDisable()
    {
        CancelAutoDisable();
        StopScanningLoop();
        ClearBarcodeGlow();
        SetBeamVisible(false);
        isScanning = false;
        currentBarcodeCollider = null;
        scanProgress = 0f;
        lastBarcodeHitTime = -999f;

        if (!scanCompleted)
        {
            GameManager.Instance?.CancelScanProgress();
        }
        else
        {
            GameManager.Instance?.HideScanProgress();
        }
    }

    private void OnDestroy()
    {
        if (beamMaterial != null)
        {
            Destroy(beamMaterial);
            beamMaterial = null;
        }
    }

    private void Update()
    {
        if (!TryGetCurrentItem(out Item currentItem))
        {
            GameManager.Instance?.ToggleScanerOff();
            return;
        }

        FollowCursorYZ(currentItem);
        UpdateScan(currentItem);
        UpdateBeam();
    }

    private void FollowCursorYZ(Item currentItem)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = mainCamera.WorldToScreenPoint(transform.position).z;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        Vector3 targetPosition = new Vector3(fixedX, worldPos.y, worldPos.z);

        if (TryFindBarcodeCollider(currentItem, out Collider barcodeCollider) &&
            TryGetColliderCenter(barcodeCollider, out Vector3 barcodeCenter))
        {
            float distance = Vector2.Distance(
                new Vector2(targetPosition.y, targetPosition.z),
                new Vector2(barcodeCenter.y, barcodeCenter.z));
            float assist = (1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, barcodeAimAssistRadius))) * aimAssistStrength;
            Vector3 assistedPosition = new Vector3(fixedX, barcodeCenter.y, barcodeCenter.z);
            targetPosition = Vector3.Lerp(targetPosition, assistedPosition, assist);
        }

        transform.position = followSpeed <= 0f
            ? targetPosition
            : Vector3.Lerp(transform.position, targetPosition, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
    }

    private void UpdateScan(Item currentItem)
    {
        Collider barcodeHit = FindScannedBarcode(currentItem);
        bool hasBarcodeHit = barcodeHit != null;

        if (scanCompleted)
        {
            return;
        }

        if (hasBarcodeHit)
        {
            if (currentBarcodeCollider != barcodeHit)
            {
                ClearBarcodeGlow();
                currentBarcodeCollider = barcodeHit;
                currentBarcodeGlow = ResolveBarcodeGlow(barcodeHit);
            }

            isScanning = true;
            lastBarcodeHitTime = Time.time;
            scanProgress = Mathf.Clamp01(scanProgress + Time.deltaTime / Mathf.Max(0.05f, scanDuration));
            StartScanningLoop();
            GameManager.Instance?.ShowScanProgress(scanProgress);
            currentBarcodeGlow?.SetScanGlow(scanProgress, true);

            if (scanProgress >= 1f)
            {
                CompleteScan();
            }

            return;
        }

        if (isScanning)
        {
            isScanning = false;
            StopScanningLoop();
            ClearBarcodeGlow();
            PlaySfx(scanLostSfx);
        }

        bool inGrace = scanProgress > 0f && Time.time - lastBarcodeHitTime <= scanGraceTime;
        if (!inGrace && scanProgress > 0f)
        {
            scanProgress = Mathf.Max(0f, scanProgress - scanProgressDecaySpeed * Time.deltaTime);
            GameManager.Instance?.ShowScanProgress(scanProgress);
        }

        if (scanProgress <= 0f)
        {
            currentBarcodeCollider = null;
            GameManager.Instance?.HideScanProgress();
        }
        else
        {
            GameManager.Instance?.ShowScanProgress(scanProgress);
        }
    }

    private void CompleteScan()
    {
        if (scanCompleted)
        {
            return;
        }

        scanCompleted = true;
        isScanning = false;
        scanProgress = 1f;
        StopScanningLoop();
        PlaySfx(scanCompleteSfx);
        currentBarcodeGlow?.SetScanGlow(1f, true);
        GameManager.Instance?.ShowScanResult();
        CancelAutoDisable();
        completeDisableCoroutine = StartCoroutine(DisableAfterComplete());
    }

    private IEnumerator DisableAfterComplete()
    {
        yield return new WaitForSeconds(scanCompleteAutoDisableDelay);
        completeDisableCoroutine = null;
        GameManager.Instance?.ToggleScanerOff();
    }

    private Collider FindScannedBarcode(Item currentItem)
    {
        if (currentItem == null || !currentItem.hasBarcode)
        {
            return null;
        }

        Collider[] hitColliders = Physics.OverlapBox(
            transform.position + transform.forward * scanDistance * 0.5f,
            scanSize * 0.5f,
            transform.rotation,
            scanLayerMask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitColliders.Length; i++)
        {
            Collider hitCollider = hitColliders[i];
            if (IsValidBarcodeCollider(hitCollider, currentItem))
            {
                return hitCollider;
            }
        }

        return null;
    }

    private bool TryFindBarcodeCollider(Item currentItem, out Collider barcodeCollider)
    {
        barcodeCollider = null;
        if (currentItem == null || !currentItem.hasBarcode)
        {
            return false;
        }

        Collider[] colliders = currentItem.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (IsValidBarcodeCollider(colliders[i], currentItem))
            {
                barcodeCollider = colliders[i];
                return true;
            }
        }

        return false;
    }

    private bool IsValidBarcodeCollider(Collider candidate, Item currentItem)
    {
        if (candidate == null || !candidate.enabled || !candidate.isTrigger || !candidate.CompareTag("Code"))
        {
            return false;
        }

        if (currentItem == null)
        {
            return false;
        }

        Transform candidateTransform = candidate.transform;
        Transform itemTransform = currentItem.transform;
        return candidateTransform.IsChildOf(itemTransform) || itemTransform.IsChildOf(candidateTransform);
    }

    private ScannerBarcodeGlow ResolveBarcodeGlow(Collider barcodeCollider)
    {
        Renderer renderer = ResolveBarcodeRenderer(barcodeCollider);
        if (renderer == null)
        {
            return null;
        }

        ScannerBarcodeGlow glow = renderer.GetComponent<ScannerBarcodeGlow>();
        if (glow == null)
        {
            glow = renderer.gameObject.AddComponent<ScannerBarcodeGlow>();
        }

        glow.Initialize(renderer);
        return glow;
    }

    private static Renderer ResolveBarcodeRenderer(Collider barcodeCollider)
    {
        if (barcodeCollider == null)
        {
            return null;
        }

        Renderer renderer = barcodeCollider.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer;
        }

        renderer = barcodeCollider.GetComponentInChildren<Renderer>(true);
        if (renderer != null)
        {
            return renderer;
        }

        return barcodeCollider.GetComponentInParent<Renderer>();
    }

    private static bool TryGetColliderCenter(Collider collider, out Vector3 center)
    {
        center = Vector3.zero;
        if (collider == null)
        {
            return false;
        }

        center = collider.bounds.center;
        return true;
    }

    private bool TryGetCurrentItem(out Item currentItem)
    {
        currentItem = null;
        return GameManager.Instance != null && GameManager.Instance.TryResolveCurrentItem(out currentItem);
    }

    private void EnsureBeamRenderer()
    {
        if (beamRenderer != null)
        {
            return;
        }

        GameObject beamObject = new GameObject("ScannerRuntimeBeam");
        beamObject.transform.SetParent(transform, false);
        beamRenderer = beamObject.AddComponent<LineRenderer>();
        beamRenderer.useWorldSpace = true;
        beamRenderer.positionCount = 2;
        beamRenderer.startWidth = beamWidth;
        beamRenderer.endWidth = beamWidth;
        beamRenderer.numCapVertices = 4;
        beamRenderer.textureMode = LineTextureMode.Stretch;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            beamMaterial = new Material(shader);
            beamRenderer.material = beamMaterial;
        }
    }

    private void UpdateBeam()
    {
        if (!showBeam || beamRenderer == null)
        {
            return;
        }

        Vector3 origin = transform.position;
        Vector3 end = origin + transform.forward * CalculateBeamLength();
        beamRenderer.SetPosition(0, origin);
        beamRenderer.SetPosition(1, end);
        beamRenderer.startWidth = beamWidth;
        beamRenderer.endWidth = beamWidth;

        float pulse = isScanning ? 0.5f + 0.5f * Mathf.Sin(Time.time * beamPulseSpeed) : 0f;
        Color color = isScanning
            ? Color.Lerp(idleBeamColor, scanningBeamColor, 0.72f + pulse * 0.28f)
            : idleBeamColor;
        beamRenderer.startColor = color;
        beamRenderer.endColor = new Color(color.r, color.g, color.b, color.a * 0.25f);
        SetBeamVisible(true);
    }

    private float CalculateBeamLength()
    {
        if (currentBarcodeCollider != null && TryGetColliderCenter(currentBarcodeCollider, out Vector3 center))
        {
            float hitDistance = Vector3.Dot(center - transform.position, transform.forward);
            if (hitDistance > 0f)
            {
                return Mathf.Clamp(hitDistance + beamHitPadding, 0.1f, Mathf.Max(scanDistance, hitDistance + beamHitPadding));
            }
        }

        return Mathf.Max(0.1f, scanDistance);
    }

    private void SetBeamVisible(bool visible)
    {
        if (beamRenderer != null)
        {
            beamRenderer.enabled = visible && showBeam;
        }
    }

    private void ClearBarcodeGlow()
    {
        if (currentBarcodeGlow != null)
        {
            currentBarcodeGlow.ClearGlow();
            currentBarcodeGlow = null;
        }
    }

    private void StartScanningLoop()
    {
        if (loopPlaying || scanningLoopSfx == null)
        {
            return;
        }

        ResolveSfxEmitter();
        sfxEmitter?.StartLoop(scanningLoopSfx);
        loopPlaying = true;
    }

    private void StopScanningLoop()
    {
        if (!loopPlaying)
        {
            return;
        }

        sfxEmitter?.StopLoop(scanningLoopSfx);
        loopPlaying = false;
    }

    private void PlaySfx(SfxCue cue)
    {
        if (cue == null)
        {
            return;
        }

        ResolveSfxEmitter();
        sfxEmitter?.Play(cue);
    }

    private void ResolveSfxEmitter()
    {
        if (sfxEmitter != null)
        {
            return;
        }

        sfxEmitter = GetComponent<SfxEmitter>();
        if (sfxEmitter == null)
        {
            sfxEmitter = gameObject.AddComponent<SfxEmitter>();
        }
    }

    private void CancelAutoDisable()
    {
        if (completeDisableCoroutine == null)
        {
            return;
        }

        StopCoroutine(completeDisableCoroutine);
        completeDisableCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Vector3 boxCenter = Vector3.forward * scanDistance * 0.5f;
        Gizmos.DrawWireCube(boxCenter, scanSize);
        Gizmos.DrawRay(Vector3.zero, Vector3.forward * scanDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, scanSize);
    }
}
