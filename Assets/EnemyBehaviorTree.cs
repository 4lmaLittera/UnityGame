using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AI.BehaviorTree;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyBehaviorTree : MonoBehaviour, IPoolableEnemy
{
    #region Serialized Fields
    [Header("Behavior Settings")]
    [Tooltip("How close the enemy must be to the player to trigger an attack swing.")]
    [SerializeField] private float _attackRange = 3f;

    [Tooltip("The distance at which an enemy will START chasing a seen player.")]
    [SerializeField] private float _chaseRange = 20f;

    [Tooltip("The distance at which an enemy will STOP chasing a player they have already detected.")]
    [SerializeField] private float _visionRange = 35f;

    [Tooltip("The radius of the 360-degree hearing zone. Setting this to 0 disables hearing.")]
    [SerializeField] private float _hearingRange = 25f;

    [Tooltip("Omnidirectional 'sixth sense' distance. If the player is within this range, the enemy always knows they are there, even behind them.")]
    [SerializeField] private float _proximityDetectionRange = 2f;

    [Tooltip("The angle of the vision cone in degrees (e.g., 135). Detection only happens inside this arc unless within proximity range.")]
    [SerializeField] private float _viewAngle = 135f;

    [Tooltip("Layers that block the enemy's line of sight (e.g., Walls, Ground).")]
    [SerializeField] private LayerMask _obstacleMask = (1 << 0) | (1 << 3); // Default and Ground

    [Tooltip("Minimum time in seconds between starting new attacks.")]
    [SerializeField] private float _attackCooldown = 2f;

    [Tooltip("How long the enemy stands still during their attack animation.")]
    [SerializeField] private float _attackDuration = 1.0f;

    [Header("Environmental Settings")]
    [Tooltip("The maximum speed when moving in a straight line on normal ground.")]
    [SerializeField] private float _maxMoveSpeed = 8f;

    [Tooltip("Speed multiplier when walking through Mud (NavMesh area).")]
    [SerializeField] private float _mudSpeedMultiplier = 0.4f;

    [Tooltip("Speed multiplier when walking through Water (NavMesh area).")]
    [SerializeField] private float _waterSpeedMultiplier = 0.6f;

    [Header("References")]
    [Tooltip("The target to chase and attack. If left empty, will find the object tagged 'Player'.")]
    [SerializeField] private Transform _player;

    [Tooltip("The Animator component for triggering attack animations. If left empty, will check children.")]
    [SerializeField] private Animator _animator;

    [Tooltip("The name of the Trigger parameter in the Animator controller used for attacks.")]
    [SerializeField] private string _attackTrigger = "attack";
    #endregion

    #region Private Fields
    private NavMeshAgent _navMeshAgent;
    private Node _rootNode;
    private float _attackEndTime;
    private bool _isDead;
    private bool _isChasing;
    private bool _hasHeardNoise;
    private Vector3 _lastNoisePosition;
    private int _mudAreaMask;
    private int _waterAreaMask;
    private EnemyStateLabel _stateLabel;
    private string _currentStateName;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        if (_navMeshAgent != null)
        {
            _navMeshAgent.speed = _maxMoveSpeed;
        }

        _stateLabel = GetComponent<EnemyStateLabel>();
        if (_stateLabel == null) _stateLabel = gameObject.AddComponent<EnemyStateLabel>();

        // Initialize Mud mask
        int mudIndex = NavMesh.GetAreaFromName("Mud");
        _mudAreaMask = (mudIndex != -1) ? (1 << mudIndex) : 0;

        // Initialize Water mask
        int waterIndex = NavMesh.GetAreaFromName("Water");
        _waterAreaMask = (waterIndex != -1) ? (1 << waterIndex) : 0;

        ConstructBehaviorTree();
    }

    void OnEnable()
    {
        NoiseSystem.OnNoiseEmitted += HandleNoise;
    }

    void OnDisable()
    {
        NoiseSystem.OnNoiseEmitted -= HandleNoise;
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
        // SAFETY: Only evaluate BT if alive and agent is properly on NavMesh
        if (_isDead || _rootNode == null || _navMeshAgent == null || !_navMeshAgent.isActiveAndEnabled || !_navMeshAgent.isOnNavMesh)
            return;

        _currentStateName = "Idle";
        _rootNode.Evaluate();
        if (_stateLabel != null) _stateLabel.SetState(_currentStateName);
    }
    #endregion

    #region Movement Logic
    /// <summary>
    /// Checks if the agent is on a special NavMesh area (like Mud or Water) and adjusts speed.
    /// </summary>
    private void UpdateEnvironmentalSpeed()
    {
        float targetSpeed = _maxMoveSpeed;

        NavMeshHit hit;
        // Sample the position directly under the agent to see what area it's on
        if (NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
        {
            if ((hit.mask & _mudAreaMask) != 0)
            {
                targetSpeed *= _mudSpeedMultiplier;
            }
            else if ((hit.mask & _waterAreaMask) != 0)
            {
                targetSpeed *= _waterSpeedMultiplier;
            }
        }

        // Apply speed directly
        _navMeshAgent.speed = targetSpeed;
    }
    #endregion

    #region Detection Logic
    private bool CanSeePlayer(float range)
    {
        if (_player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        // 1. Proximity
        if (distanceToPlayer <= _proximityDetectionRange) return true;

        // 2. Vision
        if (distanceToPlayer <= range)
        {
            Vector3 directionToPlayer = (_player.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, directionToPlayer) < _viewAngle / 2f)
            {
                Vector3 eyePos = transform.position + Vector3.up * 1.5f;
                if (!Physics.Raycast(eyePos, directionToPlayer, distanceToPlayer, _obstacleMask))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void HandleNoise(Vector3 position, float radius)
    {
        if (_isDead) return;

        float distanceToNoise = Vector3.Distance(transform.position, position);
        
        if (distanceToNoise <= radius || distanceToNoise <= _hearingRange)
        {
            _hasHeardNoise = true;
            _lastNoisePosition = position;
        }
    }
    #endregion

    #region Behavior Tree Construction
    private void ConstructBehaviorTree()
    {
        // Helper to safely access NavMeshAgent
        bool IsAgentValid() => _navMeshAgent != null && _navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh;

        // 0. Stay Still Node
        ConditionNode isMidAttack = new ConditionNode(() => Time.time < _attackEndTime);
        ActionNode freezeAgent = new ActionNode(() =>
        {
            if (IsAgentValid())
            {
                _navMeshAgent.isStopped = true;
                _navMeshAgent.velocity = Vector3.zero;
            }
            _currentStateName = "Striking";
            return NodeState.Running;
        });
        Sequence stayStillSequence = new Sequence(new List<Node> { isMidAttack, freezeAgent });

        // 1. Attack Sequence — Cooldown decorator owns the attack rate-limit
        ConditionNode canAttack = new ConditionNode(() =>
            _player != null &&
            Vector3.Distance(transform.position, _player.position) <= _attackRange &&
            CanSeePlayer(_visionRange));

        ActionNode performAttack = new ActionNode(() =>
        {
            _attackEndTime = Time.time + _attackDuration;
            if (_animator != null) _animator.SetTrigger(_attackTrigger);

            if (IsAgentValid())
            {
                _navMeshAgent.isStopped = true;
                _navMeshAgent.velocity = Vector3.zero;
            }

            _hasHeardNoise = false;
            _currentStateName = "Attacking";
            return NodeState.Success;
        });

        Cooldown attackCooldown = new Cooldown(_attackCooldown, performAttack);
        Sequence triggerAttackSequence = new Sequence(new List<Node> { canAttack, attackCooldown });

        // 2. Chase Parallel — scan sensors AND move at the same time
        //    Scan branch: refreshes environmental speed and vision hysteresis flag
        ActionNode scanEnvironment = new ActionNode(() =>
        {
            UpdateEnvironmentalSpeed();
            _isChasing = _isChasing ? CanSeePlayer(_visionRange) : CanSeePlayer(_chaseRange);
            return _isChasing ? NodeState.Success : NodeState.Failure;
        });

        //    Movement branch: drives the NavMeshAgent to the player's position
        ActionNode chasePlayer = new ActionNode(() =>
        {
            if (!_isChasing) return NodeState.Failure;
            if (IsAgentValid())
            {
                _navMeshAgent.isStopped = false;
                _navMeshAgent.SetDestination(_player.position);
            }
            _hasHeardNoise = false;
            _currentStateName = "Chasing";
            return NodeState.Running;
        });

        Parallel chaseParallel = new Parallel(
            new List<Node> { scanEnvironment, chasePlayer },
            ParallelPolicy.RequireAll,
            ParallelPolicy.RequireOne);

        // 3. Noise Chase Sequence
        ConditionNode hasHeardNoise = new ConditionNode(() => _hasHeardNoise);
        ActionNode moveToNoise = new ActionNode(() =>
        {
            if (!IsAgentValid()) return NodeState.Failure;

            _navMeshAgent.isStopped = false;
            _navMeshAgent.SetDestination(_lastNoisePosition);
            _currentStateName = "Investigating";

            if (!_navMeshAgent.pathPending && _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance + 0.5f)
            {
                _hasHeardNoise = false;
                return NodeState.Success;
            }
            return NodeState.Running;
        });

        Sequence noiseChaseSequence = new Sequence(new List<Node> { hasHeardNoise, moveToNoise });

        // 4. Root Selector
        _rootNode = new Selector(new List<Node> { stayStillSequence, triggerAttackSequence, chaseParallel, noiseChaseSequence });
    }
    #endregion

    #region IPoolableEnemy Implementation
    public void OnSpawn()
    {
        _isDead = false;
        _isChasing = false;
        _attackEndTime = 0f;
        _hasHeardNoise = false;
        if (_navMeshAgent != null)
        {
            // Reset agent if enabled
            if (_navMeshAgent.isActiveAndEnabled && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.isStopped = false;
                _navMeshAgent.Warp(transform.position);
            }
        }
    }

    public void OnDespawn()
    {
        _isDead = true;
        _isChasing = false;
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
        Gizmos.DrawWireSphere(transform.position, _hearingRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _chaseRange);

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, _visionRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _proximityDetectionRange);

        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 leftBoundary = Quaternion.Euler(0, -_viewAngle / 2f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, _viewAngle / 2f, 0) * transform.forward;
        
        Gizmos.color = Color.white;
        Gizmos.DrawRay(eyePos, leftBoundary * _visionRange);
        Gizmos.DrawRay(eyePos, rightBoundary * _visionRange);

        int segments = 10;
        Vector3 prevPoint = eyePos + leftBoundary * _visionRange;
        for (int i = 1; i <= segments; i++)
        {
            float angle = -_viewAngle / 2f + (_viewAngle / segments) * i;
            Vector3 nextPoint = eyePos + Quaternion.Euler(0, angle, 0) * transform.forward * _visionRange;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        if (_hasHeardNoise)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_lastNoisePosition, 1f);
            Gizmos.DrawLine(eyePos, _lastNoisePosition);
        }
    }
    #endregion
}
