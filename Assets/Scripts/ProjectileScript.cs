using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileScript : MonoBehaviour
{
    public GameObject target;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float damage = 5f;

    [Header("Launch")]
    [SerializeField] private bool snapToGunTipOnSpawn = true;
    [SerializeField] private float gunTipForwardClearance = 0.05f; // push slightly beyond the tip so we don't start inside the mesh
    private Transform launchGun;

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
        // If TowerScript provided a gun, optionally snap spawn to the tip of the gun mesh.
        if (snapToGunTipOnSpawn && launchGun != null)
        {
            SnapToGunTip(launchGun);
        }

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

    // Set by TowerScript on spawn so we can compute the gun tip accurately.
    public void SetLaunchGun(Transform gunTransform)
    {
        launchGun = gunTransform;
    }

    private void SnapToGunTip(Transform gunTransform)
    {
        if (gunTransform == null) return;

        // Try to find renderers under the gun (the prefab gun is named "Gun" and has a MeshRenderer).
        Renderer[] renderers = gunTransform.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            // Fallback: at least match gun position/rotation.
            transform.SetPositionAndRotation(gunTransform.position, gunTransform.rotation);
            return;
        }

        // Use the actual shot direction if we already have a target; this makes it consistent across turret variants.
        Vector3 dir = gunTransform.forward.normalized;
        if (target != null)
        {
            Vector3 aimPoint = GetAimPoint(target) + aimOffset;
            Vector3 to = aimPoint - gunTransform.position;
            if (to.sqrMagnitude > 0.0001f) dir = to.normalized;
        }
        float bestDot = float.NegativeInfinity;
        Vector3 bestPoint = gunTransform.position;

        // For each renderer AABB in world space, pick the support point in 'dir' (farthest corner along dir).
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Bounds b = renderers[i].bounds;
            Vector3 ext = b.extents;
            // Use >= 0 instead of Mathf.Sign to avoid 0 results (which would bias toward the center).
            Vector3 sign = new Vector3(dir.x >= 0f ? 1f : -1f, dir.y >= 0f ? 1f : -1f, dir.z >= 0f ? 1f : -1f);
            Vector3 support = b.center + Vector3.Scale(ext, sign);

            float d = Vector3.Dot(support, dir);
            if (d > bestDot)
            {
                bestDot = d;
                bestPoint = support;
            }
        }

        transform.SetPositionAndRotation(bestPoint + dir * gunTipForwardClearance, gunTransform.rotation);
    }
}
