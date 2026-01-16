using UnityEngine;
using Fearfront.Common;

public class PlayerInventory : MonoBehaviour
{
    private readonly System.Collections.Generic.Dictionary<ResourceType,int> bag =
        new System.Collections.Generic.Dictionary<ResourceType,int>();

    void Awake()
    {
        // Ensure all enum values exist so Add/Get won't KeyNotFound when new types are introduced.
        foreach (ResourceType t in System.Enum.GetValues(typeof(ResourceType)))
        {
            if (!bag.ContainsKey(t)) bag[t] = 0;
        }
    }

    public void Add(ResourceType t, int a)
    {
        if (!bag.ContainsKey(t)) bag[t] = 0;
        bag[t] += a;
    }

    public int Get(ResourceType t)
    {
        if (!bag.TryGetValue(t, out int v)) return 0;
        return v;
    }

    /// <summary>
    /// Snapshot of current contents (copy) so other systems can iterate all types safely.
    /// </summary>
    public System.Collections.Generic.Dictionary<ResourceType,int> GetSnapshot()
    {
        return new System.Collections.Generic.Dictionary<ResourceType,int>(bag);
    }

    /// <summary>
    /// Removes up to <paramref name="amount"/> items of type <paramref name="t"/> and returns the amount actually removed.
    /// Never makes the inventory go negative.
    /// </summary>
    public int RemoveUpTo(ResourceType t, int amount)
    {
        if (amount <= 0) return 0;
        int have = Get(t);
        int take = Mathf.Min(have, amount);
        if (!bag.ContainsKey(t)) bag[t] = 0;
        bag[t] = have - take;
        return take;
    }

    /// <summary>
    /// Tries to remove exactly <paramref name="amount"/> items. Returns true if successful.
    /// </summary>
    public bool TryRemove(ResourceType t, int amount)
    {
        if (amount <= 0) return true;
        int have = Get(t);
        if (have < amount) return false;
        if (!bag.ContainsKey(t)) bag[t] = 0;
        bag[t] = have - amount;
        return true;
    }
}