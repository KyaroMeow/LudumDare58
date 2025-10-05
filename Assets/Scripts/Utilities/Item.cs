using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("Item Properties")]
    public bool isDefective = false;
    public bool isSorted = false;

    [Header("UV Properties")]
    public bool hasUVStain = false;
    public Renderer stainRenderer;

    public void SetUVVisibility(bool isVisible)
    {
        if (stainRenderer != null)
        {
            stainRenderer.enabled = isVisible;
        }
    }
    
}
