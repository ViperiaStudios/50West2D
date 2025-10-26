using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [Header("Common (70%) Prefabs")]
    public List<GameObject> commonPrefabs;

    [Header("Rare (30%) Prefabs - Optional")]
    public List<GameObject> rarePrefabs;
    
    [Header("Gas Tank Spawning")]
    public GameObject gasTankPrefab;
    public float gasTankSpawnInterval = 15f; // Spawn gas tank every X seconds

    [Header("Early Obstacles (0-25s) - Available from Start")]
    public List<GameObject> earlyObstaclePrefabs;

    [Header("Mid-Game Obstacles (25s+) - Added to Pool at 25s")]
    public List<GameObject> midGameObstaclePrefabs;

    [Header("Late-Game Obstacles (50s+) - Added to Pool at 50s")]
    public List<GameObject> lateGameObstaclePrefabs;

    [Header("Obstacle Spawn Settings")]
    public float obstacleSpawnInterval = 3f;
    public float spawnIntervalDecreaseAmount = 0.2f; // How much faster to spawn each milestone
    public float minimumSpawnInterval = 1f; // Fastest possible spawn rate
    
    [Header("Spawn Lane Positions")]
    [Tooltip("Y position for high lane obstacles (based on player max Y: 1.37)")]
    public float highLaneY = 1.2f;
    [Tooltip("Y position for low lane obstacles (based on player min Y: -0.73)")]
    public float lowLaneY = -0.6f;
    [Tooltip("Random variance for lane positions (±)")]
    public float laneVariance = 0.1f;
    
    [Header("Progressive Spawn Rate")]
    [Tooltip("Every X seconds, spawn obstacles faster")]
    public float spawnRateIncreaseInterval = 40f;
    
    [Header("Legacy Obstacle Prefabs (Deprecated)")]
    public List<GameObject> obstaclePrefabs;

    public int poolSize = 10;
    public float spawnInterval = 1.5f;
    
    private float originalSpawnInterval; // Store original for turbo boost
    private bool turboBoostActive = false;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private Queue<GameObject> obstaclePool = new Queue<GameObject>();
    private Queue<GameObject> gasTankPool = new Queue<GameObject>();
    private List<GameObject> activeObstaclePrefabs = new List<GameObject>(); // Current available obstacles
    private float timer = 0f;
    private float obstacleTimer = 0f;
    private float gasTankTimer = 0f;
    private float gameTime = 0f;
    private bool midGameUnlocked = false;
    private bool lateGameUnlocked = false;
    private float currentObstacleSpawnInterval; // Current spawn interval (decreases over time)
    private float nextSpawnRateIncrease = 0f; // When to increase spawn rate next
    private string lastSpawnedObstacleType = ""; // Track last spawned obstacle type name

    void Start()
    {
        // Initialize food object pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject prefabToUse = GetRandomPrefab();
            GameObject obj = Instantiate(prefabToUse, transform);
            obj.SetActive(false);

            var mover = obj.GetComponent<PooledObjectMover>();
            if (mover != null)
                mover.Initialize(this);

            pool.Enqueue(obj);
        }

        // Start with early obstacles only
        activeObstaclePrefabs.AddRange(earlyObstaclePrefabs);
        
        // Initialize obstacle pool with early obstacles
        InitializeObstaclePool(activeObstaclePrefabs, obstaclePool, "Initial");
        
        // Initialize gas tank pool
        if (gasTankPrefab != null)
        {
            for (int i = 0; i < 5; i++) // Smaller pool for gas tanks
            {
                GameObject obj = Instantiate(gasTankPrefab, transform);
                obj.SetActive(false);
                
                var gasTank = obj.GetComponent<GasTankPickup>();
                if (gasTank != null)
                    gasTank.Initialize(this);
                
                gasTankPool.Enqueue(obj);
            }
            Debug.Log($"[ObjectPool] Gas tank pool initialized with 5 objects");
        }

        // Initialize legacy obstacle pool (for backward compatibility)
        if (obstaclePrefabs.Count > 0)
        {
            Debug.LogWarning("[ObjectPool] Legacy obstacle prefabs detected. Please migrate to the new system (Early/Mid/Late obstacles).");
        }
        
        // Initialize spawn rate
        currentObstacleSpawnInterval = obstacleSpawnInterval;
        nextSpawnRateIncrease = spawnRateIncreaseInterval;
        originalSpawnInterval = spawnInterval; // Store for turbo boost
        
        Debug.Log($"[ObjectPool] Starting with {activeObstaclePrefabs.Count} early obstacle types");
        
        // Check if mid/late game obstacles are assigned
        if (midGameObstaclePrefabs.Count == 0)
        {
            Debug.LogWarning("[ObjectPool] No Mid-Game obstacle prefabs assigned!");
        }
        else
        {
            Debug.Log($"[ObjectPool] {midGameObstaclePrefabs.Count} mid-game obstacle types will unlock at 25s");
        }
        
        if (lateGameObstaclePrefabs.Count == 0)
        {
            Debug.LogWarning("[ObjectPool] No Late-Game obstacle prefabs assigned!");
        }
        else
        {
            Debug.Log($"[ObjectPool] {lateGameObstaclePrefabs.Count} late-game obstacle types will unlock at 50s");
        }
        
        Debug.Log($"[ObjectPool] Spawn rate increases every {spawnRateIncreaseInterval}s (decreasing interval by {spawnIntervalDecreaseAmount}s)");
    }

    void Update()
    {
        // Track game time
        gameTime += Time.deltaTime;
        
        // Unlock mid-game obstacles at 25 seconds
        if (!midGameUnlocked && gameTime >= 25f && midGameObstaclePrefabs.Count > 0)
        {
            midGameUnlocked = true;
            activeObstaclePrefabs.AddRange(midGameObstaclePrefabs);
            
            // Add mid-game obstacles to the pool
            InitializeObstaclePool(midGameObstaclePrefabs, obstaclePool, "Mid-Game");
            
            Debug.Log($"[ObjectPool] Mid-game obstacles UNLOCKED! Now {activeObstaclePrefabs.Count} obstacle types available");
        }
        
        // Unlock late-game obstacles at 50 seconds
        if (!lateGameUnlocked && gameTime >= 50f && lateGameObstaclePrefabs.Count > 0)
        {
            lateGameUnlocked = true;
            activeObstaclePrefabs.AddRange(lateGameObstaclePrefabs);
            
            // Add late-game obstacles to the pool
            InitializeObstaclePool(lateGameObstaclePrefabs, obstaclePool, "Late-Game");
            
            Debug.Log($"[ObjectPool] Late-game obstacles UNLOCKED! Now {activeObstaclePrefabs.Count} obstacle types available");
        }
        
        // Progressively increase spawn rate every X seconds
        if (gameTime >= nextSpawnRateIncrease && currentObstacleSpawnInterval > minimumSpawnInterval)
        {
            currentObstacleSpawnInterval = Mathf.Max(minimumSpawnInterval, currentObstacleSpawnInterval - spawnIntervalDecreaseAmount);
            
            nextSpawnRateIncrease = gameTime + spawnRateIncreaseInterval;
            Debug.Log($"[ObjectPool] SPAWN RATE INCREASED! Now spawning every {currentObstacleSpawnInterval}s");
        }
        
        // Food spawning timer
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;

            Vector2 spawnPos = new Vector2(11f, Random.Range(-1f, 1.4f));
            GetFromPool(spawnPos);
        }

        // Obstacle spawning timer (progressively faster)
        if (activeObstaclePrefabs.Count > 0)
        {
            obstacleTimer += Time.deltaTime;
            if (obstacleTimer >= currentObstacleSpawnInterval)
            {
                obstacleTimer = 0f;

                // Randomly choose high or low lane
                float yPos = Random.value > 0.5f ? highLaneY : lowLaneY;
                
                // Add small variance to make it less rigid
                yPos += Random.Range(-laneVariance, laneVariance);
                
                Vector2 spawnPos = new Vector2(11f, yPos);
                GetObstacleFromPool(spawnPos);
            }
        }
        
        // Gas tank spawning timer (spawns continuously)
        if (gasTankPrefab != null)
        {
            gasTankTimer += Time.deltaTime;
            if (gasTankTimer >= gasTankSpawnInterval)
            {
                gasTankTimer = 0f;

                // Spawn gas tanks in middle lane for easier collection
                Vector2 spawnPos = new Vector2(11f, Random.Range(-0.2f, 0.4f));
                GetGasTankFromPool(spawnPos);
            }
        }
    }

    GameObject GetRandomPrefab()
    {
        float roll = Random.value;

        if (roll <= 0.7f && commonPrefabs.Count > 0)
        {
            return commonPrefabs[Random.Range(0, commonPrefabs.Count)];
        }
        else if (rarePrefabs.Count > 0)
        {
            return rarePrefabs[Random.Range(0, rarePrefabs.Count)];
        }

        // Fallback
        return commonPrefabs.Count > 0 ? commonPrefabs[0] : null;
    }

    public GameObject GetFromPool(Vector2 position)
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.transform.position = position;
            obj.SetActive(true);

            var mover = obj.GetComponent<PooledObjectMover>();
            if (mover != null)
            {
                mover.Initialize(this);

                // Reset movement-related state
                mover.ResetState(); // ← You'll implement this
            }

            return obj;
        }
        else
        {
            GameObject prefabToUse = GetRandomPrefab();
            GameObject obj = Instantiate(prefabToUse, position, Quaternion.identity);
            obj.GetComponent<PooledObjectMover>()?.Initialize(this);
            return obj;
        }
    }

    public GameObject GetObstacleFromPool(Vector2 position)
    {
        if (activeObstaclePrefabs.Count == 0)
        {
            Debug.LogWarning("[ObjectPool] No active obstacle prefabs available!");
            return null;
        }

        if (obstaclePool.Count > 0)
        {
            GameObject obj = obstaclePool.Dequeue();
            
            // Try to avoid spawning the same obstacle type consecutively
            if (obstaclePool.Count > 2 && !string.IsNullOrEmpty(lastSpawnedObstacleType))
            {
                string currentObstacleType = obj.name.Replace("(Clone)", "").Trim();
                
                // If this is the same as the last spawned obstacle, try to find a different one
                if (currentObstacleType == lastSpawnedObstacleType)
                {
                    // Put it back and try up to 5 times to get a different obstacle
                    List<GameObject> tempList = new List<GameObject>();
                    tempList.Add(obj);
                    
                    bool foundDifferent = false;
                    for (int attempt = 0; attempt < 5 && obstaclePool.Count > 0; attempt++)
                    {
                        obj = obstaclePool.Dequeue();
                        currentObstacleType = obj.name.Replace("(Clone)", "").Trim();
                        
                        if (currentObstacleType != lastSpawnedObstacleType)
                        {
                            foundDifferent = true;
                            break;
                        }
                        else
                        {
                            tempList.Add(obj);
                        }
                    }
                    
                    // Put back the ones we didn't use
                    foreach (GameObject tempObj in tempList)
                    {
                        if (tempObj != obj) // Don't put back the one we're using
                        {
                            obstaclePool.Enqueue(tempObj);
                        }
                    }
                }
            }
            
            obj.transform.position = position;
            
            // Track this obstacle type
            lastSpawnedObstacleType = obj.name.Replace("(Clone)", "").Trim();
            
            // Reset state BEFORE activating the object
            // Initialize ObstacleController
            var obstacle = obj.GetComponent<ObstacleController>();
            if (obstacle != null)
            {
                obstacle.Initialize(this);
                obstacle.ResetState();
            }

            // Initialize BouncingTireController
            var bouncingTire = obj.GetComponent<BouncingTireController>();
            if (bouncingTire != null)
            {
                bouncingTire.Initialize(this);
                bouncingTire.ResetState();
            }

            // Initialize SimpleBouncingTire
            var simpleTire = obj.GetComponent<SimpleBouncingTire>();
            if (simpleTire != null)
            {
                simpleTire.Initialize(this);
                simpleTire.ResetState();
            }
            
            // Activate the object AFTER resetting state
            obj.SetActive(true);

            return obj;
        }
        else
        {
            // Create new obstacle if pool is empty - try to avoid duplicates
            GameObject obstaclePrefab;
            
            if (activeObstaclePrefabs.Count > 1 && !string.IsNullOrEmpty(lastSpawnedObstacleType))
            {
                // Try to pick a different obstacle type
                int attempts = 0;
                do
                {
                    obstaclePrefab = activeObstaclePrefabs[Random.Range(0, activeObstaclePrefabs.Count)];
                    attempts++;
                }
                while (obstaclePrefab.name == lastSpawnedObstacleType && attempts < 5);
            }
            else
            {
                obstaclePrefab = activeObstaclePrefabs[Random.Range(0, activeObstaclePrefabs.Count)];
            }
            
            GameObject obj = Instantiate(obstaclePrefab, position, Quaternion.identity);
            
            // Track this obstacle type
            lastSpawnedObstacleType = obstaclePrefab.name;
            
            // Initialize the appropriate controller
            obj.GetComponent<ObstacleController>()?.Initialize(this);
            obj.GetComponent<BouncingTireController>()?.Initialize(this);
            obj.GetComponent<SimpleBouncingTire>()?.Initialize(this);
            
            return obj;
        }
    }

    // Get gas tank from pool
    public GameObject GetGasTankFromPool(Vector2 position)
    {
        if (gasTankPool.Count > 0)
        {
            GameObject obj = gasTankPool.Dequeue();
            obj.transform.position = position;
            
            var gasTank = obj.GetComponent<GasTankPickup>();
            if (gasTank != null)
            {
                gasTank.Initialize(this);
                gasTank.ResetState();
            }
            
            obj.SetActive(true);
            Debug.Log($"[ObjectPool] Gas tank spawned at {position}");
            return obj;
        }
        else
        {
            // Create new gas tank if pool is empty
            GameObject obj = Instantiate(gasTankPrefab, position, Quaternion.identity);
            obj.GetComponent<GasTankPickup>()?.Initialize(this);
            Debug.Log($"[ObjectPool] Gas tank created (pool empty) at {position}");
            return obj;
        }
    }

    public void ReturnToPool(GameObject obj)
    {
        // Check what type of object it is
        if (obj.GetComponent<GasTankPickup>() != null)
        {
            obj.SetActive(false);
            gasTankPool.Enqueue(obj);
        }
        else if (obj.GetComponent<ObstacleController>() != null || 
            obj.GetComponent<BouncingTireController>() != null || 
            obj.GetComponent<SimpleBouncingTire>() != null)
        {
            obj.SetActive(false);
            obstaclePool.Enqueue(obj);
        }
        else
        {
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    // Method to enable/disable turbo boost mode (2x spawn rate)
    public void SetTurboBoostMode(bool active)
    {
        turboBoostActive = active;
        
        if (active)
        {
            spawnInterval = originalSpawnInterval / 2f; // Double spawn rate
            Debug.Log($"[ObjectPool] Turbo boost ON! Food spawn rate: {spawnInterval}s (2x faster)");
        }
        else
        {
            spawnInterval = originalSpawnInterval; // Restore original
            Debug.Log($"[ObjectPool] Turbo boost OFF! Food spawn rate restored to: {spawnInterval}s");
        }
    }

    // Helper method to initialize obstacle pools
    void InitializeObstaclePool(List<GameObject> prefabs, Queue<GameObject> pool, string poolName)
    {
        if (prefabs.Count > 0)
        {
            // Create a balanced distribution of obstacle types
            int objectsPerPrefab = Mathf.Max(2, poolSize / prefabs.Count); // At least 2 of each type
            int objectsCreated = 0;
            
            // Create multiple instances of each prefab for variety
            foreach (GameObject prefab in prefabs)
            {
                for (int j = 0; j < objectsPerPrefab && objectsCreated < poolSize; j++)
                {
                    GameObject obj = Instantiate(prefab, transform);
                    obj.SetActive(false);

                    // Check for ObstacleController first
                    var obstacle = obj.GetComponent<ObstacleController>();
                    if (obstacle != null)
                        obstacle.Initialize(this);
                    
                    // Check for SimpleBouncingTire
                    var simpleBouncingTire = obj.GetComponent<SimpleBouncingTire>();
                    if (simpleBouncingTire != null)
                    {
                        simpleBouncingTire.Initialize(this);
                        Debug.Log($"{poolName} SimpleBouncingTire ({prefab.name}) found and initialized!");
                    }
                    
                    // Check for BouncingTireController (legacy)
                    var bouncingTire = obj.GetComponent<BouncingTireController>();
                    if (bouncingTire != null)
                        bouncingTire.Initialize(this);

                    pool.Enqueue(obj);
                    objectsCreated++;
                }
            }
            
            Debug.Log($"{poolName} obstacle pool initialized with {objectsCreated} objects from {prefabs.Count} prefab types");
        }
    }

}
