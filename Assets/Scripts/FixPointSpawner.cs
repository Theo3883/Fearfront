using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FixedPointSpawnerWave : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public Transform point;         // poziție fixă
        public GameObject prefab;       // prefabul Tree/Stone (cu ResourceNode)
        [HideInInspector] public bool alive;   // e spawn-uit și ne-epuizat?
    }

    [Header("Slots (toți copacii)")]
    public List<Slot> slots = new();

    [Header("Respawn Wave")]
    [Min(0f)] public float waveRespawnDelay = 30f;
    bool waveRunning = false;

    void Start()
    {
        // spawn inițial în TOATE sloturile
        foreach (var s in slots) SpawnInto(s);
    }

    void SpawnInto(Slot s)
    {
        if (!s.point || !s.prefab) return;

        var go = Instantiate(s.prefab, s.point.position, s.point.rotation, transform);
        var node = go.GetComponent<ResourceNode>();
        if (!node)
        {
            Debug.LogError("[WaveSpawner] Prefab-ul nu are ResourceNode!");
            return;
        }

        s.alive = true;               // slotul e activ acum
        node.OnDepleted += _ => OnSlotDepleted(s);
    }

    void OnSlotDepleted(Slot s)
    {
        s.alive = false;

        // dacă toate sloturile sunt moarte și nu rulează deja un wave → pornește wave-ul
        if (!waveRunning && AllSlotsDepleted())
            StartCoroutine(RespawnWaveAfterDelay());
    }

    bool AllSlotsDepleted()
    {
        foreach (var s in slots)
            if (s.alive) return false;
        return true;
    }

    IEnumerator RespawnWaveAfterDelay()
    {
        waveRunning = true;
        Debug.Log($"[WaveSpawner] Toți copacii epuizați. Respawn wave în {waveRespawnDelay}s…");
        yield return new WaitForSeconds(waveRespawnDelay);

        // respawn în TOATE sloturile
        foreach (var s in slots)
            SpawnInto(s);

        waveRunning = false;
        Debug.Log("[WaveSpawner] Wave complet: toți copacii au reapărut.");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 0.3f, 0.6f);
        if (slots == null) return;
        foreach (var s in slots)
            if (s != null && s.point != null)
            {
                Gizmos.DrawSphere(s.point.position, 0.2f);
                Gizmos.DrawWireCube(s.point.position, Vector3.one * 0.4f);
            }
    }
#endif
}
