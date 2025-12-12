# System Architecture & Visual Guide

## System Overview Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                      GAME SCENE                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│  │ XROrigin     │    │ EnemySpawner │    │ NavMesh      │      │
│  │ (Player)     │    │              │    │ (Baked)      │      │
│  ├──────────────┤    ├──────────────┤    └──────────────┘      │
│  │ PlayerHealth │    │ - Spawns     │                            │
│  │ PlayerDamage │    │   enemies    │    ┌──────────────┐      │
│  │ Rigidbody    │    │ - Controls   │    │ Enemy Routes │      │
│  │ Collider     │    │   waves      │    │ (Waypoints)  │      │
│  └──────────────┘    │ - Selects    │    └──────────────┘      │
│       ▲              │   type       │                            │
│       │              └──────────────┘                            │
│       │                     │                                     │
│       │                     ▼                                     │
│       │              ┌──────────────────────────┐               │
│       │              │  EnemyData Presets       │               │
│       │              │  (ScriptableObjects)     │               │
│       │              ├──────────────────────────┤               │
│       │              │ • FastSpider.asset       │               │
│       │              │ • TankSpider.asset       │               │
│       │              │ • VenomSpider.asset      │               │
│       │              │ • GoliathSpider.asset    │               │
│       │              └──────────────────────────┘               │
│       │                     │                                     │
│       │                     ▼                                     │
│       │        ┌────────────────────────┐                       │
│       │        │  Enemy Instance        │                       │
│       │        │  (Prefab Clone)        │                       │
│       │        ├────────────────────────┤                       │
│       │        │ • Enemy.cs             │                       │
│       │        │ • NavMeshAgent         │                       │
│       │        │ • Rigidbody            │                       │
│       │        │ • Renderer (color)     │                       │
│       │        │ • Collider             │                       │
│       │        └────────────────────────┘                       │
│       │                  ▲                                        │
│       │                  │ Detects                               │
│       │                  │ Attacks                               │
│       │                  │                                        │
│       └──────────────────┘                                        │
│                                                                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Data Flow: Spawning an Enemy

```
User clicks Play
      │
      ▼
EnemySpawner.Start()
      │
      ├─ Check prefab assigned
      ├─ Check routes assigned
      └─ Start SpawnWavesCoroutine()
            │
            ▼
      Wave 1 Starts
            │
            ├─ Wait delayBetweenWaves
            ▼
      SpawnWaveCoroutine()
            │
            ├─ Repeat 10 times (or configured count):
            │
            ├─> SpawnEnemy()
            │       │
            │       ├─ Instantiate enemy prefab
            │       ├─ Get Enemy component
            │       ├─ Get random route
            │       ├─ Get waypoints from route
            │       ├─ Call enemy.Initialize()
            │       │
            │       └─> RandomizeEnemyType()
            │               │
            │               ├─ Get random EnemyData based on difficulty
            │               ├─ Call enemy.SetEnemyData(data)
            │               │
            │               ▼
            │               Enemy.SetEnemyData()
            │               │
            │               ├─ Store enemyData reference
            │               └─ Call LoadStatsFromData()
            │                       │
            │                       ├─ Load moveSpeed, health, damage from EnemyData
            │                       └─ Call ApplyVisualDifferentiation()
            │                               │
            │                               ├─ Set color (renderer.material.color)
            │                               └─ Set scale (transform.localScale)
            │
            ├─ Wait spawnInterval between each
            │
            ▼
      Wave 1 Complete
            │
            ├─ Wait delayBetweenWaves
            │
            ▼
      Wave 2 Starts (repeat)
      
NOTE: If infiniteWaves=false, stops after 1 wave
```

---

## Data Flow: Combat (Enemy Attacks Player)

