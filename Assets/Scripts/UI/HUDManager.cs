using System;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;
    
    public GameObject itemScanHUD;
    public ItemMarkerUI itemMarkerUI;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void showItemScanHUD(Item currentItem = null)
    {
        itemScanHUD.SetActive(true);
        itemMarkerUI?.BeginItem(currentItem);
    }
    
    public void hideItemScanHUD()
    {
        itemMarkerUI?.EndItem();
        itemScanHUD.SetActive(false);
    }
}
