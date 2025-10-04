using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SortButtons : MonoBehaviour
{
    [SerializeField] private Item.ItemClass buttonClass;
    
    public void OnSortButtonClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SortItem(buttonClass);
    }
}
