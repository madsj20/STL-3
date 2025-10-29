using UnityEngine;
using System.Collections;

public class PulseEffect : MonoBehaviour
{
    public float approachSpeed = 0.02f;
    public float growthBound = 2f;
    public float shrinkBound = 0.5f;
    private float currentRatio = 1f;
    
    private Vector3 originalScale;
    private Coroutine routine;
    private bool keepGoing = true;

    void Awake() 
    {
        // Store the original scale
        originalScale = transform.localScale;
        routine = StartCoroutine(Pulse());
    }

    IEnumerator Pulse()
    {
        while (keepGoing)
        {
            // Grow phase
            while (currentRatio < growthBound)
            {
                currentRatio = Mathf.MoveTowards(currentRatio, growthBound, approachSpeed);
                transform.localScale = originalScale * currentRatio;
                yield return null;
            }

            // Shrink phase
            while (currentRatio > shrinkBound)
            {
                currentRatio = Mathf.MoveTowards(currentRatio, shrinkBound, approachSpeed);
                transform.localScale = originalScale * currentRatio;
                yield return null;
            }
        }
    }
}