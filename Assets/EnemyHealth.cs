using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour, IPoolableEnemy
{
    #region Serialized Fields
    [Header("Health Settings")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _ragdollDuration = 3f;
    #endregion

    #region Private Fields
    private float _currentHealth;
    private IRagdollHandler _ragdollHandler;
    private bool _isDead = false;
    private EnemyPoolManager _poolManager;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _currentHealth = _maxHealth;
        _ragdollHandler = GetComponent<IRagdollHandler>();
    }
    #endregion

    #region Public Methods
    public void SetPoolManager(EnemyPoolManager manager)
    {
        _poolManager = manager;
    }

    public void TakeDamage(float amount, Vector3 impactForce, Vector3 impactPoint, Rigidbody hitBone = null)
    {
        if (_isDead) return;

        _currentHealth -= amount;
        if (_currentHealth <= 0)
        {
            Die(impactForce, impactPoint, hitBone);
        }
    }
    #endregion

    #region Private Methods
    private void Die(Vector3 impactForce, Vector3 impactPoint, Rigidbody hitBone = null)
    {
        _isDead = true;

        if (_ragdollHandler != null)
        {
            _ragdollHandler.TriggerRagdoll(impactForce, impactPoint, hitBone);
        }

        // Start cleanup process
        StartCoroutine(CleanupRoutine());
    }

    private IEnumerator CleanupRoutine()
    {
        // Wait for the ragdoll to be visible and settle
        yield return new WaitForSeconds(_ragdollDuration);

        // Return to pool instead of destroying
        if (_poolManager != null)
        {
            _poolManager.ReturnToPool(this.gameObject);
        }
        else
        {
            // Fallback if not using the pool manager yet
            gameObject.SetActive(false);
        }
    }
    #endregion

    #region IPoolableEnemy Implementation
    public void OnSpawn()
    {
        _isDead = false;
        _currentHealth = _maxHealth;
    }

    public void OnDespawn()
    {
        // Cancel any ongoing cleanup if despawned early
        StopAllCoroutines();
    }
    #endregion
}

