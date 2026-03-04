using UnityEngine;

/// <summary>
/// Scales and positions a mesh (typically a cylinder) between two points.
/// Follows the Taut Cable Standards: Midpoint for position, LookAt for rotation, Distance/2 for Z-scale.
/// </summary>
public class StretchedMeshLink : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    /// <summary>
    /// Programmatically sets the points for the link.
    /// </summary>
    public void SetPoints(Transform start, Transform end)
    {
        startPoint = start;
        endPoint = end;
    }

    void LateUpdate()
    {
        if (startPoint == null || endPoint == null) return;

        UpdateTransform();
    }

    private void UpdateTransform()
    {
        Vector3 posA = startPoint.position;
        Vector3 posB = endPoint.position;

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
}
