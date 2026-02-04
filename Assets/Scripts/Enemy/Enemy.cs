using UnityEngine;
using UnityEngine.AI;
using System;

/// <summary>
/// Enemy coordinator that manages health, death, and components (EnemyMovement, EnemyStateMachine, NavMeshPlayerDetector).
/// This is a simplified refactoring that delegates movement and state logic to specialized components.
/// </summary>
public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;
    private float healthMax = 0f;
    private float currentHealth = 0f;
    private Vector3 initialScale = Vector3.one;
    
    [SerializeField] private NavMeshPlayerDetector playerDetector;
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private EnemyStateMachine stateMachine;
    private Rigidbody rb;
    private NavMeshAgent agent;
    
    private SpiderDismantle spiderDismantle;
    private bool isDead = false;
    
    private EnemySpawner spawner;
    
    // Attack-related fields
    private Transform playerTransform;
    private PlayerHealth playerHealthRef;
    private float attackCooldownTimer = 0f;
    
    // ===== Events =====
    public event Action<EnemyState> OnStateChanged;
    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    private void Awake()
    {
        // Get references to components
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        spiderDismantle = GetComponent<SpiderDismantle>();
        initialScale = transform.localScale;
        
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
        
        // Apply relative scaling with random variation (±15%)
        float scaleVariation = UnityEngine.Random.Range(0.85f, 1.15f);
        float multiplier = Mathf.Clamp(enemyData.VisualScaleMultiplier, 0.1f, 5.0f);
        transform.localScale = initialScale * multiplier * scaleVariation;
        
        // Apply color from EnemyData with random HSV variation
        Color typeColor = enemyData.TypeColor;
        
        // Add per-instance color variety (except for Boss variants)
        if (enemyData.VariantType != EnemyVariantType.Boss)
        {
            Color.RGBToHSV(typeColor, out float h, out float s, out float v);
            h = (h + UnityEngine.Random.Range(-0.1f, 0.1f) + 1f) % 1.0f; // Hue shift ±10%
            s = Mathf.Clamp01(s * UnityEngine.Random.Range(0.8f, 1.2f)); // Saturation variation ±20%
            typeColor = Color.HSVToRGB(h, s, v);
        }
        bool isGhost = enemyData.Family == EnemyFamily.Ghost;
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
                    float intensity = 1.0f;
                    if (EnemyVisualsConfig.Instance != null)
                        intensity = EnemyVisualsConfig.Instance.BaseColorTintIntensity;

                    // FORCE BOOST for Ghosts (Dark texture needs more tint)
                    if (isGhost) intensity = Mathf.Max(intensity, 0.8f);

                    // Lerp from White (no tint) to TypeColor based on intensity
                    Color appliedColor = Color.Lerp(Color.white, typeColor, intensity);

                    if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", appliedColor);
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", appliedColor);

                    // Some shaders use emission for visible tinting
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        // Boost emission for Ghosts to make them glow in their color
                        float emissionBoost = isGhost ? 2.0f : 0.35f;
                        mat.SetColor("_EmissionColor", typeColor * emissionBoost);
                        mat.EnableKeyword("_EMISSION");
                    }

                    tinted[i] = mat;
                }

                renderer.materials = tinted;
            }
        }
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
            return;
        }
        
        enemyMovement.UpdateMovement();
        
        PlayerHealth playerHealth = PlayerHealth.Instance;
        Vector3 playerPosition = Vector3.zero;
        bool havePlayerPosition = false;
        
        // Priority 1: PlayerHealth singleton (most reliable)
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
            // Priority 3: Main Camera (fallback)
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
        
        OnDeath?.Invoke();

        DisableAllComponents();

        if (spiderDismantle != null)
        {
            if (enemyData != null && (enemyData.Family == EnemyFamily.Ghost || enemyData.Family == EnemyFamily.Chicken))
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
        if (enemyData == null || playerHealthRef == null)
        {
            return;
        }
        
        if (playerHealthRef.IsImmune)
        {
            return;
        }

        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        Vector3 enemyPos = transform.position;
        float distanceXZ = Vector2.Distance(new Vector2(enemyPos.x, enemyPos.z), new Vector2(playerPosition.x, playerPosition.z));
        float attackRange = enemyData.AttackRange;
        float distanceY = Mathf.Abs(enemyPos.y - playerPosition.y);

        Vector3 directionToPlayer = (playerPosition - enemyPos).normalized;
        directionToPlayer.y = 0;
        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }

        if (distanceXZ > attackRange)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(playerPosition, out hit, 5f, NavMesh.AllAreas))
                {
                    agent.isStopped = false;
                    agent.SetDestination(hit.position);
                }
            }
            return;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (distanceY < 3.0f && attackCooldownTimer <= 0f)
        {
            if (playerHealthRef.IsAlive())
            {
                playerHealthRef.Damage(enemyData.AttackDamage);
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

