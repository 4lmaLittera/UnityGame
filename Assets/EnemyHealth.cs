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

    #region Collision Handling
    private void OnCollisionEnter(Collision collision)
    {
        // Direct hits on the root
        if (collision.gameObject.GetComponent<DaggerProjectile>() != null)
        {
            Rigidbody daggerRb = collision.gameObject.GetComponent<Rigidbody>();
            Vector3 force = daggerRb != null ? daggerRb.linearVelocity * 2f : Vector3.zero;

            TakeDamage(_maxHealth, force, collision.contacts[0].point);
        }
    }
    #endregion
}

