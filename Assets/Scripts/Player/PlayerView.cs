using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationDuration = 0.3f; // Длительность поворота
    public float rotationAngle = 90f; // Угол поворота
    [HideInInspector] public bool canRotate = true;
    private bool isRotating = false;
    private float rotationProgress = 0f;
    private Quaternion startRotation;
    private Quaternion targetRotation;
    private void Update()
    {
        if (canRotate)
        {
            HandleRotation();
        }
    }
     private void HandleRotation()
    {
        // Обработка ввода для поворота
        if (!isRotating)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                StartRotation(-1); // Поворот влево
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                StartRotation(1); // Поворот вправо
            }
        }
        
        // Выполнение поворота
        if (isRotating)
        {
            rotationProgress += Time.deltaTime / rotationDuration;
            
            // Плавное интерполирование с ease-out
            float easedProgress = Mathf.SmoothStep(0f, 1f, rotationProgress);
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, easedProgress);
            
            // Завершение поворота
            if (rotationProgress >= 1f)
            {
                isRotating = false;
                rotationProgress = 0f;
                transform.rotation = targetRotation; // Точно в цель
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
}
