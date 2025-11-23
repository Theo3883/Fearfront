using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FixedPointSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public Transform point;          // poziția fixă
        public GameObject prefab;        // prefab-ul (Tree, Rock, etc.)
        [HideInInspector] public bool active;
        [HideInInspector] public bool usedThisRound;
    }

    [Header("Puncte (Tree + Rock)")]
    public List<Slot> slots = new();

    [Header("Control random")]
    [Min(1)] public int targetActive = 4;    // câte resurse simultan
    public bool useEachSlotOnce = true;      // folosește toate punctele înainte de reset
    public float spawnReplaceDelay = 0.25f;  // delay mic între spawn-uri
    public float roundRefillDelay = 10f;     // pauză între "runde"

    private bool refillRunning = false;

    void Start()
    {
        StartCoroutine(RefillUntilTarget());
    }

    IEnumerator RefillUntilTarget()
    {
        if (refillRunning) yield break;
        refillRunning = true;

        while (CountActive() < targetActive)
        {
            var s = PickRandomAvailableSlot();
            if (s == null)
            {
                // toate punctele folosite => așteptăm o rundă nouă
                if (useEachSlotOnce)
                {
                    Debug.Log($"[Spawner] Toate punctele au fost folosite. Încep rundă nouă în {roundRefillDelay}s...");
                    yield return new WaitForSeconds(roundRefillDelay);
                    ResetRoundFlags();
                    continue;
                }
                else break;
            }

            SpawnInto(s);
            yield return new WaitForSeconds(spawnReplaceDelay);
        }

        refillRunning = false;
    }

    int CountActive()
    {
        int count = 0;
        foreach (var s in slots)
            if (s.active) count++;
        return count;
    }

    Slot PickRandomAvailableSlot()
    {
        List<Slot> available = new();
        foreach (var s in slots)
            if (!s.active && !s.usedThisRound)
                available.Add(s);

        if (available.Count == 0) return null;
        return available[Random.Range(0, available.Count)];
    }

    void ResetRoundFlags()
    {
        foreach (var s in slots)
            s.usedThisRound = false;
    }

    void SpawnInto(Slot s)
    {
        if (!s.point || !s.prefab) return;

        var go = Instantiate(s.prefab, s.point.position, s.point.rotation, transform);
        var node = go.GetComponent<ResourceNode>();
        if (!node)
        {
            Debug.LogError("[Spawner] Prefab-ul nu are ResourceNode!");
            Destroy(go);
            return;
        }

        s.active = true;
        s.usedThisRound = true;

        // abonare la evenimentul OnDepleted
        node.OnDepleted += _ => OnSlotDepleted(s);
    }

    void OnSlotDepleted(Slot s)
    {
        s.active = false;
        StartCoroutine(RefillUntilTarget());
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
