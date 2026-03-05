using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DaggerMeshGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Cool Dagger Mesh")]
    public static void GenerateDaggerMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "DaggerMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // We will build the dagger facing forward along the Z axis.
        // The handle will be slightly negative Z, crossguard around 0, blade extending positive Z.

        float bladeLength = 0.5f;
        float bladeWidth = 0.08f;
        float bladeThickness = 0.02f;
        
        float handleLength = 0.15f;
        float handleRadius = 0.02f;
        
        float guardWidth = 0.2f;
        float guardLength = 0.02f;
        float guardThickness = 0.03f;

        // --- 1. Generate Blade ---
        int bladeStart = vertices.Count;
        // Base of blade (at crossguard)
        vertices.Add(new Vector3(-bladeWidth/2, 0, 0)); // 0: Left
        vertices.Add(new Vector3(0, bladeThickness/2, 0)); // 1: Top
        vertices.Add(new Vector3(bladeWidth/2, 0, 0)); // 2: Right
        vertices.Add(new Vector3(0, -bladeThickness/2, 0)); // 3: Bottom
        
        // Mid of blade (thickest part before point)
        float midZ = bladeLength * 0.7f;
        vertices.Add(new Vector3(-bladeWidth/2, 0, midZ)); // 4: Left Mid
        vertices.Add(new Vector3(0, bladeThickness/2, midZ)); // 5: Top Mid
        vertices.Add(new Vector3(bladeWidth/2, 0, midZ)); // 6: Right Mid
        vertices.Add(new Vector3(0, -bladeThickness/2, midZ)); // 7: Bottom Mid
        
        // Tip of blade
        vertices.Add(new Vector3(0, 0, bladeLength)); // 8: Tip
        
        // Blade triangles
        // Base to Mid
        AddQuad(triangles, bladeStart+0, bladeStart+1, bladeStart+5, bladeStart+4); // Top-left
        AddQuad(triangles, bladeStart+1, bladeStart+2, bladeStart+6, bladeStart+5); // Top-right
        AddQuad(triangles, bladeStart+2, bladeStart+3, bladeStart+7, bladeStart+6); // Bottom-right
        AddQuad(triangles, bladeStart+3, bladeStart+0, bladeStart+4, bladeStart+7); // Bottom-left
        
        // Mid to Tip
        AddTriangle(triangles, bladeStart+4, bladeStart+5, bladeStart+8); // Top-left tip
        AddTriangle(triangles, bladeStart+5, bladeStart+6, bladeStart+8); // Top-right tip
        AddTriangle(triangles, bladeStart+6, bladeStart+7, bladeStart+8); // Bottom-right tip
        AddTriangle(triangles, bladeStart+7, bladeStart+4, bladeStart+8); // Bottom-left tip

        // --- 2. Generate Crossguard ---
        int guardStart = vertices.Count;
        float gzMin = -guardLength;
        float gzMax = 0;
        
        // We'll just make a simple box for the crossguard
        vertices.Add(new Vector3(-guardWidth/2, guardThickness/2, gzMin)); // 0: Top-Left-Back
        vertices.Add(new Vector3(guardWidth/2, guardThickness/2, gzMin));  // 1: Top-Right-Back
        vertices.Add(new Vector3(-guardWidth/2, guardThickness/2, gzMax)); // 2: Top-Left-Front
        vertices.Add(new Vector3(guardWidth/2, guardThickness/2, gzMax));  // 3: Top-Right-Front
        vertices.Add(new Vector3(-guardWidth/2, -guardThickness/2, gzMin));// 4: Bot-Left-Back
        vertices.Add(new Vector3(guardWidth/2, -guardThickness/2, gzMin)); // 5: Bot-Right-Back
        vertices.Add(new Vector3(-guardWidth/2, -guardThickness/2, gzMax));// 6: Bot-Left-Front
        vertices.Add(new Vector3(guardWidth/2, -guardThickness/2, gzMax)); // 7: Bot-Right-Front
        
        // Top
        AddQuad(triangles, guardStart+0, guardStart+2, guardStart+3, guardStart+1);
        // Bottom
        AddQuad(triangles, guardStart+4, guardStart+5, guardStart+7, guardStart+6);
        // Front
        AddQuad(triangles, guardStart+2, guardStart+6, guardStart+7, guardStart+3);
        // Back
        AddQuad(triangles, guardStart+0, guardStart+1, guardStart+5, guardStart+4);
        // Left
        AddQuad(triangles, guardStart+0, guardStart+4, guardStart+6, guardStart+2);
        // Right
        AddQuad(triangles, guardStart+1, guardStart+3, guardStart+7, guardStart+5);

        // --- 3. Generate Handle ---
        int handleStart = vertices.Count;
        int segments = 8;
        float hMinZ = -guardLength - handleLength;
        float hMaxZ = -guardLength;
        
        // Handle Top ring
        for(int i = 0; i < segments; i++) {
            float angle = i * Mathf.PI * 2f / segments;
            vertices.Add(new Vector3(Mathf.Cos(angle)*handleRadius, Mathf.Sin(angle)*handleRadius, hMaxZ));
        }
        // Handle Bottom ring
        for(int i = 0; i < segments; i++) {
            float angle = i * Mathf.PI * 2f / segments;
            vertices.Add(new Vector3(Mathf.Cos(angle)*handleRadius, Mathf.Sin(angle)*handleRadius, hMinZ));
        }
        
        // Handle sides
        for(int i = 0; i < segments; i++) {
            int next = (i + 1) % segments;
            AddQuad(triangles, handleStart + i, handleStart + i + segments, handleStart + next + segments, handleStart + next);
        }
        
        // Handle bottom cap (pommel)
        int pommelIndex = vertices.Count;
        vertices.Add(new Vector3(0, 0, hMinZ - 0.02f)); // Pommel tip
        for(int i = 0; i < segments; i++) {
            int next = (i + 1) % segments;
            AddTriangle(triangles, handleStart + segments + i, pommelIndex, handleStart + segments + next);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Models"))
        {
            AssetDatabase.CreateFolder("Assets", "Models");
        }

        string path = "Assets/Models/DaggerMesh.asset";
        Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        
        if (existingMesh != null)
        {
            existingMesh.Clear();
            existingMesh.vertices = mesh.vertices;
            existingMesh.triangles = mesh.triangles;
            existingMesh.normals = mesh.normals;
            existingMesh.bounds = mesh.bounds;
            EditorUtility.SetDirty(existingMesh);
        }
        else
        {
            AssetDatabase.CreateAsset(mesh, path);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Cool Dagger Mesh generated at " + path);
    }
    
    private static void AddTriangle(List<int> triangles, int v0, int v1, int v2)
    {
        triangles.Add(v0);
        triangles.Add(v1);
        triangles.Add(v2);
    }
    
    private static void AddQuad(List<int> triangles, int v0, int v1, int v2, int v3)
    {
        // Two triangles for a quad
        triangles.Add(v0);
        triangles.Add(v1);
        triangles.Add(v2);
        
        triangles.Add(v0);
        triangles.Add(v2);
        triangles.Add(v3);
    }
}