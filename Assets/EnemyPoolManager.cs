using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[System.Serializable]
public class EnemyPoolConfig
{
    [Tooltip("The enemy prefab to spawn")]
    public GameObject EnemyPrefab;
    [Tooltip("How many of this enemy type to keep in the pool")]
    public int PoolSize = 50;
    [Tooltip("Higher weight means this enemy is more likely to spawn")]
    public float SpawnWeight = 1f;
}

public class EnemyPoolManager : MonoBehaviour
{
    #region Serialized Fields
    [Header("Pool Settings")]
    [SerializeField] private EnemyPoolConfig[] _enemyPools;

    [Header("Spawn Settings")]
    [SerializeField] private Transform _player;
    [SerializeField] private float _spawnRadius = 25f;
    [SerializeField] private float _spawnInterval = 1f;
    
    [Header("NavMesh Settings")]
    [Tooltip("How far off the radial point we search for a valid NavMesh point")]
    [SerializeField] private float _navMeshSearchDistance = 5f;

    [Header("Spawn Surface Filters")]
    [Tooltip("Layer(s) that enemies should never spawn on, e.g. Water")]
    [SerializeField] private LayerMask _forbiddenSpawnLayers;
    [Tooltip("Tag that enemies should never spawn on")]
    [SerializeField] private string _forbiddenSpawnTag = "Water";
    [SerializeField] private float _surfaceProbeHeight = 2f;
    [SerializeField] private float _surfaceProbeDistance = 8f;
    #endregion

    #region Private Fields
    // Dictionary linking a prefab to its specific pool list
    private Dictionary<GameObject, List<GameObject>> _pools = new();
    private float _totalSpawnWeight;
    private float _nextSpawnTime;
    private bool _canUseForbiddenTag;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        if (_player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _player = playerObj.transform;
            else Debug.LogError("EnemyPoolManager: No Player found!");
        }

        // Auto-include a "Water" layer when available and no mask was configured.
        if (_forbiddenSpawnLayers.value == 0)
        {
            int waterLayer = LayerMask.NameToLayer("Water");
            if (waterLayer >= 0)
            {
                _forbiddenSpawnLayers = 1 << waterLayer;
            }
        }

        _canUseForbiddenTag = IsTagDefined(_forbiddenSpawnTag);
        if (!_canUseForbiddenTag && !string.IsNullOrEmpty(_forbiddenSpawnTag))
        {
            Debug.LogWarning($"EnemyPoolManager: Forbidden spawn tag '{_forbiddenSpawnTag}' is not defined. Tag filtering is disabled.");
        }

