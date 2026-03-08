using UnityEngine;

public interface IDamageSource
{
    float Damage { get; }
    Vector3 GetImpactForce(Vector3 currentVelocity);
    
    // Per-weapon throw settings
    float BaseThrowForce { get; }
    float UpwardForce { get; }
    float ThrowCooldown { get; }

    // Per-weapon rotation settings
    Vector3 RotationAxis { get; }
    float RotationSpeed { get; }
}

public interface IProjectileEffect
{
    void OnLaunch(Rigidbody rb);
    void OnFlightUpdate(Rigidbody rb);
    void OnImpact(Collision collision, Rigidbody rb);
}
