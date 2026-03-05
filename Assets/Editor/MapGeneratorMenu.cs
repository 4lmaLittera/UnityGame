using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

public class MapGeneratorMenu : EditorWindow
{
    [MenuItem("Tools/Generate Simple Map")]
    public static void GenerateMap()
    {
        // Prevent duplicate generation
        if (GameObject.Find("GeneratedMapEnvironment") != null)
        {
            Debug.LogWarning("Map already exists! Delete the 'GeneratedMapEnvironment' GameObject if you want to regenerate it.");
            return;
        }

        GameObject mapRoot = new GameObject("GeneratedMapEnvironment");

        // 1. Create a large floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(5, 1, 5); // 50x50 units
        floor.transform.parent = mapRoot.transform;
        
        GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);

        // 2. Create boundary walls
        CreateWall(new Vector3(0, 2.5f, 25), new Vector3(50, 5, 1), mapRoot.transform);
        CreateWall(new Vector3(0, 2.5f, -25), new Vector3(50, 5, 1), mapRoot.transform);
        CreateWall(new Vector3(25, 2.5f, 0), new Vector3(1, 5, 50), mapRoot.transform);
        CreateWall(new Vector3(-25, 2.5f, 0), new Vector3(1, 5, 50), mapRoot.transform);

        // 3. Create some cover/obstacles
        CreateObstacle(new Vector3(10, 1.5f, 10), new Vector3(3, 3, 3), mapRoot.transform);
        CreateObstacle(new Vector3(-10, 1.5f, -10), new Vector3(3, 3, 3), mapRoot.transform);
        CreateObstacle(new Vector3(10, 1.5f, -10), new Vector3(3, 3, 3), mapRoot.transform);
        CreateObstacle(new Vector3(-10, 1.5f, 10), new Vector3(3, 3, 3), mapRoot.transform);
        CreateObstacle(new Vector3(0, 1.5f, 0), new Vector3(4, 3, 4), mapRoot.transform); // Center piece

        // 4. Spawn Enemies
        // Find the existing enemy in the scene to clone it
        EnemyMovement existingEnemy = GameObject.FindObjectOfType<EnemyMovement>();
        if (existingEnemy != null)
        {
            SpawnEnemy(existingEnemy.gameObject, new Vector3(15, 1f, 15), mapRoot.transform);
            SpawnEnemy(existingEnemy.gameObject, new Vector3(-15, 1f, -15), mapRoot.transform);
            SpawnEnemy(existingEnemy.gameObject, new Vector3(15, 1f, -15), mapRoot.transform);
            SpawnEnemy(existingEnemy.gameObject, new Vector3(-15, 1f, 15), mapRoot.transform);
            
            Debug.Log("Simple Map and Enemies Generated Successfully!");
        }
        else 
        {
            Debug.LogWarning("Map generated, but could not find an existing 'Enemy' in the scene to clone. Please place one manually or check your scripts.");
        }

        // Remind user to bake navmesh
        Debug.Log("IMPORTANT: Remember to bake your NavMesh so the enemies can move! Go to Window > AI > Navigation, select the 'Bake' tab, and click 'Bake'.");
    }

    private static void CreateWall(Vector3 position, Vector3 scale, Transform parent)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "BoundaryWall";
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.transform.parent = parent;
        GameObjectUtility.SetStaticEditorFlags(wall, StaticEditorFlags.NavigationStatic);
    }

    private static void CreateObstacle(Vector3 position, Vector3 scale, Transform parent)
    {
        GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obs.name = "CoverObstacle";
        obs.transform.position = position;
        obs.transform.localScale = scale;
        obs.transform.parent = parent;
        GameObjectUtility.SetStaticEditorFlags(obs, StaticEditorFlags.NavigationStatic);
    }

    private static void SpawnEnemy(GameObject prefabToClone, Vector3 position, Transform parent)
    {
        // Clone the existing enemy in the scene
        GameObject newEnemy = Instantiate(prefabToClone, position, Quaternion.identity);
        newEnemy.name = "Enemy_Spawned";
        newEnemy.transform.parent = parent;
        
        // Ensure the NavMeshAgent is enabled on the clone
        NavMeshAgent agent = newEnemy.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = true;
        
        EnemyMovement movement = newEnemy.GetComponent<EnemyMovement>();
        if (movement != null) movement.enabled = true;
    }
}