        InitializePool();
    }

    void Update()
    {
        if (Time.time >= _nextSpawnTime)
        {
            SpawnEnemy();
            _nextSpawnTime = Time.time + _spawnInterval;
        }
    }
    #endregion

    #region Pool Logic
    private void InitializePool()
    {
        if (_enemyPools == null || _enemyPools.Length == 0)
        {
            Debug.LogWarning("EnemyPoolManager: No enemy pools configured!");
            return;
        }

        foreach (var config in _enemyPools)
        {
            if (config.EnemyPrefab == null) continue;

            var list = new List<GameObject>();
            for (int i = 0; i < config.PoolSize; i++)
            {
                GameObject enemy = Instantiate(config.EnemyPrefab, transform);
                enemy.SetActive(false);
                
                // Link the health script to this manager for callbacks
                var health = enemy.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    health.SetPoolManager(this);
                }

                list.Add(enemy);
            }
            
            _pools.Add(config.EnemyPrefab, list);
            _totalSpawnWeight += config.SpawnWeight;
        }
    }

    private GameObject GetInactiveEnemy()
    {
        if (_pools.Count == 0) return null;

        // 1. Choose a random enemy type based on weight
        float randomVal = Random.Range(0f, _totalSpawnWeight);
        float currentWeight = 0f;
        GameObject selectedPrefab = null;
        
        foreach (var config in _enemyPools)
        {
            if (config.EnemyPrefab == null) continue;

            currentWeight += config.SpawnWeight;
            if (randomVal <= currentWeight)
            {
                selectedPrefab = config.EnemyPrefab;
                break;
            }
        }

        // 2. Try to find an inactive enemy in the chosen pool
        if (selectedPrefab != null && _pools.TryGetValue(selectedPrefab, out var selectedList))
        {
            foreach (var enemy in selectedList)
            {
                if (!enemy.activeInHierarchy) return enemy;
            }
        }
        
        // 3. Fallback: If the chosen pool is completely full (all active), 
        // just find ANY inactive enemy from other pools to keep the horde pressure up
        foreach (var list in _pools.Values)
        {
            foreach (var enemy in list)
            {
                if (!enemy.activeInHierarchy) return enemy;
            }
        }
        
        return null; // All pools are completely full
    }

    public void ReturnToPool(GameObject enemy)
    {
        // Inform all components they are despawning
        var poolables = enemy.GetComponents<IPoolableEnemy>();
        foreach (var p in poolables)
        {
            p.OnDespawn();
        }

        enemy.SetActive(false);
    }
    #endregion

    #region Spawning Logic
    private void SpawnEnemy()
    {
        if (_player == null) return;

        GameObject enemy = GetInactiveEnemy();
        if (enemy == null) return; // Reached max active enemies across all pools

        // 1. Calculate a random angle
        float angle = Random.Range(0f, Mathf.PI * 2f);
        
        // 2. Convert angle to a position on a circle around the player
        Vector3 spawnDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        Vector3 targetSpawnPoint = _player.position + (spawnDirection * _spawnRadius);

        // 3. Ensure the point is on the NavMesh so the agent doesn't break
        if (NavMesh.SamplePosition(targetSpawnPoint, out NavMeshHit hit, _navMeshSearchDistance, NavMesh.AllAreas))
        {
            if (IsForbiddenSpawnSurface(hit.position))
            {
                return;
            }

            // 4. Verify the 3D volume at that point is clear of geometry before placing.
            //    NavMesh.SamplePosition only checks the surface, not the space above it.
            NavMeshAgent agentComp = enemy.GetComponent<NavMeshAgent>();
            float agentRadius = agentComp != null ? agentComp.radius : 0.5f;
            float agentHeight = agentComp != null ? agentComp.height : 2f;
            
            // Lift the bottom up slightly to avoid false positives with the floor it's standing on
            Vector3 capsuleBottom = hit.position + Vector3.up * (agentRadius + 0.1f);
            Vector3 capsuleTop    = hit.position + Vector3.up * (agentHeight - agentRadius);

            // We only want to check against actual obstacles, not Triggers or Water.
            // DefaultRaycastLayers includes everything except IgnoreRaycast layer.
            // We then subtract any layers marked as forbidden (like Water).
            int checkMask = Physics.DefaultRaycastLayers & ~_forbiddenSpawnLayers.value;

            if (Physics.CheckCapsule(capsuleBottom, capsuleTop, agentRadius * 0.9f, checkMask, QueryTriggerInteraction.Ignore))
            {
                // Spawn point is blocked by geometry — skip and retry next interval.
                return;
            }

            // Move it exactly to the valid NavMesh point
            enemy.transform.position = hit.position;
            
            // Activate first, so Awake/OnEnable run before OnSpawn
            enemy.SetActive(true);

            // Inform all components they are spawning
            var poolables = enemy.GetComponents<IPoolableEnemy>();
            foreach (var p in poolables)
            {
                p.OnSpawn();
            }
        }
        else
        {
            // The calculated radial point was too far from a walkable surface (e.g. inside a mountain)
            // We skip this spawn frame to keep performance high, it will try again next interval.
        }
    }

    private bool IsForbiddenSpawnSurface(Vector3 navMeshPoint)
    {
        Vector3 probeStart = navMeshPoint + Vector3.up * _surfaceProbeHeight;
        if (!Physics.Raycast(probeStart, Vector3.down, out RaycastHit hitInfo, _surfaceProbeDistance, ~0, QueryTriggerInteraction.Collide))
        {
            return false;
        }

        bool blockedByLayer = (_forbiddenSpawnLayers.value & (1 << hitInfo.collider.gameObject.layer)) != 0;
        bool blockedByTag = _canUseForbiddenTag && hitInfo.collider.CompareTag(_forbiddenSpawnTag);

        return blockedByLayer || blockedByTag;
    }

    private bool IsTagDefined(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
        {
            return false;
        }

        try
        {
            GameObject.FindWithTag(tagName);
            return true;
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_player != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawWireSphere(_player.position, _spawnRadius);
        }
    }
    #endregion
}
