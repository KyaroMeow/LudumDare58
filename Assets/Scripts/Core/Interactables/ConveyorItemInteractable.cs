using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ConveyorItemInteractable : MonoBehaviour, IInteractable
{
    public ToolType toolTypeForDisassemble = ToolType.None;//если стоит "none" предмет не разборный

    [SerializeField] private GameObject detail; // то что игрок получит при удачном разборе
    [SerializeField] private GameObject trash;//  то что игрок получит при неправильном разборе

    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Transform _originalParent;
    public void Interact(Transform holdPosition)
    {
        PlayerView.Instance.BlockMovement();
        
        _originalPosition = transform.position;
        _originalRotation = transform.rotation;
        _originalParent = transform.parent;
        
        transform.SetParent(holdPosition);
        transform.localPosition = Vector3.zero;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;
        
        HUDManager.Instance.showItemScanHUD();
        PlayerItemInspection.Instance.BeginInspection(gameObject);
    }

    public void StopInteract()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = false;
        
        PlayerView.Instance.UnlockMovement();
        transform.SetParent(_originalParent);
        transform.localPosition = _originalPosition;
        transform.localRotation = _originalRotation;
        
        GameManager.Instance.ToggleScanerOff();
        HUDManager.Instance.hideItemScanHUD();
        PlayerItemInspection.Instance.EndInspection();
    }

    public void TryDisassemble(ToolType toolType)
    {
       if(toolTypeForDisassemble == toolType)
       {
            if(detail != null)
                Instantiate(detail, transform.position, Quaternion.identity); 
       }
       else
       {
            if (trash != null)
                Instantiate(trash, transform.position, Quaternion.identity);
            
       }
        Destroy(gameObject);
    }
}