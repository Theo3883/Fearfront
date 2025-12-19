# NavMesh Tower Defense Enemy System - Complete Setup Guide

This guide walks you through setting up and using the new NavMesh-based enemy pathfinding system with player attack mechanics and multiple enemy types.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Step-by-Step Setup](#step-by-step-setup)
3. [Creating Enemy Variants](#creating-enemy-variants)
4. [Configuring EnemySpawner](#configuring-enemyspawner)
5. [Testing & Verification](#testing--verification)
6. [Customizing Enemy Types](#customizing-enemy-types)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

Before starting, ensure you have:

- **Unity 6.0** (or compatible version) with Universal Render Pipeline
- **XR Interaction Toolkit** 3.2.2 (for VR player controller)
- **NavMesh in your scene** (baked and ready)
- **XROrigin** (VR player rig) already in your scene
- **EnemyRoute** objects with waypoints defined

### Recommended Project Structure

```
Assets/
├── Scripts/
│   ├── Enemy.cs
│   ├── EnemyData.cs
│   ├── EnemySpawner.cs
│   ├── EnemyState.cs
│   ├── EnemyType.cs
│   ├── PlayerHealth.cs
│   ├── PlayerDamage.cs
│   ├── SpawnDifficulty.cs
│   └── SpiderInteractable.cs
├── Resources/
│   └── EnemyVariants/  (where presets go)
└── Scenes/
    └── YourGameScene.unity
```

---

## Step-by-Step Setup

### Step 1: Tag the Player (XROrigin)

The enemies need to find the player. Set up the tag first:

1. In your scene hierarchy, select **XROrigin** (your VR player)
2. In the Inspector, find the **Tag** dropdown (top-right)
3. Click the dropdown and select **"Player"**
   - If "Player" tag doesn't exist, click "Add Tag..." and create it
4. Save

**Why?** The Enemy script uses this tag to find the player via `GameObject.FindWithTag("Player")`

---

### Step 2: Add PlayerHealth Component to XROrigin

The player needs a health system for enemies to damage:

1. Select **XROrigin** in hierarchy
2. In Inspector, click **Add Component**
3. Search for and add **PlayerHealth**
4. Configure settings (leave defaults for now):
   - **Max Health:** 100 (player starts with 100 HP)
   - **Start Health:** 100
   - **Move On Death Disable:** XROrigin (so player can't move when dead)
   - **Movement Component:** XROrigin's movement controller (if using locomotion)

**What it does:**
- Singleton system accessible from anywhere via `PlayerHealth.Instance`
- Fires events when taking damage (`OnHealthChanged`)
- Fires events when dying (`OnDeath`)
- Handles respawning

---

### Step 3: Add PlayerDamage Component to XROrigin

This handles damage feedback:

1. Select **XROrigin** in hierarchy
2. Click **Add Component**
3. Search for and add **PlayerDamage**
4. Configure:
   - **Flash Damage Color:** Yellow (visual feedback when hit)
   - **Flash Duration:** 0.15 seconds

**What it does:**
- Listens to damage events from PlayerHealth
- Flashes the screen/player when hit
- Optional damage feedback effects

---

### Step 4: Ensure Enemy Prefab has NavMeshAgent

Your enemy prefab needs the NavMeshAgent component:

1. Open your **spider/enemy prefab** in the project
2. Select the prefab (e.g., "Spider 1.prefab")
3. In Inspector, check for **NavMeshAgent** component
   - If missing, click **Add Component** and search for "NavMeshAgent"
   - Configure:
     - **Speed:** 3.5 (will be overridden by enemy type)
     - **Stopping Distance:** 0.1
     - **Auto Braking:** ON
     - **Auto Repath:** ON

**Required Components on Prefab:**
- ✅ Enemy.cs
- ✅ NavMeshAgent
- ✅ Rigidbody
- ✅ Renderer (for color differentiation)
- ✅ Collider (for detection)

---

### Step 5: Bake NavMesh in Your Scene

Enemies navigate using a NavMesh. You must bake it:

1. In menu: **Window > AI > Navigation**
2. In the Navigation window that opens, go to the **Bake** tab
3. **Select all static geometry:**
   - Terrain (if using)
   - Buildings/walls
   - Ground/platform
4. Configure bake settings:
   - **Agent Radius:** 0.5 (spider size)
   - **Agent Height:** 2 (spider height)
   - **Max Slope:** 45°
   - **Step Height:** 0.4
5. Click **Bake**

**Result:** Blue/teal area in scene view shows where enemies can walk

⚠️ **Important:** If NavMesh isn't baked, enemies won't move!

---

### Step 6: Create Enemy Variant Presets

Create ScriptableObject assets for the 4 enemy types:

#### Option A: Manual Creation (5 minutes)

1. Right-click in **Assets/Resources/EnemyVariants/** folder
2. **Create > ScriptableObject > EnemyData**
3. Name it **"FastSpider"**
4. In Inspector, configure:

| Field | Value |
|-------|-------|
| Type | FastSpider |
| Move Speed | 4.5 |
| Health | 20 |
| Max Health | 20 |
| Attack Damage | 8 |
| Attack Range | 2 |
| Attack Cooldown | 1.5 |
| Detection Radius | 5 |
| Type Color | Light Blue |
| Visual Scale | 0.8 |

5. Save and repeat for:
   - **TankSpider**: Speed 2, Health 80, Damage 15, Scale 1.2
   - **VenomSpider**: Speed 3.5, Health 35, Damage 12, Range 3, Scale 1.0
   - **GoliathSpider**: Speed 1.5, Health 120, Damage 20, Radius 8, Scale 1.3

#### Option B: Via Code (if Editor script exists)

If you have an Editor utility script:

1. In Unity menu: **Assets > Create > Enemy Variants > Create All Presets**
2. This automatically creates all 4 presets

**Result:** 4 `.asset` files in `Assets/Resources/EnemyVariants/`

---

### Step 7: Configure EnemySpawner

Link the enemy variants to the spawner:

1. Select **EnemySpawner** in your scene
2. In Inspector, find these fields:

| Field | Value |
|-------|-------|
| Enemy Prefab | Your spider prefab |
| Available Routes | Assign your EnemyRoute objects (e.g., "Route1", "Route2") |
| Enemies To Spawn | 10 |
| Spawn Interval | 0.5 |
| Delay Between Waves | 5 |
| Infinite Waves | ON (or OFF if you want finite waves) |
| **Enemy Type Variants** | Drag the 4 preset assets here (FastSpider, TankSpider, VenomSpider, GoliathSpider) |
| **Difficulty Preset** | Select: Easy, Normal, or Hard |

**What these do:**
- **Enemy Type Variants:** List of available enemy types
- **Difficulty Preset:** Controls spawn distribution:
  - **Easy:** 70% FastSpider, 30% TankSpider
  - **Normal:** 50% Fast, 30% Tank, 20% Venom
  - **Hard:** 30% Fast, 30% Tank, 25% Venom, 15% Goliath

---

## Creating Enemy Variants

### Enemy Type Properties

Each EnemyData ScriptableObject defines one enemy type:

#### FastSpider (Threat Level: LOW)
- **Speed:** 4.5 m/s (fastest)
- **Health:** 20 HP
- **Damage:** 8 per hit
- **Attack Range:** 2m
- **Detection Radius:** 5m
- **Color:** Light Blue
- **Scale:** 0.8x
- **Best for:** Early waves, training

#### TankSpider (Threat Level: MEDIUM)
- **Speed:** 2 m/s (slowest)
- **Health:** 80 HP (4x FastSpider)
- **Damage:** 15 per hit
- **Attack Range:** 2m
- **Detection Radius:** 5m
- **Color:** Red
- **Scale:** 1.2x (larger)
- **Best for:** Mid-game challenge

#### VenomSpider (Threat Level: MEDIUM-HIGH)
- **Speed:** 3.5 m/s
- **Health:** 35 HP
- **Damage:** 12 per hit
- **Attack Range:** 3m (longer!)
- **Detection Radius:** 5m
- **Color:** Green
- **Scale:** 1.0x (normal)
- **Best for:** Ranged threat

#### GoliathSpider (Threat Level: HIGH)
- **Speed:** 1.5 m/s (slowest)
- **Health:** 120 HP (6x FastSpider)
- **Damage:** 20 per hit (2x FastSpider)
- **Attack Range:** 2m
- **Detection Radius:** 8m (larger detection!)
- **Color:** Dark Red
- **Scale:** 1.3x (largest)
- **Best for:** Boss-like encounters

---

### Creating Custom Enemy Type

To create your own enemy type:

1. Right-click in Assets folder: **Create > ScriptableObject > EnemyData**
2. Name it (e.g., "SpiderElite")
3. In Inspector:
   - **Type:** Choose from dropdown (or leave as-is)
   - **Move Speed:** 3.0
   - **Health:** 50
   - **Max Health:** 50
   - **Attack Damage:** 12
   - **Attack Range:** 2.5
   - **Attack Cooldown:** 1.5
   - **Detection Radius:** 6
   - **Type Color:** Pick a color
   - **Visual Scale:** 1.1
4. Drag into **EnemySpawner > Enemy Type Variants**

---

## Configuring EnemySpawner

### Spawn Settings

| Setting | Explanation |
|---------|-------------|
| **Enemies To Spawn** | How many enemies per wave |
| **Spawn Interval** | Delay between spawning each enemy (e.g., 0.5 = spawn 2/sec) |
| **Delay Between Waves** | Time to wait after wave ends before next wave starts |
| **Infinite Waves** | ON = endless waves; OFF = single wave |
| **Wave Time Threshold** | Time limit for a wave (optional) |

### Difficulty Presets

Change difficulty by setting **Difficulty Preset**:

#### Easy
- Mostly FastSpiders (simple, weak)
- Some TankSpiders for durability challenge
- **Player can handle solo**

#### Normal (Default)
- Mix of Fast and Tank with some Venom
- **Balanced challenge**

#### Hard
- All 4 types with equal distribution
- GoliathSpiders appear
- **Requires skillful play**

---

## Testing & Verification

### Quick Test (5 minutes)

1. **Press Play** in Editor
2. **Observe:**
   - Enemies spawn at spawn point
   - They move along waypoints
   - They change color/size (different types)
   - They detect and attack the player
   - Player health decreases
   - Player can grab enemies to stop them

### Health & Damage Verification

| Test | Expected Result |
|------|-----------------|
| Look at player health | Should be 100/100 |
| Enemy attacks | Player health decreases (e.g., 92/100) |
| Fast attack multiple | Health drops faster from multiple enemies |
| Grab spider | Enemy stops moving, doesn't attack |
| Player at 0 health | Game should respond (death state) |

### Enemy Behavior Verification

| Enemy Type | Expected | Check |
|-----------|----------|-------|
| FastSpider | Moves quickly, lighter colored | ✓ Speed > TankSpider |
| TankSpider | Slow, darker/red, large | ✓ Size > FastSpider |
| VenomSpider | Medium speed, longer range | ✓ Attacks from 3m away |
| GoliathSpider | Very slow, huge, heavy hits | ✓ Detects from 8m radius |

---

## Customizing Enemy Types

### Change Spawn Distribution

1. Select **EnemySpawner**
2. Change **Difficulty Preset:**
   - Easy → Normal → Hard
3. Watch spawn distribution change

### Adjust Difficulty Dynamically (In Code)

If you want to change difficulty during gameplay:

```csharp
// Get spawner reference
EnemySpawner spawner = GetComponent<EnemySpawner>();

// Change difficulty
spawner.SetDifficultyPreset(SpawnDifficulty.Hard);

// Add new enemy type variant
EnemyData newType = Resources.Load<EnemyData>("EnemyVariants/CustomSpider");
spawner.AddEnemyTypeVariant(newType);
```

### Modify Attack Balance

If enemies are too strong/weak:

1. Select an EnemyData asset (e.g., "TankSpider")
2. Adjust **Attack Damage:** (10 = reasonable, 20 = one-shot)
3. Adjust **Attack Cooldown:** (1.5 = fast, 3.0 = slow)
4. Test in game

### Example Nerf (Too Difficult)

Reduce all damage by 2:
- FastSpider: 8 → 6
- TankSpider: 15 → 13
- VenomSpider: 12 → 10
- GoliathSpider: 20 → 18

---

## Troubleshooting

### Problem: Enemies don't move

**Cause:** NavMesh not baked

**Fix:**
1. Window > AI > Navigation
2. Click **Bake** tab
3. Ensure terrain/ground is marked as "Walkable"
4. Click **Bake**
5. Check scene view for blue NavMesh area

### Problem: Enemies don't attack player

**Cause:** Player not tagged as "Player"

**Fix:**
1. Select XROrigin
2. Tag: Set to "Player"
3. Restart scene

**Alternative cause:** PlayerHealth not on XROrigin

**Fix:**
1. Add PlayerHealth component to XROrigin
2. Configure max health
3. Restart scene

### Problem: Enemies all look the same

**Cause:** EnemyData presets not assigned or invalid

**Fix:**
1. Select EnemySpawner
2. In Inspector, check **Enemy Type Variants**
3. All 4 presets should be listed
4. If empty, manually drag assets from Assets/Resources/EnemyVariants/
5. Check each asset is valid (IsValid checkbox in inspector)

### Problem: Enemies don't spawn

**Cause:** Enemy prefab missing NavMeshAgent component

**Fix:**
1. Open enemy prefab
2. Add NavMeshAgent component
3. Configure settings
4. Save prefab

**Alternative cause:** EnemyRoute not assigned

**Fix:**
1. Select EnemySpawner
2. In Inspector: **Available Routes**
3. Drag EnemyRoute objects into list (e.g., "Route1", "Route2")

### Problem: Spawn distribution wrong (always same type)

**Cause:** Enemy Type Variants list has invalid/missing data

**Fix:**
1. Select EnemySpawner
2. Check each asset in **Enemy Type Variants** list
3. Remove null entries
4. Re-add missing presets

### Problem: Errors in console

**Common errors:**

| Error | Solution |
|-------|----------|
| "NavMeshAgent required" | Add NavMeshAgent to enemy prefab |
| "Routes not assigned" | Drag EnemyRoute objects into EnemySpawner |
| "Player not found" | Tag XROrigin as "Player" |
| "PlayerHealth not found" | Add PlayerHealth to XROrigin |

---

## Performance Tips

### For 100+ Concurrent Enemies

1. **Enable Batching:**
   - Enemies use same material
   - Colors applied via material properties

2. **Optimize NavMesh:**
   - Reduce NavMesh detail if too complex
   - Use larger agent radius (0.5m)

3. **Object Pooling (Future):**
   - Reuse destroyed enemies instead of Instantiate/Destroy
   - Reduces garbage collection

4. **LOD (Level of Detail):**
   - Distant enemies render simpler geometry
   - Only close enemies show full detail

---

## Advanced Customization

### Create New Enemy Type

1. **Create EnemyData asset:**
   ```
   Right-click > Create > ScriptableObject > EnemyData
   Name: "SpiderChampion"
   ```

2. **Configure stats:**
   - Type: (custom)
   - Speed: 2.5
   - Health: 60
   - Damage: 14
   - Detection Radius: 7
   - Color: Purple
   - Scale: 1.15

3. **Add to spawner:**
   - EnemySpawner > Enemy Type Variants
   - Drag "SpiderChampion" asset

4. **Include in difficulty:**
   - Modify EnemySpawner code if you want it in specific difficulties

### Modify Spawn Pattern

To customize spawn patterns (e.g., "waves of specific types"):

Currently uses **random distribution** based on difficulty. To change:

1. Open **EnemySpawner.cs**
2. Modify `GetDifficultyDistribution()` method
3. Change probability arrays (must sum to 1.0)

Example: 80% Fast, 20% Tank
```csharp
return (
    new[] { EnemyType.FastSpider, EnemyType.TankSpider },
    new[] { 0.8f, 0.2f }  // Probabilities
);
```

---

## Summary Checklist

- [ ] XROrigin tagged as "Player"
- [ ] PlayerHealth added to XROrigin
- [ ] PlayerDamage added to XROrigin
- [ ] Enemy prefab has NavMeshAgent
- [ ] NavMesh baked in scene
- [ ] 4 EnemyData presets created in Assets/Resources/EnemyVariants/
- [ ] EnemySpawner configured with:
  - [ ] Enemy prefab assigned
  - [ ] Routes assigned
  - [ ] Enemy Type Variants populated
  - [ ] Difficulty set to desired level
- [ ] Play and verify:
  - [ ] Enemies spawn
  - [ ] Enemies move along paths
  - [ ] Enemies different colors/sizes
  - [ ] Enemies detect and attack player
  - [ ] Player health decreases
  - [ ] Game is fun!

---

## Next Steps

Once basic setup works:

1. **Tune difficulty:** Adjust enemy damage/speed to match game feel
2. **Add waves:** Increase enemies over time for progression
3. **UI integration:** Show player health bar (PlayerHealth has events)
4. **Sound effects:** Add attack/death sounds per enemy type
5. **Visual effects:** Particle effects on death, damage feedback

---

**Questions?** Check [Enemy.cs](Assets/Scripts/Enemy.cs), [EnemySpawner.cs](Assets/Scripts/EnemySpawner.cs), or [PlayerHealth.cs](Assets/Scripts/PlayerHealth.cs) for detailed comments and implementation.
