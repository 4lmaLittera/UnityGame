using UnityEngine;

public class GameSettings : MonoBehaviour
{
    [Header("Frame Rate Settings")]
    [SerializeField] private int _targetFrameRate = 60;
    [SerializeField] private bool _disableVSync = true;

    void Awake()
    {
        // For targetFrameRate to work, VSync must be disabled (0)
        if (_disableVSync)
        {
            QualitySettings.vSyncCount = 0;
        }

        Application.targetFrameRate = _targetFrameRate;
        
        Debug.Log($"[GameSettings] Target Frame Rate set to: {_targetFrameRate}");
    }
}
