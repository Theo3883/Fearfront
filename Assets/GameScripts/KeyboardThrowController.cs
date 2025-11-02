using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

// Keyboard throw controller: Grab with G, hold H to charge, release H to throw toward the hand's forward.
public class KeyboardThrowController : MonoBehaviour
{
    [Header("Throw Settings")]
    [Tooltip("Minimum throw force")]
    public float minThrowForce = 5f;
    
    [Tooltip("Maximum throw force")]
    public float maxThrowForce = 15f;
    
    [Tooltip("Time to reach maximum power (seconds)")]
    public float chargeTime = 2f;
    
    [Tooltip("Throw angle in degrees relative to hand direction (0 = straight forward from hand, 45 = upward arc)")]
    public float throwAngle = 45f;
    
    [Header("Visual Feedback")]
    [Tooltip("Optional: UI element to show power charge")]
    public UnityEngine.UI.Slider powerBar;
    
    [Header("References")]
    [Tooltip("The camera/head transform to throw from (usually XR Origin Camera)")]
    public Transform throwOrigin;
    
    [Tooltip("Left hand XR controller (will auto-find if not set)")]
    public XRBaseInteractor leftHandController;
    
    [Tooltip("Right hand XR controller (will auto-find if not set)")]
    public XRBaseInteractor rightHandController;
    
    private bool isCharging = false;
    private float chargeStartTime = 0f;
    private float currentPower = 0f;
    private BBasketball currentlyHeldBall = null;
    private XRBaseInteractor currentInteractor = null;
    
    private void Start()
    {
        // If no throw origin is set, try to find the camera
        if (throwOrigin == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                throwOrigin = mainCam.transform;
                Debug.Log("[KeyboardThrowController] Using Main Camera as throw origin");
            }
            else
            {
                Debug.LogWarning("[KeyboardThrowController] No throw origin set and couldn't find Main Camera!");
            }
        }
        
        // Auto-find controllers if not set
        if (leftHandController == null || rightHandController == null)
        {
            XRBaseInteractor[] interactors = FindObjectsOfType<XRBaseInteractor>();
            foreach (var interactor in interactors)
            {
                string name = interactor.gameObject.name.ToLower();
                if (name.Contains("left") && leftHandController == null)
                {
                    leftHandController = interactor;
                    Debug.Log("[KeyboardThrowController] Found left hand controller: " + interactor.gameObject.name);
                }
                else if (name.Contains("right") && rightHandController == null)
                {
                    rightHandController = interactor;
                    Debug.Log("[KeyboardThrowController] Found right hand controller: " + interactor.gameObject.name);
                }
            }
        }
        
