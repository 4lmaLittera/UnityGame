using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    #region Private Fields
    private PlayerMotor _motor;
    private PlayerMovementAbilities _abilities;
    private GrapplingHook _grapplingHook;
    private ThrowingSystem _throwingSystem;
    private Vector2 _moveInput;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _motor = GetComponent<PlayerMotor>();
        _abilities = GetComponent<PlayerMovementAbilities>();
        _grapplingHook = GetComponent<GrapplingHook>();
        _throwingSystem = GetComponent<ThrowingSystem>();
    }

    void FixedUpdate()
    {
        _motor.ProcessMove(_moveInput);
    }
    #endregion

    #region Input Handlers
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

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && _throwingSystem != null)
        {
            _throwingSystem.ThrowPrimary();
        }
    }

    public void OnSecondaryAttack(InputValue value)
    {
        if (value.isPressed && _throwingSystem != null)
        {
            _throwingSystem.ThrowSecondary();
        }
    }
    #endregion
}
