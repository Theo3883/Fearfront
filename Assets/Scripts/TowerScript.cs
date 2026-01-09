using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerScript : MonoBehaviour
{
    private GameObject desirableEnemy = null;

    [Header("Shooting")]
    [SerializeField] private GameObject projectile;
    [SerializeField] private Transform gun; // assign in Inspector or auto-found (supports Gun/Gun 1/RotatingGuns/etc.)
    [SerializeField] private Transform turretPivot; // optional: child named "Turret" in the sci-fi prefab
    [SerializeField] private Vector3 muzzleLocalOffset = new Vector3(0f, 0f, 0.5f); // fallback when we can't compute tip
    [SerializeField] private float muzzleWorldClearance = 0.05f; // push slightly beyond the tip so it doesn't spawn inside
    [SerializeField] private float fireIntervalSeconds = 2f;
    [SerializeField] private float projectileDamage = 5f; // set different values on Turret 1a/1b/1c/1d prefabs

    [Header("VFX")]
    [Tooltip("Optional particle prefab spawned at the muzzle whenever the tower shoots.")]
    [SerializeField] private GameObject muzzleVfxPrefab;
    [SerializeField] private bool parentMuzzleVfxToGun = false;

    [Header("Aiming")]
    [SerializeField] private float turnSpeed = 8f; // higher = snappier (smoothing factor)
    [SerializeField] private float turretYawOffsetDegrees = 0f; // set to 90/-90 if the model faces sideways
    [SerializeField] private bool rotateGunPitch = true;
    [SerializeField] private float gunPitchSpeed = 10f; // higher = snappier
    [SerializeField] private float gunPitchOffsetDegrees = 0f; // set if gun points slightly up/down by default

    [Header("Detection (fallback)")]
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private LayerMask enemyLayerMask = 0; // if 0, we'll try auto-mask "Enemy"
    [SerializeField] private bool retargetOnlyOnNewDetection = true; // avoids jitter: don't switch targets every frame
    private SphereCollider rangeTrigger;

    List<GameObject> enemies = new List<GameObject>();

    private Quaternion gunInitialLocalRotation = Quaternion.identity;
    // Start is called before the first frame update
    void Start()
    {
        if (gun == null)
        {
            gun = FindBestGunTransform(transform);
        }
        if (turretPivot == null)
        {
            turretPivot = FindChildByName(transform, "turret");
        }
        rangeTrigger = GetComponent<SphereCollider>();
        if (enemyLayerMask == 0)
        {
            enemyLayerMask = LayerMask.GetMask("Enemy");
        }

        if (gun != null) gunInitialLocalRotation = gun.localRotation;
        StartCoroutine(shootLogic());
    }

    // Update is called once per frame
    void Update()
    {
        enemies.RemoveAll(item => item == null);///inamicii care ies din range

        // Fallback: if trigger didn't populate list (mis-centered collider, no rigidbodies, etc.),
        // actively scan around the tower so it still works.
        if (enemies.Count == 0)
        {
            RefreshEnemiesInRange();
        }

        // Keep tracking the same target; only acquire a new one if the current target is gone/out of range.
        if (desirableEnemy == null || !enemies.Contains(desirableEnemy))
        {
            desirableEnemy = PickClosestEnemy();
        }

        AimAtEnemy(desirableEnemy);
    }

    IEnumerator shootLogic()
    {
        while (true)
        {
            if (desirableEnemy != null)
            {
                if (projectile == null)
                {
                    yield return new WaitForSeconds(fireIntervalSeconds);
                    continue;
                }

                Vector3 spawnPos = transform.position;
                Quaternion spawnRot = transform.rotation;

                if (gun != null)
                {
                    spawnRot = gun.rotation;

                    Vector3 aimPoint = GetEnemyAimPoint(desirableEnemy);
                    Vector3 dir = aimPoint - gun.position;
                    if (dir.sqrMagnitude < 0.0001f) dir = gun.forward;

                    // Compute the tip along the actual shot direction (works across different gun axis setups).
                    if (!TryComputeGunTipWorldPosition(gun, dir.normalized, muzzleWorldClearance, out spawnPos))
                    {
                        // Fallback: local offset from gun pivot
                        spawnPos = gun.TransformPoint(muzzleLocalOffset);
                    }
                }

                SpawnMuzzleVfx(spawnPos, spawnRot);

                GameObject newProjectile = Instantiate(projectile, spawnPos, spawnRot);
                ProjectileScript script = newProjectile.GetComponent<ProjectileScript>();
                if (script!=null)
                {
                    script.SetDamage(projectileDamage);
                    script.target = desirableEnemy;
                }
            }
            yield return new WaitForSeconds(fireIntervalSeconds);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") && !enemies.Contains(other.gameObject))
        {
            enemies.Add(other.gameObject);
            Debug.Log("INAMIC NOU");

            // Optional: only change target when a NEW enemy is detected.
            if (retargetOnlyOnNewDetection)
            {
                desirableEnemy = other.gameObject;
            }
            else if (desirableEnemy == null)
            {
                desirableEnemy = other.gameObject;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        enemies.Remove(other.gameObject);
        if (desirableEnemy == other.gameObject)
        {
            desirableEnemy = null;
        }
    }

    private GameObject PickClosestEnemy()
    {
        if (enemies.Count == 0) return null;

        GameObject best = null;
        float bestSqr = float.PositiveInfinity;
        Vector3 origin = gun != null ? gun.position : transform.position;

        foreach (GameObject e in enemies)
        {
            if (e == null) continue;
            float d = (e.transform.position - origin).sqrMagnitude;
            if (d < bestSqr)
            {
                bestSqr = d;
                best = e;
            }
        }
        return best;
    }

    private void RefreshEnemiesInRange()
    {
        float radius = detectionRadius;
        Vector3 center = transform.position;

        if (rangeTrigger != null)
        {
            center = rangeTrigger.transform.TransformPoint(rangeTrigger.center);
            float maxScale = Mathf.Max(rangeTrigger.transform.lossyScale.x, rangeTrigger.transform.lossyScale.y, rangeTrigger.transform.lossyScale.z);
            radius = rangeTrigger.radius * maxScale;
        }

        Collider[] hits = Physics.OverlapSphere(center, radius, enemyLayerMask == 0 ? ~0 : enemyLayerMask);
        for (int i = 0; i < hits.Length; i++)
        {
            GameObject go = hits[i].gameObject;
            if (go != null && go.CompareTag("Enemy") && !enemies.Contains(go))
            {
                enemies.Add(go);
            }
        }
    }

    private void AimAtEnemy(GameObject enemy)
    {
        if (enemy == null) return;

        Vector3 aimPoint = GetEnemyAimPoint(enemy);

        // ---- Yaw (turretPivot) ----
        Transform yawPivot = turretPivot != null ? turretPivot : transform;

        Vector3 toEnemyWorld = aimPoint - yawPivot.position;
        toEnemyWorld.y = 0f; // yaw-only
        if (toEnemyWorld.sqrMagnitude < 0.0001f) return;

        Quaternion desiredYawWorld =
            Quaternion.LookRotation(toEnemyWorld.normalized, Vector3.up) *
            Quaternion.Euler(0f, turretYawOffsetDegrees, 0f);

        float yawT = 1f - Mathf.Exp(-Mathf.Max(0.01f, turnSpeed) * Time.deltaTime);
        yawPivot.rotation = Quaternion.Slerp(yawPivot.rotation, desiredYawWorld, yawT);

        // ---- Pitch (gun) ----
        if (!rotateGunPitch || gun == null) return;

        Vector3 toEnemyFromGunWorld = aimPoint - gun.position;
        if (toEnemyFromGunWorld.sqrMagnitude < 0.0001f) return;

        // Compute pitch in gun parent local space, then apply only X rotation (keeps yaw handled by turret pivot).
        Transform gunParent = gun.parent;
        Vector3 toEnemyFromGunLocal = gunParent != null
            ? gunParent.InverseTransformDirection(toEnemyFromGunWorld.normalized)
            : toEnemyFromGunWorld.normalized;

        float horizontal = new Vector2(toEnemyFromGunLocal.x, toEnemyFromGunLocal.z).magnitude;
        float pitchAngle = -Mathf.Atan2(toEnemyFromGunLocal.y, Mathf.Max(0.0001f, horizontal)) * Mathf.Rad2Deg;

        Quaternion desiredGunLocal = gunInitialLocalRotation * Quaternion.Euler(pitchAngle + gunPitchOffsetDegrees, 0f, 0f);
        float pitchT = 1f - Mathf.Exp(-Mathf.Max(0.01f, gunPitchSpeed) * Time.deltaTime);
        gun.localRotation = Quaternion.Slerp(gun.localRotation, desiredGunLocal, pitchT);
    }

    private static Vector3 GetEnemyAimPoint(GameObject enemy)
    {
        if (enemy == null) return Vector3.zero;
        Collider c = enemy.GetComponentInChildren<Collider>();
        if (c != null) return c.bounds.center;
        return enemy.transform.position;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                return child;

            Transform found = FindChildByName(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform FindBestGunTransform(Transform root)
    {
        if (root == null) return null;

        Transform best = null;
        int bestScore = int.MinValue;

        // Prefer transforms that contain "gun" (Gun / Gun 1 / RotatingGuns / etc.) and have renderers under them.
        var queue = new System.Collections.Generic.Queue<Transform>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            Transform cur = queue.Dequeue();
            if (cur == null) continue;

            string n = cur.name ?? string.Empty;
            string lower = n.ToLowerInvariant();
            bool nameMatches = lower == "gun" || lower.StartsWith("gun ") || lower.Contains("gun");

            if (nameMatches)
            {
                int score = 0;
                if (lower == "gun") score += 1000;
                if (lower.StartsWith("gun ")) score += 800;
                if (lower.Contains("rotatingguns")) score += 700;
                if (cur.GetComponentInChildren<Renderer>() != null) score += 200;
                score += cur.childCount; // slight preference for roots with children

                if (score > bestScore)
                {
                    bestScore = score;
                    best = cur;
                }
            }

            for (int i = 0; i < cur.childCount; i++)
                queue.Enqueue(cur.GetChild(i));
        }

        return best;
    }

    private static bool TryComputeGunTipWorldPosition(Transform gunRoot, Vector3 directionWorld, float clearance, out Vector3 tipPos)
    {
        tipPos = gunRoot != null ? gunRoot.position : Vector3.zero;
        if (gunRoot == null) return false;

        Renderer[] renderers = gunRoot.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return false;

        Vector3 dir = directionWorld.sqrMagnitude > 0.0001f ? directionWorld.normalized : gunRoot.forward;

        float bestDot = float.NegativeInfinity;
        Vector3 bestPoint = gunRoot.position;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Bounds b = renderers[i].bounds;
            Vector3 ext = b.extents;

            // Use >= 0 instead of Mathf.Sign to avoid 0 results.
            Vector3 sign = new Vector3(dir.x >= 0f ? 1f : -1f, dir.y >= 0f ? 1f : -1f, dir.z >= 0f ? 1f : -1f);
            Vector3 support = b.center + Vector3.Scale(ext, sign);

            float d = Vector3.Dot(support, dir);
            if (d > bestDot)
            {
                bestDot = d;
                bestPoint = support;
            }
        }

        tipPos = bestPoint + dir * Mathf.Max(0f, clearance);
        return true;
    }

    private void SpawnMuzzleVfx(Vector3 position, Quaternion rotation)
    {
        if (muzzleVfxPrefab == null) return;

        Transform parent = (parentMuzzleVfxToGun && gun != null) ? gun : null;
        GameObject vfx = Instantiate(muzzleVfxPrefab, position, rotation, parent);

        // Auto-destroy after particle finishes (best-effort).
        float lifetime = GetMaxParticleLifetimeSeconds(vfx);
        Destroy(vfx, Mathf.Max(0.25f, lifetime));
    }

    private static float GetMaxParticleLifetimeSeconds(GameObject vfxRoot)
    {
        if (vfxRoot == null) return 1f;

        float max = 1f;
        ParticleSystem[] systems = vfxRoot.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null) continue;
            var main = systems[i].main;

            // Duration + startLifetime (max) approximates the time particles are visible.
            float duration = main.duration;
            float lifetime = main.startLifetime.constantMax;
            if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                lifetime = main.startLifetime.constantMax;
            else if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                lifetime = main.startLifetime.constant;

            max = Mathf.Max(max, duration + lifetime);
        }

        return max;
    }
}
