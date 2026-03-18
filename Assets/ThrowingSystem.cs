using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class ThrowingSystem : MonoBehaviour
{
    #region Serialized Fields
    [Header("Weapon Slots")]
    [SerializeField] private GameObject _primaryWeaponPrefab;
    [SerializeField] private GameObject _secondaryWeaponPrefab;
    
    [Header("References")]
    [SerializeField] private Transform _throwPoint;

    [Header("System Settings")]
    [SerializeField] private int _maxActiveProjectiles = 10;

    [Header("Default Physics Fallback")]
    [Tooltip("Used only if the projectile prefab doesn't have an IDamageSource component")]
    [SerializeField] private float _defaultThrowForce = 25f;
    [SerializeField] private float _defaultUpwardForce = 5f;
    [SerializeField] private float _defaultThrowCooldown = 0.5f;

    [Header("Throw Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _primaryThrowClip;
    [SerializeField] private AudioClip _secondaryThrowClip;
    [Range(0f, 1f)]
    [SerializeField] private float _primaryThrowVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float _secondaryThrowVolume = 1f;
    [SerializeField] private float _throwNoiseRadius = 20f;
    #endregion

    #region Private Fields
    private float _primaryNextThrowTime;
    private float _secondaryNextThrowTime;
    private float _lastPrimaryCooldown = 0.1f;
    private float _lastSecondaryCooldown = 0.1f;
    
    private readonly Queue<GameObject> _activeProjectiles = new Queue<GameObject>();
    private Collider _playerCollider;
    #endregion

    #region Properties
    /// <summary>
    /// Returns a value between 0 (just thrown) and 1 (ready to throw) for the Primary slot.
    /// </summary>
    public float PrimaryCooldownRatio => GetRatio(_primaryNextThrowTime, _lastPrimaryCooldown);

    /// <summary>
    /// Returns a value between 0 (just thrown) and 1 (ready to throw) for the Secondary slot.
    /// </summary>
    public float SecondaryCooldownRatio => GetRatio(_secondaryNextThrowTime, _lastSecondaryCooldown);

    private float GetRatio(float nextTime, float lastCooldown)
    {
        if (Time.time >= nextTime) return 1f;
        if (lastCooldown <= 0f) return 1f;
        return Mathf.Clamp01(1f - ((nextTime - Time.time) / lastCooldown));
    }
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _playerCollider = GetComponent<Collider>();
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Throws the weapon assigned to the Primary slot.
    /// </summary>
    public void ThrowPrimary()
    {
        if (Time.time < _primaryNextThrowTime) return;
        
        float cooldown = GetWeaponCooldown(_primaryWeaponPrefab);
        _lastPrimaryCooldown = cooldown;
        _primaryNextThrowTime = Time.time + cooldown;

        PlayThrowSound(_primaryThrowClip, _primaryThrowVolume);
        ThrowWeapon(_primaryWeaponPrefab);
    }

    /// <summary>
    /// Throws the weapon assigned to the Secondary slot.
    /// </summary>
    public void ThrowSecondary()
    {
        if (Time.time < _secondaryNextThrowTime) return;

        float cooldown = GetWeaponCooldown(_secondaryWeaponPrefab);
        _lastSecondaryCooldown = cooldown;
        _secondaryNextThrowTime = Time.time + cooldown;

        PlayThrowSound(_secondaryThrowClip, _secondaryThrowVolume);
        ThrowWeapon(_secondaryWeaponPrefab);
    }

    private void PlayThrowSound(AudioClip clip, float volume)
    {
        if (_audioSource == null || clip == null) return;
        _audioSource.PlayOneShot(clip, volume);
    }

    private float GetWeaponCooldown(GameObject prefab)
    {
        if (prefab == null) return _defaultThrowCooldown;
        IDamageSource ds = prefab.GetComponent<IDamageSource>();
        return (ds != null) ? ds.ThrowCooldown : _defaultThrowCooldown;
    }

    private void ThrowWeapon(GameObject weaponPrefab)
    {
        if (weaponPrefab == null)
        {
            Debug.LogWarning($"[ThrowingSystem] No prefab assigned for this weapon slot on {gameObject.name}");
            return;
        }

        GameObject projectile = Instantiate(weaponPrefab, _throwPoint.position, _throwPoint.rotation);

        // Emit noise for AI detection
        NoiseSystem.EmitNoise(_throwPoint.position, _throwNoiseRadius);

        // --- PREVENTION: Do not hit the player ---
        Collider projectileCollider = projectile.GetComponent<Collider>();
        if (projectileCollider != null && _playerCollider != null)
        {
            Physics.IgnoreCollision(projectileCollider, _playerCollider);
        }

        // Manage active projectiles list
        _activeProjectiles.Enqueue(projectile);

        // If we exceed the limit, destroy the oldest one
        if (_activeProjectiles.Count > _maxActiveProjectiles)
        {
            GameObject oldest = _activeProjectiles.Dequeue();
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
    }
    #endregion
}
