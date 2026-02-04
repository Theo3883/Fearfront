using UnityEngine;

/// <summary>
/// Centralized configuration for enemy visual effects.
/// Add this to an empty GameObject in the scene (e.g., "EnemyVisualsManager").
/// Allows real-time tweaking of health bars and damage flashes for all enemies.
/// </summary>
public class EnemyVisualsConfig : MonoBehaviour
{
    public static EnemyVisualsConfig Instance { get; private set; }

    [Header("Health Bar Settings")]
    [Tooltip("Position offset relative to enemy root")]
    public Vector3 HealthBarOffset = new Vector3(0, 2.5f, 0);
    
    [Tooltip("Scale of the health bar canvas")]
    public Vector3 HealthBarScale = new Vector3(0.003f, 0.003f, 0.003f);

    [Header("Damage Flash Settings")]
    public Color FlashColor = new Color(1f, 0f, 0f, 1f); // Pure Red
    [Range(0f, 10f)] public float FlashIntensity = 1.0f; // Lowered default to avoid whiteout
    public float FlashDuration = 0.15f;

    [Header("Enemy Type Visuals")]
    [Tooltip("How strongly to tint the enemy base color based on its type (0 = No Tint, 1 = Full Tint)")]
    [Range(0f, 1f)] public float BaseColorTintIntensity = 0.3f; // Default low to preserve texture details

    [Header("Highlight Settings")]
    [Tooltip("Color to tint/emit on enemies when hovered (Red for visibility)")]
    public Color HoverHighlightColor = new Color(1f, 0f, 0f, 1f); // Bright Red

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }



}
