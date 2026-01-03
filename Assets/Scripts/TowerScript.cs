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

    [Header("Aiming")]
    [SerializeField] private float turnSpeed = 8f; // higher = snappier

    [Header("Detection (fallback)")]
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private LayerMask enemyLayerMask = 0; // if 0, we'll try auto-mask "Enemy"
    private SphereCollider rangeTrigger;

    List<GameObject> enemies = new List<GameObject>();
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

        desirableEnemy = PickClosestEnemy();

        // Rotate tower to face the currently selected enemy
        if (desirableEnemy != null)
        {
            Transform pivot = turretPivot != null ? turretPivot : transform;
            Vector3 toEnemy = desirableEnemy.transform.position - pivot.position;
            toEnemy.y = 0f; // yaw-only rotation
            if (toEnemy.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toEnemy.normalized, Vector3.up);
                pivot.rotation = Quaternion.Slerp(pivot.rotation, targetRot, turnSpeed * Time.deltaTime);
            }
        }
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
        }
    }

    private void OnTriggerExit(Collider other)
    {
        enemies.Remove(other.gameObject);
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
