using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    #region Private Fields
    private PlayerMotor _motor;
    private PlayerMovementAbilities _abilities;
    private GrapplingHook _grapplingHook;
    private ThrowingSystem _throwingSystem;
    private PlayerHealth _health;
    private Vector2 _moveInput;
    private bool _isDead = false;
    #endregion

    #region Properties
    public Vector2 MoveInput => _moveInput;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        _motor = GetComponent<PlayerMotor>();
        _abilities = GetComponent<PlayerMovementAbilities>();
        _grapplingHook = GetComponent<GrapplingHook>();
        _throwingSystem = GetComponent<ThrowingSystem>();
        _health = GetComponent<PlayerHealth>();

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

    void FixedUpdate()
    {
        if (_isDead) return;
        _motor.ProcessMove(_moveInput);
    }
    #endregion

    #region Input Handlers
    public void OnMove(InputValue value)
    {
        if (_isDead)
        {
            _moveInput = Vector2.zero;
            return;
        }
        _moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (_isDead) return;
        if (value.isPressed)
        {
            _abilities.ExecuteJump();
            _grapplingHook.StopGrapple();
        }
    }

    public void OnGrapple(InputValue value)
    {
        if (_isDead) return;
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
        if (_isDead) return;
        if (value.isPressed && _throwingSystem != null)
        {
            _throwingSystem.ThrowPrimary();
        }
    }

    public void OnSecondaryAttack(InputValue value)
    {
        if (_isDead) return;
        if (value.isPressed && _throwingSystem != null)
        {
            _throwingSystem.ThrowSecondary();
        }
    }
    #endregion

    #region Private Methods
    private void HandlePlayerDeath()
    {
        _isDead = true;
        _moveInput = Vector2.zero;
        
        // Stop any active abilities
        if (_grapplingHook != null) _grapplingHook.StopGrapple();
    }
    #endregion
}
