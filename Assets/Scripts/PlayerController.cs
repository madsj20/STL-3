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
    private AudioSource audioSource;

    // Audio clips and settings
    public AudioClip driveSound;
    public AudioClip crashSound;
    public AudioClip hornSound;
    [Range(0f, 1f)]
    public float driveSoundVolume = 0.5f;
    [Range(0f, 1f)]
    public float crashSoundVolume = 0.8f;

    private bool isMoving = false;
    private bool isHolding = false;
    private bool isRotating = false;
    private bool isCrashed = false; // Prevent further movement after collision
    public bool isIdle => !isMoving && !isRotating && !isHolding;

    public float rotateDuration = 0.25f; // how long a 90° turn takes

    [SerializeField] private RaceTimer timer; // Reference to the RaceTimer in this scene

    private void Awake()
    {
        if (!timer) timer = FindFirstObjectByType<RaceTimer>();
    }

    void Start()
    {
        gridManager = FindAnyObjectByType<GridManager>();
        animator = GetComponent<Animator>();
        
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure AudioSource for looping drive sound
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = driveSoundVolume;

        // Separate SFX source (not looping)
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
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
        
        // Stop any audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
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
    }
    
    public void MoveDown()
    {
        if (isMoving || isRotating || isHolding || isCrashed) return;
        TryMove(Vector2Int.down); // (0,-1)
        StartCoroutine(RotateTo(Vector2Int.down));
    }
    
    public void MoveLeft()
    {
        if (isMoving || isRotating || isHolding || isCrashed) return;
        TryMove(Vector2Int.left); // (-1,0)
        StartCoroutine(RotateTo(Vector2Int.left));
    }
    
    public void MoveRight()
    {
        if (isMoving || isRotating || isHolding || isCrashed) return;
        TryMove(Vector2Int.right); // (1,0)
        StartCoroutine(RotateTo(Vector2Int.right));
    }
    
    public void SetSpeed(float newSpeed)
    {
        // Clamp speed to avoid division by zero or extreme speeds
        float roundedSpeed = Mathf.Round(newSpeed); // Snap to nearest whole number
        float clampedSpeed = Mathf.Clamp(roundedSpeed, 0.5f, 5f);
        moveDuration = 1f / clampedSpeed;
    }

    public void PlayHorn()
    {
        if (sfxSource == null || hornSound == null) return;
        sfxSource.PlayOneShot(hornSound, 0.5f);
    }

    public void Hold(float delay)
    {
        StartCoroutine(HandleHold(delay));
    }

    private IEnumerator MoveTo(Vector2Int newPos)
    {
        isMoving = true;
        
        // Play drive sound when starting movement
        PlayDriveSound();
        
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
        
        // Stop drive sound when movement ends
        StopDriveSound();
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
        
        // Stop drive sound during hold
        StopDriveSound();
        
        yield return new WaitForSeconds(delay);
        isHolding = false;
    }

    private void PlayDriveSound()
    {
        if (audioSource == null || driveSound == null) return;
        
        if (!audioSource.isPlaying)
        {
            audioSource.clip = driveSound;
            audioSource.volume = driveSoundVolume;
            audioSource.Play();
        }
    }

    private void StopDriveSound()
    {
        if (audioSource == null) return;
        
        // Only stop if not moving and not rotating
        if (!isMoving && !isRotating && audioSource.isPlaying)
        {
            audioSource.Stop();
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
        
        // Stop drive sound and play crash sound
        StopDriveSound();
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
        
        // Stop any audio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
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
        }
    }
}