using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using Fearfront.Common;

/// <summary>
/// SIMPLU: Adauga asta pe turnuri pentru upgrade in VR
/// - Pointezi cu maneta
/// - Tragi trigger
/// - Daca ai 50 copaci + 50 pietre = UPGRADE!
/// </summary>
[RequireComponent(typeof(TurretVariantGroup))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class TurretUpgradeVR : MonoBehaviour
{
    [Header("Cost de Upgrade")]
    [SerializeField] private int woodCost = 50;  // Copaci
    [SerializeField] private int stoneCost = 30;  // Pietre

    [Header("Visual Feedback (Optional)")]
    [SerializeField] private Color canUpgradeColor = Color.green;
    [SerializeField] private Color cannotUpgradeColor = Color.red;
    [SerializeField] private Renderer turretRenderer;

    [Header("Upgrade UI (Optional)")]
    [SerializeField] private GameObject upgradeUiRoot;
    [SerializeField] private TMP_Text upgradeTitleText;
    [SerializeField] private TMP_Text upgradeCostText;
    [SerializeField] private TMP_Text upgradeHintText;

    [Header("UI Auto Fit")]
    [SerializeField] private bool autoFitUiToTurret = true;
    [SerializeField] private float uiWidthToTurretRatio = 5f;
    [SerializeField] private float uiYOffset = 1.5f;
    [SerializeField] private float minUiWorldWidth = 0.3f;
    [SerializeField] private float maxUiWorldWidth = 2.0f;
    [SerializeField] private float minUiScale = 0.01f;
    [SerializeField] private float maxUiScale = 0.2f;
    [SerializeField] private bool autoSizeText = true;
    [SerializeField] private float textAutoSizeMin = 2f;
    [SerializeField] private float textAutoSizeMax = 7f;

    [Header("UI Visibility")]
    [SerializeField] private bool showOnlyOnHover = true;
    [SerializeField] private float minShowSeconds = 0.25f;
    [SerializeField] private float exitLingerSeconds = 0.75f;

    [Header("Proximity (Optional)")]
    [SerializeField] private bool showByProximity = true;
    [SerializeField] private float proximityShowRadius = 4f;  // show when player is closer than this
    [SerializeField] private float proximityHideRadius = 5f;  // hide when player is farther than this (hysteresis)
    [SerializeField] private Transform playerTransform;        // if null, auto-find XR camera or PlayerHealth

    [Header("Audio")]
    // Build sound moved to TowerScript.cs

    private TurretVariantGroup variantGroup;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private AudioSource audioSource;
    private PlayerInventory inventory;
    private bool isHovering = false;
    private float visibleUntil = 0f;
    private bool proximityVisible = false;

    void Awake()
    {
        variantGroup = GetComponent<TurretVariantGroup>();
        inventory = FindFirstObjectByType<PlayerInventory>();
        
        if (inventory == null)
        {
            Debug.LogError($"[TurretUpgradeVR] Nu gasesc PlayerInventory in scena!");
        }

        // UI-ul trebuie asignat manual in inspector (prefab salvat). Nicio auto-generare.
    }

    void Start()
    {
        // Conecteaza event-urile AICI in Start, nu in OnEnable
        // Pentru ca XRSimpleInteractable s-ar putea sa nu fie ready in Awake/OnEnable
        ConnectEvents();

        // Auto-find player transform for proximity
        if (playerTransform == null)
        {
            var cam = Camera.main;
            if (cam != null) playerTransform = cam.transform;
        }
        if (playerTransform == null)
        {
            var ph = FindFirstObjectByType<PlayerHealth>();
            if (ph != null) playerTransform = ph.transform;
        }
    }

    void OnEnable()
    {
        // Backup: incearca sa conecteze si aici
        Invoke(nameof(ConnectEvents), 0.1f);
    }

    void OnDisable()
    {
        DisconnectEvents();
    }

    private void ConnectEvents()
    {
        // Gaseste componenta (poate a fost adaugata dupa Awake)
        if (interactable == null)
        {
            interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (interactable == null)
        {
            Debug.LogError($"[TurretUpgradeVR] {gameObject.name}: XRSimpleInteractable LIPSESTE! Adauga-l!");
            return;
        }

        // Asigura coliderele pentru interactable (daca lista este goala)
        try
        {
            var list = interactable.colliders;
            if (list == null || list.Count == 0)
            {
                var found = GetComponentsInChildren<Collider>(true);
                foreach (var c in found)
                {
                    if (c != null && !c.isTrigger)
                        interactable.colliders.Add(c);
                }
                Debug.Log($"[TurretUpgradeVR] {gameObject.name}: Assigned {interactable.colliders.Count} collider(s) to XRSimpleInteractable.");
            }

            // Default interaction layer if none is set (ensures ray hits)
            if (interactable.interactionLayers == 0)
            {
                interactable.interactionLayers = InteractionLayerMask.GetMask("Default");
                Debug.Log($"[TurretUpgradeVR] {gameObject.name}: Set interaction layer to Default.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TurretUpgradeVR] {gameObject.name}: Could not auto-assign colliders: {e.Message}");
        }

        // Deconecteaza mai intai (pentru a evita duplicate)
        DisconnectEvents();

        // Conecteaza event-urile
        interactable.selectEntered.AddListener(OnTriggerPulled);
        interactable.hoverEntered.AddListener(OnPointerEnter);
        interactable.hoverExited.AddListener(OnPointerExit);

        Debug.Log($"<color=cyan>[TurretUpgradeVR] {gameObject.name}: Events CONNECTED! ✓</color>");
    }

    private void DisconnectEvents()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnTriggerPulled);
            interactable.hoverEntered.RemoveListener(OnPointerEnter);
            interactable.hoverExited.RemoveListener(OnPointerExit);
        }
    }

    // Apelat cand player-ul trage trigger
    private void OnTriggerPulled(SelectEnterEventArgs args)
    {
        // Arata UI si actualizeaza textul chiar si fara hover
        Debug.Log($"[TurretUpgradeVR] Trigger pulled on '{gameObject.name}'. Showing UI.");
        RefreshUpgradeUI();
        SetUpgradeUiVisible(true);
        visibleUntil = Time.unscaledTime + Mathf.Max(minShowSeconds, 0.1f);
        TryUpgrade();
    }

    // Apelat cand player-ul pointeaza la tureta
    private void OnPointerEnter(HoverEnterEventArgs args)
    {
        Debug.Log($"[TurretUpgradeVR] Hover ENTER on '{gameObject.name}'.");
        isHovering = true;
        ShowUpgradePreview();
        visibleUntil = Time.unscaledTime + Mathf.Max(minShowSeconds, 0.1f);
    }

    // Apelat cand player-ul nu mai pointeaza la tureta
    private void OnPointerExit(HoverExitEventArgs args)
    {
        Debug.Log($"[TurretUpgradeVR] Hover EXIT on '{gameObject.name}'.");
        isHovering = false;
        // Do not hide immediately; allow a short linger to avoid flicker when ray intersects UI or nearby geometry
        visibleUntil = Time.unscaledTime + Mathf.Max(exitLingerSeconds, 0.1f);
    }

    /// <summary>
    /// Incearca sa faca upgrade la tureta
    /// </summary>
    public bool TryUpgrade()
    {
        if (inventory == null || variantGroup == null)
        {
            Debug.LogWarning("[TurretUpgradeVR] Lipsesc componente!");
            return false;
        }

        // Verifica daca e deja la nivel maxim
        if (variantGroup.HasBuiltTurret && variantGroup.CurrentVariant == TurretVariantGroup.TurretVariant.Turret1d)
        {
            return false;
        }

        // Verifica resursele
        int currentWood = inventory.Get(ResourceType.Tree);
        int currentStone = inventory.Get(ResourceType.Stone);

        if (currentWood < woodCost || currentStone < stoneCost)
        {
            return false;
        }

        // UPGRADE!
        bool woodRemoved = inventory.TryRemove(ResourceType.Tree, woodCost);
        bool stoneRemoved = inventory.TryRemove(ResourceType.Stone, stoneCost);

        if (woodRemoved && stoneRemoved)
        {
            // Play sound via TowerScript if present
            TowerScript tower = GetComponent<TowerScript>();
            if (tower != null)
            {
                tower.PlayBuildSound();
            }

            variantGroup.TryUpgradeOnce();

            RefreshUpgradeUI();
            return true;
        }
        else
        {
            // The instruction's snippet for the else block uses 'cost.WoodCost' and 'cost.StoneCost'
            // which are not defined in the current context. I will adapt it to use the existing 'woodCost' and 'stoneCost'.
            Debug.Log($"[TurretUpgradeVR] Cannot upgrade! Need {woodCost} wood, {stoneCost} stone.");
            return false;
        }
    }

    /// <summary>
    /// Arata vizual daca poti face upgrade (verde/rosu)
    /// </summary>
    private void ShowUpgradePreview()
    {
        if (turretRenderer != null)
        {
            bool canAfford = CanAffordUpgrade();
            Color previewColor = canAfford ? canUpgradeColor : cannotUpgradeColor;

            // Schimba culoarea pentru highlight
            MaterialPropertyBlock props = new MaterialPropertyBlock();
            turretRenderer.GetPropertyBlock(props);
            props.SetColor("_EmissionColor", previewColor * 0.3f);
            turretRenderer.SetPropertyBlock(props);
        }

        RefreshUpgradeUI();
        SetUpgradeUiVisible(true);
    }

    private void HideUpgradePreview()
    {
        if (turretRenderer != null)
        {
            MaterialPropertyBlock props = new MaterialPropertyBlock();
            turretRenderer.GetPropertyBlock(props);
            props.SetColor("_EmissionColor", Color.black);
            turretRenderer.SetPropertyBlock(props);
        }

        SetUpgradeUiVisible(false);
    }

    private bool CanAffordUpgrade()
    {
        if (inventory == null) return false;
        if (variantGroup.HasBuiltTurret && variantGroup.CurrentVariant == TurretVariantGroup.TurretVariant.Turret1d) return false;

        int wood = inventory.Get(ResourceType.Tree);
        int stone = inventory.Get(ResourceType.Stone);

        return wood >= woodCost && stone >= stoneCost;
    }

    public void RefreshUpgradeUI()
    {
        if (upgradeTitleText == null && upgradeCostText == null && upgradeHintText == null) return;
        if (variantGroup == null) return;

        bool isMax = variantGroup.HasBuiltTurret && variantGroup.CurrentVariant == TurretVariantGroup.TurretVariant.Turret1d;
        string title;

        if (!variantGroup.HasBuiltTurret)
            title = "Build Turret 1a";
        else if (variantGroup.CurrentVariant == TurretVariantGroup.TurretVariant.Turret1a)
            title = "Upgrade to Turret 1b";
        else if (variantGroup.CurrentVariant == TurretVariantGroup.TurretVariant.Turret1b)
            title = "Upgrade to Turret 1c";
        else if (variantGroup.CurrentVariant == TurretVariantGroup.TurretVariant.Turret1c)
            title = "Upgrade to Turret 1d";
        else
            title = "Max level";

        if (upgradeTitleText != null) upgradeTitleText.text = title;

        if (upgradeCostText != null)
        {
            upgradeCostText.text = isMax
                ? "Cost: MAX"
                : $"Cost: {woodCost} Wood + {stoneCost} Stone";
        }

        if (upgradeHintText != null)
        {
            upgradeHintText.text = isMax
                ? ""
                : "Pull trigger to upgrade";
        }

        ApplyTextAutoSize();
        FitUiToTurret();

        FaceUiToCamera();
    }

    public void SetUpgradeUiVisible(bool visible)
    {
        if (upgradeUiRoot == null) return;
        if (upgradeUiRoot.activeSelf == visible) return;
        Debug.Log($"[TurretUpgradeVR] {(visible ? "Show" : "Hide")} UI on '{gameObject.name}'.");
        upgradeUiRoot.SetActive(visible);
        if (visible)
        {
            ApplyTextAutoSize();
            FitUiToTurret();
            FaceUiToCamera();
        }
    }

    private void FaceUiToCamera()
    {
        if (upgradeUiRoot == null) return;
        Camera cam = Camera.main;
        if (cam == null)
        {
            // fallback: first enabled camera
            var cams = GameObject.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (cams != null && cams.Length > 0) cam = cams[0];
        }
        if (cam == null) return;

        // billboard toward camera without tilting forward/back
        Vector3 dir = upgradeUiRoot.transform.position - cam.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        upgradeUiRoot.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    private void Update()
    {
        // Keep billboard updated while visible
        if (upgradeUiRoot != null && upgradeUiRoot.activeSelf)
        {
            if (autoFitUiToTurret)
                FitUiToTurret();
            FaceUiToCamera();
        }

        // Hover-based visibility management
        if (showOnlyOnHover && upgradeUiRoot != null)
        {
            if (!isHovering && upgradeUiRoot.activeSelf && Time.unscaledTime > visibleUntil)
            {
                HideUpgradePreview();
            }
        }

        // Proximity-based visibility (optional, independent of hover)
        if (showByProximity && playerTransform != null && upgradeUiRoot != null)
        {
            float d = Vector3.Distance(playerTransform.position, transform.position);
            bool shouldShow = d <= proximityShowRadius;
            bool shouldHide = d >= proximityHideRadius;

            if (!proximityVisible && shouldShow)
            {
                proximityVisible = true;
                RefreshUpgradeUI();
                SetUpgradeUiVisible(true);
            }
            else if (proximityVisible && shouldHide)
            {
                proximityVisible = false;
                if (!isHovering) // do not hide if actively hovering
                    SetUpgradeUiVisible(false);
            }
        }
    }

    public void ShowUI()
    {
        RefreshUpgradeUI();
        SetUpgradeUiVisible(true);
    }

    public void HideUI()
    {
        SetUpgradeUiVisible(false);
    }

    private void ApplyTextAutoSize()
    {
        if (!autoSizeText) return;

        ApplyTextAutoSize(upgradeTitleText);
        ApplyTextAutoSize(upgradeCostText);
        ApplyTextAutoSize(upgradeHintText);
    }

    private void ApplyTextAutoSize(TMP_Text text)
    {
        if (text == null) return;
        text.enableAutoSizing = true;
        text.fontSizeMin = textAutoSizeMin;
        text.fontSizeMax = textAutoSizeMax;
    }

    private void FitUiToTurret()
    {
        if (!autoFitUiToTurret || upgradeUiRoot == null) return;

        Bounds bounds = GetTurretBounds();
        if (bounds.size.sqrMagnitude <= 0.0001f) return;

        // Position UI above turret
        Vector3 targetPos = bounds.center + Vector3.up * (bounds.extents.y + uiYOffset);
        upgradeUiRoot.transform.position = targetPos;

        // Scale UI to fit turret width
        RectTransform rect = upgradeUiRoot.GetComponent<RectTransform>();
        if (rect == null) rect = upgradeUiRoot.GetComponentInChildren<RectTransform>();
        if (rect == null) return;

        float rectWidth = rect.rect.width;
        if (rectWidth <= 0.0001f) return;

        float desiredWorldWidth = Mathf.Clamp(bounds.size.x * uiWidthToTurretRatio, minUiWorldWidth, maxUiWorldWidth);
        float targetScale = desiredWorldWidth / rectWidth;
        targetScale = Mathf.Clamp(targetScale, minUiScale, maxUiScale);

        upgradeUiRoot.transform.localScale = Vector3.one * targetScale;
    }

    private Bounds GetTurretBounds()
    {
        if (turretRenderer != null)
            return turretRenderer.bounds;

        var renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return new Bounds(transform.position, Vector3.zero);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        return b;
    }

    // Nu mai generam automat UI; foloseste prefab-ul si asigneaza referintele.
}
