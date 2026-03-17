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

    [Header("Water Avoidance")]
    [Tooltip("Layer(s) that enemies should not stand on, e.g. Water")]
    [SerializeField] private LayerMask _forbiddenSurfaceLayers;
    [SerializeField] private string _forbiddenSurfaceTag = "Water";
    [SerializeField] private float _waterCheckDistance = 3f;
    [SerializeField] private float _dryRecoveryRadius = 12f;
    [SerializeField] private float _playerSurfaceProbeHeight = 2f;
    [SerializeField] private float _playerSurfaceProbeDistance = 8f;
    #endregion

    #region Private Fields
    private NavMeshAgent _navMeshAgent;
    private bool _canUseForbiddenSurfaceTag;
    private bool _hasLastKnownDryPlayerPosition;
    private Vector3 _lastKnownDryPlayerPosition;
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
            int waterArea = NavMesh.GetAreaFromName("Water");
            if (waterArea >= 0)
            {
                _navMeshAgent.areaMask &= ~(1 << waterArea);
            }
        }

        if (_forbiddenSurfaceLayers.value == 0)
        {
            int waterLayer = LayerMask.NameToLayer("Water");
            if (waterLayer >= 0)
            {
                _forbiddenSurfaceLayers = 1 << waterLayer;
            }
        }

        _canUseForbiddenSurfaceTag = IsTagDefined(_forbiddenSurfaceTag);
    }

    void Update()
    {
        if (_player != null && _navMeshAgent != null && _navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh)
        {
            if (IsStandingOnForbiddenSurface())
            {
                TrySetDryEscapeDestination();
                return;
            }

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

            // Prevent stepping into forbidden surfaces (water) at the edge.
            if (WillStepIntoForbiddenSurface())
            {
                _navMeshAgent.ResetPath();
                return;
            }

            SnapToGroundSurface();
        }
    }

    // The NavMeshAgent interpolates height linearly across nav triangles, which diverges
    // from curved or detailed slope geometry. This corrects vertical clipping each frame.
    private void SnapToGroundSurface()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        if (TryGetSurfaceBelow(rayOrigin, 2f, _groundLayer, QueryTriggerInteraction.Ignore, transform, out RaycastHit hit) && !IsForbiddenCollider(hit.collider))
        {
            Vector3 corrected = _navMeshAgent.nextPosition;
            corrected.y = hit.point.y;
            _navMeshAgent.nextPosition = corrected;
        }
    }

    private bool IsStandingOnForbiddenSurface()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 1f;
        if (!TryGetSurfaceBelow(rayOrigin, _waterCheckDistance, ~0, QueryTriggerInteraction.Collide, transform, out RaycastHit hit))
        {
            return false;
        }

        return IsForbiddenCollider(hit.collider);
    }

    private bool TryGetChaseTarget(out Vector3 chaseTarget)
    {
        chaseTarget = default;
        if (_player == null)
        {
            return false;
        }

        if (!IsTransformOverForbiddenSurface(_player, _playerSurfaceProbeHeight, _playerSurfaceProbeDistance))
        {
            if (NavMesh.SamplePosition(_player.position, out NavMeshHit dryPlayerHit, _dryRecoveryRadius, NavMesh.AllAreas)
                && !IsForbiddenSurfacePoint(dryPlayerHit.position))
            {
                _lastKnownDryPlayerPosition = dryPlayerHit.position;
                _hasLastKnownDryPlayerPosition = true;
                chaseTarget = _lastKnownDryPlayerPosition;
                return true;
            }
        }

        if (_hasLastKnownDryPlayerPosition)
        {
            chaseTarget = _lastKnownDryPlayerPosition;
            return true;
        }

        return false;
    }

    private bool IsTransformOverForbiddenSurface(Transform target, float probeHeight, float probeDistance)
    {
        if (target == null)
        {
            return false;
        }

        Vector3 rayOrigin = target.position + Vector3.up * probeHeight;
        if (!TryGetSurfaceBelow(rayOrigin, probeDistance, ~0, QueryTriggerInteraction.Collide, target, out RaycastHit hit))
        {
            return false;
        }

        return IsForbiddenCollider(hit.collider);
    }

    private bool IsForbiddenCollider(Collider col)
    {
        if (col == null)
        {
            return false;
        }

        bool blockedByLayer = (_forbiddenSurfaceLayers.value & (1 << col.gameObject.layer)) != 0;
        bool blockedByTag = _canUseForbiddenSurfaceTag && col.CompareTag(_forbiddenSurfaceTag);

        return blockedByLayer || blockedByTag;
    }

    private bool WillStepIntoForbiddenSurface()
    {
        Vector3 desiredVelocity = _navMeshAgent.desiredVelocity;
        if (desiredVelocity.sqrMagnitude < 0.001f)
        {
            return false;
        }

        float lookAhead = Mathf.Max(_navMeshAgent.radius * 1.2f, 0.7f);
        Vector3 forwardCheck = transform.position + desiredVelocity.normalized * lookAhead;
        return IsForbiddenSurfacePoint(forwardCheck);
    }

    private void TrySetDryEscapeDestination()
    {
        Vector3 center = transform.position;
        const int attempts = 12;

        for (int i = 0; i < attempts; i++)
        {
            Vector2 offset2D = Random.insideUnitCircle * _dryRecoveryRadius;
            Vector3 candidate = center + new Vector3(offset2D.x, 0f, offset2D.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit navHit, _dryRecoveryRadius, NavMesh.AllAreas) && !IsForbiddenSurfacePoint(navHit.position))
            {
                _navMeshAgent.SetDestination(navHit.position);
                return;
            }
        }

        // If no dry point is found, stop movement to prevent jitter.
        _navMeshAgent.ResetPath();
    }

    private bool IsForbiddenSurfacePoint(Vector3 point)
    {
        Vector3 probeStart = point + Vector3.up * 2f;
        if (!TryGetSurfaceBelow(probeStart, 6f, ~0, QueryTriggerInteraction.Collide, null, out RaycastHit hit))
        {
            return false;
        }

        return IsForbiddenCollider(hit.collider);
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
            if (col == null)
            {
                continue;
            }

            if (ignoredRoot != null && col.transform.IsChildOf(ignoredRoot))
            {
                continue;
            }

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
            _hasLastKnownDryPlayerPosition = false;

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

        _hasLastKnownDryPlayerPosition = false;
    }

    private void ApplyRandomAvoidancePriority()
    {
        if (_navMeshAgent == null)
        {
            return;
        }

        int minPriority = Mathf.Clamp(Mathf.Min(_avoidancePriorityRange.x, _avoidancePriorityRange.y), 0, 99);
        int maxPriority = Mathf.Clamp(Mathf.Max(_avoidancePriorityRange.x, _avoidancePriorityRange.y), 0, 99);

        _navMeshAgent.avoidancePriority = Random.Range(minPriority, maxPriority + 1);
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
    #endregion
}
