using UnityEngine;

/// <summary>
/// ScriptableObject that defines sounds for an enemy family (Spider, Ghost, Chicken).
/// One asset per family - shared by all variants of that family.
/// </summary>
[CreateAssetMenu(fileName = "EnemySoundData", menuName = "Fearfront/Enemy Sound Data")]
public class EnemySoundData : ScriptableObject
{
    [Header("Family Identification")]
    [SerializeField] private EnemyFamily family;
    
    [Header("Spawn Sound")]
    [SerializeField] private AudioClip spawnSound;
    [Range(0f, 1f)] [SerializeField] private float spawnVolume = 1f;

    [Header("Ambient Sound (Loop)")]
    [SerializeField] private AudioClip ambientSound;
    [Range(0f, 1f)] [SerializeField] private float ambientVolume = 0.5f;

    [Header("Attack Sound")]
    [SerializeField] private AudioClip attackSound;
    [Range(0f, 1f)] [SerializeField] private float attackVolume = 1f;

    [Header("Death Sound")]
    [SerializeField] private AudioClip deathSound;
    [Range(0f, 1f)] [SerializeField] private float deathVolume = 1f;

    // Properties
    public EnemyFamily Family => family;
    public AudioClip SpawnSound => spawnSound;
    public float SpawnVolume => spawnVolume;
    public AudioClip AmbientSound => ambientSound;
    public float AmbientVolume => ambientVolume;
    public AudioClip AttackSound => attackSound;
    public float AttackVolume => attackVolume;
    public AudioClip DeathSound => deathSound;
    public float DeathVolume => deathVolume;
}
