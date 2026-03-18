using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// A classic, simplified Finite State Machine (Baigtinės būsenos mašina) for aggressive enemies.
/// This version includes Cornering Speed Control to slow down during sharp turns.
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

    [Header("Cornering Settings")]
    [Tooltip("The maximum speed when moving in a straight line.")]
    [SerializeField] private float _maxMoveSpeed = 8f;

    [Tooltip("The speed multiplier when taking a sharp 90-degree turn.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float _turnSpeedMultiplier = 0.4f;

    [Tooltip("How quickly the speed adjusts to the required cornering speed.")]
    [SerializeField] private float _speedAdaptationRate = 5f;

    [Header("Environmental Settings")]
    [Tooltip("Speed multiplier when walking through Mud (NavMesh area).")]
    [SerializeField] private float _mudSpeedMultiplier = 0.4f;

    [Tooltip("Speed multiplier when walking through Water (NavMesh area).")]
    [SerializeField] private float _waterSpeedMultiplier = 0.6f;

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
    private float _currentSpeedTarget;
    private int _mudAreaMask;
    private int _waterAreaMask;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        if (_navMeshAgent != null)
        {
            _navMeshAgent.speed = _maxMoveSpeed;
            _currentSpeedTarget = _maxMoveSpeed;
        }

        // Initialize Mud mask (Mud is index 5 by default in this project)
        int mudIndex = NavMesh.GetAreaFromName("Mud");
        _mudAreaMask = (mudIndex != -1) ? (1 << mudIndex) : 0;

        // Initialize Water mask
        int waterIndex = NavMesh.GetAreaFromName("Water");
        _waterAreaMask = (waterIndex != -1) ? (1 << waterIndex) : 0;
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

        ApplyCorneringSpeed();
        UpdateFSM();
    }
    #endregion

    #region FSM Logic
    private void UpdateFSM()
    {
        if (_player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        switch (_currentState)
        {
            case EnemyState.Idle:
                if (distanceToPlayer <= _detectionRange)
                {
                    _currentState = EnemyState.Chasing;
                }
                break;

            case EnemyState.Chasing:
                _navMeshAgent.isStopped = false;
                _navMeshAgent.SetDestination(_player.position);

                if (distanceToPlayer <= _attackRange)
                {
                    _currentState = EnemyState.Attacking;
                }
                break;

            case EnemyState.Attacking:
                _navMeshAgent.isStopped = true;
                _navMeshAgent.velocity = Vector3.zero;

                RotateTowardsPlayer();

                if (Time.time >= _nextAttackTime)
                {
                    PerformAttack();
                }

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

    #region Cornering Logic
    /// <summary>
    /// Checks the path ahead and reduces speed if a sharp turn is coming up.
    /// Also slows down if the enemy is walking through mud or water.
    /// </summary>
    private void ApplyCorneringSpeed()
    {
        if (_currentState != EnemyState.Chasing) return;

        float baseSpeed = _maxMoveSpeed;

        // 1. Apply Environmental Slowdown (Mud and Water)
        NavMeshHit hit;
        // Sample the position directly under the agent to see what area it's on
        if (NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
        {
            if ((hit.mask & _mudAreaMask) != 0)
            {
                baseSpeed *= _mudSpeedMultiplier;
            }
            else if ((hit.mask & _waterAreaMask) != 0)
            {
                baseSpeed *= _waterSpeedMultiplier;
            }
        }

        float targetSpeed = baseSpeed;
        // 2. Apply Cornering Slowdown
        // If we have a path with at least 2 points (current and next corner)
        if (_navMeshAgent.hasPath && _navMeshAgent.path.corners.Length > 1)
        {
            // Vector to the immediate steering target
            Vector3 vectorToNextCorner = (_navMeshAgent.steeringTarget - transform.position).normalized;

            // Calculate angle between our current forward and the direction we need to go
            float angle = Vector3.Angle(transform.forward, vectorToNextCorner);

            // If angle is sharp, reduce speed
            // Example: 0 deg = 1.0x, 90 deg = _turnSpeedMultiplier, 180 deg = even slower
            float speedFactor = Mathf.Clamp01(1f - (angle / 120f)); // Normalizes angle to 0-1 range over 120 degrees

            // Blend between slow turn speed and the current base speed
            targetSpeed = Mathf.Lerp(baseSpeed * _turnSpeedMultiplier, baseSpeed, speedFactor);
        }

        // Smoothly interpolate the actual agent speed to avoid jittery movement
        _navMeshAgent.speed = Mathf.MoveTowards(_navMeshAgent.speed, targetSpeed, _speedAdaptationRate * Time.deltaTime);
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
            _navMeshAgent.speed = _maxMoveSpeed;
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);

        if (_navMeshAgent != null && _navMeshAgent.hasPath)
        {
            Gizmos.color = Color.blue;
            var corners = _navMeshAgent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }
    #endregion
}
