using System;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public static PlayerInteraction Instance;
    private static int closeActionConsumedFrame = -1;

    public Camera playerCamera;
    public Hands hands;
    public Transform holdPosition;
    public float interactionDistance = 10f;

    [Header("Raycast Priority")]
    [SerializeField] private bool usePriorityRaycast = true;

    private IInteractable currentInteractable;
    private OutlineEffect currentInteractableOutline;
    private PlayerHeldItem heldItem;

    public PlayerHeldItem CurrentHeldItem => heldItem;
    public bool HasCurrentInteractable => currentInteractable != null;
    public static bool WasCloseActionConsumedThisFrame => closeActionConsumedFrame == Time.frameCount;
    public static bool IsCloseContextActive =>
        (Instance != null && Instance.HasCurrentInteractable) ||
        TrashBinInteractable.IsTrashUiOpen ||
        VentHandInteractable.IsCraftUiOpen;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        heldItem = GetComponent<PlayerHeldItem>();
        if (heldItem == null)
        {
            heldItem = gameObject.AddComponent<PlayerHeldItem>();
        }
    }

    private void Update()
    {
        if (TrashBinInteractable.IsTrashUiOpen)
        {
            return;
        }

        HandleInteraction();
        HandleToolCancelInput();

        if (GetCloseActionKeyDown() && currentInteractable != null)
        {
            MarkCloseActionConsumed();
            HandleStopInteraction();
        }
    }

    public static bool GetCloseActionKeyDown(bool includeTab = false)
    {
        return (includeTab && Input.GetKeyDown(KeyCode.Tab)) ||
               Input.GetKeyDown(KeyCode.E) ||
               Input.GetKeyDown(KeyCode.Escape) ||
               Input.GetKeyDown(KeyCode.Space) ||
               Input.GetKeyDown(KeyCode.Return) ||
               Input.GetKeyDown(KeyCode.KeypadEnter);
    }

    public static void MarkCloseActionConsumed()
    {
        closeActionConsumedFrame = Time.frameCount;
    }

    private void HandleToolCancelInput()
    {
        if (Input.GetKeyDown(KeyCode.Q) && heldItem != null && heldItem.HasTool)
        {
            ToolType toolType = heldItem.CurrentToolType;
            heldItem.ClearTool();
            Debug.Log($"Tool deselected: {toolType}");
        }
    }

    private void HandleInteraction()
    {
        if (playerCamera == null)
        {
            DisableOutline();
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (usePriorityRaycast)
        {
            HandlePriorityInteractionRaycast(ray);
            return;
        }

        HandleLegacyInteractionRaycast(ray);
    }

    private void HandlePriorityInteractionRaycast(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance);
        if (hits == null || hits.Length == 0)
        {
            DisableOutline();
            return;
        }

        IInteractable bestInteractable = null;
        RaycastHit bestHit = default;
        int bestPriority = int.MinValue;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            IInteractable interactable = ResolveInteractable(hit.collider);
            if (interactable == null || !CanInteractWith(interactable))
            {
                continue;
            }

            int priority = GetInteractablePriority(interactable);
            bool betterPriority = priority > bestPriority;
            bool samePriorityCloser = priority == bestPriority && hit.distance < bestDistance;

            if (betterPriority || samePriorityCloser)
            {
                bestInteractable = interactable;
                bestHit = hit;
                bestPriority = priority;
                bestDistance = hit.distance;
            }
        }

        if (bestInteractable == null)
        {
            DisableOutline();
            return;
        }

        HandleOutline(bestHit);

        if (Input.GetMouseButtonDown(0))
        {
            HandleInteractionClick(bestInteractable);
        }
    }

    private void HandleLegacyInteractionRaycast(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            IInteractable interactable = ResolveInteractable(hit.collider);
            if (interactable != null && CanInteractWith(interactable))
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

    private IInteractable ResolveInteractable(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        MonoBehaviour[] behaviours = hitCollider.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour != null && behaviour.enabled && behaviour is IInteractable interactable)
            {
                return interactable;
            }
        }

        behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour != null && behaviour.enabled && behaviour is IInteractable interactable)
            {
                return interactable;
            }
        }

        return null;
    }

    private int GetInteractablePriority(IInteractable interactable)
    {
        if (interactable is AnomalyBallInteractable)
        {
            return 1100;
        }

        if (interactable is VentHandKeyPickup)
        {
            return 1000;
        }

        if (interactable is Instrument)
        {
            return 900;
        }

        if (interactable is ToolCaseLock)
        {
            return 800;
        }

        if (interactable is VentHandInteractable)
        {
            return 700;
        }

        if (interactable is SubmitItemInteractable)
        {
            return 600;
        }

        if (interactable is TableScaner || interactable is TableFlashlight)
        {
            return 500;
        }

        if (interactable is ConveyorItemInteractable)
        {
            return 400;
        }

        return 100;
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
        else if (interactable is SubmitItemInteractable)
        {
            interactable.Interact(holdPosition);
        }
        else if (interactable is IOneShotInteractable)
        {
            interactable.Interact(holdPosition);
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
        if (heldItem == null)
        {
            Debug.LogWarning("Cannot select tool because PlayerHeldItem is missing.");
            return;
        }

        if (tool != null && tool.toolType == ToolType.Steal)
        {
            Debug.Log("Steal is available from the inspection button and does not need a physical tool.");
            return;
        }

        if (heldItem.IsHoldingTool(tool))
        {
            ToolType toolType = heldItem.CurrentToolType;
            heldItem.ClearTool();
            Debug.Log($"Tool deselected: {toolType}");
            return;
        }

        if (!heldItem.TrySelectTool(tool))
        {
            return;
        }

        tool.Interact(holdPosition);
        if (tool.IsPicked)
        {
            hands?.PlayTakeItem();
            Debug.Log($"Tool selected: {tool.toolType}");
        }
        else
        {
            heldItem.ClearTool();
        }
    }

    private void HandleOtherInteraction(IInteractable interactable)
    {
        if (heldItem == null || !heldItem.HasTool)
        {
            currentInteractable = interactable;
            currentInteractable.Interact(holdPosition);
        }
        else if (interactable is ConveyorItemInteractable item)
        {
            item.TryDisassemble(heldItem.CurrentToolType);
        }
    }

    private void HandleOutline(RaycastHit hit)
    {
        OutlineEffect outline = hit.collider != null
            ? hit.collider.GetComponentInParent<OutlineEffect>()
            : null;

        if (outline == currentInteractableOutline)
        {
            return;
        }

        DisableOutline();

        if (outline != null)
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

    public void ClearCurrentInteractable(IInteractable interactable)
    {
        if (ReferenceEquals(currentInteractable, interactable))
        {
            currentInteractable = null;
        }
    }

    private bool CanInteractWith(IInteractable interactable)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsStoryInteractionLocked && !IsAllowedDuringStoryLock(interactable))
        {
            return false;
        }

        if (interactable is VentHandKeyPickup)
        {
            return true;
        }

        if (currentInteractable == null)
        {
            return true;
        }

        if (ReferenceEquals(currentInteractable, interactable))
        {
            return false;
        }

        return interactable is TableScaner ||
               interactable is TableFlashlight ||
               interactable is SubmitItemInteractable;
    }

    private bool IsAllowedDuringStoryLock(IInteractable interactable)
    {
        return interactable is VentHandKeyPickup ||
               interactable is VentHandInteractable ||
               (interactable is TrashBinInteractable &&
                VentHandIntroController.Instance != null &&
                VentHandIntroController.Instance.IsWaitingForKeyInventorySpace);
    }
}

