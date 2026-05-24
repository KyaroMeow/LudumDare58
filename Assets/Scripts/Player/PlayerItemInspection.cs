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
    
    private GameObject _currentHeldItem;
    private float nextRotateSfxTime;

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
    }

    public void EndInspection()
    {
        _currentHeldItem = null;
        uvLighter.ToggleLighterOff();
    }
    
    private void HandleInspection()
    {
        if (_currentHeldItem == null)
            return;
        
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
