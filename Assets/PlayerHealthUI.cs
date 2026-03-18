using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connects the PlayerHealth logic to the UI elements.
/// Supports a discrete Heart Image array, as well as optional Sliders/Fill Images.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    #region Serialized Fields
    [Header("Dependencies")]
    [SerializeField] private PlayerHealth _playerHealth;
    
    [Header("Heart UI Elements")]
    [Tooltip("Array of UI Images representing discrete health chunks (e.g., 3 hearts).")]
    [SerializeField] private Image[] _heartImages;
    [Tooltip("Sprite for a full heart.")]
    [SerializeField] private Sprite _fullHeartSprite;
    [Tooltip("Sprite for an empty heart.")]
    [SerializeField] private Sprite _emptyHeartSprite;

    [Header("Continuous UI Elements (Optional)")]
    [Tooltip("A UI Slider to represent health.")]
    [SerializeField] private Slider _healthSlider;
    [Tooltip("A UI Image to represent health as a fill amount.")]
    [SerializeField] private Image _healthFillImage;
    
    [Header("Settings")]
    [Tooltip("If true, the continuous UI will update smoothly with a Lerp.")]
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

        UpdateContinuousUI(Time.deltaTime * _smoothSpeed);
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
        
        UpdateHeartUI(currentHealth, maxHealth);

        if (!_smoothUpdate)
        {
            UpdateContinuousUI(1.0f); // Instant update
        }
    }
    #endregion

    #region Private Methods
    private void UpdateHeartUI(float currentHealth, float maxHealth)
    {
        if (_heartImages == null || _heartImages.Length == 0) return;

        // Calculate how much health a single heart represents
        float healthPerHeart = maxHealth / _heartImages.Length;
        
        for (int i = 0; i < _heartImages.Length; i++)
        {
            if (_heartImages[i] == null) continue;

            // If the image is set to "Filled", we can do partial heart depletion (Zelda style)
            if (_heartImages[i].type == Image.Type.Filled)
            {
                float heartHealthThreshold = i * healthPerHeart;
                float heartFill = (currentHealth - heartHealthThreshold) / healthPerHeart;
                _heartImages[i].fillAmount = Mathf.Clamp01(heartFill);
            }
            else
            {
                // Otherwise, do standard full/empty sprite swapping
                // We consider the heart "full" if the current health is greater than the base threshold of this heart
                bool isHeartFull = currentHealth > (i * healthPerHeart) + (healthPerHeart * 0.01f); // Epsilon added for floating point safety

                if (isHeartFull)
                {
                    if (_fullHeartSprite != null) _heartImages[i].sprite = _fullHeartSprite;
                    _heartImages[i].enabled = true;
                }
                else
                {
                    if (_emptyHeartSprite != null) 
                    {
                        _heartImages[i].sprite = _emptyHeartSprite;
                        _heartImages[i].enabled = true;
                    }
                    else 
                    {
                        // Fallback: If no empty sprite is provided, just hide the heart image entirely
                        _heartImages[i].enabled = false;
                    }
                }
            }
        }
    }

    private void UpdateContinuousUI(float lerpFactor)
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
