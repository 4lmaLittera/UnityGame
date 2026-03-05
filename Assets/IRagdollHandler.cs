using UnityEngine;

public interface IRagdollHandler
{
    void TriggerRagdoll(Vector3 impactForce, Vector3 impactPoint, Rigidbody hitBone = null);
}

