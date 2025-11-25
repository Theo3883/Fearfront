// Assets/Editor/ConformTerrainToMesh.cs
using UnityEngine;
using UnityEditor;

public class ConformTerrainToMesh : EditorWindow
{
    Terrain terrain;
    MeshCollider sourceMesh; // your shape
    float raycastHeight = 1000f; // start ray above the highest point
    float offset = 0f; // add/subtract to push terrain up/down

    [MenuItem("Tools/Terrain/Conform To Mesh")]
    static void ShowWindow() => GetWindow<ConformTerrainToMesh>("Conform To Mesh");

    void OnGUI()
    {
        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);
        sourceMesh = (MeshCollider)EditorGUILayout.ObjectField("Source MeshCollider", sourceMesh, typeof(MeshCollider), true);
        raycastHeight = EditorGUILayout.FloatField("Raycast Height", raycastHeight);
        offset = EditorGUILayout.FloatField("Height Offset", offset);

        if (GUILayout.Button("Conform"))
            Conform();
    }

    void Conform()
    {
        if (!terrain || !sourceMesh) { Debug.LogError("Assign a Terrain and a MeshCollider."); return; }

        var td = terrain.terrainData;
        int res = td.heightmapResolution;

        var size = td.size;                   // world size of terrain
        var origin = terrain.transform.position;

        float[,] heights = new float[res, res];

        // Iterate over heightmap samples
        for (int z = 0; z < res; z++)
        {
            for (int x = 0; x < res; x++)
            {
                // sample position in world space at this heightmap pixel (x,z)
                float wx = origin.x + (x / (float)(res - 1)) * size.x;
                float wz = origin.z + (z / (float)(res - 1)) * size.z;

                Vector3 rayStart = new Vector3(wx, origin.y + size.y + raycastHeight, wz);
                if (Physics.Raycast(rayStart, Vector3.down, out var hit, Mathf.Infinity))
                {
                    // Normalize height to [0..1] relative to terrain bottom/top
                    float h = (hit.point.y + offset - origin.y) / size.y;
                    heights[z, x] = Mathf.Clamp01(h);
                }
                else
                {
                    // No mesh under this sample – keep existing terrain height
                    heights[z, x] = td.GetHeight(z, x) / size.y;
                }
            }
        }

        td.SetHeightsDelayLOD(0, 0, heights);
        td.ApplyDelayedHeightmapModification();
        Debug.Log("Terrain conformed to mesh.");
    }
}
