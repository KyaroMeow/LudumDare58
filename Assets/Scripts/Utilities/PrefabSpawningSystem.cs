using System.Collections;
using UnityEngine;

public class PrefabSpawningSystem : MonoBehaviour
{
    [Header("Настройки спавна")]
    [Tooltip("Префабы для спавна")]
    public GameObject[] prefabsToSpawn;
    
    [Tooltip("Время жизни объектов (секунды)")]
    public float destroyAfterSeconds = 5f;
    
    [Tooltip("Автоматически начать спавн при старте")]
    public bool autoStart = false;

    [Header("Точка спавна")]
    [Tooltip("Если не задан, используется текущий GameObject")]
    public Transform spawnPoint;

    [Header("Интервал между спавном")]
    [Tooltip("Задержка перед спавном следующего объекта после удаления предыдущего")]
    public float delayBetweenSpawns = 0.1f;

    private Coroutine spawningCoroutine;
    private bool isSpawning = false;

    private void Start()
    {
        if (spawnPoint == null)
            spawnPoint = transform;

        if (autoStart)
            StartInfiniteSpawning();
    }

    public void StartInfiniteSpawning()
    {
        if (prefabsToSpawn.Length == 0)
        {
            Debug.LogError("Не заданы префабы для спавна!");
            return;
        }

        if (isSpawning)
        {
            Debug.LogWarning("Спавн уже запущен!");
            return;
        }

        isSpawning = true;
        spawningCoroutine = StartCoroutine(InfiniteSpawnRoutine());
    }

    public void StopInfiniteSpawning()
    {
        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
            spawningCoroutine = null;
        }
        isSpawning = false;
    }

    private IEnumerator InfiniteSpawnRoutine()
    {
        while (isSpawning)
        {
            // Выбираем случайный префаб
            GameObject randomPrefab = GetRandomPrefab();
            
            if (randomPrefab != null)
            {
                // Спавним префаб
                GameObject spawnedObject = SpawnPrefab(randomPrefab);
                
                // Запускаем корутину для спавна следующего объекта после удаления текущего
                yield return StartCoroutine(SpawnNextAfterDestroy(spawnedObject));
            }
            
            yield return new WaitForSeconds(delayBetweenSpawns);
        }
    }

    private IEnumerator SpawnNextAfterDestroy(GameObject targetObject)
    {
        // Ждем пока объект не будет уничтожен
        while (targetObject != null)
        {
            yield return null;
        }
        
        // После уничтожения объекта можно добавить дополнительную задержку
        yield return new WaitForSeconds(delayBetweenSpawns);
    }

    private GameObject GetRandomPrefab()
    {
        if (prefabsToSpawn.Length == 0) return null;
        
        int randomIndex = Random.Range(0, prefabsToSpawn.Length);
        return prefabsToSpawn[randomIndex];
    }

    public GameObject SpawnPrefab(GameObject prefab)
    {
        if (prefab == null) return null;

        GameObject newInstance = Instantiate(
            prefab,
            spawnPoint.position,
            spawnPoint.rotation
        );
        newInstance.layer = 0;
        // Уничтожаем объект через заданное время
        Destroy(newInstance, destroyAfterSeconds);

        return newInstance;
    }

    // Для спавна конкретного префаба по индексу
    public void SpawnSpecificPrefab(int index)
    {
        if (index < 0 || index >= prefabsToSpawn.Length)
        {
            Debug.LogError($"Неверный индекс префаба: {index}");
            return;
        }
        
        GameObject spawnedObject = SpawnPrefab(prefabsToSpawn[index]);
        
        // Если бесконечный спавн активен, не запускаем дополнительную логику
        if (!isSpawning)
        {
            StartCoroutine(SpawnNextAfterDestroy(spawnedObject));
        }
    }

    private void OnDestroy()
    {
        StopInfiniteSpawning();
    }
}