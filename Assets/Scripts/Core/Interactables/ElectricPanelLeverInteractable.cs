using System.Collections;
using UnityEngine;

public class ElectricPanelLeverInteractable : MonoBehaviour, IInteractable, IOneShotInteractable
{
    [SerializeField] private ElectricPanelController panelController;
    [SerializeField] private Collider ventHandCollider;
    [SerializeField] private VentHandInteractable  ventHandInteractable;
    [SerializeField] private float activateHandTimer;

    public void Interact(Transform holdPosition)
    {
        ElectricPanelController controller = ResolveController();
        if (controller == null)
        {
            Debug.LogWarning("Electric panel lever could not find or create ElectricPanelController.");
            return;
        }

        controller.RegisterLeverVisual(transform);
        if (controller.TryActivateBlackout())
        {
            StartCoroutine(ActivateHandCoroutine());
            Debug.Log("Electric panel lever activated.");
        }
    }

    public void StopInteract()
    {
    }

    private ElectricPanelController ResolveController()
    {
        if (panelController != null)
        {
            panelController.EnsureInitialized();
            return panelController;
        }

        panelController = ElectricPanelController.Instance;
        if (panelController == null)
        {
            panelController = FindFirstObjectByType<ElectricPanelController>();
        }

        if (panelController == null)
        {
            GameObject controllerObject = new GameObject("ElectricPanelController_AutoBootstrap");
            panelController = controllerObject.AddComponent<ElectricPanelController>();
            Debug.LogWarning("ElectricPanelController was created automatically for testing. Add it to the electric panel in the final scene setup.");
        }

        panelController.EnsureInitialized();
        return panelController;
    }

    private IEnumerator ActivateHandCoroutine()
    {
        yield return new WaitForSeconds(activateHandTimer);
        
        ventHandCollider.enabled = true;
        ventHandInteractable.enabled = true;
    }
}
