using UnityEngine;

public class SubmitItemInteractable : MonoBehaviour, IInteractable
{
    public void Interact(Transform holdPosition)
    {
        GameManager.Instance?.SubmitCurrentItem();
    }

    public void StopInteract()
    {
    }
}