```
Enemy Update Loop (Every Frame)
      │
      ▼
UpdateState()
      │
      ├─ Switch on currentState:
      │
      ├─── MOVING state:
      │        │
      │        ├─ UpdateMovingState()
      │        │    │
      │        │    ├─ DetectPlayerInRange()
      │        │    │    │
      │        │    │    ├─ Physics.OverlapSphere(position, detectionRadius)
      │        │    │    ├─ Check if player is within range
      │        │    │    │
      │        │    │    └─ If player found:
      │        │    │        └─ TransitionToAttacking()
      │        │    │                │
      │        │    │                ├─ currentState = ATTACKING
      │        │    │                ├─ agent.isStopped = true
      │        │    │                ├─ Fire OnStateChanged event
      │        │    │                └─ Set attackCooldownTimer
      │        │    │
      │        │    └─ MoveAlongPath()
      │        │        └─ agent.SetDestination(nextWaypoint)
      │        │
      │        └─ (back to Update Loop)
      │
      ├─── ATTACKING state:
      │        │
      │        └─ UpdateAttackingState()
      │            │
      │            ├─ DetectAndAttackPlayer()
      │            │    │
      │            │    ├─ If player NOT found or out of range:
      │            │    │    └─ TransitionToMoving()  ◄─ Return to moving
      │            │    │
      │            │    ├─ Calculate distance to player
      │            │    │
      │            │    ├─ If distance > detectionRadius:
      │            │    │    └─ TransitionToMoving()
      │            │    │
      │            │    ├─ If distance <= attackRange:
      │            │    │    │
      │            │    │    ├─ RotateTowardsPlayer()
      │            │    │    │
      │            │    │    ├─ Update attackCooldownTimer:
      │            │    │    │    attackCooldownTimer -= Time.deltaTime
      │            │    │    │
      │            │    │    └─ If attackCooldownTimer <= 0:
      │            │    │            │
      │            │    │            └─ ExecuteAttack()
      │            │    │                    │
      │            │    │                    ├─ PlayerHealth.Instance.Damage(attackDamage)
      │            │    │                    │    │
      │            │    │                    │    └─ PlayerHealth:
      │            │    │                    │        ├─ currentHealth -= attackDamage
      │            │    │                    │        ├─ Fire OnHealthChanged event
      │            │    │                    │        │   (connected to health bar UI)
      │            │    │                    │        │
      │            │    │                    │        └─ If health <= 0:
      │            │    │                    │            ├─ isAlive = false
      │            │    │                    │            ├─ Fire OnDeath event
      │            │    │                    │            └─ Disable movement
      │            │    │                    │
      │            │    │                    ├─ Reset attackCooldownTimer
      │            │    │                    └─ Continue attacking
      │            │    │
      │            │    └─ Else (not in range):
      │            │        ├─ agent.isStopped = false
      │            │        └─ agent.SetDestination(playerPosition)
      │            │           (move closer to player)
      │            │
      │            └─ (back to Update Loop)
      │
      └─ IDLE/STUNNED: No action
```

---

## State Machine Transitions

```
                    ┌──────────────┐
                    │   MOVING     │ ◄─────────────┐
                    │ (Following   │               │
                    │  waypoints)  │               │
                    └──────────────┘               │
                          │                        │
                          │ Player detected       │ Player leaves
                          │ within range          │ range
                          ▼                        │
                    ┌──────────────┐               │
                    │  ATTACKING   │───────────────┘
                    │ (Combat with │
                    │  player)     │
                    └──────────────┘
                          ▲
                          │ Called manually
                          │ (for phase 3+)
                    ┌──────────────┐
                    │    IDLE      │
                    │ (Paused,     │
                    │  grabbed)    │
                    └──────────────┘
                          ▲
                          │ Grabbed by player
                          │
                    ┌──────────────┐
                    │   STUNNED    │
                    │ (Disabled,   │
                    │  waiting)    │
                    └──────────────┘

Events:
- OnStateChanged(newState) fires on every transition
- Events connected to logic systems (UI, sound, visual effects)
```

---

## EnemyData ScriptableObject Structure

