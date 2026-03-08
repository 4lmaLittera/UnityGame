using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody), typeof(NavMeshAgent))]
public class PartRagdollHandler : MonoBehaviour, IRagdollHandler, IPoolableEnemy
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
        
        // Find and potentially disable root colliders so they don't block hits to body parts
        _rootColliders.AddRange(GetComponents<Collider>());
        foreach (var col in _rootColliders)
        {
            col.enabled = false; // Disable root colliders to allow hits to reach bones
        }

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
            rb.detectCollisions = true;
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

        // 1. Disable Animator so physics can take over bones
        Animator animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = false;

        // 2. Disable root logic and physics
        if (_agent != null) _agent.enabled = false;

        var movement = GetComponent<EnemyMovement>();
        if (movement != null) movement.enabled = false;

        foreach (var col in _rootColliders) col.enabled = false;
        _rootRb.isKinematic = true;

        // 3. Enable bone physics
        foreach (var rb in _boneRigidbodies)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.useGravity = true;
        }

        // 4. Apply force to specific bone or generic center
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

    #region IPoolableEnemy Implementation
    public void OnSpawn()
    {
        _isRagdoll = false;

        // Reset Root
        if (_rootRb != null)
        {
            _rootRb.isKinematic = true; // Root is kinematic while agent moves
        }

        // Disable root colliders again as they might have been toggled
        foreach (var col in _rootColliders)
        {
            col.enabled = false; 
        }

        // Re-enable logic
        if (_agent != null) _agent.enabled = true;
        var movement = GetComponent<EnemyMovement>();
        if (movement != null) movement.enabled = true;

        // Reset Bones to animation state
        foreach (var rb in _boneRigidbodies)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            // detectCollisions stays true so they can be hit
        }

        // Force the Animator to snap bones back from their physics ragdoll pose
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
        }
    }

    public void OnDespawn()
    {
        // Cleanup happens in OnSpawn for next use, but we can do it here too if needed
    }
    #endregion
}
