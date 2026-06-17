using UnityEngine;

[DisallowMultipleComponent]
public class AnomalyBallInteractable : MonoBehaviour, IInteractable, IOneShotInteractable
{
    [SerializeField] private AnomallyController controller;

    private Collider[] colliders;

    private void Awake()
    {
        CacheColliders();
        EnsureOutline();
    }

    private void OnEnable()
    {
        SetCollidersEnabled(true);
    }

    private void OnDisable()
    {
        SetCollidersEnabled(false);
    }

    public void Configure(AnomallyController anomalyController)
    {
        controller = anomalyController;
        CacheColliders();
        EnsureOutline();
    }

    public void Interact(Transform holdPosition)
    {
        ResolveController();
        controller?.ClickOnBall();
    }

    public void StopInteract()
    {
    }

    public void SetCollidersEnabled(bool enabled)
    {
        CacheColliders();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = enabled;
                colliders[i].isTrigger = false;
            }
        }
    }

    private void CacheColliders()
    {
        if (colliders != null && colliders.Length > 0)
        {
            return;
        }

        Collider ownCollider = GetComponent<Collider>();
        if (ownCollider == null)
        {
            ownCollider = gameObject.AddComponent<SphereCollider>();
        }

        colliders = GetComponents<Collider>();
    }

    private void EnsureOutline()
    {
        if (GetComponentInParent<OutlineEffect>() == null)
        {
            OutlineEffect outline = gameObject.AddComponent<OutlineEffect>();
            outline.enabled = false;
        }
    }

    private void ResolveController()
    {
        if (controller != null)
        {
            return;
        }

        controller = GetComponentInParent<AnomallyController>();
        if (controller == null)
        {
            controller = FindFirstObjectByType<AnomallyController>();
        }
    }
}
