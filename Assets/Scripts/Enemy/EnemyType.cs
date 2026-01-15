/// <summary>
/// Enum defining all available enemy type variants
/// </summary>
public enum EnemyType
{
    FastSpider,     // High speed (4.5 m/s), low health (20), low damage (8)
    TankSpider,     // Low speed (2 m/s), high health (80), high damage (15)
    VenomSpider,    // Medium speed (3.5 m/s), medium health (35), medium damage (12), longer range (3m)
    GoliathSpider,  // Very low speed (1.5 m/s), very high health (120), very high damage (20), ranged detection (8m)

    // Chicken variants
    FastChicken,
    TankChicken,
    RabidChicken,
    GiantChicken,

    // Ghost variants
    WispGhost,
    PhantomGhost,
    PoltergeistGhost,
    ReaperGhost
}
