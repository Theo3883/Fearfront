using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpiderDismantle : MonoBehaviour
{
    [Header("Model Parts")]
    [Tooltip("Drag the Body, Head, and all Leg objects here.")]
    [SerializeField] private List<GameObject> bodyParts;

    [Header("Dismantle Settings")]
    [SerializeField] private float explosionForce = 1f; // Slight push so they don't land perfectly flat
    [SerializeField] private float dismantleDelay = 2f; // How long they stay on ground before sinking
    [SerializeField] private float sinkSpeed = 1.0f;    // How fast they sink into the ground (units/second)
    [SerializeField] private float sinkDuration = 3.0f; // How long the sink motion lasts

    public void ConfigureTimings(float newDismantleDelay, float newSinkSpeed, float newSinkDuration)
    {
        dismantleDelay = Mathf.Max(0f, newDismantleDelay);
        sinkSpeed = Mathf.Max(0f, newSinkSpeed);
        sinkDuration = Mathf.Max(0.01f, newSinkDuration);
    }

    public void ActivateDismantle()
    {
        // 1. Stop the Animator so it doesn't try to force the parts back into position
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;

        // 2. Stop the main collider/movement so the "ghost" of the spider doesn't block player
        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null) mainCollider.enabled = false;
        
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        // 3. Dismantle logic
        foreach (GameObject part in bodyParts)
        {
            if (part == null) continue;

            // A. Detach from parent so it moves independently
            part.transform.SetParent(null);

            // B. Add Collider if missing (needed for physics collision with floor)
            if (part.GetComponent<Collider>() == null)
            {
                // MeshCollider is most accurate for "puzzle pieces" but expensive. 
                // Use BoxCollider if performance is an issue.
                MeshCollider mc = part.AddComponent<MeshCollider>();
                mc.convex = true;
            }

            // C. Add Rigidbody for gravity
            Rigidbody rb = part.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = part.AddComponent<Rigidbody>();
            }

            // D. Add a tiny random push so they look like they "crumbled"
            Vector3 randomDir = Random.insideUnitSphere;
            rb.AddForce(randomDir * explosionForce, ForceMode.Impulse);
        }

        // 4. Start the clean-up process
        StartCoroutine(SinkAndDestroyRoutine());
    }

    private IEnumerator SinkAndDestroyRoutine()
    {
        // Wait for the parts to settle on the ground
        yield return new WaitForSeconds(dismantleDelay);

        float timer = 0f;
        
        // Disable physics so we can move them manually downwards
        foreach (GameObject part in bodyParts)
        {
            if (part != null && part.GetComponent<Rigidbody>() != null)
            {
                part.GetComponent<Rigidbody>().isKinematic = true; // Stop physics
                
                // Optional: Turn off collision so player can walk through them while sinking
                if(part.GetComponent<Collider>() != null)
                    part.GetComponent<Collider>().enabled = false;
            }
        }

        // Sink loop
        while (timer < sinkDuration)
        {
            timer += Time.deltaTime;
            
            foreach (GameObject part in bodyParts)
            {
                if (part != null)
                {
                    // Move the part strictly Down
                    part.transform.Translate(Vector3.down * sinkSpeed * Time.deltaTime, Space.World);
                }
            }
            yield return null;
        }

        // Finally, destroy the actual parts
        foreach (GameObject part in bodyParts)
        {
            if (part != null) Destroy(part);
        }

        // Destroy the empty root object (the "ghost" of the spider)
        Destroy(gameObject);
    }
    // ... inside SpiderDismantle class ...

    [ContextMenu("Auto Find Body Parts")]
    private void AutoFindParts()
    {
        bodyParts = new List<GameObject>();
        
        // Find all objects inside this spider that have a visible mesh (Renderer)
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
        
        foreach (Renderer r in allRenderers)
        {
            // Add the GameObject attached to the renderer
            bodyParts.Add(r.gameObject);
        }
        
        Debug.Log($"Found {bodyParts.Count} body parts automatically!");
    }
}