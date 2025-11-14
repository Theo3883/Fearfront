using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float stoppingDistance = 0.1f;
    [SerializeField] private float lookaheadDistance = 3f;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    
    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private EnemySpawner spawner;
    private bool isMoving = true;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Initialize(Transform[] path, EnemySpawner enemySpawner)
    {
        waypoints = path;
        spawner = enemySpawner;
        currentWaypointIndex = 0;
        isMoving = true;
        
        if (waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            UpdateRotationTowardPath();
        }
    }

    private void Update()
    {
        if (!isMoving || waypoints == null || waypoints.Length == 0)
            return;

        MoveAlongPath();
    }

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
