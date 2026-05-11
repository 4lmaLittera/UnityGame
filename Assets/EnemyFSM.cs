using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Baigtinės būsenos mašina (BBM) — Warrok priešui. Sensorių duomenys (regos jutiklis +
/// atstumo jutiklis) perjungia būsenas, o judėjimas vykdomas per NavMesh.
/// </summary>
public enum EnemyState { Patrolling, Chasing, Attacking, Searching, Dead }

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyFSM : MonoBehaviour, IPoolableEnemy
{
    #region Serialized Fields
    [Header("FSM Settings")]
    [Tooltip("The current state of the Finite State Machine.")]
    [SerializeField] private EnemyState _currentState = EnemyState.Patrolling;

    [Tooltip("Maximum distance the vision sensor can see the player.")]
    [SerializeField] private float _detectionRange = 40f;

    [Tooltip("How close the enemy must be to the player to trigger an attack.")]
    [SerializeField] private float _attackRange = 2.5f;

    [Tooltip("Minimum time in seconds between starting new attacks.")]
    [SerializeField] private float _attackCooldown = 1.5f;

    [Header("Vision Sensor")]
    [Tooltip("Field-of-view arc in degrees. Outside this arc the player is invisible.")]
    [Range(30f, 360f)]
    [SerializeField] private float _viewAngle = 140f;

    [Tooltip("Layers that block line of sight (walls, ground).")]
    [SerializeField] private LayerMask _obstacleMask = (1 << 0) | (1 << 3);

    [Tooltip("If the player is closer than this, they are detected even behind the enemy.")]
    [SerializeField] private float _proximityDetectionRange = 2.5f;

    [Header("Patrol")]
    [Tooltip("Random-patrol radius around the spawn position.")]
    [SerializeField] private float _patrolRadius = 15f;

    [Tooltip("How long the enemy idles at each patrol waypoint before picking the next one.")]
    [SerializeField] private float _patrolWaitTime = 2f;

    [Header("Search")]
    [Tooltip("How long (seconds) the enemy searches the last known player position before returning to patrol.")]
    [SerializeField] private float _searchDuration = 5f;

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
    private EnemyStateLabel _stateLabel;
    private float _nextAttackTime;
    private int _mudAreaMask;
    private int _waterAreaMask;

    private Vector3 _spawnAnchor;
    private Vector3 _patrolTarget;
    private float _patrolWaitEndTime;

    private Vector3 _lastKnownPlayerPos;
    private float _searchEndTime;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        _stateLabel = GetComponent<EnemyStateLabel>();
        if (_stateLabel == null) _stateLabel = gameObject.AddComponent<EnemyStateLabel>();

        if (_navMeshAgent != null)
        {
            _navMeshAgent.speed = _maxMoveSpeed;
        }

        int mudIndex = NavMesh.GetAreaFromName("Mud");
        _mudAreaMask = (mudIndex != -1) ? (1 << mudIndex) : 0;

        int waterIndex = NavMesh.GetAreaFromName("Water");
        _waterAreaMask = (waterIndex != -1) ? (1 << waterIndex) : 0;

        _spawnAnchor = transform.position;
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
        if (_currentState == EnemyState.Dead || _navMeshAgent == null
            || !_navMeshAgent.isActiveAndEnabled || !_navMeshAgent.isOnNavMesh)
            return;

        ApplyCorneringSpeed();
        UpdateFSM();
        PublishStateLabel();
    }
    #endregion

    #region Vision Sensor
    /// <summary>
    /// The regos jutiklis — cone-of-vision + line-of-sight raycast, plus omnidirectional
    /// proximity fallback so the player can't stand on top of the enemy unnoticed.
    /// </summary>
    private bool CanSeePlayer()
    {
        if (_player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        if (distanceToPlayer <= _proximityDetectionRange) return true;
        if (distanceToPlayer > _detectionRange) return false;

        Vector3 directionToPlayer = (_player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, directionToPlayer) > _viewAngle * 0.5f) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.6f;
        return !Physics.Raycast(eyePos, directionToPlayer, distanceToPlayer, _obstacleMask);
    }
    #endregion

    #region FSM Logic
    private void UpdateFSM()
    {
        switch (_currentState)
        {
            case EnemyState.Patrolling: TickPatrolling(); break;
            case EnemyState.Chasing:    TickChasing();    break;
            case EnemyState.Attacking:  TickAttacking();  break;
            case EnemyState.Searching:  TickSearching();  break;
        }
    }

    private void TickPatrolling()
    {
        // Sensor check — if we see the player, transition to Chasing.
        if (CanSeePlayer())
        {
            _lastKnownPlayerPos = _player.position;
            TransitionTo(EnemyState.Chasing);
            return;
        }

        // A path is being computed this frame — don't mistake it for "no path".
        if (_navMeshAgent.pathPending) return;

        bool walking = _navMeshAgent.hasPath
                       && _navMeshAgent.remainingDistance > _navMeshAgent.stoppingDistance + 0.2f;

        if (walking)
        {
            _navMeshAgent.isStopped = false;
            return;
        }

        // Arrived (or never had a destination). Idle until the wait window elapses,
        // then pick the next patrol point. Do NOT latch isStopped — the agent is
        // already at rest, and latching it would freeze us if the next re-pick
        // happens to overlap a frame where pathPending flips.
        if (Time.time < _patrolWaitEndTime) return;

        if (TryPickPatrolPoint(out Vector3 point))
        {
            _patrolTarget = point;
            _navMeshAgent.isStopped = false;
            _navMeshAgent.SetDestination(_patrolTarget);
            _patrolWaitEndTime = Time.time + _patrolWaitTime;
        }
    }

    private void TickChasing()
    {
        if (_player == null) { TransitionTo(EnemyState.Patrolling); return; }

        _navMeshAgent.isStopped = false;

        if (CanSeePlayer())
        {
            _lastKnownPlayerPos = _player.position;
            _navMeshAgent.SetDestination(_player.position);

            if (Vector3.Distance(transform.position, _player.position) <= _attackRange)
            {
                TransitionTo(EnemyState.Attacking);
            }
            return;
        }

        // Lost line of sight — fall back to searching the last known position.
        _searchEndTime = Time.time + _searchDuration;
        _navMeshAgent.SetDestination(_lastKnownPlayerPos);
        TransitionTo(EnemyState.Searching);
    }

    private void TickAttacking()
    {
        _navMeshAgent.isStopped = true;
        _navMeshAgent.velocity = Vector3.zero;
        RotateTowardsPlayer();

        if (Time.time >= _nextAttackTime)
        {
            PerformAttack();
        }

        if (_player == null
            || Vector3.Distance(transform.position, _player.position) > _attackRange)
        {
            TransitionTo(EnemyState.Chasing);
        }
    }

    private void TickSearching()
    {
        // Regained sight? Jump straight back to chasing.
        if (CanSeePlayer())
        {
            _lastKnownPlayerPos = _player.position;
            TransitionTo(EnemyState.Chasing);
            return;
        }

        _navMeshAgent.isStopped = false;

        bool reached = !_navMeshAgent.pathPending
                       && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance + 0.5f;

        if (reached || Time.time >= _searchEndTime)
        {
            TransitionTo(EnemyState.Patrolling);
        }
    }

    private void TransitionTo(EnemyState next)
    {
        _currentState = next;
        _patrolWaitEndTime = 0f;
    }

    private bool TryPickPatrolPoint(out Vector3 point)
    {
        Vector2 rand = Random.insideUnitCircle * _patrolRadius;
        Vector3 candidate = _spawnAnchor + new Vector3(rand.x, 0f, rand.y);
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, _patrolRadius, NavMesh.AllAreas))
        {
            point = hit.position;
            return true;
        }
        point = Vector3.zero;
        return false;
    }

    private void PerformAttack()
    {
        _nextAttackTime = Time.time + _attackCooldown;
        if (_animator != null) _animator.SetTrigger(_attackTrigger);
    }

    private void RotateTowardsPlayer()
    {
        if (_player == null) return;
        Vector3 direction = (_player.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
    #endregion

    #region Debug Label
    private void PublishStateLabel()
    {
        if (_stateLabel == null) return;
        _stateLabel.SetState(_currentState switch
        {
            EnemyState.Patrolling => "Patrolling",
            EnemyState.Chasing    => "Chasing",
            EnemyState.Attacking  => "Attack!",
            EnemyState.Searching  => "Searching",
            _                     => "Dead"
        });
    }
    #endregion

    #region Cornering Logic
    /// <summary>
    /// Reduces speed on sharp corners and on Mud/Water NavMesh areas.
    /// </summary>
    private void ApplyCorneringSpeed()
    {
        float baseSpeed = _maxMoveSpeed;

        NavMeshHit hit;
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
        if (_navMeshAgent.hasPath && _navMeshAgent.path.corners.Length > 1)
        {
            Vector3 vectorToNextCorner = (_navMeshAgent.steeringTarget - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, vectorToNextCorner);
            float speedFactor = Mathf.Clamp01(1f - (angle / 120f));
            targetSpeed = Mathf.Lerp(baseSpeed * _turnSpeedMultiplier, baseSpeed, speedFactor);
        }

        _navMeshAgent.speed = Mathf.MoveTowards(_navMeshAgent.speed, targetSpeed, _speedAdaptationRate * Time.deltaTime);
    }
    #endregion

    #region IPoolableEnemy Implementation
    public void OnSpawn()
    {
        _currentState = EnemyState.Patrolling;
        _spawnAnchor = transform.position;
        _patrolWaitEndTime = 0f;

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

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _proximityDetectionRange);

        // Vision cone
        Vector3 eyePos = transform.position + Vector3.up * 1.6f;
        Vector3 leftBoundary  = Quaternion.Euler(0, -_viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0,  _viewAngle * 0.5f, 0) * transform.forward;
        Gizmos.color = Color.white;
        Gizmos.DrawRay(eyePos, leftBoundary  * _detectionRange);
        Gizmos.DrawRay(eyePos, rightBoundary * _detectionRange);

        // Patrol radius around spawn anchor
        Gizmos.color = Color.green;
        Vector3 anchor = Application.isPlaying ? _spawnAnchor : transform.position;
        Gizmos.DrawWireSphere(anchor, _patrolRadius);

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
