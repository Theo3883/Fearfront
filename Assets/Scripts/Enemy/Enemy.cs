using UnityEngine;
using UnityEngine.AI;
using System;

/// <summary>
/// Enemy coordinator that manages health, death, and components (EnemyMovement, EnemyStateMachine, NavMeshPlayerDetector).
/// This is a simplified refactoring that delegates movement and state logic to specialized components.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;
    private float healthMax = 0f;
    private float currentHealth = 0f;
    
    [SerializeField] private NavMeshPlayerDetector playerDetector;
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private EnemyStateMachine stateMachine;
    private Rigidbody rb;
    private NavMeshAgent agent;
    
    private SpiderDismantle spiderDismantle;
    private bool isDead = false;
    
    private EnemySpawner spawner;
    private AudioSource audioSource;
    
    // Attack-related fields
    private Transform playerTransform;
    private PlayerHealth playerHealthRef;
    private float attackCooldownTimer = 0f;
    
    // ===== Events =====
    public event Action<EnemyState> OnStateChanged;
    public event Action<float, float> OnHealthChanged;

    private void Awake()
    {
        // Get references to components
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        spiderDismantle = GetComponent<SpiderDismantle>();
        audioSource = GetComponent<AudioSource>();
        
        // Configure Rigidbody to be kinematic (NavMeshAgent handles movement)
        // This prevents physics-based pushing between enemies
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // Get component references for coordination
        playerDetector = GetComponent<NavMeshPlayerDetector>();
        enemyMovement = GetComponent<EnemyMovement>();
        stateMachine = GetComponent<EnemyStateMachine>();
        
        if (playerDetector == null)
            Debug.LogError($"Enemy '{gameObject.name}' missing NavMeshPlayerDetector component!");
        if (enemyMovement == null)
            Debug.LogError($"Enemy '{gameObject.name}' missing EnemyMovement component!");
        if (stateMachine == null)
            Debug.LogError($"Enemy '{gameObject.name}' missing EnemyStateMachine component!");
        
        // Wire up state change event forwarding from stateMachine to Enemy
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged += (newState) => OnStateChanged?.Invoke(newState);
        }
    }

    /// <summary>
    /// Sets EnemyData for this enemy instance
    /// Loads health and other stats from data
    /// FAILS HARD if data is null
    /// </summary>
    public void SetEnemyData(EnemyData data)
    {
        // FAIL HARD if data is null
        if (data == null)
        {
            Debug.LogError("Enemy.SetEnemyData: EnemyData cannot be null!");
            return;
        }

        enemyData = data;
        if (enemyData.IsValid())
        {
            healthMax = enemyData.MaxHealth;
            currentHealth = enemyData.Health;
        }
        else
        {
            Debug.LogError($"Enemy.SetEnemyData: EnemyData '{data.name}' is not valid!");
        }
        
        ApplyVisualDifferentiation();
    }


    /// <summary>
    /// Applies visual differentiation (scale and color) from EnemyData to the enemy GameObject
    /// </summary>
    private void ApplyVisualDifferentiation()
    {
        if (enemyData == null) return;
        
        // Get visual scale from EnemyData and clamp to reasonable range [0.5, 12.0]
        float scale = Mathf.Clamp(enemyData.VisualScale, 0.5f, 12.0f);
        transform.localScale = Vector3.one * scale;
        
        // Apply color from EnemyData to all child renderers
        Color typeColor = enemyData.TypeColor;
        bool isGhost = IsGhostType(enemyData.Type);
        Shader urpLit = null;
        if (isGhost)
        {
            urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit");
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                Material[] shared = renderer.materials;
                if (shared == null || shared.Length == 0)
                    continue;

                Material[] tinted = new Material[shared.Length];
                for (int i = 0; i < shared.Length; i++)
                {
                    Material src = shared[i];
                    if (src == null)
                    {
                        tinted[i] = null;
                        continue;
                    }

                    Material mat = new Material(src);

                    // Fix Unity "pink" error shader on Ghost prefabs by switching to a URP shader.
                    // Note: if the prefab was authored for Built-in/HDRP, URP will render it pink until converted.
                    if (isGhost && urpLit != null)
                    {
                        Shader shader = mat.shader;
                        if (shader == null || shader.name == "Hidden/InternalErrorShader")
                        {
                            mat.shader = urpLit;
                        }
                    }

                    // Support both Built-in and SRP shaders (URP/HDRP)
                    if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", typeColor);
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", typeColor);

                    // Some shaders use emission for visible tinting
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", typeColor * 0.35f);
                        mat.EnableKeyword("_EMISSION");
                    }

                    tinted[i] = mat;
                }

                renderer.materials = tinted;
            }
        }
    }

    private bool IsGhostType(EnemyType type)
    {
        return type == EnemyType.WispGhost || type == EnemyType.PhantomGhost || type == EnemyType.PoltergeistGhost || type == EnemyType.ReaperGhost;
    }

    /// <summary>
    /// Initialize the enemy with waypoints and spawner reference
    /// Sets up component dependencies and initial state
    /// Waypoints are NOT stored in Enemy; they are passed directly to EnemyMovement
    /// </summary>
    public void Initialize(Transform[] path, EnemySpawner enemySpawner)
    {
        spawner = enemySpawner;
        isDead = false;
        
        // The most reliable way to find the player is via the PlayerHealth singleton.
        // Ensure PlayerHealth is attached to the object that actually moves (e.g., XR Origin).
        PlayerHealth playerHealth = PlayerHealth.Instance;
        Transform playerTransformLocal = null;
        
        if (playerHealth != null)
        {
            playerTransformLocal = playerHealth.transform;
        }
        else
        {
            // Fallback 1: Look for XR Origin (common in VR)
            GameObject xrOrigin = GameObject.Find("XR Origin") ?? GameObject.Find("XROrigin");
            if (xrOrigin != null)
            {
                playerTransformLocal = xrOrigin.transform;
            }
            // Fallback 2: Tagged Player
            else
            {
                GameObject playerObj = null;
                try { playerObj = GameObject.FindWithTag("Player"); } catch { playerObj = null; }
                if (playerObj != null)
                    playerTransformLocal = playerObj.transform;
                // Fallback 3: Main Camera
                else if (Camera.main != null)
                    playerTransformLocal = Camera.main.transform;
            }
        }

        // Store player references for use in attack logic
        this.playerTransform = playerTransformLocal;
        this.playerHealthRef = playerHealth;

        if (playerTransformLocal != null && playerDetector != null)
        {
            playerDetector.SetPlayerReference(playerTransformLocal);
        }
        
        // Initialize movement with waypoints (Enemy does NOT store waypoints)
        if (enemyMovement != null && path != null && path.Length > 0)
        {
            enemyMovement.Initialize(path, enemyData);
            
            // Subscribe to final waypoint reached event for proper despawning
            enemyMovement.OnFinalWaypointReached += HandleFinalWaypointReached;
        }
        
        // Initialize state machine with detection range from enemy data or default
        float detectionRadius = 25f;
        if (enemyData != null && enemyData.IsValid())
        {
            detectionRadius = enemyData.DetectionRadius;
        }
        
        if (stateMachine != null && playerDetector != null)
        {
            stateMachine.Initialize(playerDetector, detectionRadius, playerHealth);
            
            // Subscribe to state machine events for proper movement control
            stateMachine.OnEngagingPlayer += HandleEngagingPlayer;
            stateMachine.OnDisengagingPlayer += HandleDisengagingPlayer;
            stateMachine.OnResumePathMovement += HandleResumePathMovement;
        }

        // Initialize Audio
        if (enemyData != null && audioSource != null)
        {
            // Configure 3D Spatial Audio
            audioSource.spatialBlend = 1.0f; // Enable full 3D audio so sound is positional
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic; // Natural sound attenuation
            audioSource.minDistance = 2f; // Distance where sound is at full volume
            audioSource.maxDistance = 30f; // Distance where sound becomes inaudible

            // Determine base volume for the AudioSource
            // Using AmbientVolume as the base volume for the source to support looping
            float baseVolume = (enemyData.AmbientSound != null) ? enemyData.AmbientVolume : 1.0f;
            audioSource.volume = baseVolume;

            // Play spawn sound (scaled relative to base volume)
            if (enemyData.SpawnSound != null)
            {
                // Calculate scale: Target / Base
                // If Base is 0.5 and Target is 1.0 -> Scale = 2.0
                float scale = (baseVolume > 0.01f) ? (enemyData.SpawnVolume / baseVolume) : enemyData.SpawnVolume;
                audioSource.PlayOneShot(enemyData.SpawnSound, scale);
            }

            // Start ambient loop
            if (enemyData.AmbientSound != null)
            {
                audioSource.clip = enemyData.AmbientSound;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up event subscriptions to prevent memory leaks
        if (enemyMovement != null)
        {
            enemyMovement.OnFinalWaypointReached -= HandleFinalWaypointReached;
        }
        
        if (stateMachine != null)
        {
            stateMachine.OnEngagingPlayer -= HandleEngagingPlayer;
            stateMachine.OnDisengagingPlayer -= HandleDisengagingPlayer;
            stateMachine.OnResumePathMovement -= HandleResumePathMovement;
        }
    }

    /// <summary>
    /// Handles when enemy reaches the final waypoint
    /// Notifies spawner to despawn this enemy
    /// </summary>
    private void HandleFinalWaypointReached()
    {
        if (spawner != null)
        {
            spawner.OnEnemyReachedEnd(this);
        }
        else
        {
            // If no spawner reference, destroy self directly
            Debug.LogWarning($"Enemy '{gameObject.name}' reached end but has no spawner reference. Destroying self.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Handles when enemy engages player - pause movement immediately.
    /// </summary>
    private void HandleEngagingPlayer()
    {
        if (enemyMovement != null)
        {
            enemyMovement.PauseMovement();
        }
    }

    /// <summary>
    /// Handles when enemy disengages from player - resume movement.
    /// </summary>
    private void HandleDisengagingPlayer()
    {
        if (enemyMovement != null)
        {
            enemyMovement.ResumeMovement();
        }
    }

    /// <summary>
    /// Handles resuming waypoint movement after disengaging from player.
    /// Simplified: just resume movement, enemy continues to current waypoint.
    /// </summary>
    private void HandleResumePathMovement(Vector3 currentPosition)
    {
        if (enemyMovement != null)
        {
            enemyMovement.ResumeMovement();
        }
    }

    private void Update()
    {
        if (isDead) return;
        
        if (stateMachine == null || playerDetector == null || enemyMovement == null)
        {
        Debug.LogWarning($"Enemy '{gameObject.name}' missing required components");
            return;
        }
        
        enemyMovement.UpdateMovement();
        
        PlayerHealth playerHealth = PlayerHealth.Instance;
        Vector3 playerPosition = Vector3.zero;
        bool havePlayerPosition = false;
        
        // Priority 1: PlayerHealth singleton transform (most reliable in VR)
        if (playerHealth != null)
        {
            playerPosition = playerHealth.transform.position;
            havePlayerPosition = true;
        }
        // Priority 2: XR Origin (VR body root)
        else
        {
            GameObject xrOrigin = GameObject.Find("XR Origin") ?? GameObject.Find("XROrigin");
            if (xrOrigin != null)
            {
                playerPosition = xrOrigin.transform.position;
                havePlayerPosition = true;
            }
            // Priority 3: Main Camera (fallback for non-VR)
            else if (Camera.main != null)
            {
                playerPosition = Camera.main.transform.position;
                havePlayerPosition = true;
            }
            // Priority 4: Tagged Player (final fallback)
            else
            {
                GameObject playerObj = null;
                try { playerObj = GameObject.FindWithTag("Player"); } catch { playerObj = null; }
                
                if (playerObj != null)
                {
                    playerPosition = playerObj.transform.position;
                    havePlayerPosition = true;
                }
            }
        }

        if (havePlayerPosition)
        {
            stateMachine.UpdateState(playerPosition);
            
            // Handle attack logic if in Attacking state
            if (stateMachine.CurrentState == EnemyState.Attacking)
            {
                HandleAttackingState(playerPosition);
            }
        }
    }

    /// <summary>
    /// Gets the current health of this enemy
    /// </summary>
    public float GetHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Gets the max health of this enemy
    /// </summary>
    public float GetMaxHealth()
    {
        return healthMax;
    }

    /// <summary>
    /// Returns whether this enemy is dead
    /// </summary>
    public bool IsDead()
    {
        return isDead;
    }

    /// <summary>
    /// Damages the enemy and triggers death if health <= 0
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth, healthMax);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Kills the enemy, disables all components, and triggers dismantle effect
    /// </summary>
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Play death sound
        if (enemyData != null && enemyData.DeathSound != null)
        {
            if (spiderDismantle != null && audioSource != null)
            {
                // Calculate relative volume if using the attached source
                float baseVolume = audioSource.volume;
                float scale = (baseVolume > 0.01f) ? (enemyData.DeathVolume / baseVolume) : enemyData.DeathVolume;
                audioSource.PlayOneShot(enemyData.DeathSound, scale);
            }
            else
            {
                // Fallback if object is destroyed immediately - use PlayClipAtPoint (absolute volume)
                AudioSource.PlayClipAtPoint(enemyData.DeathSound, transform.position, enemyData.DeathVolume);
            }
        }

        DisableAllComponents();

        if (spiderDismantle != null)
        {
            if (enemyData != null && (IsGhostType(enemyData.Type) || IsChickenType(enemyData.Type)))
            {
                // Chicken/Ghost meshes tend to have pivots that make them appear to sink too fast.
                // Slow the motion down while preserving overall sink distance.
                spiderDismantle.ConfigureTimings(newDismantleDelay: 3f, newSinkSpeed: 0.5f, newSinkDuration: 6f);
            }

            spiderDismantle.ActivateDismantle();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private static bool IsChickenType(EnemyType type)
    {
        return type == EnemyType.FastChicken
               || type == EnemyType.TankChicken
               || type == EnemyType.RabidChicken
               || type == EnemyType.GiantChicken;
    }

    /// <summary>
    /// Disables all movement and combat components
    /// </summary>
    private void DisableAllComponents()
    {
        if (enemyMovement != null)
            enemyMovement.enabled = false;
        
        if (stateMachine != null)
            stateMachine.enabled = false;
        
        if (playerDetector != null)
            playerDetector.enabled = false;
        
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }
    }

    /// <summary>
    /// Re-enables all components after death (useful for respawning scenarios)
    /// </summary>
    public void ResumeAllComponents()
    {
        if (isDead) return;
        
        if (enemyMovement != null)
            enemyMovement.enabled = true;
        
        if (stateMachine != null)
            stateMachine.enabled = true;
        
        if (playerDetector != null)
            playerDetector.enabled = true;
        
        if (rb != null)
            rb.isKinematic = false;
    }

    /// <summary>
    /// Handles attack logic when enemy is in Attacking state.
    /// Enemy stays on path - pauses to attack when player is in range.
    /// Movement is paused by OnEngagingPlayer event.
    /// </summary>
    private void HandleAttackingState(Vector3 playerPosition)
    {
        if (enemyData == null || playerTransform == null || playerHealthRef == null)
        {
            return;
        }
        
        // Don't attack if player is immune
        if (playerHealthRef.IsImmune)
        {
            return;
        }

        // Update cooldown timer
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerPosition);
        float attackRange = enemyData.AttackRange;

        // Rotate to face player
        Vector3 directionToPlayer = (playerPosition - transform.position).normalized;
        directionToPlayer.y = 0; // Keep rotation horizontal
        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }

        // Execute attack if player is in attack range and cooldown is ready
        if (distanceToPlayer <= attackRange && attackCooldownTimer <= 0f)
        {
            if (playerHealthRef.IsAlive())
            {
                playerHealthRef.Damage(enemyData.AttackDamage);
                
                // Play attack sound
                if (audioSource != null && enemyData.AttackSound != null)
                {
                    float baseVolume = audioSource.volume;
                    float scale = (baseVolume > 0.01f) ? (enemyData.AttackVolume / baseVolume) : enemyData.AttackVolume;
                    audioSource.PlayOneShot(enemyData.AttackSound, scale);
                }
                
                attackCooldownTimer = enemyData.AttackCooldown;
            }
        }
    }


    // ===== BACKWARD COMPATIBILITY: State transition methods =====
    // These methods are kept for backward compatibility with existing code
    // They delegate to the EnemyStateMachine where actual state logic lives

    /// <summary>
    /// Gets the current state of the enemy (from EnemyStateMachine)
    /// </summary>
    public EnemyState GetCurrentState()
    {
        if (stateMachine != null)
            return stateMachine.CurrentState;
        return EnemyState.Moving;
    }

    /// <summary>
    /// Transitions the enemy to Attacking state
    /// </summary>
    public void TransitionToAttacking()
    {
        if (stateMachine != null)
        {
            stateMachine.ForceStateChange(EnemyState.Attacking);
        }
        else if (enemyMovement != null)
        {
            enemyMovement.PauseMovement();
        }
    }

    /// <summary>
    /// Transitions the enemy to Idle state
    /// </summary>
    public void TransitionToIdle()
    {
        if (stateMachine != null)
        {
            stateMachine.ForceStateChange(EnemyState.Idle);
        }
        else if (enemyMovement != null)
        {
            enemyMovement.PauseMovement();
        }
    }

    /// <summary>
    /// Transitions the enemy to Moving state
    /// </summary>
    public void TransitionToMoving()
    {
        if (stateMachine != null)
        {
            stateMachine.ForceStateChange(EnemyState.Moving);
        }
        else if (enemyMovement != null)
        {
            enemyMovement.ResumeMovement();
        }
    }

    /// <summary>
    /// Transitions the enemy to Stunned state
    /// </summary>
    public void TransitionToStunned()
    {
        if (stateMachine != null)
        {
            stateMachine.ForceStateChange(EnemyState.Stunned);
        }
        else if (enemyMovement != null)
        {
            enemyMovement.PauseMovement();
        }
    }

    /// <summary>
    /// Recovers the enemy from stunned state
    /// </summary>
    public void ResumeFromStun()
    {
        if (stateMachine != null)
        {
            stateMachine.ForceStateChange(EnemyState.Moving);
        }
        else if (enemyMovement != null)
        {
            enemyMovement.ResumeMovement();
        }
    }

    /// <summary>
    /// Sets move speed (for compatibility, delegates to EnemyMovement if possible)
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        // Speed management is now in EnemyMovement
    }

    /// <summary>
    /// Stops movement (for compatibility)
    /// </summary>
    public void StopMoving()
    {
        if (enemyMovement != null)
            enemyMovement.PauseMovement();
    }

    /// <summary>
    /// Resumes movement (for compatibility)
    /// </summary>
    public void ResumeMoving()
    {
        if (enemyMovement != null)
            enemyMovement.ResumeMovement();
    }

    /// <summary>
    /// Applies a difficulty multiplier to the enemy's stats (Health, Damage, Speed)
    /// </summary>
    public void ApplyDifficulty(float multiplier)
    {
        if (multiplier <= 1.0f) return;

        // Scale Health
        float oldMax = healthMax;
        healthMax *= multiplier;
        currentHealth = (currentHealth / oldMax) * healthMax; // Maintain % health if called mid-life (though usually called at spawn)
        
        OnHealthChanged?.Invoke(currentHealth, healthMax);

        // Scale Move Speed
        if (enemyMovement != null)
        {
            // Note: EnemyMovement config is usually in EnemyData, but we might need to override it 
            // or we assume EnemyMovement reads from EnemyData. 
            // Since EnemyMovement.Initialize reads from EnemyData, we can't easily change it there without a setter.
            // However, we can modify the agent speed directly if needed, or if EnemyMovement has a method.
            // Checking EnemyData again... it's a ScriptableObject, so we shouldn't modify it directly at runtime 
            // as it would affect all enemies.
            
            // To properly scale speed, we should adjust the NavMeshAgent or the component controlling it.
            if (agent != null)
            {
                agent.speed *= Mathf.Sqrt(multiplier); // Scale speed less aggressively (sqrt)
            }
        }
    }
}

