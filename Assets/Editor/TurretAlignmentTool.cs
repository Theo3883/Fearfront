using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only tool: for every "Towers X" parent in the active scene, copy Turret 1a transform
/// (position+rotation) onto Turret 1b/1c/1d.
/// </summary>
public static class TurretAlignmentTool
{
    [MenuItem("Tools/Fearfront/Align Turrets (1b/1c/1d) To Turret 1a", priority = 2100)]
    public static void AlignAllTowerGroups()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || !active.isLoaded)
        {
            Debug.LogWarning("No active loaded scene found.");
            return;
        }

        GameObject[] roots = active.GetRootGameObjects();
        int groupsFound = 0;
        int turretsAligned = 0;
        int groupsMissingTurretA = 0;

        foreach (GameObject root in roots)
        {
            if (root == null) continue;

            // Find all groups under this root (including root itself).
            foreach (Transform group in root.GetComponentsInChildren<Transform>(true))
            {
                if (group == null) continue;
                if (!group.name.StartsWith("Towers", StringComparison.OrdinalIgnoreCase)) continue;

                groupsFound++;

                Transform turretA = FindDeepChild(group, "Turret 1a");
                if (turretA == null)
                {
                    groupsMissingTurretA++;
                    continue;
                }

                Vector3 worldPos = turretA.position;
                Quaternion worldRot = turretA.rotation;

                turretsAligned += AlignIfPresent(group, "Turret 1b", worldPos, worldRot);
                turretsAligned += AlignIfPresent(group, "Turret 1c", worldPos, worldRot);
                turretsAligned += AlignIfPresent(group, "Turret 1d", worldPos, worldRot);
            }
        }

        if (turretsAligned > 0)
        {
            EditorSceneManager.MarkSceneDirty(active);
        }

        Debug.Log(
            $"TurretAlignmentTool: groupsFound={groupsFound}, groupsMissingTurretA={groupsMissingTurretA}, turretsAligned={turretsAligned}"
        );
    }

    private static int AlignIfPresent(Transform group, string turretName, Vector3 worldPos, Quaternion worldRot)
    {
        Transform t = FindDeepChild(group, turretName);
        if (t == null) return 0;

        Undo.RecordObject(t, $"Align {turretName} to Turret 1a");
        t.SetPositionAndRotation(worldPos, worldRot);
        PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        return 1;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null) return null;

        // BFS is a bit safer than recursion depth-wise.
        var queue = new Queue<Transform>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            Transform cur = queue.Dequeue();
            if (cur == null) continue;
            if (string.Equals(cur.name, name, StringComparison.Ordinal))
                return cur;

            for (int i = 0; i < cur.childCount; i++)
                queue.Enqueue(cur.GetChild(i));
        }

        return null;
    }
}


