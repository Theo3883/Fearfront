using UnityEngine;
using Fearfront.Common;

public class PlayerInventory : MonoBehaviour
{
    private readonly System.Collections.Generic.Dictionary<ResourceType,int> bag =
        new System.Collections.Generic.Dictionary<ResourceType,int>() {
            { ResourceType.Tree, 0 }, { ResourceType.Stone, 0 }
        };

    void Awake(){ }
    public void Add(ResourceType t, int a){ bag[t]+=a; }
    public int Get(ResourceType t)=> bag[t];

    /// <summary>
    /// Removes up to <paramref name="amount"/> items of type <paramref name="t"/> and returns the amount actually removed.
    /// Never makes the inventory go negative.
    /// </summary>
    public int RemoveUpTo(ResourceType t, int amount)
    {
        if (amount <= 0) return 0;
        int have = bag[t];
        int take = Mathf.Min(have, amount);
        bag[t] = have - take;
        return take;
    }

    /// <summary>
    /// Tries to remove exactly <paramref name="amount"/> items. Returns true if successful.
    /// </summary>
    public bool TryRemove(ResourceType t, int amount)
    {
        if (amount <= 0) return true;
        int have = bag[t];
        if (have < amount) return false;
        bag[t] = have - amount;
        return true;
    }
}