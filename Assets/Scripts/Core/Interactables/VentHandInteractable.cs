using UnityEngine;

public class VentHandInteractable : MonoBehaviour, IInteractable, IOneShotInteractable
{
    [SerializeField] private VentHandIntroController introController;
    [SerializeField] private bool logWhenCraftIsUnavailable = true;
    [SerializeField] private float craftUnavailableLogCooldown = 2f;

    private float lastCraftUnavailableLogTime = -999f;

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
            if (logWhenCraftIsUnavailable && Time.unscaledTime - lastCraftUnavailableLogTime >= craftUnavailableLogCooldown)
            {
                lastCraftUnavailableLogTime = Time.unscaledTime;
                Debug.Log("Craft interface is not implemented yet.");
            }

            return;
        }

        Debug.Log("Vent hand craft interaction hook reached. Craft UI will be implemented later.");
    }

    public void StopInteract()
    {
    }
}
