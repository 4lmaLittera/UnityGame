using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class DaggerProjectile : MonoBehaviour
{
    #region Serialized Fields
    [Header("Behavior")]
    [FormerlySerializedAs("lifetime")]
    [SerializeField] private float _lifetime = 5f;
    
    [FormerlySerializedAs("faceTravelDirection")]
    [SerializeField] private bool _faceTravelDirection = true;
    
    [FormerlySerializedAs("rotationSpeed")]
    [SerializeField] private float _rotationSpeed = 10f;
    #endregion

    #region Private Fields
    private Rigidbody _rb;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Automatically destroy after lifetime
        Destroy(gameObject, _lifetime);
    }

    void FixedUpdate()
    {
        if (_faceTravelDirection && _rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            // Rotate smoothly to face the direction of travel
            Quaternion targetRotation = Quaternion.LookRotation(_rb.linearVelocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * _rotationSpeed);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Optional: Stick to the wall on impact
        _rb.isKinematic = true;
        _rb.detectCollisions = false;
        transform.SetParent(collision.transform);
        
        // Stop the cleanup timer since it's stuck now (or just let it die)
    }
    #endregion
}
