using UnityEngine;
using System.Collections;

public class PulseEffect : MonoBehaviour
{
    public float pulseSpeed = 1f;
    public float growthBound = 1.1f;
    public float shrinkBound = 0.9f;
    private float currentRatio = 1f;
    
    private Vector3 originalScale;
    private Coroutine routine;
    private bool isPulsing = false;

    void Awake() 
    {
        originalScale = transform.localScale;
    }

    // Call this to start pulsing
    public void StartPulsing()
    {
        if (isPulsing) return;
        
        isPulsing = true;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Pulse());
    }

    // Call this to stop pulsing and reset scale
    public void StopPulsing()
    {
        isPulsing = false;
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        
        // Reset to original scale
        transform.localScale = originalScale;
        currentRatio = 1f;
    }

    IEnumerator Pulse()
    {
        while (isPulsing)
        {
            // Grow phase
            while (currentRatio < growthBound && isPulsing)
            {
                currentRatio = Mathf.MoveTowards(currentRatio, growthBound, pulseSpeed * Time.deltaTime);
                transform.localScale = originalScale * currentRatio;
                yield return null;
            }

            // Shrink phase
            while (currentRatio > shrinkBound && isPulsing)
            {
                currentRatio = Mathf.MoveTowards(currentRatio, shrinkBound, pulseSpeed * Time.deltaTime);
                transform.localScale = originalScale * currentRatio;
                yield return null;
            }
        }
    }
}