```
┌─────────────────────────────────────────────────┐
│         EnemyData.asset (ScriptableObject)      │
├─────────────────────────────────────────────────┤
│                                                  │
│  ENEMY TYPE IDENTIFICATION                      │
│  • Type: enum (FastSpider, TankSpider, etc.)   │
│  • Name: string (UI display)                    │
│                                                  │
│  MOVEMENT STATS                                  │
│  • MoveSpeed: float (3.5 m/s)                   │
│                                                  │
│  HEALTH STATS                                    │
│  • Health: float (current, copy)                │
│  • MaxHealth: float (20, 80, 120, etc.)         │
│                                                  │
│  COMBAT STATS                                    │
│  • AttackDamage: float (8, 15, 12, 20)         │
│  • AttackRange: float (2, 3 meters)             │
│  • AttackCooldown: float (1.5 seconds)          │
│                                                  │
│  DETECTION STATS                                 │
│  • DetectionRadius: float (5, 8 meters)         │
│                                                  │
│  VISUAL STATS                                    │
│  • TypeColor: Color (Red, Blue, Green, etc.)    │
│  • VisualScale: float (0.8, 1.0, 1.2, 1.3)     │
│                                                  │
│  VALIDATION                                      │
│  • IsValid(): Checks all values > 0             │
│                                                  │
└─────────────────────────────────────────────────┘
         ▲
         │ Loaded by
         │
    Enemy.cs
    └─ SetEnemyData(EnemyData data)
       └─ LoadStatsFromData()
          └─ ApplyVisualDifferentiation()
```

---

## File Dependencies

```
CORE FILES:
└─ Enemy.cs
   ├─ (requires) NavMeshAgent
   ├─ (uses) EnemyState.cs
   ├─ (loads) EnemyData.cs
   ├─ (finds) PlayerHealth.cs
   └─ (moves along) EnemyRoute.cs

└─ EnemySpawner.cs
   ├─ (spawns) Enemy.cs
   ├─ (uses) EnemyData.cs
   ├─ (uses) EnemyType.cs
   ├─ (uses) SpawnDifficulty.cs
   └─ (follows) EnemyRoute.cs

└─ PlayerHealth.cs (Singleton)
   ├─ (damaged by) Enemy.cs
   ├─ (accessed by) PlayerDamage.cs
   └─ (UI listens to) Events

SUPPORTING:
└─ EnemyData.cs (ScriptableObject)
└─ EnemyState.cs (Enum)
└─ EnemyType.cs (Enum)
└─ SpawnDifficulty.cs (Enum)
└─ PlayerDamage.cs (Component)
└─ SpiderInteractable.cs (VR grab interaction)
└─ EnemyRoute.cs (Waypoint container)
```

---

## Spawn Distribution Algorithm

```
When spawning an enemy:

1. Get Difficulty Preset
   ├─ Easy → (Types: [Fast, Tank], Chances: [0.7, 0.3])
   ├─ Normal → (Types: [Fast, Tank, Venom], Chances: [0.5, 0.3, 0.2])
   └─ Hard → (Types: [Fast, Tank, Venom, Goliath], Chances: [0.3, 0.3, 0.25, 0.15])

2. Generate random float (0.0 to 1.0)

3. Accumulate probabilities:
   ├─ If rand <= 0.5 → FastSpider (50% of time in Normal)
   ├─ If rand <= 0.8 → TankSpider (30% of time in Normal)
   └─ If rand <= 1.0 → VenomSpider (20% of time in Normal)

4. FindEnemyDataByType(selectedType)
   └─ Search EnemySpawner.enemyTypeVariants for matching EnemyData

5. Instantiate enemy and SetEnemyData(data)
   └─ Enemy loads stats and applies visuals

Example Wave (Normal difficulty):
- Enemy 1: rand=0.45 → FastSpider (blue, fast)
- Enemy 2: rand=0.75 → TankSpider (red, slow)
- Enemy 3: rand=0.32 → FastSpider (blue, fast)
- Enemy 4: rand=0.95 → VenomSpider (green, ranged)
- ...etc
```

---

## Scene Setup Checklist with Visuals

