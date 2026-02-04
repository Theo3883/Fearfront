using UnityEngine;
using System.Collections;
using Sydewa; // Needed for LightingManager

public class BackgroundMusic : MonoBehaviour
{
    private static BackgroundMusic instance;
    private AudioSource audioSource;

    [Header("Audio Settings")]
    [Tooltip("Music to play during the Day.")]
    public AudioClip dayClip;
    [Tooltip("Music to play during the Night.")]
    public AudioClip nightClip;
    
    [Range(0f, 1f)]
    [Tooltip("Volume of the background music.")]
    public float volume = 0.5f;
    [Tooltip("Duration of the crossfade between tracks.")]
    public float fadeDuration = 2.0f;

    [Tooltip("If true, the music object will persist between scene loads.")]
    public bool persistBetweenScenes = true;

    private Coroutine fadeCoroutine;

    void Awake()
    {
        // Singleton Implementation
        if (persistBetweenScenes)
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure AudioSource
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
    }

    void Start()
    {
        HookIntoLightingManager();
        
        // Default to day music if nothing else happens, or wait for event?
        // Let's default to Day.
        if (dayClip != null)
        {
            PlayMusic(dayClip);
        }
    }

    void Update()
    {
        // If not fading, allow realtime volume adjustment
        if (fadeCoroutine == null && Mathf.Abs(audioSource.volume - volume) > 0.01f)
        {
            audioSource.volume = volume;
        }
    }

    private void HookIntoLightingManager()
    {
        LightingManager[] managers = FindObjectsByType<LightingManager>(FindObjectsSortMode.None);
        
        if (managers == null || managers.Length == 0)
        {
            Debug.LogWarning("BackgroundMusic: No LightingManager found via FindObjectsByType.");
            return;
        }

        LightingManager targetManager = null;
        foreach (var manager in managers)
        {
            if (manager.events != null && manager.events.Count > 0)
            {
                targetManager = manager;
                break;
            }
        }
        
        if (targetManager == null) return;

        foreach (var evt in targetManager.events)
        {
            if (evt.eventName == "Start Night")
            {
                evt.Event.AddListener(OnStartNight);
            }
            else if (evt.eventName == "Start Day")
            {
                evt.Event.AddListener(OnStartDay);
            }
        }
    }

    public void OnStartDay()
    {
        Debug.Log("BackgroundMusic: Switching to Day Music");
        if (dayClip != null && audioSource.clip != dayClip)
        {
            if (fadeDuration > 0)
                FadeTo(dayClip);
            else
                PlayMusic(dayClip);
        }
    }

    public void OnStartNight()
    {
        Debug.Log("BackgroundMusic: Switching to Night Music");
        if (nightClip != null && audioSource.clip != nightClip)
        {
            if (fadeDuration > 0)
                FadeTo(nightClip);
            else
                PlayMusic(nightClip);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        
        // If we are fading, stop it
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
    }

    public void StopMusic()
    {
        audioSource.Stop();
    }

    private void FadeTo(AudioClip newClip)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(newClip));
    }

    private IEnumerator FadeRoutine(AudioClip newClip)
    {
        float startVolume = audioSource.volume;

        // Fade out
        for (float t = 0; t < fadeDuration / 2; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0, t / (fadeDuration / 2));
            yield return null;
        }
        audioSource.volume = 0;
        audioSource.Stop();

        // Swap
        audioSource.clip = newClip;
        audioSource.Play();

        // Fade in
        for (float t = 0; t < fadeDuration / 2; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(0, volume, t / (fadeDuration / 2));
            yield return null;
        }
        audioSource.volume = volume;
        fadeCoroutine = null;
    }
}

