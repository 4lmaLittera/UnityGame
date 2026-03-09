using System;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMotor : MonoBehaviour
{
    #region Serialized Fields
    [Header("Settings")]
    [FormerlySerializedAs("pushForce")]
    [SerializeField] private float _pushForce = 20f;
    
    [FormerlySerializedAs("maxSpeed")]
    [SerializeField] private float _maxSpeed = 10f;
    
    [Range(0, 1)]
    [FormerlySerializedAs("airControlMultiplier")]
    [SerializeField] private float _airControlMultiplier = 0.2f;
    #endregion

    #region Private Fields
    private Rigidbody _rb;
    private PlayerMovementAbilities _abilities;
    #endregion

    #region Properties
    public Vector3 CurrentVelocity => _rb.linearVelocity;
    public float MaxSpeed => _maxSpeed;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _abilities = GetComponent<PlayerMovementAbilities>();

        // Standard Professional Physics Setup
        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Low damping allows our Physics Material to handle the "feel" of stopping
        _rb.linearDamping = 0.1f;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Called by PlayerInputHandler to move the player.
    /// </summary>
    public void ProcessMove(Vector2 input)
    {
        // 1. Calculate direction based on player's orientation
        Vector3 moveDir = (transform.right * input.x + transform.forward * input.y).normalized;

        // 2. Determine how much power we have based on ground state
        float currentForce = _abilities.IsGrounded ? _pushForce : _pushForce * _airControlMultiplier;

        // 3. Isolate horizontal velocity to check against Max Speed
        Vector3 horizontalVel = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);

        // 4. Only apply force if we are under the speed limit OR trying to turn around
        if (horizontalVel.magnitude < _maxSpeed || Vector3.Dot(moveDir, horizontalVel) < 0)
        {
            _rb.AddForce(moveDir * currentForce, ForceMode.Acceleration);
        }
    }
    #endregion
}
