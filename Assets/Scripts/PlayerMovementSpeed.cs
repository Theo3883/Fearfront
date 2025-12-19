using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

/// <summary>
/// Controls player movement speed by adjusting locomotion provider speeds
/// </summary>
public class PlayerMovementSpeed : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.5f;
    
    private List<MonoBehaviour> speedComponents = new List<MonoBehaviour>();
    private System.Collections.Generic.Dictionary<MonoBehaviour, System.Reflection.MemberInfo> speedComponentMembers = new System.Collections.Generic.Dictionary<MonoBehaviour, System.Reflection.MemberInfo>();
    private static PlayerMovementSpeed instance;

    public static PlayerMovementSpeed Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PlayerMovementSpeed>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // Find all MonoBehaviour components in the scene (include inactive) and
        // collect those that expose either a public writable "moveSpeed" property
        // or a public/serialized field named "moveSpeed" (case-insensitive).
        speedComponents.Clear();
        speedComponentMembers.Clear();
            // Use the appropriate Object.FindObjects* API depending on Unity version to avoid
            // deprecated overloads and maintain compatibility across Unity releases.
        MonoBehaviour[] allComponents = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
        foreach (MonoBehaviour component in allComponents)
        {
            if (component == null) continue;

            var t = component.GetType();

            PropertyInfo moveSpeedProp = t.GetProperty("moveSpeed", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (moveSpeedProp != null && moveSpeedProp.CanWrite)
            {
                speedComponents.Add(component);
                speedComponentMembers[component] = moveSpeedProp;
                continue;
            }

            FieldInfo moveSpeedField = t.GetField("moveSpeed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (moveSpeedField != null)
            {
                speedComponents.Add(component);
                speedComponentMembers[component] = moveSpeedField;
                continue;
            }

            // Fuzzy match: look for any float field/property containing both "move" and "speed"
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            bool matched = false;
            foreach (var p in props)
            {
                if (!p.CanWrite) continue;
                var name = p.Name.ToLowerInvariant();
                if (name.Contains("move") && name.Contains("speed") && (p.PropertyType == typeof(float) || p.PropertyType == typeof(double)))
                {
                    speedComponents.Add(component);
                    speedComponentMembers[component] = p;
                    matched = true;
                    break;
                }
            }
            if (matched) continue;

            var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in fields)
            {
                var name = f.Name.ToLowerInvariant();
                if (name.Contains("move") && name.Contains("speed") && (f.FieldType == typeof(float) || f.FieldType == typeof(double)))
                {
                    speedComponents.Add(component);
                    speedComponentMembers[component] = f;
                    break;
                }
            }
        }

        if (speedComponents.Count == 0)
        {
            Debug.LogWarning("PlayerMovementSpeed: No components with moveSpeed-like property/field found in scene!");
        }
        else
        {
            Debug.Log($"PlayerMovementSpeed: Found {speedComponents.Count} candidate components for moveSpeed updates");
            // Log detailed matches for diagnosis
            foreach (var kv in speedComponentMembers)
            {
                var comp = kv.Key;
                var member = kv.Value;
                Debug.Log($"PlayerMovementSpeed: Component={comp.GetType().FullName}, Member={member.MemberType} {member.Name}");
            }
            ApplyMoveSpeed();
        }
    }

    /// <summary>
    /// Sets the player movement speed
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
        ApplyMoveSpeed();
        Debug.Log($"Player movement speed set to {moveSpeed:F1} units/second");
    }

    /// <summary>
    /// Gets the current movement speed
    /// </summary>
    public float GetMoveSpeed()
    {
        return moveSpeed;
    }

    /// <summary>
    /// Applies the current move speed to all components that have moveSpeed property
    /// </summary>
    private void ApplyMoveSpeed()
    {
        // Prefer using the discovered member map for direct setting
        if (speedComponentMembers.Count > 0)
        {
            foreach (var kv in speedComponentMembers)
            {
                var component = kv.Key;
                var member = kv.Value;
                if (component == null || member == null) continue;
                try
                {
                    if (member is PropertyInfo p)
                    {
                        if (p.PropertyType == typeof(float)) p.SetValue(component, moveSpeed);
                        else if (p.PropertyType == typeof(double)) p.SetValue(component, (double)moveSpeed);
                        else Debug.LogWarning($"PlayerMovementSpeed: Property {p.Name} on {component.GetType().Name} is not float/double");
                    }
                    else if (member is FieldInfo f)
                    {
                        if (f.FieldType == typeof(float)) f.SetValue(component, moveSpeed);
                        else if (f.FieldType == typeof(double)) f.SetValue(component, (double)moveSpeed);
                        else Debug.LogWarning($"PlayerMovementSpeed: Field {f.Name} on {component.GetType().Name} is not float/double");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to set {member.Name} on {component.GetType().Name}: {e.Message}");
                }
            }
            return;
        }

        // Fallback: original behavior (legacy exact-name search)
        foreach (MonoBehaviour component in speedComponents)
        {
            if (component == null) continue;

            var t = component.GetType();

            // Try property first
            PropertyInfo moveSpeedProp = t.GetProperty("moveSpeed", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (moveSpeedProp != null && moveSpeedProp.CanWrite)
            {
                try
                {
                    moveSpeedProp.SetValue(component, moveSpeed);
                    continue;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to set moveSpeed property on {t.Name}: {e.Message}");
                }
            }

            // Fallback to field (public or private serialized)
            FieldInfo moveSpeedField = t.GetField("moveSpeed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (moveSpeedField != null)
            {
                try
                {
                    moveSpeedField.SetValue(component, moveSpeed);
                    continue;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to set moveSpeed field on {t.Name}: {e.Message}");
                }
            }
        }
    }
}
