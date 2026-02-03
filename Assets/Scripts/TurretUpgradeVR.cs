using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
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

    private TurretVariantGroup variantGroup;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private PlayerInventory inventory;

    void Awake()
    {
        variantGroup = GetComponent<TurretVariantGroup>();
        inventory = FindFirstObjectByType<PlayerInventory>();
        
        if (inventory == null)
        {
            Debug.LogError($"[TurretUpgradeVR] Nu gasesc PlayerInventory in scena!");
        }
    }

    void Start()
    {
        // Conecteaza event-urile AICI in Start, nu in OnEnable
        // Pentru ca XRSimpleInteractable s-ar putea sa nu fie ready in Awake/OnEnable
        ConnectEvents();
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

        if (interactable == null)
        {
            Debug.LogError($"[TurretUpgradeVR] {gameObject.name}: XRSimpleInteractable LIPSESTE! Adauga-l!");
            return;
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
        TryUpgrade();
    }

    // Apelat cand player-ul pointeaza la tureta
    private void OnPointerEnter(HoverEnterEventArgs args)
    {
        ShowUpgradePreview();
    }

    // Apelat cand player-ul nu mai pointeaza la tureta
    private void OnPointerExit(HoverExitEventArgs args)
    {
        HideUpgradePreview();
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
        if (variantGroup.CurrentVariant == TurretVariantGroup.TurretVariant.Turret1d)
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
            variantGroup.TryUpgradeOnce();
            return true;
        }
        else
        {
            Debug.LogError("[TurretUpgradeVR] Eroare la deducerea resurselor!");
            return false;
        }
    }

    /// <summary>
    /// Arata vizual daca poti face upgrade (verde/rosu)
    /// </summary>
    private void ShowUpgradePreview()
    {
        if (turretRenderer == null) return;

        bool canAfford = CanAffordUpgrade();
        Color previewColor = canAfford ? canUpgradeColor : cannotUpgradeColor;

        // Schimba culoarea pentru highlight
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        turretRenderer.GetPropertyBlock(props);
        props.SetColor("_EmissionColor", previewColor * 0.3f);
        turretRenderer.SetPropertyBlock(props);
    }

    private void HideUpgradePreview()
    {
        if (turretRenderer == null) return;

        MaterialPropertyBlock props = new MaterialPropertyBlock();
        turretRenderer.GetPropertyBlock(props);
        props.SetColor("_EmissionColor", Color.black);
        turretRenderer.SetPropertyBlock(props);
    }

    private bool CanAffordUpgrade()
    {
        if (inventory == null) return false;
        if (variantGroup.CurrentVariant == TurretVariantGroup.TurretVariant.Turret1d) return false;

        int wood = inventory.Get(ResourceType.Tree);
        int stone = inventory.Get(ResourceType.Stone);

        return wood >= woodCost && stone >= stoneCost;
    }
}
