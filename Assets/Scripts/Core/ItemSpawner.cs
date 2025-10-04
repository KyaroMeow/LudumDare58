using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] itemsPrefab;
    public void SpawnItem()
    {
        int randomNumber = Random.Range(0, itemsPrefab.Length);
        GameObject newRandomItem = itemsPrefab[randomNumber];
        GameObject spawnedItem = Instantiate(newRandomItem,transform.position,Quaternion.identity);
        GameManager.Instance.currentItem = spawnedItem;
    }
}
