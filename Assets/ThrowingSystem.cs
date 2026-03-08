using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class ThrowingSystem : MonoBehaviour
{
    #region Serialized Fields
    [Header("References")]
    [SerializeField] private GameObject _weaponPrefab;
    [SerializeField] private Transform _throwPoint;

    [Header("System Settings")]
    [SerializeField] private float _throwCooldown = 0.5f;
    [SerializeField] private int _maxDaggers = 10;

    [Header("Default Physics Fallback")]
    [Tooltip("Used only if the projectile prefab doesn't have an IDamageSource component")]
    [SerializeField] private float _defaultThrowForce = 25f;
    [SerializeField] private float _defaultUpwardForce = 5f;
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
        if (rb == null) return;

        // Get force and rotation settings from the weapon itself
        float currentThrowForce = _defaultThrowForce;
        float currentUpwardForce = _defaultUpwardForce;

        IDamageSource damageSource = projectile.GetComponent<IDamageSource>();
        if (damageSource != null)
        {
            currentThrowForce = damageSource.BaseThrowForce;
            currentUpwardForce = damageSource.UpwardForce;
        }

        // Calculate direction with upward arc
        Vector3 throwDirection = _throwPoint.forward * currentThrowForce + Vector3.up * currentUpwardForce;

        rb.AddForce(throwDirection, ForceMode.Impulse);

        // Note: Projectiles with IDamageSource now handle their own spin mathematically in FixedUpdate.
    }
    #endregion
}
