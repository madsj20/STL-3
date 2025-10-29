using System;
using UnityEngine;
using TMPro; // Remove if you prefer UnityEngine.UI.Text

[DefaultExecutionOrder(-50)] // Ensures it updates early (before most scripts)
public class RaceTimer : MonoBehaviour
{
    [Header("UI (optional)")]
    [SerializeField] private TMP_Text uiText; // Reference to your TMP text in the Canvas

    [Header("Behaviour")]
    [Tooltip("Start timing automatically when the scene loads.")]
    [SerializeField] private bool autoStartOnSceneLoad = true;

    [Tooltip("If true, timer ignores Time.timeScale (useful if you pause the game).")]
    [SerializeField] private bool useUnscaledTime = false;

    // --- Public read-only properties ---
    public bool IsRunning { get; private set; }     // Is the timer currently active?
    public float CurrentTime { get; private set; }  // The current elapsed time in seconds

    // --- Optional events (for other scripts to react) ---
    public event Action OnTimerStarted;
    public event Action OnTimerStopped;
    public event Action OnTimerReset;
    public event Action<float> OnTimerChanged; // Fires every frame while running or when manually updated

    private void Start()
    {
        // Reset timer to 0 when the scene starts
        ResetTimer(0f);

        // Automatically start counting if enabled
        if (autoStartOnSceneLoad)
            StartTimer();
    }

    private void Update()
    {
        // Only update while the timer is running
        if (!IsRunning) return;

        // Choose delta time depending on whether you want the timer to ignore Time.timeScale
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        // Increment current time
        CurrentTime += dt;

        // Update the UI text, if one is assigned
        if (uiText)
            uiText.text = FormatTime(CurrentTime);

        // Notify listeners that time changed (optional)
        OnTimerChanged?.Invoke(CurrentTime);
    }

    // --------------------------------------------------------------------
    // --- Public control methods (can be called from other scripts) ---
    // --------------------------------------------------------------------

    /// <summary>Starts or resumes the timer.</summary>
    public void StartTimer()
    {
        if (IsRunning) return; // Ignore if already running
        IsRunning = true;
        OnTimerStarted?.Invoke(); // Notify listeners
    }

    /// <summary>Stops (pauses) the timer.</summary>
    public void StopTimer()
    {
        if (!IsRunning) return; // Ignore if already stopped
        IsRunning = false;
        OnTimerStopped?.Invoke(); // Notify listeners
    }

    /// <summary>Resets the timer to a specific start value (default = 0).</summary>
    public void ResetTimer(float startAt = 0f)
    {
        CurrentTime = Mathf.Max(0f, startAt); // Ensure no negative time
        if (uiText)
            uiText.text = FormatTime(CurrentTime);

        OnTimerReset?.Invoke();
        OnTimerChanged?.Invoke(CurrentTime);
    }

    /// <summary>Manually set the timer to a specific value.</summary>
    public void SetTime(float time)
    {
        CurrentTime = Mathf.Max(0f, time);
        if (uiText)
            uiText.text = FormatTime(CurrentTime);

        OnTimerChanged?.Invoke(CurrentTime);
    }

    // --------------------------------------------------------------------
    // --- Accessor methods ---
    // --------------------------------------------------------------------

    /// <summary>Get the current time as a formatted string (MM:SS.mmm).</summary>
    public string GetFormattedTime() => FormatTime(CurrentTime);

    /// <summary>Get the current time as raw seconds (unformatted float).</summary>
    public float GetRawTime() => CurrentTime;

    // --------------------------------------------------------------------
    // --- Helper / Utility ---
    // --------------------------------------------------------------------

    /// <summary>Formats a float time into MM:SS.mmm format.</summary>
    public static string FormatTime(float time)
    {
        int minutes = (int)(time / 60f);                      // Whole minutes
        int seconds = (int)(time % 60f);                      // Remaining seconds
        int millis = (int)((time - Mathf.Floor(time)) * 1000); // Milliseconds
        return $"{minutes:00}:{seconds:00}.{millis:000}";
    }
}
