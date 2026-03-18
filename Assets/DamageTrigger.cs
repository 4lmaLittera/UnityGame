using UnityEngine;

/// <summary>
/// Attach this to a Collider (Is Trigger) childed to an enemy bone.
/// Deals damage and knockback to the player on contact.
/// </summary>
public class DamageTrigger : MonoBehaviour
{
    #region Serialized Fields
    [Header("Damage Settings")]
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _knockbackForce = 15f;
    
    [Header("Cooldown")]
    [Tooltip("Prevents multiple hits in a single frame/swing.")]
    [SerializeField] private float _hitCooldown = 0.5f;
    #endregion

    #region Private Fields
    private float _lastHitTime;
    #endregion

    #region Unity Lifecycle
    private void OnTriggerEnter(Collider other)
    {
        // 1. Only hit the Player
        if (!other.CompareTag("Player")) return;

        // 2. Check cooldown
        if (Time.time < _lastHitTime + _hitCooldown) return;

        // 3. Apply damage and knockback
        if (other.TryGetComponent<PlayerHealth>(out var health))
        {
            // Calculate direction: from this collider to the player
            Vector3 knockbackDir = (other.transform.position - transform.position).normalized;
            
            // Add a slight upward "pop" to lift the player slightly off the ground, reducing friction resistance
            knockbackDir.y += 0.2f;
            knockbackDir.Normalize();

            health.TakeDamage(_damage, knockbackDir * _knockbackForce);
            _lastHitTime = Time.time;
        }
    }
    #endregion
}
