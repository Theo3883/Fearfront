using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Displays a "NIGHT X" overlay at the start of each night with smooth fade animations.
/// Based on DeathOverlay.cs implementation.
/// </summary>
public class NightAnnouncementOverlay : MonoBehaviour
{
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI nightText;
    
    [Header("Animation Settings")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    private CanvasGroup canvasGroup;
    
    private void Awake()
    {
        // Ensure CanvasGroup exists for alpha fade animations
        if (overlayPanel != null)
        {
            canvasGroup = overlayPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = overlayPanel.AddComponent<CanvasGroup>();
            }
            overlayPanel.SetActive(false);
        }
        
        ConfigureCanvas();
    }
    
    /// <summary>
    /// Shows the night announcement overlay with the specified night number
    /// </summary>
    public void ShowNightAnnouncement(int nightNumber)
    {
        if (overlayPanel == null || nightText == null)
        {
            Debug.LogWarning("NightAnnouncementOverlay: Missing UI components!");
            return;
        }
        
        StopAllCoroutines();
        StartCoroutine(ShowAnnouncementRoutine(nightNumber));
    }
    
    private IEnumerator ShowAnnouncementRoutine(int nightNumber)
    {
        // Setup
        nightText.text = $"NIGHT {nightNumber}";
        overlayPanel.SetActive(true);
        canvasGroup.alpha = 0f;
        
        // Fade In
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        
        // Hold
        yield return new WaitForSeconds(displayDuration);
        
        // Fade Out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        
        // Cleanup
        canvasGroup.alpha = 0f;
        overlayPanel.SetActive(false);
    }
    
    /// <summary>
    /// Configures the Canvas for proper VR visibility (copied from DeathOverlay.cs)
    /// </summary>
    private void ConfigureCanvas()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            
            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }
            
            if (canvas.gameObject.GetComponent<VRHeadFollower>() == null)
            {
                canvas.gameObject.AddComponent<VRHeadFollower>();
            }
            
            canvas.sortingOrder = 32767;
            
            if (canvas.transform.localScale == Vector3.one)
            {
                canvas.transform.localScale = Vector3.one * 0.001f;
            }
        }
    }
}
