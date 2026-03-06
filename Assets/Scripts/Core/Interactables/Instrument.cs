using UnityEngine;

public class Instrument : MonoBehaviour, IInteractable
{
    [SerializeField] GameObject visibleModel;
    [SerializeField] GameObject noVisibleModel;
    public void Interact(Transform holdPosition)
    {
        if (visibleModel != null && noVisibleModel != null)
        {
            visibleModel.SetActive(false);
            noVisibleModel.SetActive(true);
        }
    }
    
    public void StopInteract()
    {
        if (visibleModel != null && noVisibleModel != null)
        {
            visibleModel.SetActive(true);
            noVisibleModel.SetActive(false);
        }
    }
}
