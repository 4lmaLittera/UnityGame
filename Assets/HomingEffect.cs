using UnityEngine;

public class HomingEffect : MonoBehaviour, IProjectileEffect
{
    [Header("Upgrade State")]
    [Tooltip("Is the homing upgrade currently active?")]
    [SerializeField] private bool _isHomingEnabled = false;

    [Header("Homing Settings")]
    [Tooltip("How sharply the weapon turns towards the target.")]
    [SerializeField] private float _turnSpeed = 15f;
    [Tooltip("How far the weapon looks for targets.")]
    [SerializeField] private float _detectionRadius = 20f;
    [Tooltip("The maximum angle (in degrees) from the forward path a target can be to be tracked.")]
    [SerializeField] private float _maxHomingAngle = 60f;
    [Tooltip("What layers should the weapon home in on?")]
    [SerializeField] private LayerMask _targetLayer;
    [Tooltip("How long after throwing before homing kicks in.")]
    [SerializeField] private float _delayBeforeHoming = 0.2f;

    private Transform _target;
    private float _launchTime;
    private bool _hasImpacted = false;

    /// <summary>
    /// Call this from your Upgrade Manager to enable/disable homing and adjust its strength.
    /// </summary>
    public void SetHomingUpgrade(bool isEnabled, float turnSpeed, float delay, float detectionRadius)
    {
        _isHomingEnabled = isEnabled;
        _turnSpeed = turnSpeed;
        _delayBeforeHoming = delay;
        _detectionRadius = detectionRadius;
    }

    /// <summary>
    /// Toggle homing on or off without changing the parameters.
    /// </summary>
    public void ToggleHoming(bool isEnabled)
    {
        _isHomingEnabled = isEnabled;
    }

    public void OnLaunch(Rigidbody rb)
    {
        _launchTime = Time.time;
        _hasImpacted = false;
        
        if (_isHomingEnabled)
        {
            FindTarget(rb.linearVelocity.normalized);
        }
    }

    public void OnFlightUpdate(Rigidbody rb)
    {
        if (!_isHomingEnabled || _hasImpacted) return;
        if (Time.time < _launchTime + _delayBeforeHoming) return;

        // If no target, try to find one
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            FindTarget(rb.linearVelocity.normalized);
            if (_target == null) return;
        }

        // Apply Homing
        Vector3 currentVelocity = rb.linearVelocity;
        float currentSpeed = currentVelocity.magnitude;
        
        // Don't home if moving too slow
        if (currentSpeed < 0.1f) return;

        // Calculate direction to target
        Vector3 directionToTarget = (_target.position - rb.position).normalized;
        Vector3 currentDirection = currentVelocity.normalized;

        // Smoothly rotate the velocity towards the target
        Vector3 newDirection = Vector3.Slerp(currentDirection, directionToTarget, _turnSpeed * Time.fixedDeltaTime).normalized;

        // Update velocity (the DaggerProjectile script will automatically rotate the mesh to match this new velocity!)
        rb.linearVelocity = newDirection * currentSpeed;
    }

    public void OnImpact(Collision collision, Rigidbody rb)
    {
        _hasImpacted = true;
        _target = null;
    }

    private void FindTarget(Vector3 forwardDirection)
    {
        // Fallback to transform forward if velocity is negligible
        if (forwardDirection.sqrMagnitude < 0.01f) forwardDirection = transform.forward;

        Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRadius, _targetLayer);
        
        float bestDot = -1f; // -1 means completely behind, 1 means perfectly in front
        float minDotRequired = Mathf.Cos(_maxHomingAngle * Mathf.Deg2Rad); // Convert angle to dot product threshold

        foreach (var hit in hits)
        {
            Vector3 directionToHit = (hit.transform.position - transform.position).normalized;
            float dot = Vector3.Dot(forwardDirection, directionToHit);

            // Only consider targets within the cone of vision
            if (dot > minDotRequired)
            {
                // Prioritize the one most directly in front (highest dot product)
                if (dot > bestDot)
                {
                    bestDot = dot;
                    _target = hit.transform;
                }
            }
        }
    }
    
    // Visualize detection cone in the Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);
        
        // Draw the edges of the detection cone to help visualize the field of view
        Vector3 forward = Application.isPlaying && TryGetComponent(out Rigidbody rb) && rb.linearVelocity.sqrMagnitude > 0.1f 
            ? rb.linearVelocity.normalized 
            : transform.forward;
            
        Quaternion leftRayRotation = Quaternion.AngleAxis(-_maxHomingAngle, transform.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(_maxHomingAngle, transform.up);
        
        Vector3 leftRayDirection = leftRayRotation * forward;
        Vector3 rightRayDirection = rightRayRotation * forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, leftRayDirection * _detectionRadius);
        Gizmos.DrawRay(transform.position, rightRayDirection * _detectionRadius);
    }
}