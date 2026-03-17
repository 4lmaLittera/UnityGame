using System;
using UnityEngine;

/// <summary>
/// Manages the player's health, damage reception, and death state.
/// Follows the 3-Tier Hierarchy: Attached to the Player Root.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    #region Events
    /// <summary>
    /// Invoked when health changes. Parameters: currentHealth, maxHealth.
    /// </summary>
    public event Action<float, float> OnHealthChanged;
    
    /// <summary>
    /// Invoked when the player's health reaches zero.
    /// </summary>
    public event Action OnPlayerDeath;
    #endregion

    #region Serialized Fields
    [Header("Health Settings")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private bool _invulnerable = false;

    [Header("Feedback")]
    [Tooltip("Global multiplier for knockback forces applied to the player.")]
    [SerializeField] private float _knockbackMultiplier = 1.0f;
    #endregion

    #region Private Fields
    private float _currentHealth;
    private bool _isDead = false;
    private PlayerMotor _motor;
    #endregion

    #region Properties
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public bool IsDead => _isDead;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _currentHealth = _maxHealth;
        _motor = GetComponent<PlayerMotor>();
    }

    private void Start()
    {
        // Initial event call to sync UI
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Reduces player health and triggers death if health <= 0.
    /// </summary>
    /// <param name="amount">Damage amount.</param>
    /// <param name="impactForce">Optional force vector (for feedback/death animation).</param>
    public void TakeDamage(float amount, Vector3 impactForce = default)
    {
        if (_isDead || _invulnerable) return;

        _currentHealth -= amount;
        _currentHealth = Mathf.Max(_currentHealth, 0);
        
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

        // Apply knockback if force is provided and player is still alive
        if (_currentHealth > 0 && impactForce != Vector3.zero && _motor != null)
        {
            _motor.ApplyKnockback(impactForce * _knockbackMultiplier);
        }

        if (_currentHealth <= 0)
        {
            Die(impactForce);
        }
    }

    /// <summary>
    /// Restores player health.
    /// </summary>
    /// <param name="amount">Amount to heal.</param>
    public void Heal(float amount)
    {
        if (_isDead) return;

        _currentHealth += amount;
        _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
        
        OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
    }
    #endregion

    #region Private Methods
    private void Die(Vector3 impactForce)
    {
        _isDead = true;
        OnPlayerDeath?.Invoke();
        
        Debug.Log("<color=red>Player has died!</color>");
        
        // Additional death logic (e.g., sound, ragdoll trigger) can be added here or in a separate script listening to OnPlayerDeath.
    }
    #endregion
}
