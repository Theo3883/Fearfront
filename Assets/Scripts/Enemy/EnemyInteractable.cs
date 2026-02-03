using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

/// <summary>
/// Enables VR interaction with enemies: hover highlight, attack on trigger.
/// Uses XRSimpleInteractable for ray-based interaction.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class EnemyInteractable : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float interactRange = 12f;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color hoverColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.15f;
    
    private XRSimpleInteractable interactable;
    private Enemy enemy;
    private float lastAttackTime = -999f;
    private Coroutine flashCoroutine;
    
    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        enemy = GetComponent<Enemy>();
    }
    
    private void OnEnable()
    {
        if (interactable == null) return;
        interactable.activated.AddListener(OnActivated);
    }
    
    private void OnDisable()
    {
        if (interactable == null) return;
        interactable.activated.RemoveListener(OnActivated);
    }
    
    private void OnActivated(ActivateEventArgs args)
    {
        Debug.Log($"[EnemyInteractable] Activated on {gameObject.name}");
        
        if (enemy == null || enemy.IsDead())
        {
            Debug.Log($"[EnemyInteractable] Skipped: enemy null or dead");
            return;
        }
        
        if (Time.time - lastAttackTime < attackCooldown)
        {
            Debug.Log($"[EnemyInteractable] Skipped: cooldown");
            return;
        }
        
        if (!IsInRange(args.interactorObject))
        {
            Debug.Log($"[EnemyInteractable] Skipped: out of range");
            return;
        }
        
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
        enemy.TakeDamage(attackDamage);
        
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashHit());
    }
    
    private IEnumerator FlashHit()
    {
        yield return new WaitForSeconds(flashDuration);
        flashCoroutine = null;
    }
}
