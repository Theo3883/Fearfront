using System;
using System.Collections.Generic;
using UnityEngine;
using Fearfront.Common;

/// <summary>
/// Attach to a Chest prefab to store items taken from PlayerInventory.
/// Call OnActivated() from XR/interaction events to deposit items.
/// </summary>
public class ChestStorage : MonoBehaviour
{
    [Serializable]
    private class StoredStack
    {
        public ResourceType type;
        public int amount;
    }

    [Header("Stored contents (serialized)")]
    [SerializeField] private List<StoredStack> stored = new List<StoredStack>()
    {
        new StoredStack{ type = ResourceType.Tree, amount = 0 },
        new StoredStack{ type = ResourceType.Stone, amount = 0 },
    };

    [Header("Behavior")]
    [SerializeField] private bool depositAllOnActivated = true;

    public event Action OnStoredChanged;

    public int GetStored(ResourceType type) => GetOrCreate(type).amount;

    /// <summary>
    /// Hook this up to your chest interaction (XR Interaction Toolkit event, button, etc.).
    /// By default deposits ALL resources from the player's inventory into the chest.
    /// </summary>
    public void OnActivated()
    {
        PlayerInventory inv = FindFirstObjectByType<PlayerInventory>();
        if (inv == null)
        {
            Debug.LogError("[ChestStorage] No PlayerInventory found in scene!");
            return;
        }

        if (depositAllOnActivated)
        {
            DepositAllFrom(inv);
        }
    }

    public void DepositAllFrom(PlayerInventory inv)
    {
        if (inv == null) return;

        foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
        {
            int have = inv.Get(t);
            if (have <= 0) continue;
            int moved = inv.RemoveUpTo(t, have);
            if (moved > 0) AddStored(t, moved);
        }
    }

    public int DepositFrom(PlayerInventory inv, ResourceType type, int amount)
    {
        if (inv == null) return 0;
        int moved = inv.RemoveUpTo(type, amount);
        if (moved > 0) AddStored(type, moved);
        return moved;
    }

    public int WithdrawTo(PlayerInventory inv, ResourceType type, int amount)
    {
        if (inv == null) return 0;
        StoredStack s = GetOrCreate(type);
        int take = Mathf.Min(amount, s.amount);
        if (take <= 0) return 0;
        s.amount -= take;
        inv.Add(type, take);
        OnStoredChanged?.Invoke();
        return take;
    }

    public void AddStored(ResourceType type, int amount)
    {
        if (amount <= 0) return;
        StoredStack s = GetOrCreate(type);
        s.amount += amount;
        OnStoredChanged?.Invoke();
    }

    private StoredStack GetOrCreate(ResourceType type)
    {
        for (int i = 0; i < stored.Count; i++)
        {
            if (stored[i] != null && stored[i].type == type)
                return stored[i];
        }
        var created = new StoredStack { type = type, amount = 0 };
        stored.Add(created);
        return created;
    }
}

