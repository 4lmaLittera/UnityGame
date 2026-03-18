using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Handles transitions and logic when the player dies.
/// </summary>
public class PlayerDeathManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string _endSceneName = "End game";
    [SerializeField] private float _delayBeforeTransition = 2.0f;

    private PlayerHealth _health;

    private void Awake()
    {
        _health = GetComponent<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.OnPlayerDeath += HandlePlayerDeath;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.OnPlayerDeath -= HandlePlayerDeath;
        }
    }

    private void HandlePlayerDeath()
    {
        StartCoroutine(TransitionToEndScene());
    }

    private IEnumerator TransitionToEndScene()
    {
        yield return new WaitForSeconds(_delayBeforeTransition);
        
        // Ensure cursor is visible before transitioning
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        SceneManager.LoadScene(_endSceneName);
    }
}
