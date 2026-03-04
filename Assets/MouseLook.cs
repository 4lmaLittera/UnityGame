using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Settings")]
    public float sensitivity = 10f;
    public Transform playerBody;

    private float _xRotation = 0f;
    private Vector2 _lookInput;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Event triggered by PlayerInput (Send Messages).
    /// </summary>
    public void OnLook(InputValue value)
    {
        _lookInput += value.Get<Vector2>();
    }

    void Update()
    {
        // 1. Process the accumulated input
        // Note: We do NOT use Time.deltaTime for Mouse Delta in the new Input System
        // because the delta is already 'distance moved since last frame'.
        float mouseX = _lookInput.x * sensitivity * 0.05f;
        float mouseY = _lookInput.y * sensitivity * 0.05f;

        // 2. Vertical (Look up/down) - Rotates the CameraHolder
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        // 3. Horizontal (Look left/right) - Rotates the entire Player Body
        playerBody.Rotate(Vector3.up * mouseX);

        // 4. Reset accumulated input for the next frame
        _lookInput = Vector2.zero;
    }
}