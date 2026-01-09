using System;
using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// Attach this to any scene object to test turret upgrade logic.
/// Every N seconds it upgrades ONE tower group (in sequence) by switching its active turret variant.
/// </summary>
public class TurretUpgradeTester : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float upgradeIntervalSeconds = 30f;
    [SerializeField] private bool startImmediately = false;

    [Header("Upgrade behavior")]
    [SerializeField] private bool upgradeOnlyTo1b = false; // if true: only 1a -> 1b, no further upgrades
    [SerializeField] private bool loopGroups = true;       // if false: stops after last group is upgraded

    [Header("Discovery")]
    [SerializeField] private bool includeInactive = true;

    private TurretVariantGroup[] groups = Array.Empty<TurretVariantGroup>();
    private int nextGroupIndex = 0;
    private Coroutine routine;

    private void OnEnable()
    {
        RefreshGroups();
        routine = StartCoroutine(Run());
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    [ContextMenu("Refresh Groups Now")]
    public void RefreshGroups()
    {
        groups = FindObjectsByType<TurretVariantGroup>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            )
            .OrderBy(g => g != null ? g.name : string.Empty, StringComparer.Ordinal)
            .ToArray();

        nextGroupIndex = 0;
        Debug.Log($"TurretUpgradeTester: found {groups.Length} TurretVariantGroup(s).");
    }

    private IEnumerator Run()
    {
        if (!startImmediately)
            yield return new WaitForSeconds(Mathf.Max(0.1f, upgradeIntervalSeconds));

        while (true)
        {
            bool didUpgrade = UpgradeNextGroup();

            if (!didUpgrade && !loopGroups)
                yield break;

            yield return new WaitForSeconds(Mathf.Max(0.1f, upgradeIntervalSeconds));
        }
    }

    private bool UpgradeNextGroup()
    {
        if (groups == null || groups.Length == 0)
        {
            Debug.LogWarning("TurretUpgradeTester: no groups found.");
            return false;
        }

        int attempts = 0;
        while (attempts < groups.Length)
        {
            if (nextGroupIndex >= groups.Length)
            {
                if (!loopGroups) return false;
                nextGroupIndex = 0;
            }

            TurretVariantGroup g = groups[nextGroupIndex];
            nextGroupIndex++;
            attempts++;

            if (g == null) continue;

            bool changed = UpgradeGroup(g);
            Debug.Log($"TurretUpgradeTester: {(changed ? "UPGRADED" : "skipped")} '{g.name}' to {g.CurrentVariant}");
            return true; // one group per tick, even if it was skipped (keeps the cadence)
        }

        return false;
    }

    private bool UpgradeGroup(TurretVariantGroup group)
    {
        if (group == null) return false;

        if (upgradeOnlyTo1b)
        {
            if (group.CurrentVariant == TurretVariantGroup.TurretVariant.Turret1a)
            {
                group.SetVariant(TurretVariantGroup.TurretVariant.Turret1b);
                return true;
            }
            return false;
        }

        return group.TryUpgradeOnce();
    }
}


