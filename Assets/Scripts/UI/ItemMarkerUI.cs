using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemMarkerUI : MonoBehaviour
{
    public enum MarkerVerdict
    {
        None = 0,
        Accept = 1,
        Reject = 2
    }

    [Serializable]
    private struct MarkerVisualBinding
    {
        public ItemMarkerType markerType;
        public GameObject selectedStateObject;
    }

    [SerializeField] private MarkerVisualBinding[] markerVisuals;
    [SerializeField] private SfxEmitter sfxEmitter;
    [SerializeField] private SfxCue markerToggleSfx;

    private readonly HashSet<ItemMarkerType> selectedMarkers = new HashSet<ItemMarkerType>();
    private Item currentItem;

    private void Awake()
    {
        RefreshVisuals();
    }

    public void BeginItem(Item item)
    {
        currentItem = item;
        LoadSelectionFromItem();
    }

    public void EndItem()
    {
        currentItem = null;
        ClearSelection();
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

        return item.GetMissedPlayerMarkerCount();
    }

    public bool HasAnySelection()
    {
        return currentItem != null
            ? currentItem.HasAnyPlayerMarkers()
            : selectedMarkers.Count > 0;
    }

    public MarkerVerdict GetCurrentVerdict()
    {
        if (selectedMarkers.Count == 0)
        {
            return MarkerVerdict.None;
        }

        if (selectedMarkers.Contains(ItemMarkerType.Ideal))
        {
            return MarkerVerdict.Accept;
        }

        if (selectedMarkers.Contains(ItemMarkerType.Defective) ||
            selectedMarkers.Contains(ItemMarkerType.Scratch) ||
            selectedMarkers.Contains(ItemMarkerType.Stain) ||
            selectedMarkers.Contains(ItemMarkerType.LegitimacyNegative) ||
            selectedMarkers.Contains(ItemMarkerType.Anomaly) ||
            selectedMarkers.Contains(ItemMarkerType.MassProduct))
        {
            return MarkerVerdict.Reject;
        }

        if (selectedMarkers.Contains(ItemMarkerType.LegitimacyPositive))
        {
            return MarkerVerdict.Accept;
        }

        return MarkerVerdict.None;
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

        currentItem.SetMarkerSelected(markerType, selectedMarkers.Contains(markerType));
        RefreshVisuals();
        PlaySfx(markerToggleSfx);
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

    private void LoadSelectionFromItem()
    {
        selectedMarkers.Clear();

        if (currentItem != null)
        {
            foreach (ItemMarkerType markerType in currentItem.GetPlayerMarkedMarkers())
            {
                selectedMarkers.Add(markerType);
            }
        }

        RefreshVisuals();
    }

    private void PlaySfx(SfxCue cue)
    {
        if (cue == null)
        {
            return;
        }

        if (sfxEmitter == null)
        {
            sfxEmitter = GetComponent<SfxEmitter>();
            if (sfxEmitter == null)
            {
                sfxEmitter = gameObject.AddComponent<SfxEmitter>();
            }
        }

        sfxEmitter.Play(cue);
    }
}
