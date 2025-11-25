using UnityEngine;
using UnityEditor;

// Editor tool: raycast down from terrain heightmap points onto a MeshCollider
// and write the heights into the terrain.
public class BendTerrainToExistingOutline : EditorWindow
{
    private Terrain terrain;
    private MeshCollider sourceCollider;
    private float raycastHeight = 50f;
    private float heightOffset = 0f;
    private int sampleStep = 1;
    private bool useColliderOnly = true;

    [MenuItem("Tools/Terrain/Bend Terrain To Mesh")]
    static void ShowWindow() => GetWindow<BendTerrainToExistingOutline>("Bend Terrain To Mesh");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Terrain -> Mesh Conform Tool", EditorStyles.boldLabel);
        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
        sourceCollider = (MeshCollider)EditorGUILayout.ObjectField("Source MeshCollider", sourceCollider, typeof(MeshCollider), true);

        raycastHeight = EditorGUILayout.FloatField("Raycast Height Above Terrain Top", raycastHeight);
        heightOffset = EditorGUILayout.FloatField("Height Offset (meters)", heightOffset);
        sampleStep = EditorGUILayout.IntField("Heightmap Sample Step", Mathf.Max(1, sampleStep));
        useColliderOnly = EditorGUILayout.Toggle(new GUIContent("Raycast Against Only Collider","If enabled, uses MeshCollider.Raycast so only the assigned collider is used."), useColliderOnly);

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(terrain == null || sourceCollider == null);
        if (GUILayout.Button("Conform Terrain Now"))
        {
            Conform();
        }
        EditorGUI.EndDisabledGroup();

        if (terrain == null || sourceCollider == null)
            EditorGUILayout.HelpBox("Assign both a Terrain and a MeshCollider. Put the MeshCollider on the mesh you want the terrain to match from above.", MessageType.Info);
    }

    void Conform()
    {
        if (terrain == null || sourceCollider == null)
        {
            Debug.LogError("Assign a Terrain and a MeshCollider first.");
            return;
        }

        TerrainData td = terrain.terrainData;
        if (td == null)
        {
            Debug.LogError("Terrain has no terrainData.");
            return;
        }

        int hmW = td.heightmapResolution;
        int hmH = td.heightmapResolution; // heightmap is square in many cases; use both for clarity

        Vector3 terrPos = terrain.transform.position;
        Vector3 size = td.size;

        float[,] heights = td.GetHeights(0, 0, hmW, hmH);

        // Register undo
        Undo.RegisterCompleteObjectUndo(td, "Conform Terrain To Mesh");

        try
        {
            int total = (hmH / sampleStep) * (hmW / sampleStep);
            int processed = 0;

            for (int y = 0; y < hmH; y += sampleStep)
            {
                for (int x = 0; x < hmW; x += sampleStep)
                {
                    float wx = terrPos.x + (x / (float)(hmW - 1)) * size.x;
                    float wz = terrPos.z + (y / (float)(hmH - 1)) * size.z;

                    Vector3 rayStart = new Vector3(wx, terrPos.y + size.y + raycastHeight, wz);
                    Ray ray = new Ray(rayStart, Vector3.down);
                    RaycastHit hit;
                    bool hitSomething = false;

                    if (useColliderOnly)
                    {
                        // Use the assigned collider's Raycast to ensure we hit only it
                        hitSomething = sourceCollider.Raycast(ray, out hit, Mathf.Infinity);
                    }
                    else
                    {
                        // Use Physics.Raycast with layer mask
                        // Raycast against all layers (user can disable useColliderOnly to allow other colliders)
                        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                        {
                            hitSomething = (hit.collider == sourceCollider);
                        }
                    }

                    if (hitSomething)
                    {
                        float worldHeight = hit.point.y + heightOffset;
                        float normalized = (worldHeight - terrPos.y) / size.y;
                        heights[y, x] = Mathf.Clamp01(normalized);
                    }
                    // else keep existing height

                    processed++;
                    if (processed % 256 == 0)
                    {
                        float prog = processed / (float)total;
                        EditorUtility.DisplayProgressBar("Conforming Terrain", $"Processing {processed}/{total}", prog);
                    }
                }
            }

            // Apply the new heights immediately
            td.SetHeights(0, 0, heights);

            EditorUtility.ClearProgressBar();
            Debug.Log("Terrain conformed to mesh.");
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("Error conforming terrain: " + ex.Message);
        }
    }
}
