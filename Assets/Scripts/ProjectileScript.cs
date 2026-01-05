using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileScript : MonoBehaviour
{
    public GameObject target;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float damage = 5f;

    [Header("Aiming")]
    [SerializeField] private bool aimAtColliderCenter = true;
    [SerializeField] private Vector3 aimOffset = Vector3.zero;
    [SerializeField] private Vector3 modelRotationOffsetEuler = new Vector3(0f, 0f, -90f); // missile mesh faces up by default

    [Header("Hit")]
    [SerializeField] private bool useTriggerHits = true; // safer when enemies/projectiles use kinematic rigidbodies
    [SerializeField] private bool useSpherecastHits = true; // recommended for fast projectiles (prevents tunneling)
    [SerializeField] private bool debugLogs = false;

    private Collider cachedCollider;
    private float hitRadius = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        // If we rely on trigger hits, ensure our collider is a trigger.
        if (useTriggerHits)
        {
            cachedCollider = GetComponent<Collider>();
            if (cachedCollider != null) cachedCollider.isTrigger = true;
        }
        else
        {
            cachedCollider = GetComponent<Collider>();
        }

        // Compute a reasonable hit radius from collider bounds (used for spherecast mode)
        if (cachedCollider != null)
        {
            // extents.x is usually good enough; clamp to avoid 0 radius
            hitRadius = Mathf.Max(0.05f, cachedCollider.bounds.extents.magnitude * 0.25f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = GetAimPoint(target) + aimOffset;
        Vector3 toTarget = targetPos - transform.position;

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            // Face the direction of travel, then apply model offset so the mesh points forward
            Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = look * Quaternion.Euler(modelRotationOffsetEuler);
        }

        // Move (but first, do a sweep hit test to avoid tunneling)
        Vector3 currentPos = transform.position;
        Vector3 nextPos = Vector3.MoveTowards(currentPos, targetPos, speed * Time.deltaTime);

        if (useSpherecastHits)
        {
            Vector3 delta = nextPos - currentPos;
            float dist = delta.magnitude;
            if (dist > 0.0001f)
            {
                Ray ray = new Ray(currentPos, delta / dist);
                // Include trigger colliders so we can hit enemies even if their colliders are triggers.
                RaycastHit[] hits = Physics.SphereCastAll(ray, hitRadius, dist, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < hits.Length; i++)
                {
                    GameObject hitGo = hits[i].collider != null ? hits[i].collider.gameObject : null;
                    if (TryDamageEnemy(hitGo))
                        return; // projectile destroyed inside TryDamageEnemy
                }
            }
        }

        transform.position = nextPos;
    }

    private Vector3 GetAimPoint(GameObject targetObject)
    {
        if (!aimAtColliderCenter || targetObject == null) return targetObject != null ? targetObject.transform.position : transform.position;

        Collider c = targetObject.GetComponentInChildren<Collider>();
        if (c != null)
        {
            return c.bounds.center;
        }
        return targetObject.transform.position;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (useTriggerHits) return;
        TryDamageEnemy(other.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useTriggerHits) return;
        TryDamageEnemy(other.gameObject);
    }

    private bool TryDamageEnemy(GameObject hitObject)
    {
        if (hitObject == null) return false;

        // Enemy colliders may be on children; use InParent.
        Enemy enemy = hitObject.GetComponentInParent<Enemy>();
        if (enemy == null && hitObject.CompareTag("Enemy"))
        {
            enemy = hitObject.GetComponent<Enemy>();
        }

        if (enemy == null) return false;

        enemy.TakeDamage(damage);
        if (debugLogs)
        {
            Debug.Log($"Projectile hit '{enemy.gameObject.name}' for {damage} damage. RemainingHealth={enemy.GetHealth()}");
        }
        Destroy(gameObject);
        return true;
    }

    // Set by TowerScript on spawn (damage depends on turret variant)
    public void SetDamage(float dmg)
    {
        damage = Mathf.Max(0f, dmg);
    }
}
