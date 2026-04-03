using UnityEngine;

public class SubmitItemInteractable : MonoBehaviour, IInteractable
{
    public void Interact(Transform holdPosition)
    {
        PlayerInteraction.Instance?.HandleStopInteraction();
        GameManager.Instance?.SubmitCurrentItem();
    }

    public void StopInteract()
    {
    }
}
