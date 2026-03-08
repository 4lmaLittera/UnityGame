using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class EnemyMovement : MonoBehaviour, IPoolableEnemy
{
    #region Serialized Fields
    [Header("References")]
    [FormerlySerializedAs("player")]
    [SerializeField] private Transform _player;
    #endregion

    #region Private Fields
    private NavMeshAgent _navMeshAgent;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        // Try to find player if not assigned
        if (_player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _player = playerObj.transform;
        }
    }

    void Update()
    {
        if (_player != null && _navMeshAgent != null && _navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh)
        {
            _navMeshAgent.SetDestination(_player.position);
        }
    }
    #endregion

    #region IPoolableEnemy Implementation
    public void OnSpawn()
    {
        if (_navMeshAgent != null)
        {
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
    #endregion
}
