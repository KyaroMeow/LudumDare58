using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationDuration = 0.3f;
    public float rotationAngle = 90f; 
    
    [Header("Camera Look Settings")]
    public float cameraLookSpeed = 2f;
    public float maxCameraAngle = 15f;
    
    [Header("Camera")]
    public Transform cameraTransform; 
    
    [HideInInspector] public bool canRotate = true;
    
    private bool isRotating = false;
    private float rotationProgress = 0f;
    private Quaternion startRotation;
    private Quaternion targetRotation;
    private Quaternion cameraStartLocalRotation;
    private Vector2 currentCameraRotation; 

    private void Start()
    {
        if (cameraTransform != null)
        {
            cameraStartLocalRotation = cameraTransform.localRotation;
            currentCameraRotation = Vector2.zero;
        }
    }

    private void Update()
    {
        if (canRotate)
        {
            HandleCameraLook();
            HandleRotation();
        }
    }
    
    private void HandleCameraLook()
    {
        if (cameraTransform == null) return;
        
        Vector2 mouseScreenPos = new Vector2(
            (Input.mousePosition.x / Screen.width) * 2 - 1,
            (Input.mousePosition.y / Screen.height) * 2 - 1
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