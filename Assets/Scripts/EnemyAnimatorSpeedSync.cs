using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Keeps the spider/enemy animation speed updated in real time based on actual NavMeshAgent velocity.
/// Your current Spider animator controller has no parameters, so we drive Animator.speed directly.
///
/// Attach this to the enemy prefab (same object that has Animator / NavMeshAgent).
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemyAnimatorSpeedSync : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;

    [Header("Speed Mapping")]
    [SerializeField] private float referenceMoveSpeed = 5f; // agent speed that maps to animSpeedAtReference
    [SerializeField] private float animSpeedAtReference = 1f;
    [SerializeField] private float minAnimSpeed = 0.25f;
    [SerializeField] private float maxAnimSpeed = 2.0f;
    [SerializeField] private float smoothing = 12f; // higher = snappier

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
    }

    private void Update()
    {
        if (animator == null) return;
        if (agent == null)
        {
            animator.speed = animSpeedAtReference;
            return;
        }

        float v = agent.velocity.magnitude;
        float refSpeed = Mathf.Max(0.01f, referenceMoveSpeed);

        // Linear mapping: (v / ref) * animSpeedAtReference, clamped
        float targetAnimSpeed = (v / refSpeed) * animSpeedAtReference;
        targetAnimSpeed = Mathf.Clamp(targetAnimSpeed, minAnimSpeed, maxAnimSpeed);

        float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, smoothing) * Time.deltaTime);
        animator.speed = Mathf.Lerp(animator.speed, targetAnimSpeed, t);
    }
}


