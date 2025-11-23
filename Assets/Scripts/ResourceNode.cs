using UnityEngine;
using System;
using System.Collections;

public class ResourceNode : MonoBehaviour
{
    public enum ResourceType { Tree, Stone }
    [SerializeField] private ParticleSystem sawdustFX;

    [Header("Config")]
    public ResourceType type = ResourceType.Tree;
    public int startAmount = 50;
    public int amountPerCollect = 10;

    [Header("(Doar dacă NU folosești spawner)")]
    public bool selfRespawn = false;
    public float selfRespawnSeconds = 30f;

    public event Action<ResourceNode> OnDepleted; 

    int amount;
    bool depleted;
    Collider col; Renderer[] rends;
    PlayerInventory inv;

    void Awake()
    {
        amount = startAmount;
        col = GetComponent<Collider>();
        rends = GetComponentsInChildren<Renderer>(true);
        inv = FindObjectOfType<PlayerInventory>();
    }

    void Start()
    {
        Debug.Log($"[ResourceNode] '{name}' initialized as {type} with {amount} units.");
    }
    [SerializeField] private Transform fxAnchor;

    public void OnActivated()
    {
        

        if (depleted) return;
        if (!inv) { Debug.LogError("[ResourceNode] No PlayerInventory found in scene!"); return; }

        int give = Mathf.Min(amountPerCollect, amount);
        amount -= give;
        inv.Add((PlayerInventory.ResourceType)type, give); // vezi PlayerInventory de mai jos
        Debug.Log($"[ResourceNode] Collected {give} {type} from '{name}'. Remaining: {amount}");
        if (sawdustFX)
        {
            Vector3 pos = fxAnchor ? fxAnchor.position : transform.position + Vector3.up * 1f;
            var fx = Instantiate(sawdustFX, pos, Quaternion.identity);
            fx.Play();  // pornește burst-ul

            var main = fx.main;
            Destroy(fx.gameObject, main.duration + main.startLifetime.constantMax + 0.1f);
        }  
        if (amount <= 0) Deplete();

    }

  

    void SelfRespawn()
    {
        amount = startAmount;
        depleted = false;
        if (col) col.enabled = true;
        foreach (var r in rends) r.enabled = true;
        Debug.Log($"[ResourceNode] '{name}' self-respawned with {amount}.");
    }

    void Deplete()
    {
        OnDepleted?.Invoke(this);   // anunță spawnerul
        Destroy(gameObject);        // dispare, va fi refăcut de spawner în wave
    }

    IEnumerator FadeAndKill()
    {
        // opțional: dezactivează coliziunea ca să nu mai fie clicabil
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        // mic „fade” al materialului (dacă shaderul permite)
        var rends = GetComponentsInChildren<Renderer>();
        float t = 0f;
        while (t < 0.25f) { t += Time.deltaTime; yield return null; }
        Destroy(gameObject);
    }

}
