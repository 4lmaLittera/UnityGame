using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

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
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // IMPORTANT: Instance the material so we don't modify the Project Asset.
        if (_feetMaterial != null)
        {
            _feetMaterial = Instantiate(_feetMaterial);

            // Pro Tip: Ensure your SphereCollider (Feet) is assigned this instanced material
            // If you have multiple colliders, you may need to find the specific 'Feet' one.
            var colliders = GetComponents<Collider>();
            foreach (var col in colliders)
            {
                if (col is SphereCollider) col.material = _feetMaterial;
            }
        }
    }

    void FixedUpdate()
    {
        // Perform the ground check
        IsGrounded = Physics.Raycast(transform.position, Vector3.down, _rayDistance, _groundLayer);

        // Detect the exact frame of landing
        if (IsGrounded && !_wasGrounded)
        {
            OnLand();
            OnLandedEvent?.Invoke(_lastVerticalVelocity);
        }

        _wasGrounded = IsGrounded;
        
        // Track this so we know how fast we were falling next frame if we hit the ground
        _lastVerticalVelocity = _rb.linearVelocity.y;
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
        }
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
