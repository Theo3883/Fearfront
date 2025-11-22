using UnityEngine;
using System.Collections.Generic;
using DefaultNamespace;

public class PlayerInventory : MonoBehaviour
{
    private Dictionary<ResourceType, int> resources = new();

    private void Awake()
    {
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            resources[type] = 0;

        Debug.Log("<color=#00FFAA>[Inventory]</color> Initialized inventory for Tree & Stone.");
    }

    public void Add(ResourceType type, int amount)
    {
        resources[type] += amount;
        Debug.Log($"<color=#00FFAA>[Inventory]</color> Added <b>{amount}</b> of <b>{type}</b>. Total: {resources[type]}");
    }

    public int Get(ResourceType type) => resources[type];
}