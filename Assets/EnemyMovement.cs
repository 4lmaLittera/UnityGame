using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class EnemyMovement : MonoBehaviour, IPoolableEnemy
{
    #region Serialized Fields
    [Header("References")]
    [FormerlySerializedAs("player")]
    [SerializeField] private Transform _player;

    [Header("Navigation")]
    [SerializeField] private Vector2Int _avoidancePriorityRange = new Vector2Int(20, 80);

    [Tooltip("Layer(s) that count as ground for slope snapping. Must match your terrain/floor layer.")]
    [SerializeField] private LayerMask _groundLayer = ~0;

    [Header("Water Interaction")]
    [Tooltip("Layer(s) that count as water for slowing down.")]
    [SerializeField] private LayerMask _waterLayers;
    [SerializeField] private string _waterTag = "Water";
    [SerializeField] private float _waterSpeedMultiplier = 0.4f;
    [SerializeField] private float _waterCheckDistance = 3f;
    [SerializeField] private float _navMeshSampleRadius = 12f;
    #endregion

    #region Private Fields
    private NavMeshAgent _navMeshAgent;
    private float _baseSpeed;
    private bool _canUseWaterTag;
    private int _waterAreaIndex = -1;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        ApplyRandomAvoidancePriority();
    }

    void Start()
    {
        // Try to find player if not assigned
        if (_player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _player = playerObj.transform;
        }

        if (_navMeshAgent != null)
        {
            _baseSpeed = _navMeshAgent.speed;
            
            _waterAreaIndex = NavMesh.GetAreaFromName("Water");
            if (_waterAreaIndex >= 0)
            {
                // Ensure we CAN walk on the water area
                _navMeshAgent.areaMask |= (1 << _waterAreaIndex);
            }
        }

        if (_waterLayers.value == 0)
        {
            int waterLayer = LayerMask.NameToLayer("Water");
            if (waterLayer >= 0)
            {
                _waterLayers = 1 << waterLayer;
            }
        }

        _canUseWaterTag = IsTagDefined(_waterTag);
    }

    void Update()
    {
        if (_player != null && _navMeshAgent != null && _navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh)
        {
            // Apply speed reduction in water
            bool inWater = IsStandingInWater();
            _navMeshAgent.speed = inWater ? _baseSpeed * _waterSpeedMultiplier : _baseSpeed;

            Vector3 chaseTarget;
            if (TryGetChaseTarget(out chaseTarget))
            {
                _navMeshAgent.SetDestination(chaseTarget);
            }
            else
            {
                _navMeshAgent.ResetPath();
            }

            // If the path is only partial (e.g. player is across a cliff the agent can't reach),
            // stop at the closest reachable point instead of jittering at the mesh boundary.
            if (_navMeshAgent.pathStatus == NavMeshPathStatus.PathPartial && !_navMeshAgent.pathPending)
            {
                _navMeshAgent.ResetPath();
            }

            SnapToGroundSurface();
        }
    }

    /// <summary>
    /// Corrects vertical clipping by snapping the agent to the actual ground geometry.
    /// </summary>
    private void SnapToGroundSurface()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        if (TryGetSurfaceBelow(rayOrigin, 2f, _groundLayer, QueryTriggerInteraction.Ignore, transform, out RaycastHit hit) && !IsWaterCollider(hit.collider))
        {
            Vector3 corrected = _navMeshAgent.nextPosition;
            corrected.y = hit.point.y;
            _navMeshAgent.nextPosition = corrected;
        }
    }

    private bool IsStandingInWater()
    {
        // Primary method: Check NavMesh Area (Supports NavMeshModifier)
        if (_navMeshAgent != null && _navMeshAgent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(_navMeshAgent.transform.position, out NavMeshHit navHit, 0.5f, NavMesh.AllAreas))
            {
                if (_waterAreaIndex != -1 && (navHit.mask & (1 << _waterAreaIndex)) != 0)
                {
                    return true;
                }
            }
        }

        // Secondary fallback: Physics Raycast (for triggers or non-baked water)
        Vector3 rayOrigin = transform.position + Vector3.up * 1f;
        if (TryGetSurfaceBelow(rayOrigin, _waterCheckDistance, ~0, QueryTriggerInteraction.Collide, transform, out RaycastHit hit))
        {
            return IsWaterCollider(hit.collider);
        }

        return false;
    }

    private bool TryGetChaseTarget(out Vector3 chaseTarget)
    {
        chaseTarget = default;
        if (_player == null)
        {
            return false;
        }

        // Always sample the closest valid point to the player on the NavMesh.
        if (NavMesh.SamplePosition(_player.position, out NavMeshHit hit, _navMeshSampleRadius, NavMesh.AllAreas))
        {
            chaseTarget = hit.position;
            return true;
        }

        return false;
    }

    private bool IsWaterCollider(Collider col)
    {
        if (col == null) return false;

        bool inWaterLayer = (_waterLayers.value & (1 << col.gameObject.layer)) != 0;
        bool hasWaterTag = _canUseWaterTag && col.CompareTag(_waterTag);

        return inWaterLayer || hasWaterTag;
    }

    private bool TryGetSurfaceBelow(Vector3 origin, float maxDistance, LayerMask layers, QueryTriggerInteraction triggerInteraction, Transform ignoredRoot, out RaycastHit selectedHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, maxDistance, layers, triggerInteraction);
        if (hits == null || hits.Length == 0)
        {
            selectedHit = default;
            return false;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null) continue;

            if (ignoredRoot != null && col.transform.IsChildOf(ignoredRoot)) continue;

            selectedHit = hits[i];
            return true;
        }

        selectedHit = default;
        return false;
    }
    #endregion

    #region IPoolableEnemy Implementation
    public void OnSpawn()
    {
        if (_navMeshAgent != null)
        {
            ApplyRandomAvoidancePriority();

            // Warp is essential to prevent the agent from trying to walk from its dead body location to the new spawn point
            _navMeshAgent.Warp(transform.position);
            
            // Only reset path if the agent is fully initialized and active on the mesh
            if (_navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.ResetPath();
            }
        }
    }

    public void OnDespawn()
    {
        if (_navMeshAgent != null && _navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh)
        {
            _navMeshAgent.ResetPath();
        }
    }

    private void ApplyRandomAvoidancePriority()
    {
        if (_navMeshAgent == null) return;

        int minPriority = Mathf.Clamp(Mathf.Min(_avoidancePriorityRange.x, _avoidancePriorityRange.y), 0, 99);
        int maxPriority = Mathf.Clamp(Mathf.Max(_avoidancePriorityRange.x, _avoidancePriorityRange.y), 0, 99);

        _navMeshAgent.avoidancePriority = Random.Range(minPriority, maxPriority + 1);
    }

    private bool IsTagDefined(string tagName)
    {
        if (string.IsNullOrEmpty(tagName)) return false;

        try
        {
            // This is a common way to check if a tag exists in Unity
            GameObject.FindWithTag(tagName);
            return true;
        }
        catch (UnityException)
        {
            return false;
        }
    }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _waterCheckDistance);
    }
    #endregion
}
