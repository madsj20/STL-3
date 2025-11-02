using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public Vector2Int gridPosition; //Current grid position
    private Vector2Int startGridPosition;
    public Vector2Int faceDirection = Vector2Int.up; //The car facing direction

    public float moveDuration = 1f; //Car speed from one tile to another

    private GridManager gridManager;
    private Animator animator;
    private AudioSource sfxSource;

    // Audio clips and settings
    public AudioClip crashSound;
    public AudioClip hornSound;
    [Range(0f, 1f)]
    public float crashSoundVolume = 0.8f;

    private bool isMoving = false;
    private bool isHolding = false;
    private bool isRotating = false;
    private bool isCrashed = false; // Prevent further movement after collision
    private bool hasWon = false;
    public bool isIdle => !isMoving && !isRotating && !isHolding;
    public bool rotationAlreadyHandled = false;

    public float rotateDuration = 0.25f; // how long a 90° turn takes
    public float oilSpinDuration = 1f; // how long the 540° spin takes when dropping oil

    [SerializeField] private RaceTimer timer; // Reference to the RaceTimer in this scene
    [SerializeField] private BrickQueManager brickQueManager;


    private void Awake()
    {
        if (!timer) timer = FindFirstObjectByType<RaceTimer>();
    }

    void Start()
    {
        gridManager = FindAnyObjectByType<GridManager>();
        animator = GetComponent<Animator>();
        
        // Setup SFX AudioSource for crash sound
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    private void Update()
    {
        if (isMoving && !hasWon)
        {
            timer.StartTimer(); // Start the timer when the car moves
        }
        else if (!isMoving && !isRotating && !isHolding && !isCrashed)
        {
            timer.StopTimer(); // Stop the timer when the car is idle
        }
    }

    // Set spawn position for the "START" RoadPiece
    public void SetSpawnWorld(Vector3 worldPos, Vector2Int dir)
    {
        transform.position = worldPos; // exact prefab position
        transform.up = new Vector3(dir.x, dir.y, 0);
        faceDirection = dir;

        // logical grid for movement
        gridPosition = Vector2Int.RoundToInt(new Vector2(worldPos.x, worldPos.y));
    }

    // Set the logic for the Clear button to respawn to the current Start piece
    public void RespawnToCurrentStart()
    {
        animator.SetBool("isCrashing", false); // reset crash animation
        isCrashed = false; // Allow movement again
        
        var pieces = Object.FindObjectsByType<RoadPiece>(FindObjectsSortMode.None);
        foreach (var p in pieces)
        {
            if (p != null && p.data != null && p.data.type == RoadPieceType.Start)
            {
                SetSpawnWorld(p.transform.position,
                            Vector2Int.RoundToInt(p.data.startDirection));
                return;
            }
        }
        // fallback if no Start piece exists
        ResetPosition();
    }

    // Makes the car drive forward if possible
    private bool TryMove(Vector2Int delta)
    {
        if (isMoving || isRotating || isHolding || isCrashed) return false;

        Vector2Int newPos = gridPosition + delta;
        Tile targetTile = gridManager.GetTileAtPosition(newPos);
        if (targetTile == null) return false;

        StartCoroutine(MoveTo(newPos)); // Start the movement coroutine
        return true;
    }

    public void MoveUp()
    {
        if (isMoving || isRotating || isHolding || isCrashed) return;
        TryMove(Vector2Int.up); // (0,1)
        StartCoroutine(RotateTo(Vector2Int.up));

        rotationAlreadyHandled = false;
    }
    
    public void MoveDown()
    {
        if (isMoving || isRotating || isHolding || isCrashed) return;
        TryMove(Vector2Int.down); // (0,-1)
        StartCoroutine(RotateTo(Vector2Int.down));

        rotationAlreadyHandled = false;
    }
    
    public void MoveLeft()
    {
        if (isMoving || isRotating || isHolding || isCrashed) return;
        TryMove(Vector2Int.left); // (-1,0)
        StartCoroutine(RotateTo(Vector2Int.left));
        rotationAlreadyHandled = false;
    }
    
    public void MoveRight()
    {
        if (isMoving || isRotating || isHolding || isCrashed) return;
        TryMove(Vector2Int.right); // (1,0)
        StartCoroutine(RotateTo(Vector2Int.right));

        rotationAlreadyHandled = false;
    }

    public void SetSpeed(float newSpeed)
    {
        // Clamp speed to avoid division by zero or extreme speeds
        float roundedSpeed = Mathf.Round(newSpeed); // Snap to nearest whole number
        float clampedSpeed = Mathf.Clamp(roundedSpeed, 0.5f, 5f);
        moveDuration = 1f / clampedSpeed;
    }
    
    public void SpinOnOil(float duration)
    {
        Debug.Log("Spinning on oil...");
        StartCoroutine(HandleOilSpin(duration));
    }

    public void PlayHorn()
    {
        if (sfxSource == null || hornSound == null) return;
        sfxSource.PlayOneShot(hornSound, 0.5f);
    }

    public void DropOil(GameObject oilPrefab, AudioClip oilSound)
    {
        StartCoroutine(HandleOilDrop(oilPrefab, oilSound));
    }

    public void Hold(float delay)
    {
        StartCoroutine(HandleHold(delay));
    }

    private IEnumerator MoveTo(Vector2Int newPos)
    {
        isMoving = true;
        
        Vector3 start = transform.position;
        Vector3 end = new Vector3(newPos.x, newPos.y, 0);

        float elapsed = 0;
        while (elapsed < moveDuration)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
        gridPosition = newPos;
        isMoving = false;
    }
    
    private IEnumerator RotateTo(Vector2Int newDir)
    {
        isRotating = true;
        Quaternion start = transform.rotation;
        Quaternion goal = Quaternion.LookRotation(Vector3.forward, new Vector3(newDir.x, newDir.y, 0));

        float t = 0f;
        while (t < rotateDuration)
        {
            transform.rotation = Quaternion.Slerp(start, goal, t / rotateDuration);
            t += Time.deltaTime;
            yield return null;
        }
        transform.rotation = goal;

        faceDirection = newDir; // Updates first when the rotation is done
        isRotating = false;
    }

    public IEnumerator HandleHold(float delay)
    {
        isHolding = true;
        yield return new WaitForSeconds(delay);
        isHolding = false;
    }
    
    private IEnumerator HandleOilSpin(float duration)
    {
        Debug.Log("Handling oil spin...");
        isHolding = true; // Prevent other actions during spin
            
        // Get current facing direction as a quaternion
        Quaternion startRotation = transform.rotation;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Rotate 540 degrees over the duration
            float angle = Mathf.Lerp(0f, 540f, elapsed / duration);
            transform.rotation = startRotation * Quaternion.Euler(0, 0, -angle);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // After 540° rotation (1.5 spins), flip the face direction to backwards
        faceDirection = -faceDirection;
        
        // THEN set final rotation to match the NEW face direction
        transform.rotation = Quaternion.LookRotation(Vector3.forward, new Vector3(faceDirection.x, faceDirection.y, 0));
        
        isHolding = false;
    }

    private IEnumerator HandleOilDrop(GameObject oilPrefab, AudioClip oilSound)
    {
        isHolding = true; // Prevent other actions during oil drop
        
        // Play sound effect
        if (sfxSource != null && oilSound != null)
        {
            sfxSource.PlayOneShot(oilSound, 0.7f);
        }
        
        // Spin 540 degrees
        Quaternion startRotation = transform.rotation;
        
        float elapsed = 0f;
        while (elapsed < oilSpinDuration)
        {
            // Rotate 540 degrees over the duration
            float angle = Mathf.Lerp(0f, 540f, elapsed / oilSpinDuration);
            transform.rotation = startRotation * Quaternion.Euler(0, 0, -angle);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // After 540° rotation (1.5 spins), flip the face direction to backwards
        faceDirection = -faceDirection;
        
        // Set final rotation to match the NEW face direction
        transform.rotation = Quaternion.LookRotation(Vector3.forward, new Vector3(faceDirection.x, faceDirection.y, 0));

        // Drop oil at current position
        if (oilPrefab != null)
        {
            Vector3 oilPosition = new Vector3(gridPosition.x, gridPosition.y, 0);
            GameObject oilInstance = Instantiate(oilPrefab, oilPosition, Quaternion.identity);

            // Disable the collider temporarily to prevent immediate trigger
            Collider2D oilCollider = oilInstance.GetComponent<Collider2D>();
            if (oilCollider != null)
            {
                oilCollider.enabled = false;
                StartCoroutine(EnableOilColliderAfterDelay(oilCollider, 1f)); // Enable after 1 second
            }

            // Optional: Add the oil to a parent container for organization
            GameObject oilContainer = GameObject.Find("DroppedOils");
            if (oilContainer == null)
            {
                oilContainer = new GameObject("DroppedOils");
            }
            oilInstance.transform.SetParent(oilContainer.transform);
        }
        
        isHolding = false;
        rotationAlreadyHandled = true;
    }

    private IEnumerator EnableOilColliderAfterDelay(Collider2D collider, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (collider != null)
        {
            collider.enabled = true;
        }
    }

    private void PlayCrashSound()
    {
        if (sfxSource == null || crashSound == null) return;
        sfxSource.PlayOneShot(crashSound, crashSoundVolume);
    }

    private void HandleCollision()
    {
        isCrashed = true;
        PlayCrashSound();
        
        if (animator != null)
        {
            animator.SetBool("isCrashing", true); // trigger crash animation
        }
    }

    public void ResetPosition()
    {
        gridPosition = startGridPosition; // Reset logical position
        transform.position = new Vector3(gridPosition.x, gridPosition.y, 0);
        faceDirection = Vector2Int.up;
        transform.up = new Vector3(faceDirection.x, faceDirection.y, 0); // Reset facing direction
        isCrashed = false; // Allow movement again
        animator.SetBool("isCrashing", false); // reset crash animation
    }

    // --- COLLISION HANDLING ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Obstacle") && !isCrashed)
        {
            HandleCollision();
        }

        // Only react if the object entering is tagged as "Goal"
        if (other.CompareTag("Goal") && timer != null)
        {
            timer.StopTimer(); // Stop counting
            Debug.Log("Final time: " + timer.GetFormattedTime());
            if (brickQueManager != null)
                brickQueManager.NotifyGoalCrossed();
            hasWon = true; // Prevent timer from going
        }
    }
    public void ResetWinState()
    {
        hasWon = false; // Allow timer to run again
    }

    // Method to get the current win state
    public void GetCurrentWinState(out bool winState)
    {
        winState = hasWon;
    }
}