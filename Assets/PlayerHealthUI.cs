using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connects the PlayerHealth logic to the UI elements without tight coupling.
/// Subscribe to PlayerHealth events and update a UI Slider or Image.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    #region Serialized Fields
    [Header("Dependencies")]
    [SerializeField] private PlayerHealth _playerHealth;
    
    [Header("UI Elements")]
    [Tooltip("A UI Slider to represent health.")]
    [SerializeField] private Slider _healthSlider;
    
    [Tooltip("A UI Image to represent health as a fill amount.")]
    [SerializeField] private Image _healthFillImage;
    
    [Header("Settings")]
    [Tooltip("If true, the UI will update smoothly with a Lerp.")]
    [SerializeField] private bool _smoothUpdate = true;
    [SerializeField] private float _smoothSpeed = 5f;
    #endregion

    #region Private Fields
    private float _targetFillAmount = 1f;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Automatically try to find PlayerHealth on the Player Root if not assigned.
        if (_playerHealth == null)
        {
            _playerHealth = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerHealth>();
        }

        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged += HandleHealthChanged;
        }
    }

    private void Update()
    {
        if (!_smoothUpdate) return;

        UpdateUI(Time.deltaTime * _smoothSpeed);
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }
    #endregion

    #region Event Handlers
    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        _targetFillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        
        if (!_smoothUpdate)
        {
            UpdateUI(1.0f); // Instant update
        }
    }
    #endregion

    #region Private Methods
    private void UpdateUI(float lerpFactor)
    {
        if (_healthSlider != null)
        {
            _healthSlider.value = _smoothUpdate 
                ? Mathf.Lerp(_healthSlider.value, _targetFillAmount, lerpFactor)
                : _targetFillAmount;
        }

        if (_healthFillImage != null)
        {
            _healthFillImage.fillAmount = _smoothUpdate 
                ? Mathf.Lerp(_healthFillImage.fillAmount, _targetFillAmount, lerpFactor)
                : _targetFillAmount;
        }
    }
    #endregion
}
