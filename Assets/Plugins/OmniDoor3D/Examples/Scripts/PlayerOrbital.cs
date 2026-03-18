namespace ABCodeworld.Examples
{
    using ABCodeworld.OmniDoor3D;
    using UnityEngine;
    using UnityEngine.InputSystem;

    public class PlayerOrbital : MonoBehaviour
    {
        public float moveSpeed = 4f;  // Movement speed of the player
        public float cameraDistance = 12f; // Distance of the camera from the player
        public float cameraHeight = 5f; // Height of the camera above the player
        public float orbitSensitivity = 0.33f; // Sensitivity of the camera orbiting
        public float minYAngle = -15f; // Minimum vertical angle of the camera
        public float maxYAngle = 70f; // Maximum vertical angle of the camera
        public float cameraSmoothTime = 0.5f; // Smooth time for camera movement

        public OmniDoor3DController doorController { get { return _doorController; } set { _doorController = value; } } // Public property to be set by interactives

        private Camera camMain;
        private CharacterController controller;
        private float yaw = 0f, pitch = 20f;
        private OmniDoor3DController _doorController;
        private Vector2 lookAxes, moveAxes;
        private Vector3 currentCamPos, camVelocity;

        private void Start()
        {
            camMain = Camera.main;
            controller = GetComponent<CharacterController>();
            currentCamPos = camMain.transform.position;
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        // InputSystem event
        private void OnMove(InputValue iv)
        {
            moveAxes = iv.Get<Vector2>().normalized;
        }

        // InputSystem event
        private void OnLook(InputValue iv)
        {
            lookAxes = iv.Get<Vector2>();
        }

        // InputSystem event
        private void OnJump() // This is merely the default name of the Input Action for the space bar. It is not used for jumping here.
        {
            if (_doorController != null)
                _doorController.ToggleDoor?.Invoke();
        }

        private void Update()
        {
            if (controller == null)
                return;

            // Orbital camera movement

            if (Mouse.current.rightButton.isPressed && lookAxes != Vector2.zero)
            {
                yaw += lookAxes.x * orbitSensitivity;
                pitch -= lookAxes.y * orbitSensitivity;
                pitch = Mathf.Clamp(pitch, minYAngle, maxYAngle);
            }

            Vector3 camOffset = Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0, 0, -cameraDistance) + Vector3.up * cameraHeight;

            if (camOffset != Vector3.zero)
            {
                currentCamPos = Vector3.SmoothDamp(currentCamPos, transform.position + camOffset, ref camVelocity, cameraSmoothTime);
                camMain.transform.position = currentCamPos;
                camMain.transform.LookAt(transform.position + Vector3.up * cameraHeight);
            }

            // Player movement
            if (moveAxes != Vector2.zero)
            {
                Vector3 camForward = new(camMain.transform.forward.x, 0f, camMain.transform.forward.z);
                Vector3 camRight = new(camMain.transform.right.x, 0f, camMain.transform.right.z);
                Vector3 move = moveSpeed * (moveAxes.x * camRight.normalized + moveAxes.y * camForward.normalized);
                controller.Move(move * Time.deltaTime);
            }
        }
    }
}