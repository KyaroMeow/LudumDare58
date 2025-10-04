using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("Item Properties")]
    public ItemClass itemClass;

    public enum ItemClass
    {
        Сheap,
        Normal,
        Valuable,
        Anomaly
    }
    
}
