using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public enum MovementState
{
    Idle,
    Moving,
    Sprinting,
    Airborne,
    Crouching,
    Grappling
}

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementAbilities : MonoBehaviour
{
    #region Events
    /// <summary>
    /// Fired when the player lands. Passes the vertical velocity just before impact.
    /// </summary>
    public event Action<float> OnLandedEvent;
    #endregion

    #region Serialized Fields
    [Header("State Debugging")]
    [SerializeField] private MovementState _currentState;

    [Header("Jump Settings")]
    [FormerlySerializedAs("jumpForce")]
    [SerializeField] private float _jumpForce = 5f;
    
    [FormerlySerializedAs("groundLayer")]
    [SerializeField] private LayerMask _groundLayer;
    
    [FormerlySerializedAs("rayDistance")]
    [SerializeField] private float _rayDistance = 1.1f;

    [Header("Landing Softness")]
    [Tooltip("The Physic Material on your Feet Collider")]
    [FormerlySerializedAs("feetMaterial")]
    [SerializeField] private PhysicsMaterial _feetMaterial;
    
    [FormerlySerializedAs("targetFriction")]
    [SerializeField] private float _targetFriction = 0.6f;
    
    [FormerlySerializedAs("frictionRestoreSpeed")]
    [SerializeField] private float _frictionRestoreSpeed = 0.2f;

    [Header("Jump Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _jumpClip;
    [Range(0f, 1f)]
    [SerializeField] private float _jumpVolume = 0.9f;
    #endregion

    #region Private Fields
    private Rigidbody _rb;
    private bool _wasGrounded;
    private float _lastVerticalVelocity;
    private Coroutine _frictionCoroutine;
    #endregion

    #region Properties
    // Professional standard: Public property with private setter
    // This allows PlayerMotor to read the state but not change it.
    public bool IsGrounded { get; private set; }
    public MovementState CurrentState => _currentState;
    public Vector3 SlopeNormal { get; private set; } = Vector3.up;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();

        // IMPORTANT: Instance the material so we don't modify the Project Asset.
        if (_feetMaterial != null)
        {
            _feetMaterial = Instantiate(_feetMaterial);

            // Pro Tip: Ensure your SphereCollider (Feet) is assigned this instanced material
            var colliders = GetComponents<Collider>();
            foreach (var col in colliders)
            {
                if (col is SphereCollider) col.material = _feetMaterial;
            }
        }
    }

    void FixedUpdate()
    {
        CheckGround();
        UpdateState();
    }

    private void CheckGround()
    {
        // Perform the ground check and capture surface normal for slope handling
        RaycastHit groundHit;
        IsGrounded = Physics.Raycast(transform.position, Vector3.down, out groundHit, _rayDistance, _groundLayer);
        SlopeNormal = IsGrounded ? groundHit.normal : Vector3.up;

        // Detect the exact frame of landing
        if (IsGrounded && !_wasGrounded)
        {
            OnLand();
            OnLandedEvent?.Invoke(_lastVerticalVelocity);
        }

        _wasGrounded = IsGrounded;
        
        // Track vertical velocity for landing effects
        _lastVerticalVelocity = _rb.linearVelocity.y;
    }

    private void UpdateState()
    {
        // If we are grounded, determine if we are idle or moving
        if (IsGrounded)
        {
            float horizontalSpeed = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z).magnitude;
            
            if (horizontalSpeed < 0.1f)
                _currentState = MovementState.Idle;
            else
                _currentState = MovementState.Moving;
        }
        else
        {
            // Only switch to Airborne if we aren't doing something special like Grappling
            if (_currentState != MovementState.Grappling)
            {
                _currentState = MovementState.Airborne;
            }
        }
    }

    // Professional Debugging: See the ray in the Scene View
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * _rayDistance);
    }
    #endregion

    #region Public Methods
    public void ExecuteJump()
    {
        if (IsGrounded)
        {
            // If we jump, cancel any ongoing 'soft landing' to ensure clean lift-off
            if (_frictionCoroutine != null) StopCoroutine(_frictionCoroutine);
            if (_feetMaterial != null) _feetMaterial.dynamicFriction = 0;

            // 2026 Standard: use linearVelocity instead of velocity
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);

            if (_audioSource != null && _jumpClip != null)
            {
                _audioSource.PlayOneShot(_jumpClip, _jumpVolume);
            }
        }
    }

    /// <summary>
    /// Allows external systems (like GrapplingHook) to manually set the state.
    /// </summary>
    public void SetState(MovementState newState)
    {
        _currentState = newState;
    }
    #endregion

    #region Private Methods & Coroutines
    private void OnLand()
    {
        if (_frictionCoroutine != null) StopCoroutine(_frictionCoroutine);
        _frictionCoroutine = StartCoroutine(RestoreFriction());
    }

    private IEnumerator RestoreFriction()
    {
        if (_feetMaterial == null) yield break;

        float elapsed = 0;

        // Start slippery to preserve horizontal momentum from the air
        _feetMaterial.dynamicFriction = 0;
        _feetMaterial.staticFriction = 0;

        while (elapsed < _frictionRestoreSpeed)
        {
            elapsed += Time.deltaTime;
            float currentFriction = Mathf.Lerp(0, _targetFriction, elapsed / _frictionRestoreSpeed);

            _feetMaterial.dynamicFriction = currentFriction;
            _feetMaterial.staticFriction = currentFriction;

            yield return null;
        }

        // Ensure we hit the exact target at the end
        _feetMaterial.dynamicFriction = _targetFriction;
        _feetMaterial.staticFriction = _targetFriction;
    }
    #endregion
}
