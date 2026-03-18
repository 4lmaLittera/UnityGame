using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    #region Serialized Fields
    [Header("Settings")]
    [SerializeField] private float _sensitivity = 15f; // Tuned for better control
    [SerializeField] private Transform _playerBody;
    #endregion

    #region Private Fields
    private float _xRotation = 0f;
    private float _yRotation = 0f;
    private Vector2 _lookInput;
    private Rigidbody _playerRb;
    private PlayerHealth _health;
    private bool _isDead = false;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        if (_playerBody != null)
        {
            _playerRb = _playerBody.GetComponent<Rigidbody>();
            _health = _playerBody.GetComponent<PlayerHealth>();
        }

        if (_health != null)
        {
            _health.OnPlayerDeath += HandlePlayerDeath;
        }
    }

    void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnPlayerDeath -= HandlePlayerDeath;
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        // Initialize rotations from current orientation
        _xRotation = transform.localEulerAngles.x;
        if (_playerBody != null)
        {
            _yRotation = _playerBody.eulerAngles.y;
        }
    }

    void Update()
    {
        if (_isDead) return;

        // 1. Process Input with deltaTime
        float mouseX = _lookInput.x * _sensitivity * Time.deltaTime;
        float mouseY = _lookInput.y * _sensitivity * Time.deltaTime;

        // 2. Vertical (Look up/down) - Direct Transform Manipulation
        // This is smooth because the CameraHolder is usually not a Rigidbody.
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        // 3. Horizontal (Look left/right) - Calculate target rotation
        _yRotation += mouseX;

        // 4. Apply to Player Body using MoveRotation for Interpolation support
        if (_playerRb != null)
        {
            // MoveRotation is the only way to get smooth results with an Interpolated Rigidbody
            _playerRb.MoveRotation(Quaternion.Euler(0f, _yRotation, 0f));
        }
        else if (_playerBody != null)
        {
            _playerBody.rotation = Quaternion.Euler(0f, _yRotation, 0f);
        }
    }
    #endregion

    #region Input Handlers
    public void OnLook(InputValue value)
    {
        if (_isDead)
        {
            _lookInput = Vector2.zero;
            return;
        }
        _lookInput = value.Get<Vector2>();
    }
    #endregion

    #region Private Methods
    private void HandlePlayerDeath()
    {
        _isDead = true;
        _lookInput = Vector2.zero;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    #endregion
}