```
SCENE HIERARCHY:

├─ XROrigin (Player VR Rig)
│  ├─ [TAG: "Player"] ◄─── CRITICAL: Must be tagged
│  ├─ PlayerHealth (Component) ◄─── Manages 100 HP
│  ├─ PlayerDamage (Component) ◄─── Damage feedback
│  ├─ XR Origin Interactor (XR Toolkit)
│  └─ [Camera, Controllers, etc.]
│
├─ Terrain (Ground)
│  └─ [NavMesh Walkable Area] ◄─── CRITICAL: Must be baked
│
├─ EnemySpawner (Object)
│  ├─ EnemySpawner (Component)
│  │  ├─ Enemy Prefab: Spider 1 (prefab with NavMeshAgent)
│  │  ├─ Available Routes: [Route1, Route2, Route3]
│  │  ├─ Enemies To Spawn: 10
│  │  ├─ Spawn Interval: 0.5
│  │  ├─ Enemy Type Variants: [FastSpider, TankSpider, VenomSpider, GoliathSpider]
│  │  └─ Difficulty Preset: Normal
│  │
│  └─ [Spawn Point (position)]
│
├─ Route1 (EnemyRoute)
│  ├─ Waypoint1 (child)
│  ├─ Waypoint2 (child)
│  └─ Waypoint3 (child)
│
├─ Route2 (EnemyRoute)
│  ├─ Waypoint1 (child)
│  └─ Waypoint2 (child)
│
├─ Route3 (EnemyRoute)
│  ├─ Waypoint1 (child)
│  ├─ Waypoint2 (child)
│  └─ Waypoint3 (child)
│
└─ [Spawned Enemies] (Runtime)
   ├─ Spider (Clone) ◄─ Instance 1
   │  ├─ Enemy (Component)
   │  ├─ NavMeshAgent
   │  ├─ Rigidbody
   │  ├─ Renderer (color = Blue for FastSpider)
   │  └─ Collider
   │
   ├─ Spider (Clone) ◄─ Instance 2
   │  ├─ Enemy (Component)
   │  ├─ NavMeshAgent
   │  ├─ Rigidbody
   │  ├─ Renderer (color = Red for TankSpider)
   │  └─ Collider
   │
   └─ ...more enemies...

ASSETS FOLDER:

├─ Scripts/
│  ├─ Enemy.cs
│  ├─ EnemySpawner.cs
│  ├─ EnemyState.cs
│  ├─ EnemyType.cs
│  ├─ EnemyData.cs
│  ├─ SpawnDifficulty.cs
│  ├─ PlayerHealth.cs
│  ├─ PlayerDamage.cs
│  └─ SpiderInteractable.cs
│
├─ Resources/
│  └─ EnemyVariants/ ◄─── Must exist
│     ├─ FastSpider.asset
│     ├─ TankSpider.asset
│     ├─ VenomSpider.asset
│     └─ GoliathSpider.asset
│
└─ Prefabs/
   └─ Spider 1.prefab ◄─── Must have NavMeshAgent
```

---

## Performance Optimization Tiers

```
TIER 1: BASIC (10-20 enemies)
├─ Single spawner
├─ All types available
└─ No optimization needed

TIER 2: MODERATE (20-50 enemies)
├─ Object pooling recommended
├─ Batch NavMesh updates
├─ Use LOD for distant enemies
└─ Monitor FPS

TIER 3: ADVANCED (50-100+ enemies)
├─ Object pooling required
├─ NavMesh agent simplification
├─ LOD system essential
├─ Spatial partitioning for detection
├─ Consider animation pooling
└─ Profile and optimize hot paths
```

---

## Event System Connections

```
EVENT FLOW:

PlayerHealth.OnHealthChanged
│
├─ (Parameter: float health, float maxHealth)
├─ Fired when: Enemy attacks, player heals
├─ Connected to:
│  └─ UI Health Bar (update visual)
│     └─ healthBar.fillAmount = health / maxHealth
│
PlayerHealth.OnDeath
│
├─ Fired when: health <= 0
├─ Connected to:
│  ├─ Game Manager (show game over)
│  ├─ Enemy Spawner (stop spawning)
│  └─ Audio Manager (play death sound)
│
PlayerDamage.OnDamageTaken
│
├─ (Parameter: float damageAmount)
├─ Fired when: PlayerHealth.Damage() called
├─ Connected to:
│  ├─ Screen flash effect
│  ├─ HUD damage indicator
│  └─ Audio Manager (damage sound)
│
Enemy.OnStateChanged
│
├─ (Parameter: EnemyState newState)
├─ Fired when: State transitions occur
├─ Connected to:
│  ├─ Animation system (update animation state)
│  ├─ AI logic (behavior changes)
│  └─ Audio Manager (attack sound on Attacking state)
```

---

**Visual guide created!** Print this or keep open while setting up in Unity.
