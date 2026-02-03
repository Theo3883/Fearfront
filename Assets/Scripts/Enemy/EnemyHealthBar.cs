using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a floating health bar above the enemy when damaged.
/// Generates the necessary UI elements at runtime to avoid prefab requirements.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0); // Above head
    [SerializeField] private Vector3 scale = new Vector3(0.003f, 0.003f, 0.003f);
    [SerializeField] private float visibleDuration = 5f;
    [SerializeField] private float width = 400f;
    [SerializeField] private float height = 40f;

    private Enemy enemy;
    private Canvas canvas;
    private GameObject canvasObj;
    private Image fillImage;
    private float hideTimer = 0f;
    private Camera mainCam;
    private Gradient healthGradient;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        CreateHealthBarUI();
        InitializeGradient();
    }

    private void OnEnable()
    {
        if (enemy != null) enemy.OnHealthChanged += OnHealthChanged;
        
        // Hide by default
        if (canvasObj != null) canvasObj.SetActive(false);
    }

    private void OnDisable()
    {
        if (enemy != null) enemy.OnHealthChanged -= OnHealthChanged;
    }

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        // Apply Config Settings if available (Live Tweaking)
        if (EnemyVisualsConfig.Instance != null && canvasObj != null)
        {
            canvasObj.transform.localPosition = EnemyVisualsConfig.Instance.HealthBarOffset;
            canvasObj.transform.localScale = EnemyVisualsConfig.Instance.HealthBarScale;
        }

        // Handle visibility timer
        if (hideTimer > 0)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0)
            {
                if (canvasObj != null) canvasObj.SetActive(false);
            }
            else
            {
                // Billboard: Face the camera
                if (mainCam != null && canvasObj != null)
                {
                    // Rotate to look at camera
                    canvasObj.transform.LookAt(canvasObj.transform.position + mainCam.transform.rotation * Vector3.forward,
                                             mainCam.transform.rotation * Vector3.up);
                }
            }
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        if (canvasObj == null || fillImage == null) return;

        // Show bar
        canvasObj.SetActive(true);
        hideTimer = visibleDuration;

        // Update fill and color
        float pct = Mathf.Clamp01(current / max);
        fillImage.fillAmount = pct;
        fillImage.color = healthGradient.Evaluate(pct);
    }

    private void InitializeGradient()
    {
        healthGradient = new Gradient();
        
        // Red at 0%, Yellow at 50%, Green at 100%
        GradientColorKey[] colors = new GradientColorKey[3];
        colors[0] = new GradientColorKey(Color.red, 0.0f);
        colors[1] = new GradientColorKey(Color.yellow, 0.5f);
        colors[2] = new GradientColorKey(Color.green, 1.0f);

        GradientAlphaKey[] alphas = new GradientAlphaKey[2];
        alphas[0] = new GradientAlphaKey(1.0f, 0.0f);
        alphas[1] = new GradientAlphaKey(1.0f, 1.0f);

        healthGradient.SetKeys(colors, alphas);
    }

    private void CreateHealthBarUI()
    {
        // 0. Create a simple white sprite (Required for Image.Type.Filled to work properly)
        Texture2D tex = new Texture2D(2, 2);
        Color[] cols = new Color[4] { Color.white, Color.white, Color.white, Color.white };
        tex.SetPixels(cols);
        tex.Apply();
        Sprite whiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));

        // 1. Create Canvas GameObject
        canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(this.transform, false);
        canvasObj.transform.localPosition = offset;
        canvasObj.transform.localScale = scale;

        // 2. Add Canvas Component
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100; // Ensure it renders on top of stuff
        
        // 3. Create Background (Black bar)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.sprite = whiteSprite; // Assign sprite
        bgImage.color = Color.black;
        
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(width + 10, height + 10); // Slight border

        // 4. Create Fill Image
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(canvasObj.transform, false);
        
        fillImage = fillObj.AddComponent<Image>();
        fillImage.sprite = whiteSprite; // Assign sprite to enable Fill
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left; // 0 = Left (Depletes from Right)
        
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0.5f, 0.5f);
        fillRect.anchorMax = new Vector2(0.5f, 0.5f);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.sizeDelta = new Vector2(width, height);
    }
}
