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

    [Header("Marker Properties")]
    [SerializeField] private bool isAnomalyItem = false;
    [SerializeField] private int extraHiddenDefectCount = 0;

    [Header("UV Properties")]
    public List<GameObject> stainSpots = new List<GameObject>();
    public GameObject[] scratches;
    [HideInInspector] public Renderer stainRenderer;
    public GameObject barcode;
    private readonly HashSet<ItemMarkerType> expectedMarkers = new HashSet<ItemMarkerType>();

    public void InitializeItem(bool defective, bool barcode, bool barcodeGood, bool stain, bool Scratches)
    {
        isDefective = defective;
        hasBarcode = barcode;
        barcodeShowsGood = barcodeGood;
        hasUVStain = stain;
        hasScratches = Scratches;
        
        UpdateVisuals();
        RebuildExpectedMarkers();
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

    public int GetMissedMarkerCount(IEnumerable<ItemMarkerType> markedMarkers)
    {
        HashSet<ItemMarkerType> markedSet = new HashSet<ItemMarkerType>(markedMarkers);
        int missedMarkers = 0;

        foreach (ItemMarkerType marker in expectedMarkers)
        {
            if (!markedSet.Contains(marker))
            {
                missedMarkers++;
            }
        }

        return missedMarkers;
    }

    public string BuildExpectedMarkersDebugText()
    {
        if (expectedMarkers.Count == 0)
        {
            return "None";
        }

        return string.Join(", ", expectedMarkers);
    }

    private void RebuildExpectedMarkers()
    {
        expectedMarkers.Clear();

        int defectCount = 0;

        if (hasScratches)
        {
            expectedMarkers.Add(ItemMarkerType.Scratch);
            defectCount++;
        }

        if (hasUVStain)
        {
            expectedMarkers.Add(ItemMarkerType.Stain);
            defectCount++;
        }

        if (hasBarcode && barcodeShowsGood)
        {
            expectedMarkers.Add(ItemMarkerType.LegitimacyPositive);
        }
        else
        {
            expectedMarkers.Add(ItemMarkerType.LegitimacyNegative);
            defectCount++;
        }

        if (isAnomalyItem)
        {
            expectedMarkers.Add(ItemMarkerType.Anomaly);
            defectCount++;
        }

        defectCount += Mathf.Max(0, extraHiddenDefectCount);

        if (defectCount == 0)
        {
            expectedMarkers.Add(ItemMarkerType.Ideal);
        }
        else
        {
            expectedMarkers.Add(ItemMarkerType.Defective);
        }

        if (defectCount > 4)
        {
            expectedMarkers.Add(ItemMarkerType.MassProduct);
        }
    }

}
