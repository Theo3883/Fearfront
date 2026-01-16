using UnityEngine;
using Fearfront.Common;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// "ResourceNode but inverted" for already-spawned chests.
/// Attach to chest GameObject (with XR Simple Interactable).
/// Hook the XR "Activated" event to call OnActivated().
///
/// Behavior: takes resources FROM PlayerInventory and stores them in ChestStorage.
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class ChestNode : MonoBehaviour
{
    [SerializeField] private ChestStorage storage;

    [Header("Deposit")]
    [SerializeField] private int amountPerActivatePerType = 10;
    [SerializeField] private int maxStoredPerType = 200; // capacity per resource type in this chest

    [Header("Withdraw (Grip + Trigger)")]
    [SerializeField] private bool withdrawAllWhenSelectedAndActivated = true;
    [SerializeField] private int withdrawAmountPerActivatePerType = 10;

    [Header("Optional feedback")]
    [SerializeField] private ParticleSystem depositFx;

    private PlayerInventory inv;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private bool isSelected; // grip/hold state

    private void Awake()
    {
        if (storage == null) storage = GetComponent<ChestStorage>();
        inv = FindFirstObjectByType<PlayerInventory>();
    }

    private void Start()
    {
        ConnectEvents();
    }

    private void OnEnable()
    {
        Invoke(nameof(ConnectEvents), 0.1f);
    }

    private void OnDisable()
    {
        DisconnectEvents();
    }

    private void ConnectEvents()
    {
        if (interactable == null)
            interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

        if (interactable == null)
        {
            Debug.LogError($"[ChestNode] {gameObject.name}: Missing XRSimpleInteractable!");
            return;
        }

        DisconnectEvents();

        // Grip/select state
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);

        // Trigger/activate
        interactable.activated.AddListener(OnActivatedEvent);
    }

    private void DisconnectEvents()
    {
        if (interactable == null) return;
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        interactable.selectExited.RemoveListener(OnSelectExited);
        interactable.activated.RemoveListener(OnActivatedEvent);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        isSelected = true;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        isSelected = false;
    }

    private void OnActivatedEvent(ActivateEventArgs args)
    {
        OnActivated();
    }

    public void OnActivated()
    {
        if (storage == null)
        {
            Debug.LogError("[ChestNode] Missing ChestStorage on chest!");
            return;
        }
        if (inv == null)
        {
            inv = FindFirstObjectByType<PlayerInventory>();
        }
        if (inv == null)
        {
            Debug.LogError("[ChestNode] No PlayerInventory found in scene!");
            return;
        }

        // Grip + Trigger = Withdraw back to inventory
        if (withdrawAllWhenSelectedAndActivated && isSelected)
        {
            // Withdraw a fixed amount (like deposit), not everything.
            storage.WithdrawSomeTo(inv, withdrawAmountPerActivatePerType);
            return;
        }

        // Deposit from inventory into chest for ALL types currently present in the inventory.
        var snapshot = inv.GetSnapshot();
        bool movedAnything = false;

        foreach (var kv in snapshot)
        {
            ResourceType type = kv.Key;
            int have = kv.Value;
            if (have <= 0) continue;

            int currentStored = storage.GetStored(type);
            int spaceLeft = Mathf.Max(0, maxStoredPerType - currentStored);
            if (spaceLeft <= 0) continue;

            int toTake = Mathf.Min(amountPerActivatePerType, Mathf.Min(spaceLeft, have));
            int moved = inv.RemoveUpTo(type, toTake);
            if (moved <= 0) continue;

            storage.AddStored(type, moved);
            movedAnything = true;
        }

        if (!movedAnything) return;

        if (depositFx != null)
        {
            var fx = Instantiate(depositFx, transform.position, Quaternion.identity);
            fx.Play();
            var main = fx.main;
            Destroy(fx.gameObject, main.duration + main.startLifetime.constantMax + 0.1f);
        }
    }
}

