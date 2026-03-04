using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Scales and positions a mesh (typically a cylinder) between two points.
/// Follows the Taut Cable Standards: Midpoint for position, LookAt for rotation, Distance/2 for Z-scale.
/// </summary>
public class StretchedMeshLink : MonoBehaviour
{
    #region Serialized Fields
    [Header("References")]
    [FormerlySerializedAs("startPoint")]
    [SerializeField] private Transform _startPoint;
    
    [FormerlySerializedAs("endPoint")]
    [SerializeField] private Transform _endPoint;
    #endregion

    #region Unity Lifecycle
    void LateUpdate()
    {
        if (_startPoint == null || _endPoint == null) return;

        UpdateTransform();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Programmatically sets the points for the link.
    /// </summary>
    public void SetPoints(Transform start, Transform end)
    {
        _startPoint = start;
        _endPoint = end;
    }
    #endregion

    #region Private Methods
    private void UpdateTransform()
    {
        Vector3 posA = _startPoint.position;
        Vector3 posB = _endPoint.position;

        // 1. Midpoint Formula for position
        transform.position = (posA + posB) / 2f;

        // 2. LookAt for rotation
        transform.LookAt(posB);

        // 3. Vector3.Distance / 2 for the Z-scale
        float distance = Vector3.Distance(posA, posB);
        Vector3 currentScale = transform.localScale;
        
        // We set the Z scale specifically as requested. 
        // Note: For a standard Unity Cylinder, we might need a 90-degree rotation 
        // on the child mesh so its height (Y) aligns with this Z axis.
        currentScale.z = distance / 2f;
        transform.localScale = currentScale;
    }
    #endregion
}
