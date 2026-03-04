using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerMotor _motor;
    private PlayerMovementAbilities _abilities;
    private GrapplingHook _grapplingHook;
    private Vector2 _moveInput;

    void Awake()
    {
        _motor = GetComponent<PlayerMotor>();
        _abilities = GetComponent<PlayerMovementAbilities>();
        _grapplingHook = GetComponent<GrapplingHook>();
    }

    public void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            _abilities.ExecuteJump();
            _grapplingHook.StopGrapple();
        }
    }

    public void OnGrapple(InputValue value)
    {
        if (value.isPressed)
        {
            _grapplingHook.StartGrapple();
        }
        else
        {
            _grapplingHook.StopGrapple();
        }
    }

    void FixedUpdate()
    {
        _motor.ProcessMove(_moveInput);
    }
}