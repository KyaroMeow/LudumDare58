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
                if (hit.collider.TryGetComponent<OutlineEffect>(out OutlineEffect outline) && _currentInteractableOutLine == null)
                {
                    _currentInteractableOutLine = outline;
                    _currentInteractableOutLine.enabled = true;
                }

                if (Input.GetMouseButtonDown(0))
                {
                    _currentInteractable = interactable;
                    _currentInteractable.Interact(holdPosition);
                    hands.PlayTakeItem();
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