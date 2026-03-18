using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyCombat : MonoBehaviour
{
    #region Serialized Fields
    [Header("Attack Settings")]
    [Tooltip("How close the enemy must be to the player to trigger an attack.")]
    [SerializeField] private float _attackRange = 2f;
    [Tooltip("Time in seconds between attacks.")]
    [SerializeField] private float _attackCooldown = 1.5f;
    [Tooltip("Name of the trigger parameter in the Animator.")]
    [SerializeField] private string _attackTriggerName = "attack";
    [Tooltip("How long the enemy should stop moving while playing the attack animation.")]
    [SerializeField] private float _attackDuration = 1.0f;

    [Header("References")]
    [Tooltip("Assign the Animator. If null, will try to find one in children.")]
    [SerializeField] private Animator _animator;
    [Tooltip("Assign the NavMeshAgent. If null, will try to find one on this GameObject.")]
    [SerializeField] private NavMeshAgent _navMeshAgent;
    [Tooltip("Assign the EnemyMovement script. If null, will try to find one on this GameObject.")]
    [SerializeField] private EnemyMovement _enemyMovement;
    [Tooltip("Assign the Player transform. If null, will find by 'Player' tag.")]
    [SerializeField] private Transform _player;
    #endregion

    #region Private Fields
    private float _nextAttackTime;
    private Coroutine _attackRoutine;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeReferences();
    }

    private void OnEnable()
    {
        InitializeReferences();
    }

    private void InitializeReferences()
    {
        if (_player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _player = playerObj.transform;
        }

        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_navMeshAgent == null) _navMeshAgent = GetComponent<NavMeshAgent>();
        if (_enemyMovement == null) _enemyMovement = GetComponent<EnemyMovement>();
    }

    private void Update()
    {
        // Fallback: If player is still null (e.g. spawned before player), try finding again
        if (_player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _player = playerObj.transform;
            return;
        }

        if (_navMeshAgent == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        // Face the player while in range, even during cooldown
        if (distanceToPlayer <= _attackRange * 1.5f)
        {
            RotateTowardsPlayer();
        }

        // Trigger attack if in range and cooldown has passed
        if (distanceToPlayer <= _attackRange && Time.time >= _nextAttackTime)
        {
            PerformAttack();
        }
    }
    #endregion

    #region Combat Logic
    private void PerformAttack()
    {
        _nextAttackTime = Time.time + _attackCooldown;
        
        if (_animator != null)
        {
            _animator.SetTrigger(_attackTriggerName);
        }

        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
        }
        _attackRoutine = StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        // Stop movement
        if (_enemyMovement != null)
        {
            _enemyMovement.CanMove = false;
        }

        // Wait for the duration of the attack swing
        yield return new WaitForSeconds(_attackDuration);

        // Resume movement
        if (_enemyMovement != null)
        {
            _enemyMovement.CanMove = true;
        }
    }

    private void RotateTowardsPlayer()
    {
        // Only rotate if we're not moving much (or stopped for attack) to avoid snapping while running
        if (_enemyMovement != null && _enemyMovement.CanMove && _navMeshAgent.velocity.sqrMagnitude > 0.1f)
            return;

        Vector3 direction = (_player.position - transform.position).normalized;
        direction.y = 0; // Keep horizontal

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
    #endregion

    #region Public Methods (For Animation Events)
    /// <summary>
    /// Can be called via an Animation Event at the end of the attack animation 
    /// instead of relying on the _attackDuration timer.
    /// </summary>
    public void AnimationEvent_FinishAttack()
    {
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            _attackRoutine = null;
        }

        if (_enemyMovement != null)
        {
            _enemyMovement.CanMove = true;
        }
    }
    #endregion
}