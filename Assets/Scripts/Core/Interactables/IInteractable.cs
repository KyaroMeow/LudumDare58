using UnityEngine;

public interface IInteractable
{
    public void Interact(Transform holdPosition);
    public void StopInteract();
}

public interface IOneShotInteractable
{
}
