using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("Item Prefabs")]
    [SerializeField] private GameObject[] itemPrefabs;
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private SfxCue conveyorDoorSfx;
    [SerializeField] private SfxEmitter doorSfxEmitter;
    [SerializeField] private GameObject bomb;
    [SerializeField] private GameObject anomalyItem;
    private bool isStoryPaused;
    private bool warnedStoryPaused;

    public bool IsStoryPaused => isStoryPaused;

    public void SetStoryPaused(bool paused)
    {
        isStoryPaused = paused;
        if (!paused)
        {
            warnedStoryPaused = false;
        }
    }

    public void SpawnBomb()
    {
        if (IsSpawnBlockedByStory())
        {
            return;
        }

        if (bomb != null)
        {
            GameObject spawnedBomb = Instantiate(bomb, transform.position, Quaternion.identity);
            AssignCurrentItem(spawnedBomb, "Bomb");
        }
        else
        {
            Debug.LogWarning("Cannot spawn bomb because bomb prefab is not assigned.");
        }
    }

    public void SpawnAnomalyItem()
    {
        if (IsSpawnBlockedByStory())
        {
            return;
        }

        if (anomalyItem == null)
        {
            Debug.LogWarning("Cannot spawn anomaly item because anomalyItem prefab is not assigned.");
            return;
        }

        GameObject spawnedItem = Instantiate(anomalyItem, transform.position, Quaternion.identity);
        AssignCurrentItem(spawnedItem, "Anomaly item");
    }

    public void SpawnItem()
    {
        if (IsSpawnBlockedByStory())
        {
            return;
        }

        if (itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.LogError("No item prefabs assigned!");
            return;
        }

        int randomIndex = Random.Range(0, itemPrefabs.Length);
        if (itemPrefabs[randomIndex] == null)
        {
            Debug.LogWarning($"Cannot spawn item because item prefab at index {randomIndex} is not assigned.");
            return;
        }

        GameObject itemToSpawn = Instantiate(itemPrefabs[randomIndex], transform.position, Quaternion.identity);

        SetupItem(itemToSpawn);
        AssignCurrentItem(itemToSpawn, "Item");

        if(doorAnimator != null)
        {
            doorAnimator.SetTrigger("open");
            PlayConveyorDoorSfx();
        }
    }

    private void PlayConveyorDoorSfx()
    {
        SfxEmitter emitter = ResolveDoorSfxEmitter();
        if (emitter == null)
        {
            return;
        }

        emitter.PlayOneShot(conveyorDoorSfx);
    }

    private SfxEmitter ResolveDoorSfxEmitter()
    {
        if (doorSfxEmitter != null)
        {
            return doorSfxEmitter;
        }

        if (doorAnimator != null)
        {
            doorSfxEmitter = doorAnimator.GetComponent<SfxEmitter>();
            if (doorSfxEmitter == null)
            {
                doorSfxEmitter = doorAnimator.gameObject.AddComponent<SfxEmitter>();
            }

            return doorSfxEmitter;
        }

        doorSfxEmitter = GetComponent<SfxEmitter>();
        if (doorSfxEmitter == null)
        {
            doorSfxEmitter = gameObject.AddComponent<SfxEmitter>();
        }

        return doorSfxEmitter;
    }

    private void SetupItem(GameObject itemObject)
    {
        if (itemObject == null)
        {
            return;
        }

        if (!itemObject.TryGetComponent<ConveyorItemInteractable>(out _))
        {
            itemObject.AddComponent<ConveyorItemInteractable>();
        }
        
        Item item = itemObject.GetComponent<Item>();
        if (item == null)
        {
            item = itemObject.GetComponentInChildren<Item>(true);
        }

        if (item == null)
        {
            Debug.LogWarning($"Spawned item '{itemObject.name}' has no Item component on root or children.");
            return;
        }

        Difficult difficulty = SettingManager.EnsureInstance()?.currentDifficulty;
        if (difficulty == null)
        {
            Debug.LogError("Cannot initialize spawned item because current difficulty is not assigned.");
            return;
        }

        bool hasStain = false;
        bool hasBarcode = true;
        bool barcodeShowsGood = true;
        bool hasScratches = false;

        // РљР°Р¶РґС‹Р№ РґРµС„РµРєС‚ РїСЂРѕРІРµСЂСЏРµС‚СЃСЏ РЅРµР·Р°РІРёСЃРёРјРѕ
        float roll = Random.Range(0f, 1f);

        // РџСЏС‚РЅРѕ
        if (roll <= difficulty.defectChance)
        {
            hasStain = true;
        }

        // РћС‚СЃСѓС‚СЃС‚РІРёРµ С€С‚СЂРёС…РєРѕРґР°
        roll = Random.Range(0f, 1f);
        if (roll <= difficulty.noBarcodeChance)
        {
            hasBarcode = false;
        }

        // РќРµРїСЂР°РІРёР»СЊРЅС‹Р№ С€С‚СЂРёС…РєРѕРґ (С‚РѕР»СЊРєРѕ РµСЃР»Рё С€С‚СЂРёС…РєРѕРґ РµСЃС‚СЊ)
        if (hasBarcode)
        {
            roll = Random.Range(0f, 1f);
            if (roll <= difficulty.wrongBarcodeChance)
            {
                barcodeShowsGood = false;
            }
        }

        // Р¦Р°СЂР°РїРёРЅС‹
        roll = Random.Range(0f, 1f);
        if (roll <= difficulty.scratchesChance)
        {
            hasScratches = true;
        }

        // РџСЂРµРґРјРµС‚ РґРµС„РµРєС‚РЅС‹Р№ РµСЃР»Рё РµСЃС‚СЊ С…РѕС‚СЏ Р±С‹ РѕРґРёРЅ РґРµС„РµРєС‚
        bool isDefective = hasStain || !hasBarcode || !barcodeShowsGood || hasScratches;

        item.InitializeItem(isDefective, hasBarcode, barcodeShowsGood, hasStain, hasScratches);
    }

    private void AssignCurrentItem(GameObject spawnedObject, string spawnType)
    {
        if (spawnedObject == null)
        {
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"Cannot assign spawned {spawnType} '{spawnedObject.name}' because GameManager.Instance is missing.");
            return;
        }

        Item rootItem = spawnedObject.GetComponent<Item>();
        if (rootItem == null)
        {
            Item childItem = spawnedObject.GetComponentInChildren<Item>(true);
            if (childItem != null)
            {
                Debug.Log($"{spawnType} '{spawnedObject.name}' has Item component on child '{childItem.gameObject.name}'.");
            }
            else
            {
                Debug.LogWarning($"{spawnType} '{spawnedObject.name}' has no Item component on root or children. It will be skipped safely if submitted as a sortable item.");
            }
        }

        GameManager.Instance.currentItem = spawnedObject;
    }

    private bool IsSpawnBlockedByStory()
    {
        if (!isStoryPaused)
        {
            return false;
        }

        if (!warnedStoryPaused)
        {
            warnedStoryPaused = true;
            Debug.Log("Item spawn skipped because vent hand intro is running.");
        }

        return true;
    }

}
    

