using UnityEngine;

/// <summary>
/// Script pentru zone unde obiectele pot fi plasate
/// Detecteaza cand un obiect grababil intra in zona
/// </summary>
[RequireComponent(typeof(Collider))]
public class PlacementZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [SerializeField] private string zoneName = "Placement Zone";
    [SerializeField] private Color idleColor = Color.gray;
    [SerializeField] private Color highlightColor = Color.cyan;
    [SerializeField] private Color successColor = Color.green;
    
    [Header("Accepted Objects")]
    [SerializeField] private string[] acceptedTags = { "Cube", "Sphere" };
    
    [Header("Snap Settings")]
    [SerializeField] private bool snapToCenter = true;
    
    private Renderer zoneRenderer;
    private GameObject currentObject;
    private bool isOccupied = false;
    private Vector3 snapPosition;
    
    void Awake()
    {
        zoneRenderer = GetComponent<Renderer>();
        if (zoneRenderer != null)
        {
            zoneRenderer.material.color = idleColor;
        }
        
        // Coliderul trebuie sa fie trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
        
        snapPosition = transform.position + Vector3.up * 0.5f;
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Verifica daca obiectul are tag-ul acceptat
        if (IsAcceptedObject(other.gameObject) && !isOccupied)
        {
            GrabbableObject grabbable = other.GetComponent<GrabbableObject>();
            
            // Highlight zona cand un obiect valid intra
            if (zoneRenderer != null && grabbable != null && !grabbable.IsGrabbed())
            {
                zoneRenderer.material.color = highlightColor;
            }
            
            Debug.Log($"{other.name} entered {zoneName}");
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        if (IsAcceptedObject(other.gameObject) && !isOccupied)
        {
            GrabbableObject grabbable = other.GetComponent<GrabbableObject>();
            
            // Daca obiectul este eliberat in zona
            if (grabbable != null && !grabbable.IsGrabbed())
            {
                PlaceObject(other.gameObject);
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (IsAcceptedObject(other.gameObject))
        {
            // Daca obiectul paraseste zona si nu este plasat
            if (currentObject != other.gameObject && zoneRenderer != null)
            {
                zoneRenderer.material.color = idleColor;
            }
            
            Debug.Log($"{other.name} exited {zoneName}");
        }
    }
    
    private void PlaceObject(GameObject obj)
    {
        currentObject = obj;
        isOccupied = true;
        
        if (zoneRenderer != null)
        {
            zoneRenderer.material.color = successColor;
        }
        
        // Snap to center daca este activat
        if (snapToCenter)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            obj.transform.position = snapPosition;
        }
        
        Debug.Log($"{obj.name} placed successfully in {zoneName}!");
        
        // Trigger event pentru InteractionManager
        InteractionManager manager = FindAnyObjectByType<InteractionManager>();
        if (manager != null)
        {
            manager.OnObjectPlaced(obj, this);
        }
    }
    
    private bool IsAcceptedObject(GameObject obj)
    {
        foreach (string tag in acceptedTags)
        {
            if (obj.CompareTag(tag))
            {
                return true;
            }
        }
        return false;
    }
    
    public void ResetZone()
    {
        currentObject = null;
        isOccupied = false;
        
        if (zoneRenderer != null)
        {
            zoneRenderer.material.color = idleColor;
        }
    }
    
    public bool IsOccupied()
    {
        return isOccupied;
    }
    
    public GameObject GetPlacedObject()
    {
        return currentObject;
    }
}

