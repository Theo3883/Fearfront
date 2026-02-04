using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reusable health bar UI component that creates and manages a visual health bar.
/// Can be used for enemies, players, or any entity that needs a health bar visualization.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("Size Settings")]
    [SerializeField] private float width = 400f;
    [SerializeField] private float height = 40f;
    [SerializeField] private float borderThickness = 10f;
    
    [Header("Color Settings")]
    [SerializeField] private bool useGradient = true;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private Color midHealthColor = Color.yellow;
    [SerializeField] private Color highHealthColor = Color.green;
    [SerializeField] private Color backgroundColor = Color.black;
    
    private Canvas canvas;
    private Image fillImage;
    private Image backgroundImage;
    private Gradient healthGradient;
    private Sprite whiteSprite;
    private bool isInitialized = false;
    
    /// <summary>
    /// The Canvas component used by this health bar
    /// </summary>
    public Canvas Canvas => canvas;
    
    /// <summary>
    /// Width of the health bar
    /// </summary>
    public float Width
    {
        get => width;
        set
        {
            width = value;
            if (isInitialized) UpdateDimensions();
        }
    }
    
    /// <summary>
    /// Height of the health bar
    /// </summary>
    public float Height
    {
        get => height;
        set
        {
            height = value;
            if (isInitialized) UpdateDimensions();
        }
    }
    
    private void Awake()
    {
        Initialize();
    }
    
    /// <summary>
    /// Initialize or reinitialize the health bar UI
    /// </summary>
    public void Initialize()
    {
        if (isInitialized) return;
        
        CreateWhiteSprite();
        InitializeGradient();
        CreateHealthBarUI();
        isInitialized = true;
    }
    
    /// <summary>
    /// Force rebuild the UI (useful after property changes)
    /// </summary>
    public void Rebuild()
    {
        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
        isInitialized = false;
        Initialize();
    }
    
    /// <summary>
    /// Update the health bar display with the given health values
    /// </summary>
    /// <param name="current">Current health value</param>
    /// <param name="max">Maximum health value</param>
    public void UpdateHealth(float current, float max)
    {
        if (!isInitialized) Initialize();
        if (fillImage == null) return;
        
        float percentage = Mathf.Clamp01(current / max);
        fillImage.fillAmount = percentage;
        
        if (useGradient)
        {
            fillImage.color = healthGradient.Evaluate(percentage);
        }
    }
    
    /// <summary>
    /// Set a custom fill color (disables gradient)
    /// </summary>
    public void SetFillColor(Color color)
    {
        useGradient = false;
        if (fillImage != null)
        {
            fillImage.color = color;
        }
    }
    
    /// <summary>
    /// Enable gradient coloring with custom colors
    /// </summary>
    public void SetGradientColors(Color low, Color mid, Color high)
    {
        lowHealthColor = low;
        midHealthColor = mid;
        highHealthColor = high;
        useGradient = true;
        InitializeGradient();
    }
    
    /// <summary>
    /// Show or hide the health bar
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (canvas != null)
        {
            canvas.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// Check if the health bar is currently visible
    /// </summary>
    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;
    
    private void CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(2, 2);
        Color[] cols = new Color[4] { Color.white, Color.white, Color.white, Color.white };
        tex.SetPixels(cols);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
    }
    
    private void InitializeGradient()
    {
        healthGradient = new Gradient();
        
        // Red at 0%, Yellow at 50%, Green at 100%
        GradientColorKey[] colors = new GradientColorKey[3];
        colors[0] = new GradientColorKey(lowHealthColor, 0.0f);
        colors[1] = new GradientColorKey(midHealthColor, 0.5f);
        colors[2] = new GradientColorKey(highHealthColor, 1.0f);

        GradientAlphaKey[] alphas = new GradientAlphaKey[2];
        alphas[0] = new GradientAlphaKey(1.0f, 0.0f);
        alphas[1] = new GradientAlphaKey(1.0f, 1.0f);

        healthGradient.SetKeys(colors, alphas);
    }
    
    private void CreateHealthBarUI()
    {
        // 1. Create Canvas GameObject as child
        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(this.transform, false);
        canvasObj.transform.localPosition = Vector3.zero;
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = Vector3.one;

        // 2. Add Canvas Component
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        
        // 3. Create Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.sprite = whiteSprite;
        backgroundImage.color = backgroundColor;
        
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(width + borderThickness, height + borderThickness);

        // 4. Create Fill Image
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(canvasObj.transform, false);
        
        fillImage = fillObj.AddComponent<Image>();
        fillImage.sprite = whiteSprite;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left; // Depletes from Right
        fillImage.color = highHealthColor;
        
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0.5f, 0.5f);
        fillRect.anchorMax = new Vector2(0.5f, 0.5f);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.sizeDelta = new Vector2(width, height);
    }
    
    private void UpdateDimensions()
    {
        if (backgroundImage != null)
        {
            RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(width + borderThickness, height + borderThickness);
        }
        
        if (fillImage != null)
        {
            RectTransform fillRect = fillImage.GetComponent<RectTransform>();
            fillRect.sizeDelta = new Vector2(width, height);
        }
    }
    
    private void OnDestroy()
    {
        if (whiteSprite != null && whiteSprite.texture != null)
        {
            Destroy(whiteSprite.texture);
            Destroy(whiteSprite);
        }
    }
}
