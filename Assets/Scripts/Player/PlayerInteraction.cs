using System;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public static PlayerInteraction Instance;

    public Camera playerCamera;
    public Hands hands;
    public Transform holdPosition;
    public float interactionDistance = 10f;

    private IInteractable currentInteractable;
    private OutlineEffect currentInteractableOutline;
    private ToolType currentTool = ToolType.None;
    private Instrument currentInstrument;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Update()
    {
        HandleInteraction();

        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            HandleStopInteraction();
        }
    }

    private void HandleInteraction()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable) && currentInteractable == null)
            {
                HandleOutline(hit);

                if (Input.GetMouseButtonDown(0))
                {
                    HandleInteractionClick(interactable);
                }
            }
            else
            {
                DisableOutline();
            }
        }
        else
        {
            DisableOutline();
        }
    }

    private void HandleInteractionClick(IInteractable interactable)
    {
        if (interactable is Instrument tool)
        {
            HandleToolInteraction(tool);
        }
        else if (interactable is TableScaner || interactable is TableFlashlight)
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
        if (currentInteractable != null)
        {
            interactable.Interact(holdPosition);
        }
    }

    private void HandleToolInteraction(Instrument tool)
    {
        if (currentInstrument == tool)
        {
            tool.StopInteract();
            currentInstrument = null;
            currentTool = ToolType.None;
            return;
        }

        if (currentInstrument != null)
        {
            currentInstrument.StopInteract();
            currentInstrument = null;
            currentTool = ToolType.None;
        }

        tool.Interact(holdPosition);
        if (tool.IsPicked)
        {
            hands.PlayTakeItem();
            currentInstrument = tool;
            currentTool = tool.toolType;
            Debug.Log($"Tool selected: {tool.toolType}");
        }
    }

    private void HandleOtherInteraction(IInteractable interactable)
    {
        if (currentTool == ToolType.None)
        {
            currentInteractable = interactable;
            currentInteractable.Interact(holdPosition);
        }
        else if (interactable is ConveyorItemInteractable item)
        {
            item.TryDisassemble(currentTool);
        }
    }

    private void HandleOutline(RaycastHit hit)
    {
        if (hit.collider.TryGetComponent(out OutlineEffect outline) && currentInteractableOutline == null)
        {
            currentInteractableOutline = outline;
            currentInteractableOutline.enabled = true;
        }
    }

    private void DisableOutline()
    {
        if (currentInteractableOutline != null)
        {
            currentInteractableOutline.enabled = false;
            currentInteractableOutline = null;
        }
    }

    public void HandleStopInteraction()
    {
        if (currentInteractable == null)
        {
            return;
        }

        currentInteractable.StopInteract();
        currentInteractable = null;
    }

    public bool IsCurrentInteractable(IInteractable interactable)
    {
        return ReferenceEquals(currentInteractable, interactable);
    }
}
