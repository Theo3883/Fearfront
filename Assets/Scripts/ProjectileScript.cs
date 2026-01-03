using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileScript : MonoBehaviour
{
    public GameObject target;
    [SerializeField] private float speed = 10f;

    [Header("Aiming")]
    [SerializeField] private bool aimAtColliderCenter = true;
    [SerializeField] private Vector3 aimOffset = Vector3.zero;
    [SerializeField] private Vector3 modelRotationOffsetEuler = new Vector3(0f, 0f, -90f); // missile mesh faces up by default

    // Start is called before the first frame update
    void Start()
    {
        
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

        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
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
        if (other.gameObject.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}
