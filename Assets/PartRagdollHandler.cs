using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody), typeof(NavMeshAgent))]
public class PartRagdollHandler : MonoBehaviour, IRagdollHandler
{
    #region Serialized Fields
    [Header("Settings")]
    [SerializeField] private Transform _ragdollRoot;
    #endregion

    #region Private Fields
    private Rigidbody _rootRb;
    private NavMeshAgent _agent;
    private List<Collider> _rootColliders = new();
    private bool _isRagdoll = false;

    private List<Rigidbody> _boneRigidbodies = new();
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _rootRb = GetComponent<Rigidbody>();
        _agent = GetComponent<NavMeshAgent>();
        _rootColliders.AddRange(GetComponents<Collider>());

        // Disable root physics while alive so NavMeshAgent can move freely
        // and CharacterJoints don't pull against a dynamic root body.
        if (_rootRb != null) _rootRb.isKinematic = true;

        if (_ragdollRoot == null) _ragdollRoot = transform;
        SetupRagdoll();
    }
    #endregion

    #region Initialization
    private void SetupRagdoll()
    {
        Rigidbody[] rbs = _ragdollRoot.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs)
        {
            if (rb == _rootRb) continue;

            rb.isKinematic = true;
            rb.detectCollisions = false;
            _boneRigidbodies.Add(rb);

            // Link bone to this handler
            var part = rb.gameObject.GetComponent<EnemyBodyPart>();
            if (part == null) part = rb.gameObject.AddComponent<EnemyBodyPart>();
            part.Setup(this);
        }
    }
    #endregion

    #region IRagdollHandler Implementation
    public void TriggerRagdoll(Vector3 impactForce, Vector3 impactPoint, Rigidbody hitBone = null)
    {
        if (_isRagdoll) return;
        _isRagdoll = true;

        // 1. Disable root logic and physics
        if (_agent != null) _agent.enabled = false;

        var movement = GetComponent<EnemyMovement>();
        if (movement != null) movement.enabled = false;

        foreach (var col in _rootColliders) col.enabled = false;
        _rootRb.isKinematic = true;

        // 2. Enable bone physics
        foreach (var rb in _boneRigidbodies)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.useGravity = true;
        }

        // 3. Apply force to specific bone or generic center
        if (hitBone != null)
        {
            hitBone.AddForceAtPosition(impactForce, impactPoint, ForceMode.Impulse);
        }
        else if (_boneRigidbodies.Count > 0)
        {
            _boneRigidbodies[0].AddForceAtPosition(impactForce, impactPoint, ForceMode.Impulse);
        }

        Debug.Log($"{name} triggered Part-Based Ragdoll!");
    }
    #endregion
}
