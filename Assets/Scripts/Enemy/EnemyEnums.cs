
/// <summary>
/// Broad classification of enemy species/model type
/// </summary>
public enum EnemyFamily 
{ 
    Spider, 
    Ghost, 
    Chicken 
}

/// <summary>
/// Generic gameplay role/variant that can apply to any family
/// </summary>
public enum EnemyVariantType 
{ 
    Normal,         // Standard balanced stats
    Fast,           // Lower health, high speed
    Tank,           // High health, slow speed, high damage
    Ranged,         // Standard health, ranged attacks (Venom/Poltergeist)
    Heavy,          // Very high health/damage, very slow (Goliath/Giant/Reaper)
    Boss            // Extreme stats
}
