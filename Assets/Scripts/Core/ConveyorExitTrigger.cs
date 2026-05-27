using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ConveyorExitTrigger : MonoBehaviour
{
    [SerializeField] private ConveyorExitController controller;

    private Collider triggerCollider;

    private void Awake()
    {
        EnsureTriggerCollider();
        ResolveController();
    }

    private void OnValidate()
    {
        EnsureTriggerCollider();
    }

    public void Configure(ConveyorExitController exitController)
    {
        controller = exitController;
        EnsureTriggerCollider();
    }

    private void OnTriggerEnter(Collider other)
    {
        ResolveController();
        if (controller == null || !controller.IsRunning)
        {
            return;
        }

        GameObject exitingItem = controller.ResolveExitingItemFromCollider(other);
        if (exitingItem == null)
        {
            return;
        }

        controller.NotifyExitTriggerReached(exitingItem);
    }

    private void EnsureTriggerCollider()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
        }

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void ResolveController()
    {
        if (controller == null)
        {
            controller = FindFirstObjectByType<ConveyorExitController>();
        }
    }
}
