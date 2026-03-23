using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemMarkerUI : MonoBehaviour
{
    [Serializable]
    private struct MarkerVisualBinding
    {
        public ItemMarkerType markerType;
        public GameObject selectedStateObject;
    }

    [SerializeField] private GameObject markerMenuRoot;
    [SerializeField] private MarkerVisualBinding[] markerVisuals;

    private readonly HashSet<ItemMarkerType> selectedMarkers = new HashSet<ItemMarkerType>();
    private Item currentItem;

    private void Awake()
    {
        RefreshVisuals();
        HideMenu();
    }

    public void BeginItem(Item item)
    {
        currentItem = item;
        ClearSelection();
        HideMenu();
    }

    public void EndItem()
    {
        currentItem = null;
        ClearSelection();
        HideMenu();
    }

    public void ToggleMenu()
    {
        if (currentItem == null || markerMenuRoot == null)
        {
            return;
        }

        markerMenuRoot.SetActive(!markerMenuRoot.activeSelf);
    }

    public void ShowMenu()
    {
        if (currentItem == null || markerMenuRoot == null)
        {
            return;
        }

        markerMenuRoot.SetActive(true);
    }

    public void HideMenu()
    {
        if (markerMenuRoot != null)
        {
            markerMenuRoot.SetActive(false);
        }
    }

    public void ToggleMarker(int markerTypeIndex)
    {
        if (!Enum.IsDefined(typeof(ItemMarkerType), markerTypeIndex))
        {
            Debug.LogWarning($"Unknown marker index: {markerTypeIndex}");
            return;
        }

        ToggleMarker((ItemMarkerType)markerTypeIndex);
    }

    public void ToggleMarkerByName(string markerTypeName)
    {
        if (!Enum.TryParse(markerTypeName, true, out ItemMarkerType markerType))
        {
            Debug.LogWarning($"Unknown marker name: {markerTypeName}");
            return;
        }

        ToggleMarker(markerType);
    }

    public int EvaluateCurrentSelection(Item item)
    {
        if (item == null)
        {
            return 0;
        }

        return item.GetMissedMarkerCount(selectedMarkers);
    }

    public string BuildSelectionDebugText()
    {
        if (selectedMarkers.Count == 0)
        {
            return "None";
        }

        return string.Join(", ", selectedMarkers);
    }

    private void ToggleMarker(ItemMarkerType markerType)
    {
        if (currentItem == null)
        {
            return;
        }

        if (!selectedMarkers.Add(markerType))
        {
            selectedMarkers.Remove(markerType);
        }

        RefreshVisuals();
    }

    private void ClearSelection()
    {
        selectedMarkers.Clear();
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        for (int i = 0; i < markerVisuals.Length; i++)
        {
            if (markerVisuals[i].selectedStateObject != null)
            {
                bool isSelected = selectedMarkers.Contains(markerVisuals[i].markerType);
                markerVisuals[i].selectedStateObject.SetActive(isSelected);
            }
        }
    }
}
