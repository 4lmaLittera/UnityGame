using UnityEditor;
using UnityEngine;

public static class PhysicsMatrixSetup
{
    [MenuItem("Tools/Setup Physics Matrix")]
    public static void Setup()
    {
        // Layer 8: Player
        // Layer 9: Projectile
        Physics.IgnoreLayerCollision(8, 9, true);
        Debug.Log("Physics Matrix Updated: Player (Layer 8) will now ignore Projectile (Layer 9).");
    }
}
