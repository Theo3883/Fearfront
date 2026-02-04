using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// HELPER: Setup automat VR pentru TOATE turelele
/// Pune asta pe orice obiect, run context menu, DONE!
/// </summary>
public class SetupAllTurretsVR : MonoBehaviour
{
    [ContextMenu("🚀 Setup TOATE Turelele pentru VR Upgrade")]
    void SetupAllTurrets()
    {
        Debug.Log("========================================");
        Debug.Log("Starting Turret VR Setup...");
        Debug.Log("========================================");

        // Gaseste TOATE TurretVariantGroup-urile (ar trebui sa fie pe Towers 1, Towers 2, etc.)
        TurretVariantGroup[] allGroups = FindObjectsByType<TurretVariantGroup>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        int setupCount = 0;

        foreach (var group in allGroups)
        {
            if (group == null) continue;

            GameObject turretObj = group.gameObject;

            // 1. Adauga XRSimpleInteractable daca lipseste
            var xrInteractable = turretObj.GetComponent<XRSimpleInteractable>();
            if (xrInteractable == null)
            {
                xrInteractable = turretObj.AddComponent<XRSimpleInteractable>();
                Debug.Log($"✓ Added XRSimpleInteractable to: {turretObj.name}");
            }

            // 2. Seteaza Interaction Layers (default = everything)
            xrInteractable.interactionLayers = UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask.GetMask("Default");

            // 3. Adauga TurretUpgradeVR daca lipseste
            if (turretObj.GetComponent<TurretUpgradeVR>() == null)
            {
                turretObj.AddComponent<TurretUpgradeVR>();
                Debug.Log($"✓ Added TurretUpgradeVR to: {turretObj.name}");
            }

            // 4. Asigura-te ca are Collider pentru raycast
            Collider col = turretObj.GetComponent<Collider>();
            if (col == null)
            {
                // Cauta collider in copii (poate e pe Turret 1a/1b)
                col = turretObj.GetComponentInChildren<Collider>();
                
                // Daca tot nu exista, adauga unul
                if (col == null)
                {
                    BoxCollider box = turretObj.AddComponent<BoxCollider>();
                    box.size = new Vector3(2, 3, 2); // Size aproximativ pentru turele
                    box.center = new Vector3(0, 1.5f, 0);
                    Debug.Log($"✓ Added BoxCollider to: {turretObj.name}");
                }
            }

            setupCount++;
        }

        Debug.Log("========================================");
        Debug.Log($"<color=green>✅ SETUP COMPLETE!</color>");
        Debug.Log($"   Setup {setupCount} turrets for VR upgrade");
        Debug.Log("========================================");
        Debug.Log("Acum:");
        Debug.Log("  1. Play in VR");
        Debug.Log("  2. Pointeaza la turele cu controller-ul");
        Debug.Log("  3. Trage trigger pentru upgrade!");
        Debug.Log("========================================");
    }

    [ContextMenu("🧹 OPRESTE Auto-Upgrade (Sterge TurretUpgradeTester)")]
    void DisableAutoUpgrade()
    {
        Debug.Log("========================================");
        Debug.Log("Searching for TurretUpgradeTester...");
        
        // Gaseste toate componentele TurretUpgradeTester
        TurretUpgradeTester[] testers = FindObjectsByType<TurretUpgradeTester>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        if (testers.Length == 0)
        {
            Debug.Log("<color=green>✅ Nu exista TurretUpgradeTester - deja OK!</color>");
            return;
        }

        foreach (var tester in testers)
        {
            if (tester != null)
            {
                GameObject obj = tester.gameObject;
                Debug.Log($"🗑️ Sterg TurretUpgradeTester de pe: {obj.name}");
                DestroyImmediate(tester);
            }
        }

        Debug.Log("========================================");
        Debug.Log($"<color=green>✅ REMOVED {testers.Length} TurretUpgradeTester(s)</color>");
        Debug.Log("Acum turelele NU se mai upgradeaza singure!");
        Debug.Log("========================================");
    }

    [ContextMenu("💰 Adauga Resurse de Test (100 copaci + 100 pietre)")]
    void AddTestResources()
    {
        PlayerInventory inv = FindFirstObjectByType<PlayerInventory>();
        if (inv != null)
        {
            inv.Add(Fearfront.Common.ResourceType.Tree, 100);
            inv.Add(Fearfront.Common.ResourceType.Stone, 100);
            Debug.Log("<color=green>💰 Added 100 Wood + 100 Stone!</color>");
        }
        else
        {
            Debug.LogError("❌ Nu gasesc PlayerInventory in scena!");
        }
    }

