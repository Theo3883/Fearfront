using DefaultNamespace;
using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    [Header("Config")]
    public ResourceType type = ResourceType.Tree;
    public int amountTotal = 50;
    public int amountPerCollect = 10;
    public float respawnSeconds = 30f;

    private bool isDepleted = false;

    void Start()
    {
        Debug.Log($"<color=#00CCFF>[ResourceNode]</color> '{name}' initialized as <b>{type}</b> with {amountTotal} units.");
    }

    public void OnActivated()
    {
        if (isDepleted)
        {
            Debug.LogWarning($"<color=#FF8800>[ResourceNode]</color> '{name}' is depleted. Waiting for respawn...");
            return;
        }

        var inv = FindObjectOfType<PlayerInventory>();
        if (!inv)
        {
            Debug.LogError("<color=#FF0000>[ResourceNode]</color> No PlayerInventory found in scene!");
            return;
        }

        int collect = Mathf.Min(amountPerCollect, amountTotal);
        amountTotal -= collect;
        inv.Add(type, collect);

        Debug.Log($"<color=#00CCFF>[ResourceNode]</color> Collected {collect} {type} from '{name}'. Remaining: {amountTotal}");

        if (amountTotal <= 0)
        {
            Deplete();
        }
    }

    private void Deplete()
    {
        isDepleted = true;
        Debug.Log($"<color=#FF8800>[ResourceNode]</color> '{name}' is now depleted. Respawning in {respawnSeconds}s.");
        gameObject.SetActive(false);
        Invoke(nameof(Respawn), respawnSeconds);
    }

    private void Respawn()
    {
        amountTotal = 50;
        isDepleted = false;
        gameObject.SetActive(true);
        Debug.Log($"<color=#00FF00>[ResourceNode]</color> '{name}' has respawned with {amountTotal} {type}!");
    }
}