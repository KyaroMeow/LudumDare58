using UnityEngine;

public class Instrument : MonoBehaviour, IInteractable
{
    public ToolType toolType;
    [SerializeField] private Material transparentMaterial;

    private Material originalMaterial;
    private Renderer[] renderers;
    private bool isPicked;
    private bool isDisabledPhysicalStealTool;

    private void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();

        if (toolType == ToolType.Steal)
        {
            DisablePhysicalStealTool();
            return;
        }

        if (renderers.Length > 0)
        {
            originalMaterial = renderers[0].material;
        }
    }

    public void Interact(Transform holdPosition)
    {
        if (isDisabledPhysicalStealTool || toolType == ToolType.Steal)
        {
            Debug.Log("Physical Steal tool is disabled. Use the Steal button while inspecting an item.");
            return;
        }

        if (isPicked)
        {
            Debug.Log($"Tool already picked: {toolType}");
            return;
        }

        if (transparentMaterial != null)
        {
            SetTransparent(true);
        }

        isPicked = true;
    }

    public void StopInteract()
    {
        if (transparentMaterial != null)
        {
            SetTransparent(false);
        }

        isPicked = false;
    }

    public void SetTransparent(bool transparent)
    {
        Material targetMaterial = transparent ? transparentMaterial : originalMaterial;

        if (renderers == null)
        {
            return;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && targetMaterial != null)
            {
                renderer.material = targetMaterial;
            }
        }
    }

    public bool IsPicked => isPicked;

    private void DisablePhysicalStealTool()
    {
        isDisabledPhysicalStealTool = true;
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider itemCollider in colliders)
        {
            if (itemCollider != null)
            {
                itemCollider.enabled = false;
            }
        }

        if (renderers != null)
        {
            foreach (Renderer itemRenderer in renderers)
            {
                if (itemRenderer != null)
                {
                    itemRenderer.enabled = false;
                }
            }
        }

        Debug.Log("Physical Steal tool disabled. Steal action is available from item inspection UI.");
    }
}
