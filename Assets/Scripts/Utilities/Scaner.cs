using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Scaner : MonoBehaviour
{
    [SerializeField] GameObject parent;
    private void Update()
    {
        ScanItem();
    }
    private void ScanItem()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.up, out hit, 3f))
        {
            if (hit.collider.isTrigger && hit.collider.CompareTag("code"))
            {
                Item item = GameManager.Instance.currentItem.GetComponent<Item>();
                if (item != null)
                {
                    GameManager.Instance.ShowScanResult(!item.isDefective);
                    parent.SetActive(false);
                }
            }
        }
    }
}
