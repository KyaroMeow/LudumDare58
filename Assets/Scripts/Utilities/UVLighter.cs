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
        if(GameManager.Instance != null && GameManager.Instance.TryResolveCurrentItem(out Item item)) item.SetUVVisibility(false);
        uVOnTable.SetActive(true);
    }
    public void ToggleLighter()
    {
        lighter.SetActive(!lighter.activeSelf);
        uVOnTable.SetActive(!lighter.activeSelf);
        if(GameManager.Instance != null && GameManager.Instance.TryResolveCurrentItem(out Item item)) item.SetUVVisibility(lighter.activeSelf);
    }
}
