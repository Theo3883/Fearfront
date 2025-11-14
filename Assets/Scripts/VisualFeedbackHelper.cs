using UnityEngine;

/// <summary>
/// Helper pentru feedback vizual - checkerboard textures si culori
/// Ajuta la crearea unui graybox environment clar
/// </summary>
public class VisualFeedbackHelper : MonoBehaviour
{
    [Header("Checkerboard Settings")]
    [SerializeField] private bool applyCheckerboard = false;
    [SerializeField] private Color color1 = Color.white;
    [SerializeField] private Color color2 = Color.gray;
    [SerializeField] private int checkerSize = 4;
    
    [Header("Material Settings")]
    [SerializeField] private bool useSimpleColor = true;
    [SerializeField] private Color simpleColor = Color.white;
    
    private Renderer objectRenderer;
    
    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        
        if (objectRenderer != null)
        {
            if (useSimpleColor && !applyCheckerboard)
            {
                objectRenderer.material.color = simpleColor;
            }
            else if (applyCheckerboard)
            {
                GenerateCheckerboardTexture();
            }
        }
    }
    
    private void GenerateCheckerboardTexture()
    {
        int textureSize = 256;
        Texture2D checkerTexture = new Texture2D(textureSize, textureSize);
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                bool isColor1 = ((x / checkerSize) + (y / checkerSize)) % 2 == 0;
                checkerTexture.SetPixel(x, y, isColor1 ? color1 : color2);
            }
        }
        
        checkerTexture.Apply();
        checkerTexture.filterMode = FilterMode.Point;
        
        if (objectRenderer != null)
        {
            objectRenderer.material.mainTexture = checkerTexture;
        }
    }
    
    // Functie pentru a schimba culoarea dinamic
    public void SetColor(Color newColor)
    {
        if (objectRenderer != null)
        {
            objectRenderer.material.color = newColor;
        }
    }
    
    // Functie pentru a aplica emisiv (glow)
    public void SetEmission(bool enabled, Color emissionColor)
    {
        if (objectRenderer != null)
        {
            if (enabled)
            {
                objectRenderer.material.EnableKeyword("_EMISSION");
                objectRenderer.material.SetColor("_EmissionColor", emissionColor);
            }
            else
            {
                objectRenderer.material.DisableKeyword("_EMISSION");
            }
        }
    }
}

