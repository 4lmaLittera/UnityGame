using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class EnemyMovement : MonoBehaviour
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
    void Start()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (_player != null && _navMeshAgent != null && _navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh)
        {
            _navMeshAgent.SetDestination(_player.position);
        }
    }
    #endregion
}
