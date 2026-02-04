using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

/// <summary>
/// Enables VR interaction with enemies: hover highlight, attack on trigger.
/// Uses XRSimpleInteractable for ray-based interaction.
/// Implements IHighlightColorProvider to provide red highlight color for enemies.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class EnemyInteractable : MonoBehaviour, IHighlightColorProvider
{
    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float interactRange = 12f;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color hoverColor = new Color(0.3f, 0.3f, 0.3f, 1f); // Dark tint for enemies
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashIntensity = 2f;
    [SerializeField] private float flashDuration = 0.15f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    [Range(0f, 1f)]
    [SerializeField] private float hitVolume = 0.7f;
    
    private XRSimpleInteractable interactable;
    private Enemy enemy;
    private Renderer[] renderers;
    private AudioSource audioSource;
    
    /// <summary>
    /// Returns the highlight color for enemies (dark tint).
    /// Called by VRHighlightManager when this enemy is hovered.
    /// </summary>
    public Color GetHighlightColor()
    {
        if (EnemyVisualsConfig.Instance != null)
            return EnemyVisualsConfig.Instance.HoverHighlightColor;
        return hoverColor;
    }
    
    private float lastAttackTime = -999f;
    private Coroutine flashCoroutine;
    
    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        enemy = GetComponent<Enemy>();
        renderers = GetComponentsInChildren<Renderer>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    private void OnEnable()
    {
        if (interactable != null) interactable.activated.AddListener(OnActivated);
        if (enemy != null) enemy.OnHealthChanged += OnHealthChanged;
    }
    
    private void OnDisable()
    {
        if (interactable != null) interactable.activated.RemoveListener(OnActivated);
        if (enemy != null) enemy.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(float current, float max)
    {
        // Flash on any damage (health decrease)
        if (current < max) 
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashHit());
        }
    }
    
    private void OnActivated(ActivateEventArgs args)
    {
        if (enemy == null || enemy.IsDead()) return;
        
        if (Time.time - lastAttackTime < attackCooldown) return;
        
        if (!IsInRange(args.interactorObject)) return;
        
        lastAttackTime = Time.time;
        AttackEnemy();
    }
    
    private bool IsInRange(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor)
    {
        if (interactor == null) return true;
        
        Transform interactorTransform = interactor.transform;
        if (interactorTransform == null) return true;
        
        float distance = Vector3.Distance(transform.position, interactorTransform.position);
        return distance <= interactRange;
    }
    
    private void AttackEnemy()
    {
        // Player attacking enemy via VR interaction
        if (enemy != null)
        {
            enemy.TakeDamage(attackDamage);
            
            // Play hit sound
            if (hitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hitSound, hitVolume);
            }
        }
    }
    
    private IEnumerator FlashHit()
    {
        Color color = flashColor;
        float intensity = flashIntensity;
        float duration = flashDuration;

        if (EnemyVisualsConfig.Instance != null)
        {
            color = EnemyVisualsConfig.Instance.FlashColor;
            intensity = EnemyVisualsConfig.Instance.FlashIntensity;
            duration = EnemyVisualsConfig.Instance.FlashDuration;
        }

        // Enable emission and set color
        foreach (var r in renderers)
        {
            if (r == null) continue;
            foreach (var mat in r.materials)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * intensity);
            }
        }

        yield return new WaitForSeconds(duration);
        
        // Restore emission (turn off or black)
        foreach (var r in renderers)
        {
            if (r == null) continue;
            foreach (var mat in r.materials)
            {
                mat.SetColor("_EmissionColor", Color.black);
                mat.DisableKeyword("_EMISSION");
            }
        }
        
        flashCoroutine = null;
    }
}
