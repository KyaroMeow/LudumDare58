using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("Item Prefabs")]
    [SerializeField] private GameObject[] itemPrefabs;
    [SerializeField] private GameObject anomalyItem;
    [SerializeField] private GameObject bomb;

    public void SpawnAnomalyItem()
    {
        GameObject itemToSpawn = Instantiate(anomalyItem, transform.position, Quaternion.identity);
        GameManager.Instance.currentItem = itemToSpawn;
    }
    
    public void SpawnItem()
    {
        if (itemPrefabs.Length == 0)
        {
            Debug.LogError("No item prefabs assigned!");
            return;
        }

        int randomIndex = Random.Range(0, itemPrefabs.Length);
        GameObject itemToSpawn = Instantiate(itemPrefabs[randomIndex], transform.position, Quaternion.identity);

        SetupItem(itemToSpawn);
        GameManager.Instance.currentItem = itemToSpawn;
    }

    private void SetupItem(GameObject itemObject)
    {
        Item item = itemObject.GetComponent<Item>();
        if (item == null) return;

        // 1. Определяем есть ли пятно (главный дефект)
        bool hasStain = Random.Range(0f, 1f) <= SettingManager.Instance.defectChance;
        
        // 2. Определяем есть ли штрихкод
        bool hasBarcode = Random.Range(0f, 1f) > SettingManager.Instance.noBarcodeChance;
        
        // 3. Определяем что показывает штрихкод
        bool barcodeShowsGood = true;
        if (hasBarcode)
        {
            // Штрихкод может показывать неверную информацию
            barcodeShowsGood = Random.Range(0f, 1f) > SettingManager.Instance.wrongBarcodeChance;
        }

        // 4. Определяем общий статус defective
        // Предмет считается defective если у него есть пятно ИЛИ штрихкод врет
        bool isDefective = hasStain || (hasBarcode && !barcodeShowsGood);

        item.InitializeItem(isDefective, hasBarcode, barcodeShowsGood, hasStain);
    }

}
    

