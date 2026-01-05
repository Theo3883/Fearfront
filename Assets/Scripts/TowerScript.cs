using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerScript : MonoBehaviour
{
    private GameObject desirableEnemy = null;

    [Header("Shooting")]
    [SerializeField] private GameObject projectile;
    [SerializeField] private Transform gun; // assign in Inspector or auto-found by name "gun"
    [SerializeField] private Transform turretPivot; // optional: child named "Turret" in the sci-fi prefab
    [SerializeField] private Vector3 muzzleLocalOffset = new Vector3(0f, 0f, 0.5f); // pushes spawn forward from gun pivot
    [SerializeField] private float fireIntervalSeconds = 2f;
    [SerializeField] private float projectileDamage = 5f; // set different values on Turret 1a/1b/1c/1d prefabs

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
            gun = FindChildByName(transform, "gun");
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

                Vector3 spawnPos = gun != null ? gun.TransformPoint(muzzleLocalOffset) : transform.position;
                Quaternion spawnRot = gun != null ? gun.rotation : transform.rotation;

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
}
