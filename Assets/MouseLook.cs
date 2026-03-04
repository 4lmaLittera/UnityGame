using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    public float sensitivity = 100f;
    public Transform playerBody;
    private float xRotation = 0f;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void OnLook(InputValue value)
    {
        Vector2 mouseDelta = value.Get<Vector2>();
        
        // DEBUG: Uncomment the line below to see if the mouse is working
        //Debug.Log("Mouse Moving: " + mouseDelta);

        float mouseX = mouseDelta.x * sensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * sensitivity * Time.deltaTime;

        // 1. Vertical (Look up/down) - Rotates this object (the Holder)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 2. Horizontal (Look left/right) - Rotates the entire Player Body
        playerBody.Rotate(Vector3.up * mouseX);
    }
}