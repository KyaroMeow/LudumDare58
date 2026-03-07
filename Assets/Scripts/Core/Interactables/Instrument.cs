using UnityEngine;

public class Instrument : MonoBehaviour, IInteractable
{
    public ToolType toolType;
    [SerializeField] private Material transparentMaterial;
    private Material originalMaterial;
    private Renderer[] renderers;
    private bool isPicked;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            originalMaterial = renderers[0].material;
        }
    }

    public void Interact(Transform holdPosition)
    {
        if (isPicked)
        {
            StopInteract();
            return;
        }
        if (transparentMaterial != null)
        {
            SetTransparent(true);
            isPicked = true;
        }
    }
    
    public void StopInteract()
    {
        if (transparentMaterial != null)
        {
            SetTransparent(false);
            isPicked = false;
        }
    }
    public void SetTransparent(bool transparent)
    {
        Material targetMaterial = transparent ? transparentMaterial : originalMaterial;

        foreach (Renderer renderer in renderers)
        {
            renderer.material = targetMaterial;
        }
    }
}
