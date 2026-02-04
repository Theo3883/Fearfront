using System;
using UnityEngine;

/// <summary>
/// Attach this to a "Towers X" parent (that contains Turret 1a/1b/1c/1d as children).
/// It will keep ONLY the selected variant active (default: Turret 1a).
/// </summary>
public class TurretVariantGroup : MonoBehaviour
{
    public enum TurretVariant
    {
        Turret1a = 0,
        Turret1b = 1,
        Turret1c = 2,
        Turret1d = 3,
    }

    [Header("Auto-find by name (recommended)")]
    [SerializeField] private string turretAName = "Turret 1a";
    [SerializeField] private string turretBName = "Turret 1b";
    [SerializeField] private string turretCName = "Turret 1c";
    [SerializeField] private string turretDName = "Turret 1d";

    [Header("Default")]
    [SerializeField] private TurretVariant activeOnStart = TurretVariant.Turret1a;
    [SerializeField] private bool startEmpty = true; // start with no turret built
    [SerializeField] private bool enforceEveryFrame = false; // optional safety if something else re-enables variants

    // Cached refs (optional)
    [SerializeField] private GameObject turret1a;
    [SerializeField] private GameObject turret1b;
    [SerializeField] private GameObject turret1c;
    [SerializeField] private GameObject turret1d;

    public TurretVariant CurrentVariant { get; private set; } = TurretVariant.Turret1a;
    public bool HasBuiltTurret => hasBuilt;

    private bool hasBuilt = true;

    private void Awake()
    {
        CacheChildrenIfNeeded();
        if (startEmpty)
        {
            hasBuilt = false;
            SetAllInactive();
        }
        else
        {
            hasBuilt = true;
            SetVariant(activeOnStart);
        }
    }

    private void LateUpdate()
    {
        if (!enforceEveryFrame) return;
        ApplyVariant(CurrentVariant);
    }

    public void CacheChildrenIfNeeded()
    {
        // Only fill missing references
        if (turret1a == null) turret1a = FindDeepChildByName(turretAName);
        if (turret1b == null) turret1b = FindDeepChildByName(turretBName);
        if (turret1c == null) turret1c = FindDeepChildByName(turretCName);
        if (turret1d == null) turret1d = FindDeepChildByName(turretDName);
    }

    public void SetVariant(TurretVariant variant)
    {
        hasBuilt = true;
        CurrentVariant = variant;
        ApplyVariant(variant);
    }

    public bool TryUpgradeOnce()
    {
        if (!hasBuilt)
        {
            SetVariant(TurretVariant.Turret1a);
            return true;
        }

        TurretVariant next = GetNextVariant(CurrentVariant);
        if (next == CurrentVariant) return false;
        SetVariant(next);
        return true;
    }

    public void SetVariantByIndex(int index)
    {
        index = Mathf.Clamp(index, 0, 3);
        SetVariant((TurretVariant)index);
    }

    private void ApplyVariant(TurretVariant variant)
    {
        CacheChildrenIfNeeded();

        // Disable all, enable only selected
        SetActiveSafe(turret1a, variant == TurretVariant.Turret1a);
        SetActiveSafe(turret1b, variant == TurretVariant.Turret1b);
        SetActiveSafe(turret1c, variant == TurretVariant.Turret1c);
        SetActiveSafe(turret1d, variant == TurretVariant.Turret1d);
    }

    private void SetAllInactive()
    {
        CacheChildrenIfNeeded();
        SetActiveSafe(turret1a, false);
        SetActiveSafe(turret1b, false);
        SetActiveSafe(turret1c, false);
        SetActiveSafe(turret1d, false);
    }

    private static TurretVariant GetNextVariant(TurretVariant current)
    {
        int i = (int)current;
        if (i >= 3) return current;
        return (TurretVariant)(i + 1);
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go == null) return;
        if (go.activeSelf == active) return;
        go.SetActive(active);
    }

    private GameObject FindDeepChildByName(string wanted)
    {
        if (string.IsNullOrWhiteSpace(wanted)) return null;

        // BFS through hierarchy
        var q = new System.Collections.Generic.Queue<Transform>();
        q.Enqueue(transform);
        while (q.Count > 0)
        {
            Transform cur = q.Dequeue();
            if (cur == null) continue;
            if (string.Equals(cur.name, wanted, StringComparison.Ordinal))
                return cur.gameObject;

            for (int i = 0; i < cur.childCount; i++)
                q.Enqueue(cur.GetChild(i));
        }
        return null;
    }

    [ContextMenu("Force Only Turret 1a")]
    private void ForceOnlyA()
    {
        SetVariant(TurretVariant.Turret1a);
    }
}


