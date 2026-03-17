using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [HideInInspector] public bool isDefective = false;
    [HideInInspector] public bool isSorted = false;
    [HideInInspector] public bool hasBarcode = true;
    [HideInInspector] public bool hasScratches = true;
    [HideInInspector] public bool barcodeShowsGood = true;
    [HideInInspector] public bool hasUVStain = false;

    [Header("UV Properties")]
    public List<GameObject> stainSpots = new List<GameObject>();
    public GameObject[] scratches;
    [HideInInspector] public Renderer stainRenderer;
    public GameObject barcode;
    public void InitializeItem(bool defective, bool barcode, bool barcodeGood, bool stain, bool Scratches)
    {
        isDefective = defective;
        hasBarcode = barcode;
        barcodeShowsGood = barcodeGood;
        hasUVStain = stain;
        hasScratches = Scratches;
        
        UpdateVisuals();
    }
    private void UpdateVisuals()
    {
        if (barcode != null)
        {
            barcode.SetActive(hasBarcode);
        }

        if (hasScratches)
        {
            SetScratchesVisibility();
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
    public void SetScratchesVisibility()
    {
        if (scratches != null)
        {
            foreach(GameObject sharp in scratches)
            {
                sharp.SetActive(true);
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
