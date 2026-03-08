using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class DaggerProjectile : MonoBehaviour, IDamageSource
{
    #region Serialized Fields
    [Header("Behavior")]
    [SerializeField] private float _lifetime = 5f;
    [SerializeField] private bool _faceTravelDirection = true;
    [SerializeField] private float _rotationSpeed = 10f;

    [Header("Damage settings")]
    [SerializeField] private float _damage = 25f;
    [SerializeField] private float _forceMultiplier = 2f;

    [Header("Throw Settings")]
    [SerializeField] private float _baseThrowForce = 35f;
    [SerializeField] private float _upwardForce = 2f;
    [SerializeField] private float _throwCooldown = 0.5f;

    [Header("Throw Rotation Settings")]
    [Tooltip("Axis the projectile spins around when thrown. Vector3.forward (Z) makes it spiral. Vector3.right (X) makes it flip.")]
    [SerializeField] private Vector3 _throwRotationAxis = Vector3.forward;
    [SerializeField] private float _throwRotationSpeed = 1000f; // Degrees per second
    #endregion

    #region Private Fields
    private Rigidbody _rb;
    private Quaternion _baseFlightRotation;
    private float _accumulatedSpin;
    private IProjectileEffect[] _effects;
    #endregion

    #region IDamageSource Implementation
    public float Damage => _damage;
    
    public Vector3 GetImpactForce(Vector3 currentVelocity)
    {
        return currentVelocity * _forceMultiplier;
    }

    public float BaseThrowForce => _baseThrowForce;
    public float UpwardForce => _upwardForce;
    public float ThrowCooldown => _throwCooldown;

    public Vector3 RotationAxis => _throwRotationAxis;
    public float RotationSpeed => _throwRotationSpeed;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _baseFlightRotation = transform.rotation;
        _effects = GetComponents<IProjectileEffect>();
    }

    void Start()
    {
        // Automatically destroy after lifetime
        Destroy(gameObject, _lifetime);

        foreach (var effect in _effects)
        {
            effect.OnLaunch(_rb);
        }
    }

    void FixedUpdate()
    {
        if (_rb.isKinematic) return;

        // 1. Calculate the base arc (where the tip should point)
        if (_faceTravelDirection && _rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetFlightRotation = Quaternion.LookRotation(_rb.linearVelocity.normalized);
            _baseFlightRotation = Quaternion.Slerp(_baseFlightRotation, targetFlightRotation, Time.fixedDeltaTime * _rotationSpeed);
        }

        // 2. Apply spin on top of the base arc (or just the arc if no spin)
        if (_throwRotationSpeed != 0f)
        {
            _accumulatedSpin += _throwRotationSpeed * Time.fixedDeltaTime;
            Quaternion localSpin = Quaternion.AngleAxis(_accumulatedSpin, _throwRotationAxis.normalized);
            _rb.MoveRotation(_baseFlightRotation * localSpin);
        }
        else
        {
            _rb.MoveRotation(_baseFlightRotation);
        }

        // 3. Process external behaviors
        foreach (var effect in _effects)
        {
            effect.OnFlightUpdate(_rb);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_rb.isKinematic) return;

        foreach (var effect in _effects)
        {
            effect.OnImpact(collision, _rb);
        }

        // Optional: Stick to the wall on impact
        _rb.isKinematic = true;
        _rb.detectCollisions = false;
        transform.SetParent(collision.transform);
    }
    #endregion
}
