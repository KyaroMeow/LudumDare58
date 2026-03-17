using System;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public static PlayerInteraction Instance;
    
    public Camera playerCamera;
    public Hands hands;
    public Transform holdPosition;
    public float interactionDistance = 10f;

    private IInteractable _currentInteractable;
    private OutlineEffect _currentInteractableOutLine;
    private ToolType currentTool = ToolType.None;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Update()
    {

        HandleInteraction();

        if (Input.GetKeyDown(KeyCode.E) && _currentInteractable != null)
        {
            HandleStopInteraction();
        }
    }


    private void HandleInteraction()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hit, interactionDistance))
        {
            if (hit.collider.TryGetComponent<IInteractable>(out var interactable) && _currentInteractable == null)
            {
                // Включаем outline
                HandleOutline(hit);

                // Обрабатываем клик
                if (Input.GetMouseButtonDown(0))
                {
                    HandleInteractionClick(interactable);
                }
            }
            else
            {
                disableOutline();
            }
        }
        else
        {
            disableOutline();
        }
    }
    private void HandleInteractionClick(IInteractable interactable)
    {
        // Проверка на инструмент
        if (interactable is Instrument tool)
        {
            HandleToolInteraction(tool);
        }
        else if(interactable is TableScaner || interactable is TableFlashlight )
        {
            HandleScanerInteraction(interactable);
        }
        else
        {
            HandleOtherInteraction(interactable);
        }
    }
    private void HandleScanerInteraction(IInteractable interactable)
    {
        if(_currentInteractable != null)
        {
        interactable.Interact(holdPosition);
        }
    }

    private void HandleToolInteraction(Instrument tool)
    {
        if (currentTool == 0) // Если нет активного инструмента
        {
            tool.Interact(holdPosition);
            hands.PlayTakeItem();

            // Устанавливаем тип инструмента
            currentTool = tool.toolType; 

            Debug.Log($"Поднят инструмент: {tool.toolType}");
        }
        else
        {
            
        }
    }

    private void HandleOtherInteraction(IInteractable interactable)
    {
        // Логика для других интерактивных объектов
        _currentInteractable = interactable;
        _currentInteractable.Interact(holdPosition);
    }

    private void HandleOutline(RaycastHit hit)
    {
        if (hit.collider.TryGetComponent<OutlineEffect>(out OutlineEffect outline) && _currentInteractableOutLine == null)
        {
            _currentInteractableOutLine = outline;
            _currentInteractableOutLine.enabled = true;
        }
    }

    private void disableOutline()
    {
        if(_currentInteractableOutLine != null)
        {
        _currentInteractableOutLine.enabled = false;
        _currentInteractableOutLine = null;
        }
    }

    

    public void HandleStopInteraction()
    {
        _currentInteractable.StopInteract();
        _currentInteractable = null;
    }
}