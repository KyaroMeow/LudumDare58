using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ConveyorCenterStopTrigger : MonoBehaviour
{
    [SerializeField] private Conveyor conveyor;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private bool autoFindReferences = true;

    private BoxCollider triggerCollider;
    private GameObject stoppedItem;
    private GameObject releasedItem;
    private bool warnedMissingConveyor;
    private bool warnedMissingGameManager;

    public GameObject StoppedItem => stoppedItem;
    public bool HasStoppedItem => stoppedItem != null;

    private void Awake()
    {
        EnsureTriggerCollider();
        ResolveReferences();
    }

    private void OnValidate()
    {
        EnsureTriggerCollider();
    }

    public void Configure(Conveyor conveyorReference, GameManager gameManagerReference)
    {
        if (conveyor == null)
        {
            conveyor = conveyorReference;
        }

        if (gameManager == null)
        {
            gameManager = gameManagerReference;
        }

        EnsureTriggerCollider();
    }

    public bool ReleaseStoppedItem(GameObject itemObject)
    {
        ResolveReferences();

        GameObject itemToRelease = itemObject != null ? itemObject : stoppedItem;
        if (itemToRelease == null)
        {
            return false;
        }

        if (stoppedItem != null && !IsSameItem(stoppedItem, itemToRelease))
        {
            return false;
        }

        releasedItem = itemToRelease;
        if (stoppedItem != null)
        {
            Debug.Log($"Conveyor center released exiting item '{stoppedItem.name}'.", this);
        }

        stoppedItem = null;

        if (conveyor != null)
        {
            conveyor.canMove = true;
        }

        return true;
    }

    public bool ClearItem(GameObject itemObject)
    {
        ResolveReferences();

        bool cleared = false;
        if (stoppedItem != null && IsSameItem(stoppedItem, itemObject))
        {
            stoppedItem = null;
            cleared = true;
        }

        if (releasedItem != null && IsSameItem(releasedItem, itemObject))
        {
            releasedItem = null;
            cleared = true;
        }

        if (conveyor != null)
        {
            conveyor.canMove = true;
        }

        if (cleared)
        {
            string itemName = itemObject != null ? itemObject.name : "null";
            Debug.Log($"Center stop reset after tool action for item '{itemName}'.", this);
        }

        return cleared;
    }

    private void OnTriggerEnter(Collider other)
    {
        ResolveReferences();

        if (conveyor == null || gameManager == null)
        {
            WarnMissingReferencesOnce();
            return;
        }

        GameObject currentItem = gameManager.currentItem;
        if (currentItem == null)
        {
            return;
        }

        GameObject enteredItem = ResolveCurrentItemFromCollider(other, currentItem);
        if (enteredItem == null || IsReleasedOrExiting(enteredItem))
        {
            return;
        }

        if (stoppedItem != null && IsSameItem(stoppedItem, enteredItem))
        {
            return;
        }

        stoppedItem = enteredItem;
        conveyor.canMove = false;
        Debug.Log($"Conveyor center stopped current item '{enteredItem.name}'.", this);
    }

    private GameObject ResolveCurrentItemFromCollider(Collider other, GameObject currentItem)
    {
        if (other == null || currentItem == null)
        {
            return null;
        }

        Transform currentTransform = currentItem.transform;
        Transform otherTransform = other.transform;
        if (other.gameObject == currentItem ||
            otherTransform.IsChildOf(currentTransform) ||
            currentTransform.IsChildOf(otherTransform))
        {
            return currentItem;
        }

        Item item = other.GetComponentInParent<Item>();
        if (item != null && IsSameItem(item.gameObject, currentItem))
        {
            return currentItem;
        }

        return null;
    }

    private bool IsReleasedOrExiting(GameObject itemObject)
    {
        if (itemObject == null)
        {
            return false;
        }

        if (releasedItem != null && IsSameItem(releasedItem, itemObject))
        {
            return true;
        }

        return itemObject.GetComponent<ConveyorExitingItemMarker>() != null;
    }

    private static bool IsSameItem(GameObject left, GameObject right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        Transform leftTransform = left.transform;
        Transform rightTransform = right.transform;
        return left == right ||
               leftTransform.IsChildOf(rightTransform) ||
               rightTransform.IsChildOf(leftTransform);
    }

    private void ResolveReferences()
    {
        if (!autoFindReferences)
        {
            return;
        }

        if (conveyor == null)
        {
            conveyor = FindFirstObjectByType<Conveyor>();
        }

        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
        }
    }

    private void EnsureTriggerCollider()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<BoxCollider>();
        }

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void WarnMissingReferencesOnce()
    {
        if (conveyor == null && !warnedMissingConveyor)
        {
            warnedMissingConveyor = true;
            Debug.LogWarning("ConveyorCenterStopTrigger cannot stop items because Conveyor is not assigned.", this);
        }

        if (gameManager == null && !warnedMissingGameManager)
        {
            warnedMissingGameManager = true;
            Debug.LogWarning("ConveyorCenterStopTrigger cannot identify the current item because GameManager is not assigned.", this);
        }
    }
}
