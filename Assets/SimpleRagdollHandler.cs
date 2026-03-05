using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody), typeof(NavMeshAgent))]
public class SimpleRagdollHandler : MonoBehaviour, IRagdollHandler
{
    private Rigidbody _rb;
    private NavMeshAgent _agent;
    private Collider _collider;
    private bool _isRagdoll = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _agent = GetComponent<NavMeshAgent>();
        _collider = GetComponent<Collider>();
    }

    public void TriggerRagdoll(Vector3 impactForce, Vector3 impactPoint, Rigidbody hitBone = null)
    {
        if (_isRagdoll) return;
        _isRagdoll = true;

        if (_agent != null) _agent.enabled = false;
        
        var movement = GetComponent<EnemyMovement>();
        if (movement != null) movement.enabled = false;
        
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.constraints = RigidbodyConstraints.None;
        
        _rb.AddForceAtPosition(impactForce, impactPoint, ForceMode.Impulse);
        
        Debug.Log($"{name} triggered Simple Ragdoll!");
    }
}
