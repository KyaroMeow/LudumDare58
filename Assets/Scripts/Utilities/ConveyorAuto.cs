using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConveyorAuto : MonoBehaviour
{
    [Header("Conveyor Settings")]
    public float conveyorSpeed = 2f;
    public float conveyorVisualSpeed = 1f;
    public bool canMove = true;
    public bool visualMoveFlip = false;
    
    [Header("Movement Direction")]
    [Tooltip("Направление движения конвейера")]
    public Direction movementDirection = Direction.Forward;
    
    public enum Direction
    {
        Forward,
        Back,
        Right,
        Left,
        Up,
        Down,
        Custom
    }
    
    [Tooltip("Произвольное направление (используется при выборе Custom)")]
    public Vector3 customDirection = Vector3.forward;
    
    [Tooltip("Использовать локальное направление (относительно объекта)")]
    public bool useLocalDirection = true;
    
    Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (canMove)
        {
            Move();
        }
    }
    
    private void Move()
    {
        if (rb != null)
        {
            Vector3 pos = rb.position;
            Vector3 moveDirection = GetMovementDirection();
            
            rb.position += moveDirection * conveyorSpeed * Time.fixedDeltaTime;
            rb.MovePosition(pos);
        }
        
        // Визуальное перемещение текстуры
        UpdateTextureOffset();
    }
    
    private Vector3 GetMovementDirection()
    {
        Vector3 direction = Vector3.zero;
        
        switch (movementDirection)
        {
            case Direction.Forward:
                direction = Vector3.forward;
                break;
            case Direction.Back:
                direction = Vector3.back;
                break;
            case Direction.Right:
                direction = Vector3.right;
                break;
            case Direction.Left:
                direction = Vector3.left;
                break;
            case Direction.Up:
                direction = Vector3.up;
                break;
            case Direction.Down:
                direction = Vector3.down;
                break;
            case Direction.Custom:
                direction = customDirection.normalized;
                break;
        }
        
        // Если используется локальное направление, преобразуем его
        if (useLocalDirection && movementDirection != Direction.Custom)
        {
            direction = transform.TransformDirection(direction);
        }
        
        return direction;
    }
    
    private void UpdateTextureOffset()
    {
        Vector2 textureOffset = Vector2.zero;
        
        // Определяем направление движения текстуры на основе выбранного направления
        Vector3 moveDirection = GetMovementDirection();
        
        if (visualMoveFlip)
        {
            // Вертикальное движение текстуры
            float verticalSpeed = Mathf.Abs(moveDirection.y) > 0.5f ? conveyorVisualSpeed : 
                                 Mathf.Abs(moveDirection.z) > 0.5f ? conveyorVisualSpeed : 0;
            textureOffset = new Vector2(0, verticalSpeed * Time.fixedDeltaTime);
        }
        else
        {
            // Горизонтальное движение текстуры
            float horizontalSpeed = Mathf.Abs(moveDirection.x) > 0.5f ? conveyorVisualSpeed : 
                                   Mathf.Abs(moveDirection.z) > 0.5f ? conveyorVisualSpeed : 0;
            textureOffset = new Vector2(horizontalSpeed * Time.fixedDeltaTime, 0);
        }
        
        GetComponent<Renderer>().material.mainTextureOffset += textureOffset;
    }
    
    // Метод для изменения направления движения через код
    public void SetDirection(Direction newDirection)
    {
        movementDirection = newDirection;
    }
    
    // Метод для установки произвольного направления
    public void SetCustomDirection(Vector3 direction)
    {
        movementDirection = Direction.Custom;
        customDirection = direction.normalized;
    }
    
    // Метод для изменения скорости
    public void SetSpeed(float newSpeed)
    {
        conveyorSpeed = newSpeed;
    }
    
    // Метод для включения/выключения движения
    public void ToggleMovement(bool state)
    {
        canMove = state;
    }
    
    // Метод для переключения между локальным и глобальным направлением
    public void SetUseLocalDirection(bool useLocal)
    {
        useLocalDirection = useLocal;
    }
    
    // Визуализация направления в редакторе
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            Vector3 direction = GetMovementDirection();
            Vector3 startPoint = transform.position;
            Vector3 endPoint = startPoint + direction * 2f;
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(startPoint, endPoint);
            Gizmos.DrawSphere(endPoint, 0.1f);
        }
    }
}