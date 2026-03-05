using UnityEngine;

public class EnemyBodyPart : MonoBehaviour
{
    private IRagdollHandler _handler;
    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Setup(IRagdollHandler handler)
    {
        _handler = handler;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_handler != null && collision.gameObject.GetComponent<DaggerProjectile>() != null)
        {
            // Calculate force from impact
            Rigidbody daggerRb = collision.gameObject.GetComponent<Rigidbody>();
            Vector3 force = daggerRb != null ? daggerRb.linearVelocity * 2f : Vector3.zero;
            
            _handler.TriggerRagdoll(force, collision.contacts[0].point, _rb);
        }
    }
}

