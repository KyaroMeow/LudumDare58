using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UVLighter : MonoBehaviour
{
    [SerializeField] private GameObject lighter;
    public void ToggleLighterOff()
    {
        lighter.SetActive(false);
        GameManager.Instance.currentItem.GetComponent<Item>().SetUVVisibility(false);
    }
    public void ToggleLighter()
    {
        lighter.SetActive(!lighter.activeSelf);
        GameManager.Instance.currentItem.GetComponent<Item>().SetUVVisibility(lighter.activeSelf);
    }
}
