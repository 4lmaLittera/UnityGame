using UnityEngine;
using UnityEngine.Serialization;

public class GrapplingHook : MonoBehaviour
{
    #region Serialized Fields
    [Header("References")]
    [FormerlySerializedAs("hookPoint")]
    [SerializeField] private Transform _hookPoint;
    
    [FormerlySerializedAs("cam")]
    [SerializeField] private Transform _cam;
    
    [FormerlySerializedAs("playerRb")]
    [SerializeField] private Rigidbody _playerRb; 
    
    [FormerlySerializedAs("grappleMesh")]
    [SerializeField] private GameObject _grappleMesh;

    [Header("Settings")]
    [FormerlySerializedAs("grappleable")]
    [SerializeField] private LayerMask _grappleable;
    
    [FormerlySerializedAs("maxDistance")]
    [SerializeField] private float _maxDistance = 100f;
    
    [FormerlySerializedAs("jointSpring")]
    [SerializeField] private float _jointSpring = 15f;
    
    [FormerlySerializedAs("jointDamper")]
    [SerializeField] private float _jointDamper = 10f;
    
    [FormerlySerializedAs("jointMassScale")]
    [SerializeField] private float _jointMassScale = 4.5f;

    [Header("Release Boost")]
    [FormerlySerializedAs("velocityPushMultiplier")]
    [SerializeField] private float _velocityPushMultiplier = 0.5f;
    
    [FormerlySerializedAs("maxReleasePush")]
    [SerializeField] private float _maxReleasePush = 20f;
    
    [FormerlySerializedAs("minReleasePush")]
    [SerializeField] private float _minReleasePush = 3f;
    
    [FormerlySerializedAs("lookDirectionWeight")]
    [Range(0, 1)]
    [SerializeField] private float _lookDirectionWeight = 0.3f;
    
    [FormerlySerializedAs("releaseUpwardBoost")]
    [SerializeField] private float _releaseUpwardBoost = 2f;
    #endregion

    #region Private Fields
    private StretchedMeshLink _meshLink;
    private Transform _grappleTargetTransform;
    private Vector3 _grapplePoint;
    private SpringJoint _joint;
    private bool _isGrappling;
    private PlayerMovementAbilities _abilities;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        // Cache movement abilities to sync states
        _abilities = _playerRb.GetComponent<PlayerMovementAbilities>();

        if (_grappleMesh != null)
        {
            _meshLink = _grappleMesh.GetComponent<StretchedMeshLink>();

            if (_meshLink == null)
            {
                Debug.LogError($"GrappleMesh '{_grappleMesh.name}' is missing a StretchedMeshLink component!", this);
                return;
            }

            // Create a target transform for the grapple point
            GameObject go = new GameObject("GrappleTarget");
            _grappleTargetTransform = go.transform;

            _meshLink.SetPoints(_hookPoint, _grappleTargetTransform);
            _grappleMesh.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (_grappleTargetTransform != null)
        {
            Destroy(_grappleTargetTransform.gameObject);
        }
    }
    #endregion

    #region Public Methods
    public void StartGrapple()
    {
        if (_isGrappling) return;

        RaycastHit hit;
        if (Physics.Raycast(_cam.position, _cam.forward, out hit, _maxDistance, _grappleable))
        {
            _isGrappling = true;

            // Notify movement system that we are now grappling
            if (_abilities != null) _abilities.SetState(MovementState.Grappling);

            _grapplePoint = hit.point;
            _joint = _playerRb.gameObject.AddComponent<SpringJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedAnchor = _grapplePoint;

            float distanceFromPoint = Vector3.Distance(_playerRb.position, _grapplePoint);
            _joint.maxDistance = distanceFromPoint * 0.8f;
            _joint.minDistance = distanceFromPoint * 0.25f;

            _joint.spring = _jointSpring;
            _joint.damper = _jointDamper;
            _joint.massScale = _jointMassScale;

            if (_grappleMesh != null)
            {
                _grappleTargetTransform.position = _grapplePoint;
                _grappleMesh.SetActive(true);
            }
        }
    }

    public void StopGrapple()
    {
        if (_isGrappling)
        {
            _isGrappling = false;

            // Switch back to Airborne (or Idle/Moving if on ground)
            if (_abilities != null) _abilities.SetState(MovementState.Airborne);

            if (_joint != null)
            {
                Vector3 currentVel = _playerRb.linearVelocity;
                float speed = currentVel.magnitude;
                Vector3 moveDir = currentVel.normalized;
                Vector3 lookDir = _cam.forward;

                Vector3 combinedDir = Vector3.Lerp(moveDir, lookDir, _lookDirectionWeight).normalized;
                combinedDir = (combinedDir + Vector3.up * 0.2f).normalized;

                float pushStrength = Mathf.Clamp(speed * _velocityPushMultiplier, _minReleasePush, _maxReleasePush);
                _playerRb.AddForce(combinedDir * pushStrength + Vector3.up * _releaseUpwardBoost, ForceMode.Impulse);

                Destroy(_joint);
                _joint = null;
            }

            if (_grappleMesh != null) _grappleMesh.SetActive(false);
        }
    }
    #endregion
}
