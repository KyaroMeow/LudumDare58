using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("Item Properties")]
    public bool isDefective = false;
    public bool isSorted = false;
    [Header("Features")]
    public bool hasBarcode = true; 
    public bool barcodeShowsGood = true; 
    public bool hasUVStain = false;

    [Header("UV Properties")]
    public List<GameObject> stainSpots = new List<GameObject>();
    public Renderer stainRenderer;
    public GameObject barcode;
    public void InitializeItem(bool defective, bool barcode, bool barcodeGood, bool stain)
    {
        isDefective = defective;
        hasBarcode = barcode;
        barcodeShowsGood = barcodeGood;
        hasUVStain = stain;
        
        UpdateVisuals();
    }
    private void UpdateVisuals()
    {
        if (barcode != null)
        {
            barcode.SetActive(hasBarcode);
        }
        
        if (hasUVStain && stainSpots.Count > 0)
        {
            int randomIndex = Random.Range(0, stainSpots.Count);
            for (int i = 0; i < stainSpots.Count; i++)
            {
                if (stainSpots[i] != null)
                {
                    stainSpots[i].SetActive(i == randomIndex);
                    stainRenderer = stainSpots[randomIndex].GetComponent<Renderer>();
                }
            }
        }
        else
        {
            foreach (GameObject stain in stainSpots)
            {
                if (stain != null) stain.SetActive(false);
            }
        }
    }
    public void SetUVVisibility(bool isVisible)
    {
        if (stainRenderer != null)
        {
            stainRenderer.enabled = isVisible;
        }
    }
    public bool ShouldBeAccepted()
    {
        return !hasUVStain && hasBarcode && barcodeShowsGood;
    }
    
}
