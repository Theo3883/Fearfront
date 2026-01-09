using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Editor-only tool that copies "gameplay" components (scripts/colliders/rigidbodies/etc.)
/// from the Turret 1b prefab root onto the Turret 1a/1c/1d prefab roots.
///
/// Visual components (Renderers/MeshFilters) are intentionally NOT touched so model differences remain.
/// </summary>
public static class TurretPrefabSyncTool
{
    private const string TurretFolder = "Assets/Sci Fi Assets/Simple SciFi Gun Turret Set/Prefabs";

    private static readonly string[] TargetPrefabNames = { "Turret 1a", "Turret 1c", "Turret 1d" };
    private const string SourcePrefabName = "Turret 1b";

    [MenuItem("Tools/Fearfront/Sync Turret 1a/1c/1d Prefabs From Turret 1b", priority = 2110)]
    public static void SyncPrefabs()
    {
        string sourcePath = $"{TurretFolder}/{SourcePrefabName}.prefab";
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath))
        {
            Debug.LogError($"TurretPrefabSyncTool: Could not find source prefab at '{sourcePath}'.");
            return;
        }

        string[] targetPaths = TargetPrefabNames
            .Select(n => $"{TurretFolder}/{n}.prefab")
            .Where(p => AssetDatabase.LoadAssetAtPath<GameObject>(p) != null)
            .ToArray();

        if (targetPaths.Length == 0)
        {
            Debug.LogWarning("TurretPrefabSyncTool: No target prefabs found to sync.");
            return;
        }

        int updated = 0;
        int skipped = 0;

        GameObject sourceRoot = PrefabUtility.LoadPrefabContents(sourcePath);
        try
        {
            if (sourceRoot == null)
            {
                Debug.LogError("TurretPrefabSyncTool: Failed to load source prefab contents.");
                return;
            }

            var sourceComponents = sourceRoot
                .GetComponents<Component>()
                .Where(IsGameplayComponent)
                .ToArray();

            foreach (string targetPath in targetPaths)
            {
                GameObject targetRoot = PrefabUtility.LoadPrefabContents(targetPath);
                try
                {
                    if (targetRoot == null)
                    {
                        skipped++;
                        continue;
                    }

                    Undo.RegisterFullObjectHierarchyUndo(targetRoot, $"Sync turret prefab from {SourcePrefabName}");

                    // Remove existing gameplay components on target root
                    foreach (var c in targetRoot.GetComponents<Component>().Where(IsGameplayComponent).ToArray())
                    {
                        UnityEngine.Object.DestroyImmediate(c);
                    }

                    // Copy gameplay components from source root
                    foreach (var src in sourceComponents)
                    {
                        if (src == null) continue;
                        ComponentUtility.CopyComponent(src);
                        ComponentUtility.PasteComponentAsNew(targetRoot);
                    }

                    PrefabUtility.SaveAsPrefabAsset(targetRoot, targetPath);
                    updated++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"TurretPrefabSyncTool: Failed syncing '{targetPath}'. {ex}");
                    skipped++;
                }
                finally
                {
                    if (targetRoot != null)
                        PrefabUtility.UnloadPrefabContents(targetRoot);
                }
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(sourceRoot);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"TurretPrefabSyncTool: updated={updated}, skipped={skipped}. Source='{SourcePrefabName}'.");
    }

    private static bool IsGameplayComponent(Component c)
    {
        if (c == null) return false;

        // Never touch Transform
        if (c is Transform) return false;

        // Keep visuals intact
        if (c is Renderer) return false;
        if (c is MeshFilter) return false;
        if (c is SkinnedMeshRenderer) return false;
        if (c is Animator) return false;

        // Everything else is considered gameplay/config (scripts, colliders, rigidbodies, agents, etc.)
        return true;
    }
}


