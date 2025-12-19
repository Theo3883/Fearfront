# Enemy AI & Spawner Setup Guide

This guide walks you through configuring the refactored Enemy system, including the new modular components (`EnemyMovement`, `EnemyStateMachine`, `NavMeshPlayerDetector`) and the `EnemySpawner`.

---

## Overview

The refactored Enemy system uses a **coordinator pattern**:
- **`Enemy.cs`** – Main coordinator; delegates to three components and manages health.
- **`EnemyMovement.cs`** – Handles waypoint navigation, agent separation, and path resumption.
- **`EnemyStateMachine.cs`** – Manages state transitions (FollowingPath ↔ AttackingPlayer) with dual-condition engagement logic.
- **`NavMeshPlayerDetector.cs`** – Detects if the player is on the NavMesh (critical for smart engagement).
- **`EnemySpawner.cs`** – Spawns waves of enemies and initializes all components.

---

## Step 1: Create an EnemyData ScriptableObject

### What is EnemyData?
`EnemyData` holds reusable enemy configuration (speed, health, damage, detection range, etc.). Avoid null warnings by assigning it in the EnemySpawner.

### How to create one:

1. **In Unity Editor**, right-click in Assets/Resources (create if needed):
   - **Create → Fearfront → Enemy Data** (or similar, depending on your menu).
   - Name it `DefaultEnemyData.asset`.

2. **If no menu item exists**, create it in code:
   - Right-click Assets → Create → Folder → Name it `Resources`.
   - Right-click Resources → Create → Script → `CreateEnemyDataMenuItem.cs`:
     ```csharp
     using UnityEditor;
     using UnityEngine;

     public class CreateEnemyDataMenuItem
     {
         [MenuItem("Assets/Create/Fearfront/Enemy Data")]
         public static void CreateEnemyData()
         {
             var asset = ScriptableObject.CreateInstance<EnemyData>();
             asset.speed = 3.5f;
             asset.maxHealth = 50f;
             asset.damagePerHit = 10f;
             asset.detectionRange = 6f;
             
             AssetDatabase.CreateAsset(asset, "Assets/Resources/DefaultEnemyData.asset");
             AssetDatabase.SaveAssets();
             EditorUtility.FocusProjectWindow();
             Selection.activeObject = asset;
         }
     }
     ```

3. **Select the created EnemyData asset** and in the Inspector set:
   - **Speed**: 3.5
   - **Max Health**: 50
   - **Damage Per Hit**: 10
   - **Detection Range**: 6.0
   - **Attack Cooldown**: 0.5 (seconds between attacks)

---

## Step 2: Create the Enemy Prefab

### 2.1 Create a base Enemy GameObject

1. In the Scene, create an empty GameObject: **Right-click Hierarchy → Create Empty** → name it `Enemy_Prefab`.
2. Add a Capsule or Sphere child mesh (or use existing enemy model).
3. Add components:
   - **Add Component → Capsule Collider** (or Box Collider if using a custom mesh).
   - **Add Component → Rigidbody** (optional; can leave as Kinematic if using NavMeshAgent).
   - **Add Component → NavMeshAgent** (from Script menu or search).
   - **Add Component → Enemy** (script).
   - **Add Component → EnemyMovement** (script).
   - **Add Component → EnemyStateMachine** (script).
   - **Add Component → NavMeshPlayerDetector** (script).

### 2.2 Wire the components in the Inspector

#### NavMeshAgent
- **Speed**: 3.5 (should match `EnemyMovement.speed`).
- **Stopping Distance**: 0.5.
- **Angular Speed**: 120.
- **Acceleration**: 8.
- Leave other defaults.

#### EnemyMovement
- **Waypoints** (Transform[]): 
  - Set **Size** to the number of waypoints you have (e.g., 3).
  - Drag waypoint Transforms into each slot (see Step 4 below for creating waypoints).
  - *Or*, drag an **EnemyRoute** GameObject (a parent with waypoint children) and the system will auto-populate.
- **Enemy Data**: Drag `DefaultEnemyData.asset` (or your EnemyData) here.
- **Speed**: 3.5.
- **Stopping Distance**: 0.5.
- **Separation Radius**: 1.0.
- **Separation Layer Mask**: Select the `Enemy` layer (see Step 3 for creating the layer).
- **Max NavMesh Recovery Attempts**: 5.
- **NavMesh Recovery Tolerance**: 1.5.

#### EnemyStateMachine
- **Player Transform**: Drag your Player GameObject from the Hierarchy.
- **Detection Range**: 6.0 (start attacking at this distance).
- **NavMeshPlayerDetector**: Drag the `NavMeshPlayerDetector` component from the same Enemy GameObject.
- **Enemy Data**: Drag `DefaultEnemyData.asset` here.

#### NavMeshPlayerDetector
- **Player Transform**: Drag your Player GameObject.
- **Tolerance**: 1.5 (how close to NavMesh the player must be to count as "on NavMesh").
- **Sample Max Distance**: 1.5.

