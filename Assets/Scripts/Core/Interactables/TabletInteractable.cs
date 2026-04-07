using UnityEngine;


public class TabletInteractable : MonoBehaviour, IInteractable
{
    public Transform holdTabletPosition;
    public GameObject homePage;
    public GameObject[] otherPages;
    
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private Transform _originalParent;
    
    public void Interact(Transform holdPosition)
    {
        PlayerView.Instance.BlockMovement();
        _originalPosition = transform.position;
        _originalRotation = transform.rotation;
        _originalParent = transform.parent;

        transform.SetParent(holdTabletPosition);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void StopInteract()
    {
        PlayerView.Instance.UnlockMovement();
        transform.SetParent(_originalParent);
        transform.localPosition = _originalPosition;
        transform.localRotation = _originalRotation;
    }

    public void GoHome()
    {
        foreach (var page in otherPages)
        {
            page.SetActive(false);
        }
        homePage.SetActive(true);
    }
    public void StartGame()
    {
        if (GameManager.Instance.isGameStarted == false)
        {
            GameManager.Instance.StartGame();
        }
    }
}