public class PlayerHeldItem : MonoBehaviour
{
    public static PlayerHeldItem Instance { get; private set; }

    public Instrument CurrentTool { get; private set; }
    public Item CurrentItem { get; private set; }
    public bool HasTool => CurrentTool != null;
    public bool HasItem => CurrentItem != null;
    public ToolType CurrentToolType => CurrentTool != null ? CurrentTool.toolType : ToolType.None;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    public bool TrySelectTool(Instrument tool)
    {
        if (tool == null)
        {
            Debug.LogWarning("Cannot select tool because Instrument is null.");
            return false;
        }

        if (IsHoldingTool(tool))
        {
            Debug.Log($"Tool already selected: {tool.toolType}");
            return false;
        }

        if (CurrentTool != null)
        {
            Debug.Log($"Tool deselected: {CurrentTool.toolType}");
        }

        ClearTool();
        CurrentTool = tool;
        return true;
    }

    public void ClearTool()
    {
        if (CurrentTool != null)
        {
            CurrentTool.StopInteract();
            CurrentTool = null;
        }
    }

    public bool IsHoldingTool(Instrument tool)
    {
        return tool != null && ReferenceEquals(CurrentTool, tool);
    }

    public bool TrySelectItem(Item item)
    {
        if (item == null)
        {
            Debug.LogWarning("Cannot select item because Item is null.");
            return false;
        }

        CurrentItem = item;
        return true;
    }

    public void ClearItem()
    {
        CurrentItem = null;
    }

    public void ClearAll()
    {
        ClearTool();
        ClearItem();
    }
}
