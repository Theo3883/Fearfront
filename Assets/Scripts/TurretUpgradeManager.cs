using UnityEngine;

/// <summary>
/// Legacy/scene-referenced component kept for backward compatibility.
/// Some scenes (e.g. LevelScene) reference this by GUID; if the script is missing Unity reports
/// "missing script attached" during build.
///
/// Current upgrade logic lives in <see cref="TurretUpgradeVR"/>.
/// </summary>
public sealed class TurretUpgradeManager : MonoBehaviour
{
    // Intentionally empty.
}

