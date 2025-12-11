using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class FindMissingScriptsInProject
{
    [MenuItem("Tools/Find Missing Scripts in Project")]
    public static void FindMissing()
    {
        int goCount = 0, missingCount = 0;
        var results = new List<string>();

        // Search all prefabs
        string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
        foreach (var g in prefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var gos = prefab.GetComponentsInChildren<Transform>(true);
            foreach (var t in gos)
            {
                goCount++;
                var components = t.gameObject.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        missingCount++;
                        results.Add($"Prefab: {path} | GameObject: {GetGameObjectPath(t.transform)} | Missing component at index {i}");
                    }
                }
            }
        }

        // Search open scenes
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    goCount++;
                    var components = t.gameObject.GetComponents<Component>();
                    for (int j = 0; j < components.Length; j++)
                    {
                        if (components[j] == null)
                        {
                            missingCount++;
                            results.Add($"Scene: {scene.path} | GameObject: {GetGameObjectPath(t.transform)} | Missing component at index {j}");
                        }
                    }
                }
            }
        }

        if (results.Count == 0)
        {
            Debug.Log($"FindMissingScripts: scanned {goCount} GameObjects — no missing scripts found.");
        }
        else
        {
            Debug.LogError($"FindMissingScripts: scanned {goCount} GameObjects — found {missingCount} missing components. See console for details.");
            foreach (var r in results)
                Debug.Log(r);
        }
    }

    static string GetGameObjectPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
