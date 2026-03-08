using UnityEngine;

public class EnemyBodyPart : MonoBehaviour
{
    [SerializeField] private float _damageMultiplier = 1f;

    private IRagdollHandler _handler;
    private EnemyHealth _health;
    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // Find health on the parent hierarchy
        _health = GetComponentInParent<EnemyHealth>();
    }

    public void Setup(IRagdollHandler handler)
    {
        _handler = handler;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Try to get any component that implements the damage source interface
        var damageSource = collision.gameObject.GetComponent<IDamageSource>();
        
        if (damageSource != null)
        {
            // Capture the relative velocity or the projectile's velocity before it's zeroed out
            // collision.relativeVelocity is often the best measure of impact intensity
            Vector3 impactVelocity = collision.relativeVelocity;
            
            // If relativeVelocity is too low (e.g. hitting from behind), fallback to the actual RB velocity if it still exists
            if (impactVelocity.sqrMagnitude < 0.1f && collision.rigidbody != null)
            {
                impactVelocity = collision.rigidbody.linearVelocity;
            }

            Vector3 force = damageSource.GetImpactForce(impactVelocity);
            
            if (_health != null)
            {
                // Multiply projectile damage by the part's specific multiplier (e.g., 2x for head)
                float finalDamage = damageSource.Damage * _damageMultiplier;
                _health.TakeDamage(finalDamage, force, collision.contacts[0].point, _rb);
            }
            else if (_handler != null)
            {
                // Fallback to immediate ragdoll if health script is missing
                _handler.TriggerRagdoll(force, collision.contacts[0].point, _rb);
            }
        }
    }
}