#### Enemy (coordinator)
- **Enemy Movement**: Drag the `EnemyMovement` component.
- **Enemy State Machine**: Drag the `EnemyStateMachine` component.
- **NavMesh Player Detector**: Drag the `NavMeshPlayerDetector` component.
- **Enemy Data**: Drag `DefaultEnemyData.asset`.
- **Max Health**: 50 (or leave as auto-filled from EnemyData).
- **Current Health**: 50.

### 2.3 Set the GameObject layer to "Enemy"

- Select the `Enemy_Prefab` GameObject.
- In the Inspector **Layer** dropdown (top-right), set to `Enemy` (create if needed: Window → Tags and Layers).
- Repeat for any child colliders.

### 2.4 Save as a Prefab

1. Drag `Enemy_Prefab` from Hierarchy into **Assets/Prefabs** (create folder if needed).
2. Delete the original from the Hierarchy (the prefab copy will remain in Assets).

---

## Step 3: Create the Enemy Layer

1. **Window → Tags and Layers**.
2. Under **Layers**, find an empty slot and enter `Enemy`.
3. Click **Save**.

Confirm in **Physics → Layer Collision Matrix** that Enemy-to-Enemy collisions are handled as needed (usually disabled to allow agents to cluster).

---

## Step 4: Create Waypoints / EnemyRoute

### Option A: Simple waypoint placement

1. Create an empty GameObject: **Right-click Hierarchy → Create Empty** → name it `EnemyRoute`.
2. Create child empty Transforms for each waypoint:
   - **Right-click EnemyRoute → Create Empty → Child** → name it `Waypoint_0`.
   - Move it to the first waypoint position.
   - Repeat for `Waypoint_1`, `Waypoint_2`, etc.
3. In the Enemy Prefab Inspector, expand **EnemyMovement**:
   - Set **Waypoints** Size to 3 (or however many you have).
   - Drag each `Waypoint_X` Transform into the array slots **in order**.

### Option B: Assign EnemyRoute script (optional)

If your project has an `EnemyRoute` component, assign it to EnemyMovement:
- Drag the EnemyRoute GameObject (parent of waypoints) into the **EnemyRoute** field on `EnemyMovement`.
- The script will auto-populate the Waypoints array from children.

---

## Step 5: Set up the NavMesh

1. **Select all walkable surfaces** in your scene (ground, platforms, etc.).
2. In the Inspector, check **Navigation Static**.
3. **Window → AI → Navigation**.
4. In the **Bake** tab:
   - Set **Agent Radius**: 0.5.
   - Set **Agent Height**: 2.0.
   - Set **Agent Slope**: 45.
   - Leave other defaults.
5. Click **Bake**.
6. Verify the NavMesh is visible (blue overlay on walkable surfaces).

---

## Step 6: Configure the EnemySpawner

### 6.1 Create the EnemySpawner GameObject

1. **Right-click Hierarchy → Create Empty** → name it `EnemySpawner`.
2. **Add Component → EnemySpawner** (script).

### 6.2 Configure EnemySpawner in Inspector

- **Enemy Prefab**: Drag the `Enemy_Prefab` from Assets/Prefabs.
- **Available Routes** (EnemyRoute[]):
  - Set **Size** to the number of routes (e.g., 1-3).
  - Drag your EnemyRoute GameObjects (created in Step 4).
- **Enemies To Spawn**: 10 (per wave).
- **Spawn Interval**: 2.0 (seconds between spawns).
- **Spawn Point**: Drag an empty Transform or create one as a child.
- **Infinite Waves**: Check if you want endless spawning.
- **Delay Between Waves**: 3.0 (seconds).
- **Wave Time Threshold**: 30.0 (max time for a wave).
- **Enemy Type Variants** (List<EnemyData>):
  - Set **Size** to the number of enemy types (e.g., 2-4).
  - Drag different `EnemyData` ScriptableObjects (Fast, Tank, Venom, etc.).
  - **IMPORTANT**: The spawner will randomly select from this list based on difficulty.
- **Difficulty Preset**: Normal (or Easy/Hard to control spawn distribution).

### 6.3 Example configuration:
```
- Enemies To Spawn: 5
- Spawn Interval: 1.5s
- Enemy Type Variants: [FastSpiderData, TankSpiderData, VenomSpiderData]
- Difficulty Preset: Normal
```

The spawner will automatically:
- Pick a random EnemyData from the variants list based on difficulty weights
- Assign it to each spawned enemy BEFORE initialization (no more null warnings!)
- Components will auto-find the Player by tag "Player" if not manually assigned

---

## Step 7: Test in Play Mode

1. **Enter Play Mode** (click Play in Unity Editor).
2. **Verify enemy behavior**:
   - Enemies spawn at spawn points.
   - Enemies follow the waypoint path smoothly.
   - If the Player is not on the NavMesh, enemies should stay on the path.
   - Move the Player onto the NavMesh and within Detection Range (6.0 units by default).
   - Enemy should stop pathing and attack the Player.
   - Move the Player off the NavMesh or far away.
   - Enemy should resume following waypoints.

### If something is wrong:

