using UnityEngine;

public class HelpingHandInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject tradeUiRoot;
    [SerializeField] private HandTradeSystem handTradeSystem;

    public void Interact(Transform holdPosition)
    {
        SecuritySystem securitySystem = GameManager.Instance != null ? GameManager.Instance.securitySystem : null;
        securitySystem?.ReportViolation("Helping hand trade");

        if (tradeUiRoot != null)
        {
            tradeUiRoot.SetActive(true);
        }
    }

    public void StopInteract()
    {
        if (tradeUiRoot != null)
        {
            tradeUiRoot.SetActive(false);
        }
    }

    public void OfferInventorySlot(int slotIndex)
    {
        handTradeSystem?.OfferFromInventorySlot(slotIndex);
    }

    public void ReturnOffer(int offerIndex)
    {
        handTradeSystem?.ReturnOffer(offerIndex);
    }

    public void SubmitTrade()
    {
        handTradeSystem?.SubmitOffer();
    }
}
