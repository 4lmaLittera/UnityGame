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

    [Header("Footstep Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private PlayerCameraEffects _cameraEffects;
    [SerializeField] private AudioClip[] _footstepClips;
    [Tooltip("Used only when no camera bob source is available.")]
    [SerializeField] private float _fallbackFootstepInterval = 0.45f;
    [Tooltip("Multiplier for bob-based cadence. 1 = exactly one step per bob peak.")]
    [SerializeField] private float _footstepCadenceMultiplier = 1f;
    [SerializeField] private float _minFootstepInterval = 0.12f;
    [SerializeField] private float _maxFootstepInterval = 0.65f;
    [SerializeField] private float _minFootstepSpeed = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float _footstepVolume = 0.7f;
    #endregion

    #region Private Fields
    private Rigidbody _rb;
    private PlayerMovementAbilities _abilities;
    private float _nextFootstepTime;
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
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
        if (_cameraEffects == null) _cameraEffects = GetComponentInChildren<PlayerCameraEffects>();

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
    /// Applies an instantaneous force to the player (e.g., from damage).
    /// </summary>
    public void ApplyKnockback(Vector3 force)
    {
        // 2026 Standard: use ForceMode.Impulse for immediate response
        _rb.AddForce(force, ForceMode.Impulse);
    }

    /// <summary>
    /// Called by PlayerInputHandler to move the player.
    /// </summary>
    public void ProcessMove(Vector2 input)
    {
        // 1. Calculate direction based on player's orientation
        Vector3 moveDir = (transform.right * input.x + transform.forward * input.y).normalized;

        // 2. Project movement along the slope surface so the player walks up/down hills
        if (_abilities.IsGrounded)
        {
            moveDir = Vector3.ProjectOnPlane(moveDir, _abilities.SlopeNormal).normalized;
        }

        // 3. Determine how much power we have based on ground state
        float currentForce = _abilities.IsGrounded ? _pushForce : _pushForce * _airControlMultiplier;

        // 4. Measure speed along the slope surface (not just horizontal) to respect max speed on slopes
        Vector3 flatVel = Vector3.ProjectOnPlane(_rb.linearVelocity, _abilities.SlopeNormal);

        // 5. Only apply force if we are under the speed limit OR trying to turn around
        if (flatVel.magnitude < _maxSpeed || Vector3.Dot(moveDir, flatVel) < 0)
        {
            _rb.AddForce(moveDir * currentForce, ForceMode.Acceleration);
        }

        float horizontalSpeed = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z).magnitude;
        TryPlayFootstep(input, horizontalSpeed);
    }

    private void TryPlayFootstep(Vector2 input, float speed)
    {
        if (!_abilities.IsGrounded) return;
        if (_audioSource == null || _footstepClips == null || _footstepClips.Length == 0) return;
        if (input.sqrMagnitude < 0.01f || speed < _minFootstepSpeed) return;
        if (Time.time < _nextFootstepTime) return;

        AudioClip clip = _footstepClips[UnityEngine.Random.Range(0, _footstepClips.Length)];
        if (clip == null) return;

        _audioSource.PlayOneShot(clip, _footstepVolume);

        _nextFootstepTime = Time.time + GetFootstepInterval(speed);
    }

    private float GetFootstepInterval(float speed)
    {
        float safeSpeed = Mathf.Max(_minFootstepSpeed, speed);
        float interval = _fallbackFootstepInterval;

        float bobFrequency = (_cameraEffects != null) ? _cameraEffects.BobFrequency : 0f;
        if (bobFrequency > 0.01f)
        {
            // Camera uses Abs(Sin(distance * frequency)); peaks are PI apart.
            interval = Mathf.PI / (safeSpeed * bobFrequency);
        }

        interval *= Mathf.Max(0.01f, _footstepCadenceMultiplier);
        return Mathf.Clamp(interval, _minFootstepInterval, _maxFootstepInterval);
    }
    #endregion
}
