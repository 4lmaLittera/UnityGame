using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class MouseLook : MonoBehaviour
{
    #region Serialized Fields
    [Header("Settings")]
    [FormerlySerializedAs("sensitivity")]
    [SerializeField] private float _sensitivity = 10f;
    
    [FormerlySerializedAs("playerBody")]
    [SerializeField] private Transform _playerBody;
    #endregion

    #region Private Fields
    private float _xRotation = 0f;
    private Vector2 _lookInput;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // 1. Process the accumulated input
        // Note: We do NOT use Time.deltaTime for Mouse Delta in the new Input System
        // because the delta is already 'distance moved since last frame'.
        float mouseX = _lookInput.x * _sensitivity * 0.05f;
        float mouseY = _lookInput.y * _sensitivity * 0.05f;

        // 2. Vertical (Look up/down) - Rotates the CameraHolder
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        // 3. Horizontal (Look left/right) - Rotates the entire Player Body
        if (_playerBody != null)
        {
            _playerBody.Rotate(Vector3.up * mouseX);
        }

        // 4. Reset accumulated input for the next frame
        _lookInput = Vector2.zero;
    }
    #endregion

    #region Input Handlers
    /// <summary>
    /// Event triggered by PlayerInput (Send Messages).
    /// </summary>
    public void OnLook(InputValue value)
    {
        _lookInput += value.Get<Vector2>();
    }
    #endregion
}
