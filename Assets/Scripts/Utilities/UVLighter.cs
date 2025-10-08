using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UVLighter : MonoBehaviour
{
    [SerializeField] private GameObject lighter;
    [SerializeField] private GameObject uVOnTable;
    public void ToggleLighterOff()
    {
        lighter.SetActive(false);
        GameManager.Instance.currentItem.GetComponent<Item>().SetUVVisibility(false);
        uVOnTable.SetActive(false);
    }
    public void ToggleLighter()
    {
        lighter.SetActive(!lighter.activeSelf);
        uVOnTable.SetActive(!lighter.activeSelf);
        GameManager.Instance.currentItem.GetComponent<Item>().SetUVVisibility(lighter.activeSelf);
    }
}
