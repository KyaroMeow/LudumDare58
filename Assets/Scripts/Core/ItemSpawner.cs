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
    public void SpawnBomb()
    {
        if (bomb != null)
        {
        GameObject spawnedBomb = Instantiate(bomb, transform.position, Quaternion.identity);
        GameManager.Instance.currentItem = spawnedBomb;
        }
    }

    public void SpawnAnomalyItem()
    {
        GameObject spawnedItem = Instantiate(anomalyItem, transform.position, Quaternion.identity);
        GameManager.Instance.currentItem = spawnedItem;
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
        if (!itemObject.TryGetComponent<ConveyorItemInteractable>(out _))
        {
            itemObject.AddComponent<ConveyorItemInteractable>();
        }
        
        Item item = itemObject.GetComponent<Item>();
        if (item == null) return;

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


}
    

