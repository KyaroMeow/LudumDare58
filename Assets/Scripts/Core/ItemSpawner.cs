using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("Item Prefabs")]
    [SerializeField] private GameObject[] itemPrefabs;
    [SerializeField] private GameObject anomalyItem;
    [SerializeField] private GameObject bomb;

    [Header("UV Stain Prefab")]
    [SerializeField] private GameObject uvStainPrefab;

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

        Item item = itemToSpawn.GetComponent<Item>();
        Debug.Log($"Spawned item - Defective: {item.isDefective}, UV Stain: {item.hasUVStain}");
    }

    private void SetupItem(GameObject itemObject)
    {
        Item item = itemObject.GetComponent<Item>();
        if (item == null) return;

        bool isDefective = Random.Range(0f, 1f) <= SettingManager.Instance.defectChance;
        item.isDefective = isDefective;

        if (isDefective)
        {
            bool hasStain = Random.Range(0f, 1f) <= SettingManager.Instance.stainChance;
            if (hasStain && uvStainPrefab != null)
            {
                AddUVStainToItem(item);
            }
        }
        else
        {
            item.hasUVStain = false;
        }
    }

    private void AddUVStainToItem(Item item)
    {
        if (item.stainSpots.Count < 1) return;

        item.hasUVStain = true;
        GameObject randomStain = item.stainSpots[Random.Range(0, item.stainSpots.Count)];
        randomStain.SetActive(true);
        item.stainRenderer = randomStain.GetComponent<Renderer>();
        item.SetUVVisibility(false);

    }
}
    

