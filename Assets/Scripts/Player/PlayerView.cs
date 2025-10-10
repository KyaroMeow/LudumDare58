using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    public static PlayerView Instance;
    [Header("Rotation Settings")]
    public float rotationDuration = 0.3f;
    public float rotationAngle = 90f; 
    
    [Header("Camera Look Settings")]
    public float cameraLookSpeed = 2f;
    public float maxCameraAngle = 15f;
    
    [Header("Camera")]
    public Transform cameraTransform; 
    
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
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (canLook) HandleCameraLook();
        if (canRotate) HandleRotation();

    }
    private void TogglePause()
    {
        isPaused = !isPaused;
        pauseMenuUI.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
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
            Quaternion.Euler(currentCameraRotation.x, currentCameraRotation.y, 0);
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
        targetRotation = startRotation * Quaternion.Euler(0, rotationAngle * direction, 0);
        rotationProgress = 0f;
    }
    
    public void ResetCameraLook()
    {
        currentCameraRotation = Vector2.zero;
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = cameraStartLocalRotation;
        }
    }
}