
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerMotor _motor;
    private PlayerMovementAbilities _abilities;
    private Vector2 _moveInput;

    void Awake()
    {
        // Link to our other components on the same object
        _motor = GetComponent<PlayerMotor>();
        _abilities = GetComponent<PlayerMovementAbilities>();
    }

    // Action Name: "Move" in Input Action Asset
    public void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>();
    }

    // Action Name: "Jump" in Input Action Asset
    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            _abilities.ExecuteJump();
        }
    }

    void FixedUpdate()
    {
        // Send the stored move data to the Motor every physics frame
        _motor.ProcessMove(_moveInput);
    }
}