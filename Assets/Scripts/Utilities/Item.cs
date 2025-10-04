using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("Item Properties")]
    public ItemClass itemClass;

    public bool isSorted = false;

    public enum ItemClass
    {
        Сheap,
        Normal,
        Valuable,
        Anomaly
    }
    
}