- **Enemy doesn't move**: Check NavMesh is baked, waypoints are assigned, NavMeshAgent is enabled.
- **Enemy moves but stops randomly**: Check `Stopping Distance` (should be 0.5), `Separation Radius` (should be 1.0).
- **Enemy doesn't engage player**: Check `Detection Range` is reasonable (6.0), player is on NavMesh, `NavMeshPlayerDetector` has Player Transform assigned.
- **Null EnemyData warning**: Make sure `EnemySpawner.enemyData` has the `DefaultEnemyData.asset` assigned, and `EnemyMovement.enemyData` is set in the prefab.

---

## Step 8: Tweak and Polish

Adjust these values in Play Mode (use Inspector to pause and modify):

- **EnemyMovement.Speed**: 2.0–5.0 (higher = faster).
- **EnemyStateMachine.Detection Range**: 5.0–10.0 (closer = harder to notice, farther = easier).
- **NavMeshPlayerDetector.Tolerance**: 1.0–2.0 (tighter = player must be more precisely on NavMesh).
- **NavMeshAgent.Stopping Distance**: 0.3–1.0 (tighter = stops closer to waypoint).
- **Separation Radius**: 0.5–1.5 (larger = more "breathing room" between enemies).

---

## Summary Checklist

- [ ] EnemyData ScriptableObject created with sensible defaults.
- [ ] Enemy Prefab created with all 5 components (Enemy, EnemyMovement, EnemyStateMachine, NavMeshPlayerDetector, NavMeshAgent).
- [ ] All component references wired in the Inspector.
- [ ] Enemy layer created and assigned to the prefab.
- [ ] Waypoints created and assigned to EnemyMovement.
- [ ] NavMesh baked and visible in Scene view.
- [ ] EnemySpawner created and configured with prefab, spawn points, and waves.
- [ ] EnemyData assigned to EnemySpawner (to avoid null warnings).
- [ ] Play Mode test: enemies spawn, follow path, engage player on NavMesh, resume path.
- [ ] Tweaked detection range, speed, and separation radius to taste.

---

## File Locations

- **Scripts**: `Assets/Scripts/Enemy/Enemy.cs`, `Assets/Scripts/EnemyMovement.cs`, `Assets/Scripts/Enemy/EnemyStateMachine.cs`, `Assets/Scripts/Enemy/NavMeshPlayerDetector.cs`, `Assets/Scripts/Enemy/EnemySpawner.cs`.
- **ScriptableObject**: `Assets/Resources/DefaultEnemyData.asset` (or your chosen location).
- **Prefab**: `Assets/Prefabs/Enemy_Prefab.prefab`.
- **Scene**: Your play scene with Player, NavMesh, spawn points, and waypoints.

---

## Key Features of the New System

1. **Dual-condition engagement**: Enemy only attacks if player is both within `Detection Range` AND on the NavMesh.
2. **Smart waypoint following**: Uses `EnemyMovement.FindNearestWaypoint()` to resume path seamlessly after combat.
3. **Agent separation**: Prevents stacking with other enemies via `GetSeparatedNavMeshPosition()`.
4. **Modular design**: Each component can be tested and tweaked independently.
5. **Event-driven**: Components communicate via C# Actions (no tight coupling).
6. **Auto-coupling**: Components automatically find the Player by tag "Player" if not manually assigned.
7. **Enemy variants**: Spawner randomly selects from `Enemy Type Variants` list based on difficulty preset.

---

## Auto-Coupling Features (New!)

The refactored system now includes **automatic component wiring**:

### Auto-Find Player
All components that need a player reference will **automatically** find it if not manually assigned:
- **NavMeshPlayerDetector**: Tries `PlayerHealth.Instance`, then `GameObject.FindWithTag("Player")`.
- **EnemyStateMachine**: Same auto-find logic for `PlayerHealth` component.
- **Enemy**: Auto-finds player during `Initialize()` if not set.

**What you need to do:**
- Make sure your Player GameObject has the tag **"Player"** (Project Settings → Tags and Layers).
- Alternatively, ensure `PlayerHealth.Instance` singleton is set up.

### Auto-Assign Enemy Data
The spawner now correctly assigns `EnemyData` **before** component initialization:
1. Spawner picks a random `EnemyData` from the `Enemy Type Variants` list.
2. Calls `enemy.SetEnemyData()` to assign it.
3. **Then** calls `enemy.Initialize()` which passes the data to all components.

**Result**: No more "EnemyMovement initialized with null EnemyData" warnings!

### Enemy Variants System
Create multiple `EnemyData` assets for different enemy types:
- **FastSpider.asset**: High speed (5.0), low health (30).
- **TankSpider.asset**: Low speed (2.0), high health (100).
- **VenomSpider.asset**: Medium speed (3.5), medium health (50), high damage.

Add all variants to the `EnemySpawner.Enemy Type Variants` list, and the spawner will randomly select based on the difficulty preset:
- **Easy**: 70% Fast, 30% Tank.
- **Normal**: 50% Fast, 30% Tank, 20% Venom.
- **Hard**: 30% Fast, 30% Tank, 25% Venom, 15% Goliath.

---

Enjoy your refactored enemy system!
