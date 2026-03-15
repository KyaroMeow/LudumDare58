using UnityEngine;

public class TableFlashlight : MonoBehaviour, IInteractable
{
    [SerializeField] private UVLighter uVLighter; 
    public void Interact(Transform holdPosition)
    {
        uVLighter.ToggleLighter();
    }

    public void StopInteract()
    {
        uVLighter.ToggleLighterOff();
    }
}
