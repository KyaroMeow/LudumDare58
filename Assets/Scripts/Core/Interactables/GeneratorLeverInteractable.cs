using UnityEngine;

public class GeneratorLeverInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private SecuritySystem securitySystem;

    public void Interact(Transform holdPosition)
    {
        if (securitySystem == null)
        {
            return;
        }

        bool result = securitySystem.TryManualShutdown();
        Debug.Log(result ? "Manual camera shutdown started." : "Generator is not ready yet.");
    }

    public void StopInteract()
    {
    }
}
