using UnityEngine;
using Fearfront.Common;

/// <summary>
/// "ResourceNode but inverted" for already-spawned chests.
/// Attach to chest GameObject (with XR Simple Interactable).
/// Hook the XR "Activated" event to call OnActivated().
///
/// Behavior: takes resources FROM PlayerInventory and stores them in ChestStorage.
/// </summary>
public class ChestNode : MonoBehaviour
{
    [SerializeField] private ChestStorage storage;

    [Header("Deposit")]
    [SerializeField] private int amountPerActivatePerType = 10;
    [SerializeField] private int maxStoredPerType = 200; // capacity per resource type in this chest

    [Header("Optional feedback")]
    [SerializeField] private ParticleSystem depositFx;

    private PlayerInventory inv;

    private void Awake()
    {
        if (storage == null) storage = GetComponent<ChestStorage>();
        inv = FindFirstObjectByType<PlayerInventory>();
    }

    public void OnActivated()
    {
        if (storage == null)
        {
            Debug.LogError("[ChestNode] Missing ChestStorage on chest!");
            return;
        }
        if (inv == null)
        {
            inv = FindFirstObjectByType<PlayerInventory>();
        }
        if (inv == null)
        {
            Debug.LogError("[ChestNode] No PlayerInventory found in scene!");
            return;
        }

        // Deposit from inventory into chest for ALL types currently present in the inventory.
        var snapshot = inv.GetSnapshot();
        bool movedAnything = false;

        foreach (var kv in snapshot)
        {
            ResourceType type = kv.Key;
            int have = kv.Value;
            if (have <= 0) continue;

            int currentStored = storage.GetStored(type);
            int spaceLeft = Mathf.Max(0, maxStoredPerType - currentStored);
            if (spaceLeft <= 0) continue;

            int toTake = Mathf.Min(amountPerActivatePerType, Mathf.Min(spaceLeft, have));
            int moved = inv.RemoveUpTo(type, toTake);
            if (moved <= 0) continue;

            storage.AddStored(type, moved);
            movedAnything = true;
        }

        if (!movedAnything) return;

        if (depositFx != null)
        {
            var fx = Instantiate(depositFx, transform.position, Quaternion.identity);
            fx.Play();
            var main = fx.main;
            Destroy(fx.gameObject, main.duration + main.startLifetime.constantMax + 0.1f);
        }
    }
}