        // Hide power bar initially
        if (powerBar != null)
        {
            powerBar.gameObject.SetActive(false);
            powerBar.minValue = 0f;
            powerBar.maxValue = 1f;
        }
    }
    
    private void Update()
    {
        // Find which ball (if any) is currently being held
        UpdateCurrentlyHeldBall();
        
        // Only allow charging if we're holding a basketball
        if (currentlyHeldBall != null)
        {
            // Press H to start charging (ONLY WHEN HOLDING A BALL)
            if (Input.GetKeyDown(KeyCode.H) && !isCharging)
            {
                StartCharging();
            }
            
            // Hold H to continue charging
            if (Input.GetKey(KeyCode.H) && isCharging)
            {
                UpdateCharge();
            }
            
            // Release H to throw
            if (Input.GetKeyUp(KeyCode.H) && isCharging)
            {
                ThrowBall();
            }
        }
        else
        {
            // Not holding anything, cancel any charging
            if (isCharging)
            {
                CancelCharge();
            }
        }
    }
    
    private void UpdateCurrentlyHeldBall()
    {
        currentlyHeldBall = null;
        currentInteractor = null;
        
        // Check left hand
        if (leftHandController != null && leftHandController.hasSelection)
        {
            var selected = leftHandController.interactablesSelected;
            foreach (var interactable in selected)
            {
                BBasketball ball = interactable.transform.GetComponentInParent<BBasketball>();
                if (ball != null)
                {
                    currentlyHeldBall = ball;
                    currentInteractor = leftHandController;
                    return;
                }
            }
        }
        
        // Check right hand
        if (rightHandController != null && rightHandController.hasSelection)
        {
            var selected = rightHandController.interactablesSelected;
            foreach (var interactable in selected)
            {
                BBasketball ball = interactable.transform.GetComponentInParent<BBasketball>();
                if (ball != null)
                {
                    currentlyHeldBall = ball;
                    currentInteractor = rightHandController;
                    return;
                }
            }
        }
    }
    
    private void StartCharging()
    {
        if (currentlyHeldBall == null)
            return;
            
        isCharging = true;
        chargeStartTime = Time.time;
        currentPower = 0f;
        
        if (powerBar != null)
        {
            powerBar.gameObject.SetActive(true);
            powerBar.value = 0f;
        }
        
        Debug.Log($"[KeyboardThrowController] Charging throw for {currentlyHeldBall.gameObject.name}...");
    }
    
    private void UpdateCharge()
    {
        float chargeProgress = (Time.time - chargeStartTime) / chargeTime;
        currentPower = Mathf.Clamp01(chargeProgress);
        
        if (powerBar != null)
        {
            powerBar.value = currentPower;
        }
    }
    
    private void CancelCharge()
    {
        isCharging = false;
        currentPower = 0f;
        
        if (powerBar != null)
        {
            powerBar.gameObject.SetActive(false);
        }
        
        Debug.Log("[KeyboardThrowController] Charge cancelled - ball was dropped");
    }
    
    private void ThrowBall()
    {
        if (currentlyHeldBall == null || currentInteractor == null)
        {
            Debug.LogWarning("[KeyboardThrowController] Cannot throw - no ball being held!");
            CancelCharge();
            return;
        }
        
        if (throwOrigin == null)
        {
            Debug.LogWarning("[KeyboardThrowController] Cannot throw - no throw origin set!");
            CancelCharge();
            return;
        }
        
        // Get the rigidbody
        Rigidbody ballRigidbody = currentlyHeldBall.GetComponentInChildren<Rigidbody>();
        if (ballRigidbody == null)
        {
            Debug.LogError("[KeyboardThrowController] Ball has no Rigidbody!");
            CancelCharge();
            return;
        }
        
        // Store the ball reference before releasing
        BBasketball ballToThrow = currentlyHeldBall;
        
        // Use hand position as release position instead of camera
        Vector3 releasePosition = currentInteractor.transform.position;
        
        // Calculate throw force and direction based on hand position and orientation
        float throwForce = Mathf.Lerp(minThrowForce, maxThrowForce, currentPower);
        
        // Use hand controller's transform for throw direction instead of camera
        Transform handTransform = currentInteractor.transform;
        Vector3 forward = handTransform.forward;
        Vector3 up = handTransform.up;
        float angleRad = throwAngle * Mathf.Deg2Rad;
        Vector3 throwDirection = (forward * Mathf.Cos(angleRad) + up * Mathf.Sin(angleRad)).normalized;
        
        // FORCE RELEASE THE BALL from the hand controller
        var grabInteractable = currentInteractor.interactablesSelected;
        foreach (var interactable in grabInteractable)
        {
            if (interactable.transform.IsChildOf(ballToThrow.transform) || 
                interactable.transform == ballToThrow.transform)
            {
                currentInteractor.interactionManager.SelectExit(currentInteractor, interactable);
                break;
            }
        }
        
        // Wait a tiny moment for physics to update after release
        StartCoroutine(ApplyThrowForceAfterRelease(ballRigidbody, throwDirection, throwForce, releasePosition, ballToThrow));
    }
    
    private System.Collections.IEnumerator ApplyThrowForceAfterRelease(Rigidbody rb, Vector3 direction, float force, Vector3 releasePos, BBasketball ball)
    {
        // Wait one physics frame
        yield return new WaitForFixedUpdate();
        
        // Now apply the throw force
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(direction * force, ForceMode.VelocityChange);
        
        // Add some spin for realism
        Vector3 randomSpin = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized * (currentPower * 2f);
        rb.angularVelocity = randomSpin;
        
        // Set the release position for scoring
        ball.SetReleasePosition(releasePos);
        
        Debug.Log($"[KeyboardThrowController] Ball thrown with {(currentPower * 100):F1}% power (Force: {force:F2}) from hand at {releasePos}");
        
        // Reset charging
        isCharging = false;
        currentPower = 0f;
        
        if (powerBar != null)
        {
            powerBar.gameObject.SetActive(false);
        }
    }
    
    private void OnGUI()
    {
        // Show charging indicator on screen if no UI slider is provided
        if (isCharging && powerBar == null)
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float barWidth = 300f;
            float barHeight = 30f;
            float barX = (screenWidth - barWidth) / 2f;
            float barY = screenHeight - 100f;
            
            // Background
            GUI.Box(new Rect(barX, barY, barWidth, barHeight), "");
            
            // Power bar
            GUI.color = Color.Lerp(Color.yellow, Color.red, currentPower);
            GUI.Box(new Rect(barX + 2, barY + 2, (barWidth - 4) * currentPower, barHeight - 4), "");
            GUI.color = Color.white;
            
            // Text
            GUI.Label(new Rect(barX, barY - 25, barWidth, 20), $"THROW POWER: {(currentPower * 100):F0}%");
            GUI.Label(new Rect(barX, barY + barHeight + 5, barWidth, 20), "Hold H to charge, Release to throw");
        }
    }
}