    [ContextMenu("🔧 FIX COLLIDERS - Fa-le Mari Si Vizibile")]
    void FixAllColliders()
    {
        Debug.Log("========================================");
        Debug.Log("Fixing Colliders for VR Raycast...");
        Debug.Log("========================================");

        TurretVariantGroup[] allGroups = FindObjectsByType<TurretVariantGroup>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        int fixedCount = 0;

        foreach (var group in allGroups)
        {
            if (group == null) continue;

            GameObject turretObj = group.gameObject;

            // Sterge collider-ele vechi de pe Towers X (daca exista)
            Collider[] oldColliders = turretObj.GetComponents<Collider>();
            foreach (var col in oldColliders)
            {
                if (col != null && !(col is SphereCollider && col.isTrigger))
                {
                    // Pastreaza doar SphereCollider-ul trigger (pentru enemy detection)
                    DestroyImmediate(col);
                }
            }

            // Adauga un BoxCollider MARE pentru VR raycast
            BoxCollider newBox = turretObj.AddComponent<BoxCollider>();
            newBox.isTrigger = false; // IMPORTANT: NU trigger!
            newBox.size = new Vector3(3f, 4f, 3f); // MARE - 3x4x3 metri
            newBox.center = new Vector3(0f, 2f, 0f); // Centrat la 2m inaltime

            Debug.Log($"✓ Fixed collider on: {turretObj.name} (Size: {newBox.size})");
            fixedCount++;
        }

        Debug.Log("========================================");
        Debug.Log($"<color=green>✅ FIXED {fixedCount} colliders!</color>");
        Debug.Log("Acum collider-ele sunt MARI si usor de lovit cu ray-ul!");
        Debug.Log("========================================");
    }

    [ContextMenu("🔍 Verifica Setup Turele")]
    void CheckTurretSetup()
    {
        TurretVariantGroup[] allGroups = FindObjectsByType<TurretVariantGroup>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Debug.Log("========================================");
        Debug.Log("TURRET STATUS CHECK:");
        Debug.Log("========================================");

        foreach (var group in allGroups)
        {
            if (group == null) continue;

            bool hasXR = group.GetComponent<XRSimpleInteractable>() != null;
            bool hasUpgrade = group.GetComponent<TurretUpgradeVR>() != null;
            
            // Cauta collider NON-trigger pe Towers X
            Collider mainCollider = null;
            foreach (var col in group.GetComponents<Collider>())
            {
                if (col != null && !col.isTrigger)
                {
                    mainCollider = col;
                    break;
                }
            }
            bool hasCollider = mainCollider != null;

            string status = (hasXR && hasUpgrade && hasCollider) ? "✅" : "❌";

            Debug.Log($"{status} {group.name}:");
            Debug.Log($"   • XRSimpleInteractable: {(hasXR ? "YES" : "MISSING")}");
            Debug.Log($"   • TurretUpgradeVR: {(hasUpgrade ? "YES" : "MISSING")}");
            Debug.Log($"   • Collider: {(hasCollider ? "YES" : "MISSING")}");
            Debug.Log($"   • Current Level: {group.CurrentVariant}");
        }

        Debug.Log("========================================");
    } // End CheckTurretSetup

    [ContextMenu("Audio 🔊 Setup Sunet pentru TOATE")]
    void SetupAudioForAll()
    {
        if (defaultShootSound == null)
        {
            Debug.LogError("‼️ ERROR: Te rog pune un sunet în câmpul 'Default Shoot Sound' al acestui script înainte să rulezi comanda!");
            return;
        }

        Debug.Log("========================================");
        Debug.Log("🔊 Starting Audio Setup (Sound + Volume)...");

        TowerScript[] allTowers = FindObjectsByType<TowerScript>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int count = 0;

        foreach (var tower in allTowers)
        {
#if UNITY_EDITOR
            UnityEditor.SerializedObject so = new UnityEditor.SerializedObject(tower);
            
            // Set Sound
            UnityEditor.SerializedProperty audioProp = so.FindProperty("shootSound");
            if (audioProp != null)
            {
                audioProp.objectReferenceValue = defaultShootSound;
            }

            // Set Volume
            UnityEditor.SerializedProperty volumeProp = so.FindProperty("shootVolume");
            if (volumeProp != null)
            {
                volumeProp.floatValue = defaultVolume;
            }

            so.ApplyModifiedProperties();
            count++;
#endif
        }

        Debug.Log($"✅ GATA! Am pus sunetul si volumul ({defaultVolume}) pe {count} turele.");
        Debug.Log("========================================");
    }
    
    [Header("Audio Setup")]
    public AudioClip defaultShootSound;
    [Range(0f, 1f)]
    public float defaultVolume = 0.5f;
}
