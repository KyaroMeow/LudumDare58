using System;
using CraftSystem;
using UnityEngine;

public class VentHandInteractable : MonoBehaviour, IInteractable, IOneShotInteractable
{
    [SerializeField] private InventoryUIController inventoryUIController;
    [SerializeField] private GameObject craftMenu;
    [SerializeField] private VentHandIntroController introController;
    [SerializeField] private bool logWhenCraftIsUnavailable = true;
    
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;
    private bool isOpen;
    
    public static bool IsCraftUiOpen { get; private set; }

    public void Interact(Transform holdPosition)
    {
        if (introController == null)
        {
            introController = VentHandIntroController.Instance;
        }

        if (introController != null && introController.HandleHandInteractionClick())
        {
            return;
        }

        if (introController == null || !introController.EnableCraftInteractionAfterIntro)
        {
            return;
        }

        
        if(!isOpen)
            OpenCraftUi();
    }

    public void StopInteract()
    {
        if(isOpen)
            CloseCraftUi();
    }

    private void OnDisable()
    {
        if(isOpen)
            CloseCraftUi();
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (PlayerInteraction.GetCloseActionKeyDown(includeTab: true))
        {
            PlayerInteraction.MarkCloseActionConsumed();
            CloseCraftUi();
        }
    }

    private void OpenCraftUi()
    {
        isOpen = true;
        IsCraftUiOpen = true;

        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerView.Instance?.BlockMovement();
        inventoryUIController?.OpenInventory(false);
        craftMenu.gameObject.SetActive(true);
        
        foreach (var c in craftMenu.GetComponentsInChildren<CraftGroupView>())
            c.Refresh();
    }

    private void CloseCraftUi()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        IsCraftUiOpen = false;

        inventoryUIController?.CloseInventory();
        PlayerView.Instance?.UnlockMovement();

        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;

        PlayerInteraction.Instance?.ClearCurrentInteractable(this);
        craftMenu.gameObject.SetActive(false);
        Debug.Log("Trash bin UI closed.");
    }
}
