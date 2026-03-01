using UnityEngine;


public class TabletInteractable : MonoBehaviour, IInteractable
{
    public Transform holdTabletPosition;
    public GameObject[] pages;
    
    private int currentPageId = 0;
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
    
    public void NextPages()
    {
        
        if (currentPageId < pages.Length - 1)
        {
            pages[currentPageId].SetActive(false);
            currentPageId++;
            pages[currentPageId].SetActive(true);
        }
        else
        {
            pages[currentPageId].SetActive(false);
            currentPageId = 0;
            pages[currentPageId].SetActive(true);
        }
    }

    public void PreviousPage()
    {
        if (currentPageId > 1)
        {
            pages[currentPageId].SetActive(false);
            currentPageId--;
            pages[currentPageId].SetActive(true);
        }
        else
        {
            pages[currentPageId].SetActive(false);
            currentPageId = pages.Length - 1;
            pages[currentPageId].SetActive(true);
        }
    }

    public void StartGame()
    {
        if (GameManager.Instance.isGameStarted == false)
        {
            GameManager.Instance.StartGame();
        }
    }
}