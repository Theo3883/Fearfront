using UnityEngine;

/// <summary>
/// Interface for components that want to provide a custom highlight color
/// when hovered in VR. Implement this on interactable objects to override
/// the default highlight color from VRHighlightManager.
/// </summary>
public interface IHighlightColorProvider
{
    /// <summary>
    /// Returns the color to use for highlighting this object.
    /// Called by VRHighlightManager when applying highlights.
    /// </summary>
    Color GetHighlightColor();
}
