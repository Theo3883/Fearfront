using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-time scene utility (Tools -> Fearfront):
/// For every "Towers X" object in the active scene, finds its child "Turret 1a" and
/// bakes that child's LOCAL position into the parent:
/// - Moves the parent so its pivot matches Turret 1a world position
/// - Counter-shifts all direct children so nothing visually moves in world space
/// Result: Turret 1a becomes (0,0,0) localPosition under its Towers group.
/// </summary>
public static class TowerGroupOffsetTool
{
    [MenuItem("Tools/Fearfront/Bake Each Tower's Turret 1a Offset Into Tower Group", priority = 2120)]
    public static void BakeTurret1aOffsetIntoEachGroup()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || !active.isLoaded)
        {
            Debug.LogWarning("TowerGroupOffsetTool: No active loaded scene.");
            return;
        }

        int groupsFound = 0;
        int groupsUpdated = 0;
        int groupsMissingTurretA = 0;

        foreach (GameObject root in active.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null) continue;
                if (!t.name.StartsWith("Towers ", StringComparison.OrdinalIgnoreCase)) continue;
                groupsFound++;

                Transform turretA = FindDeepChildByName(t, "Turret 1a");
                if (turretA == null)
                {
                    groupsMissingTurretA++;
                    continue;
                }

                // turretA position expressed in t local space
                Vector3 deltaLocal = t.InverseTransformPoint(turretA.position);
                if (deltaLocal.sqrMagnitude < 0.0000001f)
                    continue; // already centered

                // Moving parent by TransformVector(deltaLocal) makes parent.position == turretA.position
                Vector3 deltaWorld = t.TransformVector(deltaLocal);

                // Counter-shift all direct children so world positions remain unchanged.
                // This assumes the Turret variants are direct children (your hierarchy shows they are).
                Undo.RecordObject(t, "Bake Turret 1a offset into tower group");
                var children = new List<Transform>(t.childCount);
                for (int i = 0; i < t.childCount; i++) children.Add(t.GetChild(i));

                foreach (Transform child in children)
                {
                    if (child == null) continue;
                    Undo.RecordObject(child, "Bake Turret 1a offset into tower group (child)");
                    child.localPosition -= deltaLocal;
                }

                t.position += deltaWorld;
                groupsUpdated++;
            }
        }

        if (groupsUpdated > 0)
        {
            EditorSceneManager.MarkSceneDirty(active);
        }

        Debug.Log($"TowerGroupOffsetTool: groupsFound={groupsFound}, updated={groupsUpdated}, missingTurret1a={groupsMissingTurretA}");
    }

    private static Transform FindDeepChildByName(Transform root, string wantedName)
    {
        if (root == null || string.IsNullOrWhiteSpace(wantedName)) return null;

        var q = new Queue<Transform>();
        q.Enqueue(root);
        while (q.Count > 0)
        {
            Transform cur = q.Dequeue();
            if (cur == null) continue;
            if (string.Equals(cur.name, wantedName, StringComparison.Ordinal))
                return cur;

            for (int i = 0; i < cur.childCount; i++)
                q.Enqueue(cur.GetChild(i));
        }

        return null;
    }
}

