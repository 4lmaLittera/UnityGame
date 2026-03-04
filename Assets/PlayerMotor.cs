using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float pushForce = 20f;
    [SerializeField] private float maxSpeed = 10f;
    [Range(0, 1)]
    [SerializeField] private float airControlMultiplier = 0.2f;

    private Rigidbody _rb;
    private PlayerMovementAbilities _abilities;

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

    /// <summary>
    /// Called by PlayerInputHandler to move the player.
    /// </summary>
    public void ProcessMove(Vector2 input)
    {
        // 1. Calculate direction based on player's orientation
        Vector3 moveDir = (transform.right * input.x + transform.forward * input.y).normalized;

        // 2. Determine how much power we have based on ground state
        float currentForce = _abilities.IsGrounded ? pushForce : pushForce * airControlMultiplier;

        // 3. Isolate horizontal velocity to check against Max Speed
        Vector3 horizontalVel = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);

        // 4. Only apply force if we are under the speed limit OR trying to turn around
        if (horizontalVel.magnitude < maxSpeed || Vector3.Dot(moveDir, horizontalVel) < 0)
        {
            _rb.AddForce(moveDir * currentForce, ForceMode.Acceleration);
        }

        //Debug.Log(horizontalVel);
    }
}