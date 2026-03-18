using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerCameraEffects : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMotor _motor;
    [SerializeField] private PlayerMovementAbilities _abilities;
    [SerializeField] private PlayerInputHandler _input;
    
    [Header("Head Bob")]
    [SerializeField] private float _bobFrequency = 1.5f;
    [SerializeField] private float _bobAmplitude = 0.05f;
    [Tooltip("How much speed is required to reach full bob amplitude?")]
    [SerializeField] private float _speedForMaxBob = 8f;

    [Header("Landing Dip")]
    [SerializeField] private float _dipMultiplier = 0.05f;
    [SerializeField] private float _maxDipAmount = 0.5f;
    [SerializeField] private float _dipSpeed = 15f;
    [SerializeField] private float _recoverySpeed = 5f;

    [Header("Strafe Tilt")]
    [SerializeField] private float _maxTiltAngle = 2f;
    [SerializeField] private float _tiltSpeed = 5f;

    [Header("Dynamic FOV")]
    [SerializeField] private float _baseFOV = 90f;
    [SerializeField] private float _maxFOV = 105f;
    [SerializeField] private float _speedForMaxFOV = 15f;
    [SerializeField] private float _fovLerpSpeed = 5f;

    private Camera _cam;
    private Vector3 _startLocalPos;
    private float _distanceTraveled;
    
    // Landing state
    private float _targetDip;
    private float _currentDip;

    public float BobFrequency => _bobFrequency;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _startLocalPos = transform.localPosition;
    }

    void OnEnable()
    {
        if (_abilities != null)
            _abilities.OnLandedEvent += HandleLanding;
    }

    void OnDisable()
    {
        if (_abilities != null)
            _abilities.OnLandedEvent -= HandleLanding;
    }

    void Update()
    {
        if (_motor == null || _input == null) return;

        Vector3 horizontalVel = new Vector3(_motor.CurrentVelocity.x, 0, _motor.CurrentVelocity.z);
        float currentSpeed = horizontalVel.magnitude;

        // 1. Dynamic FOV
        float speedRatio = Mathf.Clamp01(currentSpeed / _speedForMaxFOV);
        float targetFOV = Mathf.Lerp(_baseFOV, _maxFOV, speedRatio);
        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, Time.deltaTime * _fovLerpSpeed);

        // 2. Head Bobbing (only on ground)
        float bobOffset = 0f;
        if (_abilities.IsGrounded && currentSpeed > 0.1f)
        {
            _distanceTraveled += currentSpeed * Time.deltaTime;
            float speedBobRatio = Mathf.Clamp01(currentSpeed / _speedForMaxBob);
            
            // Absolute value of Sin makes it bounce up and down instead of going below center
            bobOffset = Mathf.Abs(Mathf.Sin(_distanceTraveled * _bobFrequency)) * _bobAmplitude * speedBobRatio;
        }
        else
        {
            // Reset bob cycle smoothly
            _distanceTraveled = 0f; 
        }

        // 3. Landing Dip
        _currentDip = Mathf.Lerp(_currentDip, _targetDip, Time.deltaTime * (_targetDip < 0 ? _dipSpeed : _recoverySpeed));
        
        // Recover target dip back to 0
        _targetDip = Mathf.MoveTowards(_targetDip, 0f, Time.deltaTime * _recoverySpeed);

        // Apply Position (Bob + Dip)
        Vector3 targetPos = _startLocalPos + Vector3.up * (bobOffset + _currentDip);
        transform.localPosition = targetPos;

        // 4. Strafe Tilt
        float targetTilt = -_input.MoveInput.x * _maxTiltAngle;
        Quaternion targetRot = Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y, targetTilt);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * _tiltSpeed);
    }

    private void HandleLanding(float impactVelocityY)
    {
        // Velocity is negative when falling. 
        // We only dip if we were actually falling (not just walking down stairs)
        if (impactVelocityY < -2f)
        {
            float dipAmount = impactVelocityY * _dipMultiplier; // Will be negative
            _targetDip = Mathf.Clamp(dipAmount, -_maxDipAmount, 0f);
        }
    }
}