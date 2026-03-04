using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class ThrowingSystem : MonoBehaviour
{
    #region Serialized Fields
    [Header("Settings")]
    [FormerlySerializedAs("weaponPrefab")]
    [SerializeField] private GameObject _weaponPrefab;

    [FormerlySerializedAs("throwPoint")]
    [SerializeField] private Transform _throwPoint;

    [FormerlySerializedAs("throwForce")]
    [SerializeField] private float _throwForce = 25f;

    [FormerlySerializedAs("throwUpwardForce")]
    [SerializeField] private float _throwUpwardForce = 5f;

    [FormerlySerializedAs("throwCooldown")]
    [SerializeField] private float _throwCooldown = 0.5f;

    [FormerlySerializedAs("maxDaggers")]
    [SerializeField] private int _maxDaggers = 10;
    #endregion

    #region Private Fields
    private float _nextThrowTime;
    private readonly Queue<GameObject> _activeDaggers = new Queue<GameObject>();
    private Collider _playerCollider;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _playerCollider = GetComponent<Collider>();
    }
    #endregion

    #region Public Methods
    public void Throw()
    {
        if (Time.time < _nextThrowTime) return;

        _nextThrowTime = Time.time + _throwCooldown;

        GameObject projectile = Instantiate(_weaponPrefab, _throwPoint.position, _throwPoint.rotation);

        // --- PREVENTION: Do not hit the player ---
        Collider projectileCollider = projectile.GetComponent<Collider>();
        if (projectileCollider != null && _playerCollider != null)
        {
            Physics.IgnoreCollision(projectileCollider, _playerCollider);
        }
        // ----------------------------------------

        // Manage active daggers list
        _activeDaggers.Enqueue(projectile);

        // If we exceed the limit, destroy the oldest one
        if (_activeDaggers.Count > _maxDaggers)
        {
            GameObject oldest = _activeDaggers.Dequeue();
            if (oldest != null)
            {
                Destroy(oldest);
            }
        }

        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        // Calculate direction with upward arc
        Vector3 throwDirection = _throwPoint.forward * _throwForce + Vector3.up * _throwUpwardForce;

        rb.AddForce(throwDirection, ForceMode.Impulse);

        // Add some random torque for a more dynamic "toss" feel
        rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
    }
    #endregion
}
