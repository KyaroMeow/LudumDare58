using UnityEngine;

public class TableScaner : MonoBehaviour, IInteractable
{
    public void Interact(Transform holdPosition)
    { 
        GameManager.Instance.ToggleScaner(); 
    }
    public void StopInteract()
    {
        GameManager.Instance.ToggleScanerOff();
    }
}
