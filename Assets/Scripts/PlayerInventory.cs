using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public enum ResourceType { Tree, Stone }
    private readonly System.Collections.Generic.Dictionary<ResourceType,int> bag =
        new System.Collections.Generic.Dictionary<ResourceType,int>() {
            { ResourceType.Tree, 0 }, { ResourceType.Stone, 0 }
        };

    void Awake(){ Debug.Log("[Inventory] Ready."); }
    public void Add(ResourceType t, int a){ bag[t]+=a; Debug.Log($"[Inventory] +{a} {t}. Total: {bag[t]}"); }
    public int Get(ResourceType t)=> bag[t];
}