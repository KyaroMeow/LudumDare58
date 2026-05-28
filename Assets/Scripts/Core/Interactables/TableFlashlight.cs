using UnityEngine;

public class TableFlashlight : MonoBehaviour, IInteractable
{
    [SerializeField] private UVLighter uVLighter;
    private bool warnedMissingUVLighter;

    public void Interact(Transform holdPosition)
    {
        if (uVLighter == null)
        {
            WarnMissingUVLighterOnce();
            return;
        }

        uVLighter.ToggleLighter();
    }

    public void StopInteract()
    {
        if (uVLighter == null)
        {
            WarnMissingUVLighterOnce();
            return;
        }

        uVLighter.ToggleLighterOff();
    }

    private void WarnMissingUVLighterOnce()
    {
        if (warnedMissingUVLighter)
        {
            return;
        }

        warnedMissingUVLighter = true;
        Debug.LogWarning("TableFlashlight cannot toggle UV because UVLighter is not assigned.", this);
    }
}
