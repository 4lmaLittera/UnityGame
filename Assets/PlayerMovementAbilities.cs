using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementAbilities : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float rayDistance = 1.1f;

    [Header("Landing Softness")]
    [Tooltip("The Physic Material on your Feet Collider")]
    [SerializeField] private PhysicsMaterial feetMaterial;
    [SerializeField] private float targetFriction = 0.6f;
    [SerializeField] private float frictionRestoreSpeed = 0.2f;

    private Rigidbody _rb;
    private bool _wasGrounded;
    private Coroutine _frictionCoroutine;

    // Professional standard: Public property with private setter
    // This allows PlayerMotor to read the state but not change it.
    public bool IsGrounded { get; private set; }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // IMPORTANT: Instance the material so we don't modify the Project Asset.
        if (feetMaterial != null)
        {
            feetMaterial = Instantiate(feetMaterial);

            // Pro Tip: Ensure your SphereCollider (Feet) is assigned this instanced material
            // If you have multiple colliders, you may need to find the specific 'Feet' one.
            var colliders = GetComponents<Collider>();
            foreach (var col in colliders)
            {
                if (col is SphereCollider) col.material = feetMaterial;
            }
        }
    }

    void FixedUpdate()
    {
        // Perform the ground check
        IsGrounded = Physics.Raycast(transform.position, Vector3.down, rayDistance, groundLayer);

        // Detect the exact frame of landing
        if (IsGrounded && !_wasGrounded)
        {
            OnLand();
        }

        _wasGrounded = IsGrounded;
    }

    private void OnLand()
    {
        if (_frictionCoroutine != null) StopCoroutine(_frictionCoroutine);
        _frictionCoroutine = StartCoroutine(RestoreFriction());
    }

    private IEnumerator RestoreFriction()
    {
        if (feetMaterial == null) yield break;

        float elapsed = 0;

        // Start slippery to preserve horizontal momentum from the air
        feetMaterial.dynamicFriction = 0;
        feetMaterial.staticFriction = 0;

        while (elapsed < frictionRestoreSpeed)
        {
            elapsed += Time.deltaTime;
            float currentFriction = Mathf.Lerp(0, targetFriction, elapsed / frictionRestoreSpeed);

            feetMaterial.dynamicFriction = currentFriction;
            feetMaterial.staticFriction = currentFriction;

            yield return null;
        }

        // Ensure we hit the exact target at the end
        feetMaterial.dynamicFriction = targetFriction;
        feetMaterial.staticFriction = targetFriction;
    }

    public void ExecuteJump()
    {
        if (IsGrounded)
        {
            // If we jump, cancel any ongoing 'soft landing' to ensure clean lift-off
            if (_frictionCoroutine != null) StopCoroutine(_frictionCoroutine);
            if (feetMaterial != null) feetMaterial.dynamicFriction = 0;

            // 2026 Standard: use linearVelocity instead of velocity
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    // Professional Debugging: See the ray in the Scene View
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * rayDistance);
    }
}