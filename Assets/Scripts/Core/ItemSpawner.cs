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
        if (uvStainPrefab == null) return;

        item.hasUVStain = true;

        GameObject stain = Instantiate(uvStainPrefab);
        stain.transform.SetParent(item.transform);

        PlaceStainOnSurface(stain, item.gameObject);

        item.stainRenderer = stain.GetComponent<Renderer>();
        item.SetUVVisibility(false);

    }
    
    private void PlaceStainOnSurface(GameObject stain, GameObject item)
    {
        Renderer itemRenderer = item.GetComponent<Renderer>();
        if (itemRenderer == null) return;

        Bounds bounds = itemRenderer.bounds;

        Vector3 surfacePoint = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.max.y + 0.02f,
            Random.Range(bounds.min.z, bounds.max.z)
        );

        stain.transform.position = surfacePoint;
        stain.transform.rotation = Quaternion.Euler(90, 0, 0);

        float randomScale = Random.Range(0.1f, 0.3f);
        stain.transform.localScale = Vector3.one * randomScale;
        stain.transform.Rotate(0, 0, Random.Range(0, 360), Space.Self);
    }
}
