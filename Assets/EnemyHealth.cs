using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody), typeof(NavMeshAgent), typeof(EnemyMovement))]
public class EnemyHealth : MonoBehaviour
{
    #region Private Fields
    private Rigidbody _rb;
    private NavMeshAgent _agent;
    private EnemyMovement _movement;
    private bool _isRagdoll = false;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _agent = GetComponent<NavMeshAgent>();
        _movement = GetComponent<EnemyMovement>();
    }
    #endregion

    #region Public Methods
    public void GoRagdoll()
    {
        if (_isRagdoll) return;
        _isRagdoll = true;

        // 1. Disable logic components
        if (_agent != null) _agent.enabled = false;
        if (_movement != null) _movement.enabled = false;

        // 2. Enable full physics
        _rb.isKinematic = false;
        _rb.useGravity = true;
        
        // Disable rotation constraints so it can tumble
        _rb.constraints = RigidbodyConstraints.None;

        Debug.Log($"{name} is now a ragdoll!", this);
    }
    #endregion

    #region Collision Handling
    private void OnCollisionEnter(Collision collision)
    {
        // Check if hit by a dagger (DaggerProjectile)
        if (collision.gameObject.GetComponent<DaggerProjectile>() != null)
        {
            GoRagdoll();
            
            // Apply impact force from the dagger's velocity
            Rigidbody daggerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (daggerRb != null)
            {
                _rb.AddForceAtPosition(daggerRb.linearVelocity * 2f, collision.contacts[0].point, ForceMode.Impulse);
            }
        }
    }
    #endregion
}
