using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class GrapplingHook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LineRenderer lr;
    [SerializeField] private Transform hookPoint;
    [SerializeField] private Transform cam;
    [SerializeField] private Rigidbody playerRb; [SerializeField] private GameObject grappleMesh;

    private StretchedMeshLink _meshLink;
    private Transform _grappleTargetTransform;


    [Header("Settings")]
    [SerializeField] private LayerMask grappleable;
    [SerializeField] private float maxDistance = 100f;
    [SerializeField] private float jointSpring = 15f;
    [SerializeField] private float jointDamper = 10f;
    [SerializeField] private float jointMassScale = 4.5f;

    [Header("Release Boost")]
    [Tooltip("How much of your current speed is added as a push when releasing.")]
    [SerializeField] private float velocityPushMultiplier = 0.5f;
    [Tooltip("The maximum push force allowed on release.")]
    [SerializeField] private float maxReleasePush = 20f;
    [Tooltip("The minimum push force allowed on release.")]
    [SerializeField] private float minReleasePush = 3f;
    [Tooltip("0 = push in movement direction, 1 = push in look direction.")]
    [Range(0, 1)]
    [SerializeField] private float lookDirectionWeight = 0.3f;
    [Tooltip("A flat upward boost added on release to help with height.")]
    [SerializeField] private float releaseUpwardBoost = 2f;

    private Vector3 _grapplePoint;
    private SpringJoint _joint;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.enabled = false; // Replace LineRenderer with GrappleMesh

        if (grappleMesh != null)
        {
            _meshLink = grappleMesh.GetComponent<StretchedMeshLink>();

            // Create a target transform for the grapple point
            // We do NOT parent it to the player so it stays fixed in world space
            GameObject go = new GameObject("GrappleTarget");
            _grappleTargetTransform = go.transform;

            _meshLink.SetPoints(hookPoint, _grappleTargetTransform);
            grappleMesh.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (_grappleTargetTransform != null)
        {
            Destroy(_grappleTargetTransform.gameObject);
        }
    }


    void LateUpdate()
    {
        DrawRope();
    }

    public void StartGrapple()
    {
        RaycastHit hit;
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxDistance, grappleable))
        {
            _grapplePoint = hit.point;
            _joint = playerRb.gameObject.AddComponent<SpringJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = _grapplePoint;

            float distanceFromPoint = Vector3.Distance(playerRb.position, _grapplePoint);

            // The distance grapple will try to keep from grapple point. 
            _joint.maxDistance = distanceFromPoint * 0.8f;
            _joint.minDistance = distanceFromPoint * 0.25f;

            // Customize these values to change the feel of the grapple
            _joint.spring = jointSpring;
            _joint.damper = jointDamper;
            _joint.massScale = jointMassScale;

            if (grappleMesh != null)
            {
                _grappleTargetTransform.position = _grapplePoint;
                grappleMesh.SetActive(true);
            }
        }
    }

    public void StopGrapple()
    {
        if (_joint != null)
        {
            // 1. Calculate the push strength based on current swing speed
            Vector3 currentVel = playerRb.linearVelocity;
            float speed = currentVel.magnitude;

            // 2. Blend the current velocity direction with the look direction
            Vector3 moveDir = currentVel.normalized;
            Vector3 lookDir = cam.forward;

            // 3. Combine directions and add upward influence
            Vector3 combinedDir = Vector3.Lerp(moveDir, lookDir, lookDirectionWeight).normalized;
            combinedDir = (combinedDir + Vector3.up * 0.2f).normalized; // Slight upward tilt to the exit vector

            float pushStrength = Mathf.Clamp(speed * velocityPushMultiplier, minReleasePush, maxReleasePush);

            // 4. Apply the impulse boost + the flat upward kick
            playerRb.AddForce(combinedDir * pushStrength + Vector3.up * releaseUpwardBoost, ForceMode.Impulse);

            // 5. Clean up
            if (grappleMesh != null) grappleMesh.SetActive(false);
            Destroy(_joint);
        }
    }

    void DrawRope()
    {
        // No longer using LineRenderer for rope drawing
    }
}