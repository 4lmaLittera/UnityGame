using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    #region Serialized Fields
    [Header("Health Settings")]
    [SerializeField] private float _maxHealth = 100f;
    #endregion

    #region Private Fields
    private float _currentHealth;
    private IRagdollHandler _ragdollHandler;
    private bool _isDead = false;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _currentHealth = _maxHealth;
        _ragdollHandler = GetComponent<IRagdollHandler>();
    }
    #endregion

    #region Public Methods
    public void TakeDamage(float amount, Vector3 impactForce, Vector3 impactPoint, Rigidbody hitBone = null)
    {
        if (_isDead) return;

        _currentHealth -= amount;
        if (_currentHealth <= 0)
        {
            Die(impactForce, impactPoint, hitBone);
        }
    }

    private void Die(Vector3 impactForce, Vector3 impactPoint, Rigidbody hitBone = null)
    {
        _isDead = true;

        if (_ragdollHandler != null)
        {
            _ragdollHandler.TriggerRagdoll(impactForce, impactPoint, hitBone);
        }
    }
    #endregion
}

