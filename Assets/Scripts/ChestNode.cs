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
    [SerializeField] private ResourceType type = ResourceType.Tree;

    [Header("Deposit")]
    [SerializeField] private int amountPerActivate = 10;
    [SerializeField] private int maxStoredForThisType = 200; // capacity for this type in this chest

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

        int current = storage.GetStored(type);
        int spaceLeft = Mathf.Max(0, maxStoredForThisType - current);
        if (spaceLeft <= 0) return;

        int toTake = Mathf.Min(amountPerActivate, spaceLeft);
        int moved = inv.RemoveUpTo(type, toTake);
        if (moved <= 0) return;

        storage.AddStored(type, moved);

        if (depositFx != null)
        {
            var fx = Instantiate(depositFx, transform.position, Quaternion.identity);
            fx.Play();
            var main = fx.main;
            Destroy(fx.gameObject, main.duration + main.startLifetime.constantMax + 0.1f);
        }
    }
}

