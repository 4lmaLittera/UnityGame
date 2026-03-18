using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A classic, simplified Finite State Machine (Baigtinės būsenos mašina) for aggressive enemies.
/// This enemy focuses on direct pursuit and combat without complex stealth or investigation logic.
/// </summary>
public enum EnemyState { Idle, Chasing, Attacking, Dead }

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyFSM : MonoBehaviour, IPoolableEnemy
{
    #region Serialized Fields
    [Header("FSM Settings")]
    [Tooltip("The current state of the Finite State Machine.")]
    [SerializeField] private EnemyState _currentState = EnemyState.Idle;

    [Tooltip("The distance at which this enemy will 'wake up' and start chasing the player.")]
    [SerializeField] private float _detectionRange = 40f;

    [Tooltip("How close the enemy must be to the player to trigger an attack.")]
    [SerializeField] private float _attackRange = 2.5f;

    [Tooltip("Minimum time in seconds between starting new attacks.")]
    [SerializeField] private float _attackCooldown = 1.5f;

    [Header("References")]
    [Tooltip("The target to chase. If empty, will find object tagged 'Player'.")]
    [SerializeField] private Transform _player;

    [Tooltip("The Animator component for attack triggers.")]
    [SerializeField] private Animator _animator;

    [Tooltip("The name of the Trigger parameter in the Animator controller.")]
    [SerializeField] private string _attackTrigger = "attack";
    #endregion

    #region Private Fields
    private NavMeshAgent _navMeshAgent;
    private float _nextAttackTime;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        if (_player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _player = playerObj.transform;
        }
    }

    void Update()
    {
        // SAFETY: Only run logic if alive and NavMesh is valid
        if (_currentState == EnemyState.Dead || _navMeshAgent == null || !_navMeshAgent.isActiveAndEnabled || !_navMeshAgent.isOnNavMesh)
            return;

        UpdateFSM();
    }
    #endregion

    #region FSM Logic
    /// <summary>
    /// Core logic for the Finite State Machine transitions.
    /// </summary>
    private void UpdateFSM()
    {
        if (_player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        switch (_currentState)
        {
            case EnemyState.Idle:
                // Simple transition: if player is within detection range, start chasing
                if (distanceToPlayer <= _detectionRange)
                {
                    _currentState = EnemyState.Chasing;
                }
                break;

            case EnemyState.Chasing:
                // Always pursue the player once detected
                _navMeshAgent.isStopped = false;
                _navMeshAgent.SetDestination(_player.position);

                // Transition to attacking if close enough
                if (distanceToPlayer <= _attackRange)
                {
                    _currentState = EnemyState.Attacking;
                }
                break;

            case EnemyState.Attacking:
                // Stop moving while attacking
                _navMeshAgent.isStopped = true;
                _navMeshAgent.velocity = Vector3.zero;

                // Face the player
                RotateTowardsPlayer();

                // Trigger attack if cooldown is ready
                if (Time.time >= _nextAttackTime)
                {
                    PerformAttack();
                }

                // If player moves away, return to chasing
                if (distanceToPlayer > _attackRange)
                {
                    _currentState = EnemyState.Chasing;
                }
                break;
        }
    }

    private void PerformAttack()
    {
        _nextAttackTime = Time.time + _attackCooldown;
        if (_animator != null) _animator.SetTrigger(_attackTrigger);
    }

    private void RotateTowardsPlayer()
    {
        Vector3 direction = (_player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
    #endregion

    #region IPoolableEnemy Implementation
    public void OnSpawn()
    {
        _currentState = EnemyState.Idle;
        if (_navMeshAgent != null && _navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh)
        {
            _navMeshAgent.isStopped = false;
            _navMeshAgent.Warp(transform.position);
        }
    }

    public void OnDespawn()
    {
        _currentState = EnemyState.Dead;
        if (_navMeshAgent != null && _navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh)
        {
            _navMeshAgent.ResetPath();
        }
    }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        // Detection Range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);

        // Attack Range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);

        if (_currentState == EnemyState.Chasing && _player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up, _player.position + Vector3.up);
        }
    }
    #endregion
}
