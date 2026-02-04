using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Editor utility to create generic EnemyData variant presets.
/// Generates stats templates (Fast, Tank, etc) that can be applied to any EnemyFamily.
/// </summary>
public class EnemyDataPresets
{
    private const string ENEMY_VARIANTS_PATH = "Assets/Resources/EnemyVariants/";

    [MenuItem("Assets/Create/Enemy Variants/Create All Generic Presets")]
    public static void CreateAllPresets()
    {
        CreateDirectory(ENEMY_VARIANTS_PATH);
        
        CreateNormalPreset();
        CreateFastPreset();
        CreateTankPreset();
        CreateRangedPreset();
        CreateHeavyPreset();
        CreateBossPreset();
        
        Debug.Log("All generic enemy variant presets created successfully!");
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Create/Enemy Variants/Generic - Normal")]
    public static void CreateNormalPreset()
    {
        CreateGenericPreset(
            "Normal",
            EnemyVariantType.Normal,
            moveSpeed: 3.5f,
            health: 20f,
            damage: 10f,
            range: 3.5f, // Increased from 2.0f
            cooldown: 1.5f,
            detection: 20f,
            color: Color.white,
            scaleMultiplier: 1.0f
        );
    }

    [MenuItem("Assets/Create/Enemy Variants/Generic - Fast")]
    public static void CreateFastPreset()
    {
        CreateGenericPreset(
            "Fast",
            EnemyVariantType.Fast,
            moveSpeed: 5.5f,
            health: 15f,
            damage: 8f,
            range: 3.5f, // Increased from 2.0f
            cooldown: 1.0f,
            detection: 25f,
            color: new Color(1f, 0.5f, 0f), // Orange
            scaleMultiplier: 0.8f
        );
    }

    [MenuItem("Assets/Create/Enemy Variants/Generic - Tank")]
    public static void CreateTankPreset()
    {
        CreateGenericPreset(
            "Tank",
            EnemyVariantType.Tank,
            moveSpeed: 2.0f,
            health: 60f,
            damage: 15f,
            range: 4.0f, // Increased from 2.0f
            cooldown: 2.0f,
            detection: 18f,
            color: new Color(0.5f, 0.2f, 0.1f), // Brown
            scaleMultiplier: 1.3f
        );
    }

    [MenuItem("Assets/Create/Enemy Variants/Generic - Ranged")]
    public static void CreateRangedPreset()
    {
        CreateGenericPreset(
            "Ranged",
            EnemyVariantType.Ranged,
            moveSpeed: 3.0f,
            health: 30f,
            damage: 12f,
            range: 15.0f, // Increased from 8.0f (Player engagement distance)
            cooldown: 2.2f,
            detection: 30f,
            color: new Color(0.5f, 0.8f, 0.2f), // Lime/Green
            scaleMultiplier: 1.0f
        );
    }

    [MenuItem("Assets/Create/Enemy Variants/Generic - Heavy")]
    public static void CreateHeavyPreset()
    {
        CreateGenericPreset(
            "Heavy",
            EnemyVariantType.Heavy,
            moveSpeed: 1.5f,
            health: 120f,
            damage: 25f,
            range: 3.0f, // Increased from 2.5f
            cooldown: 2.5f,
            detection: 25f,
            color: new Color(0.3f, 0.1f, 0.3f), // Purple
            scaleMultiplier: 1.6f
        );
    }

    [MenuItem("Assets/Create/Enemy Variants/Generic - Boss")]
    public static void CreateBossPreset()
    {
        CreateGenericPreset(
            "Boss",
            EnemyVariantType.Boss,
            moveSpeed: 1.8f,
            health: 300f,
            damage: 40f,
            range: 5.0f, // Increased from 3.5f
            cooldown: 3.0f,
            detection: 50f,
            color: new Color(0.8f, 0.0f, 0.0f), // Dark Red
            scaleMultiplier: 2.5f
        );
    }

    /// <summary>
    /// Helper to create a generic enemy data asset
    /// </summary>
    private static void CreateGenericPreset(
        string variantName,
        EnemyVariantType variantType,
        float moveSpeed,
        float health,
        float damage,
        float range,
        float cooldown,
        float detection,
        Color color,
        float scaleMultiplier)
    {
        EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
        
        // Use reflection to set private fields
        SetField(data, "enemyName", variantName);
        SetField(data, "family", EnemyFamily.Spider); // Default family, user should change if needed
        SetField(data, "variantType", variantType);
        
        SetField(data, "moveSpeed", moveSpeed);
        SetField(data, "health", health);
        SetField(data, "maxHealth", health); // Start with full health
        
        SetField(data, "attackDamage", damage);
        SetField(data, "attackRange", range);
        SetField(data, "attackCooldown", cooldown);
        SetField(data, "detectionRadius", detection);
        
        SetField(data, "typeColor", color);
        SetField(data, "visualScaleMultiplier", scaleMultiplier);
        
        SaveEnemyData(data, variantName);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = typeof(EnemyData).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
        else
        {
            Debug.LogError($"Could not find field '{fieldName}' in EnemyData");
        }
    }

    private static void CreateDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parentPath = Path.GetDirectoryName(path).Replace('\\', '/');
            string folderName = Path.GetFileName(path.TrimEnd('/'));
            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }

    private static void SaveEnemyData(EnemyData data, string typeName)
    {
        CreateDirectory(ENEMY_VARIANTS_PATH);
        string path = $"{ENEMY_VARIANTS_PATH}{typeName}.asset";
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        AssetDatabase.CreateAsset(data, path);
    }
}
