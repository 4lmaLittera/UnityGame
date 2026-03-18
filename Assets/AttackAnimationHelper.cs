using UnityEngine;

/// <summary>
/// Attach this to the Root of the enemy (where the Animator is).
/// Receives Animation Events to toggle the DamageTrigger collider on and off.
/// </summary>
public class AttackAnimationHelper : MonoBehaviour
{
    #region Serialized Fields
    [Header("Dependencies")]
    [Tooltip("The Collider attached to the enemy's attack bone (e.g., hand or weapon).")]
    [SerializeField] private Collider _attackCollider;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Ensure the collider starts disabled so it doesn't deal damage while walking
        if (_attackCollider != null)
        {
            _attackCollider.enabled = false;
        }
    }
    #endregion

    #region Animation Events
    /// <summary>
    /// Called via Animation Event at the exact frame the attack swing begins.
    /// </summary>
    public void StartAttack()
    {
        if (_attackCollider != null) 
        {
            _attackCollider.enabled = true;
        }
    }

    /// <summary>
    /// Called via Animation Event at the exact frame the attack swing ends.
    /// </summary>
    public void EndAttack()
    {
        if (_attackCollider != null) 
        {
            _attackCollider.enabled = false;
        }
    }
    #endregion
}
