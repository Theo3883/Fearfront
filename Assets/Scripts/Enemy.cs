using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float stoppingDistance = 0.1f;
    [SerializeField] private float lookaheadDistance = 3f;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    
    // --- NEW: Health Settings ---
    [Header("Health & Death")]
    [SerializeField] private int maxHealth = 1;
    private int currentHealth;
    // ----------------------------

    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private EnemySpawner spawner;
    private bool isMoving = true;
    private Rigidbody rb;
    
    // --- NEW: Reference to Dismantle Script ---
    private SpiderDismantle spiderDismantle; 
    private bool isDead = false;
    // ------------------------------------------

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // --- NEW: Grab the component ---
        spiderDismantle = GetComponent<SpiderDismantle>();
        currentHealth = maxHealth;
        // -------------------------------
    }

    public void Initialize(Transform[] path, EnemySpawner enemySpawner)
    {
        waypoints = path;
        spawner = enemySpawner;
        currentWaypointIndex = 0;
        isMoving = true;
        
        // --- NEW: Reset Health on Initialize (for object pooling if used later) ---
        isDead = false;
        currentHealth = maxHealth;
        // -------------------------------------------------------------------------
        
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            UpdateRotationTowardPath();
        }
    }

    private void Update()
    {
        // --- NEW: Guard Clause ---
        // If we are dead, do absolutely nothing. Don't move, don't rotate.
        if (isDead) return;
        // -------------------------

        // Check if spider is grabbed - stop movement
        SpiderInteractable spiderInteractable = GetComponent<SpiderInteractable>();
        if (spiderInteractable != null && spiderInteractable.IsGrabbed())
        {
            if (isMoving)
            {
                StopMoving();
            }
            return; // Don't move while grabbed
        }
        
        if (!isMoving || waypoints == null || waypoints.Length == 0)
            return;

        MoveAlongPath();
    }

    // ... [MoveAlongPath, UpdateRotationTowardPath, GetLookaheadDirection, GetLookaheadPosition remain unchanged] ...

    private void MoveAlongPath()
    {
        if (currentWaypointIndex >= waypoints.Length)
        {
            ReachedEnd();
            return;
        }

        Transform currentWaypoint = waypoints[currentWaypointIndex];
        Vector3 direction = (currentWaypoint.position - transform.position).normalized;
        
        if (rb != null)
        {
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            transform.position += direction * moveSpeed * Time.deltaTime;
        }

        UpdateRotationTowardPath();

        float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint.position);
        if (distanceToWaypoint < stoppingDistance)
        {
            currentWaypointIndex++;
        }
    }

    private void UpdateRotationTowardPath()
    {
        Vector3 lookDirection = GetLookaheadDirection();

        if (lookDirection.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            targetRotation *= Quaternion.Euler(rotationOffset);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private Vector3 GetLookaheadDirection()
    {
        Vector3 lookaheadPos = GetLookaheadPosition();
        return (lookaheadPos - transform.position).normalized;
    }

    private Vector3 GetLookaheadPosition()
    {
        if (currentWaypointIndex >= waypoints.Length)
            return waypoints[waypoints.Length - 1].position;

        Vector3 currentPos = transform.position;
        Vector3 currentTarget = waypoints[currentWaypointIndex].position;
        
        float distToCurrentTarget = Vector3.Distance(currentPos, currentTarget);

        if (distToCurrentTarget > lookaheadDistance)
        {
            return currentPos + (currentTarget - currentPos).normalized * lookaheadDistance;
        }

        if (currentWaypointIndex + 1 < waypoints.Length)
        {
            Vector3 nextTarget = waypoints[currentWaypointIndex + 1].position;
            float remainingDist = lookaheadDistance - distToCurrentTarget;
            return currentTarget + (nextTarget - currentTarget).normalized * remainingDist;
        }

        return currentTarget;
    }

    private void ReachedEnd()
    {
        isMoving = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
        spawner.OnEnemyReachedEnd(this);
        Destroy(gameObject);
    }
    
    // --- ADD THIS NEW METHOD ---
    public void ActivateSelfDestruct(float delay)
    {
        // "Invoke" is a built-in Unity function that runs a method after a delay
        Invoke("Die", delay);
    }
    // ---------------------------
    
    // --- NEW: Health and Death Logic ---
    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        isMoving = false;

        // 1. Stop Physics immediately
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // Stop the root object from falling/moving
        }

        // 2. Trigger the Dismantle Effect
        if (spiderDismantle != null)
        {
            spiderDismantle.ActivateDismantle();
        }
        else
        {
            // Fallback if you forgot to add the script
            Destroy(gameObject);
        }

        // 3. Disable this script so Update() stops running entirely
        this.enabled = false;
    }
    // -----------------------------------

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public void StopMoving()
    {
        isMoving = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    public void ResumeMoving()
    {
        isMoving = true;
    }
}