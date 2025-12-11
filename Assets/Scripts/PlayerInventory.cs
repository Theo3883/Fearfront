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
}