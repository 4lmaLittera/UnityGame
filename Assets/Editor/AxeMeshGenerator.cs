using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AxeMeshGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Axe Mesh")]
    public static void GenerateAxeMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "AxeMesh";

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        float handleLength = 1.0f;
        float handleRadius = 0.03f;
        int handleSegments = 8;

        float headWidth = 0.3f;
        float headHeight = 0.4f;
        float headThickness = 0.05f;

        // --- 1. Generate Handle ---
        int handleStart = vertices.Count;
        
        // Handle Top ring
        for(int i = 0; i < handleSegments; i++) {
            float angle = i * Mathf.PI * 2f / handleSegments;
            vertices.Add(new Vector3(Mathf.Cos(angle)*handleRadius, handleLength, Mathf.Sin(angle)*handleRadius));
        }
        // Handle Bottom ring
        for(int i = 0; i < handleSegments; i++) {
            float angle = i * Mathf.PI * 2f / handleSegments;
            vertices.Add(new Vector3(Mathf.Cos(angle)*handleRadius, 0, Mathf.Sin(angle)*handleRadius));
        }
        
        // Handle sides
        for(int i = 0; i < handleSegments; i++) {
            int next = (i + 1) % handleSegments;
            AddQuad(triangles, handleStart + i, handleStart + i + handleSegments, handleStart + next + handleSegments, handleStart + next);
        }

        // --- 2. Generate Axe Head ---
        int headStart = vertices.Count;
        float headYOffset = handleLength - 0.2f; // Position head near the top

        // Base of head (attached to handle), now with thickness on X, sitting at Z=0
        vertices.Add(new Vector3(-headThickness/2, headYOffset - headHeight/4, 0)); // 0: Bottom-Left-Back
        vertices.Add(new Vector3(headThickness/2, headYOffset - headHeight/4, 0));  // 1: Bottom-Right-Back
        vertices.Add(new Vector3(-headThickness/2, headYOffset + headHeight/4, 0)); // 2: Top-Left-Back
        vertices.Add(new Vector3(headThickness/2, headYOffset + headHeight/4, 0));  // 3: Top-Right-Back

        // Edge of blade (sharp part) extending to +Z
        vertices.Add(new Vector3(0, headYOffset - headHeight/2, headWidth)); // 4: Bottom Edge (Front)
        vertices.Add(new Vector3(0, headYOffset + headHeight/2, headWidth)); // 5: Top Edge (Front)

        // Left Face
        AddTriangle(triangles, headStart + 0, headStart + 2, headStart + 4);
        AddTriangle(triangles, headStart + 2, headStart + 5, headStart + 4);
        
        // Right Face
        AddTriangle(triangles, headStart + 1, headStart + 4, headStart + 3);
        AddTriangle(triangles, headStart + 3, headStart + 4, headStart + 5);

        // Top Face
        AddTriangle(triangles, headStart + 2, headStart + 3, headStart + 5);

        // Bottom Face
        AddTriangle(triangles, headStart + 0, headStart + 4, headStart + 1);

        // Back Face (the part flush with the handle)
        AddQuad(triangles, headStart + 0, headStart + 1, headStart + 3, headStart + 2);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (!AssetDatabase.IsValidFolder("Assets/Models"))
        {
            AssetDatabase.CreateFolder("Assets", "Models");
        }

        string path = "Assets/Models/AxeMesh.asset";
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
        
        Debug.Log("Axe Mesh generated at " + path);
    }

    private static void AddTriangle(List<int> triangles, int v0, int v1, int v2)
    {
        triangles.Add(v0);
        triangles.Add(v1);
        triangles.Add(v2);
    }
    
    private static void AddQuad(List<int> triangles, int v0, int v1, int v2, int v3)
    {
        triangles.Add(v0);
        triangles.Add(v1);
        triangles.Add(v2);
        
        triangles.Add(v0);
        triangles.Add(v2);
        triangles.Add(v3);
    }
}