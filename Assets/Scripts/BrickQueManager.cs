using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BrickQueManager : MonoBehaviour
{
    // Define the possible actions the player can take
    public enum ActionType { MoveForward, TurnLeft, TurnRight, MoveBackward, PlayHorn, DropOil, SpeedBoost, None }

    public PlayerController player;
    public TMP_Text queueLabel;    // shows the queued actions
    public float commandDelay = 0.05f; // small gap between commands
    public PulseEffect playButtonPulse;

    public Transform PanelThatPlaysTheSequence; //The panel that holds the inventory slots
    public Slot slotPrefab; // prefab used to create additional slots when needed

    // Oil drop settings
    public GameObject oilPrefab; // Assign your oil road piece prefab in inspector
    public AudioClip oilDropSound; // Sound when dropping oil

    // Highlight settings
    public float highlightScale = 1.1f; // Slight scale increase

    private readonly Queue<ActionType> queue = new Queue<ActionType>();
    private bool isPlaying = false;
    private bool isPaused = false;
    private int currentExecutingIndex = -1; // Track which slot is currently executing
    private int pausedAtIndex = -1; // Remember where we paused
    private GameObject pausedBrickGO; // remember the brick that was paused on

    [SerializeField] private RaceTimer timer; // Reference to the RaceTimer in this scene

    
    [Header("Speed Boost")]
    [SerializeField] private float speedBoostMultiplier = 2f;  // 2x faster
    [SerializeField] private int   speedBoostMoves = 2;   // affect next two moves

    private int   boostMovesLeft = 0;
    private float? originalMoveDuration = null;



    public GameObject WarningUI;

    // --- Replay support ---
    // Accumulates all commands that have been executed across Play presses.
    private readonly List<ActionType> replayQueue = new List<ActionType>();
    // True when we are currently playing the accumulated replay (avoid re-recording).
    private bool isReplaying = false;

    private void Awake()
    {
        if (!timer) timer = FindFirstObjectByType<RaceTimer>();
    }

    private void Start()
    {
        // Make sure all slot backgrounds are OFF at scene start
        if (PanelThatPlaysTheSequence == null) return;

        for (int i = 0; i < PanelThatPlaysTheSequence.childCount; i++)
        {
            var slot = PanelThatPlaysTheSequence.GetChild(i).GetComponent<Slot>();
            if (slot != null)
                slot.SetBackgroundActive(false);
        }
    }

    private void Update()
    {
        // Ensure there's always more than one empty slot available
        EnsureSlots();
    }

    public void ClearQueue()
    {
        queue.Clear();
        RefreshLabel();
    }

    //play the queued commands
    public void Play()
    {
        // stop pulsing immediately when Play is clicked
        if (playButtonPulse != null) playButtonPulse.StopPulsing();
    
        if (isPaused)
        {
            isPaused = false;
            AudioListener.pause = false;

            int occupiedStartIndex = pausedAtIndex;

            //1 Check which brick slot we have pauseed on
            int pausedPanelIndex = PanelIndexFromOccupiedIndex(pausedAtIndex);
            Slot pausedSlot = null;
            if (pausedPanelIndex >= 0 && pausedPanelIndex < PanelThatPlaysTheSequence.childCount)
                pausedSlot = PanelThatPlaysTheSequence.GetChild(pausedPanelIndex).GetComponent<Slot>();

            //2 Is it the same brick as before paused?
            bool unchanged = (pausedSlot != null && pausedSlot.brickPrefab == pausedBrickGO);

            //3 If the brick is unchanged -> skip by +1
            if (unchanged)
                occupiedStartIndex = pausedAtIndex + 1;

            //4 if the brick is changed -> Run the code from the current position
            int panelStartIndex = PanelIndexFromOccupiedIndex(occupiedStartIndex);
            RebuildQueueFromIndex(panelStartIndex); // builds the rest form the correct place
            pausedAtIndex = occupiedStartIndex;

            RemoveAllHighlights();
            if (!isPlaying) StartCoroutine(Run());

            pausedBrickGO = null;
            return;
        }

        if (!isPlaying) StartCoroutine(Run());
    }

    public void Pause()
    {
        if (!isPlaying) return;

        isPaused = true;
        pausedAtIndex = currentExecutingIndex; // Remember current position

        // Save the reference to the brick we stood on
        var pausedPanelIndex = PanelIndexFromOccupiedIndex(pausedAtIndex);
        if (pausedPanelIndex >= 0 && pausedPanelIndex < PanelThatPlaysTheSequence.childCount)
        {
            var slot = PanelThatPlaysTheSequence.GetChild(pausedPanelIndex).GetComponent<Slot>();
            pausedBrickGO = slot != null ? slot.brickPrefab : null;
        }

        StopAllCoroutines();
        isPlaying = false;

        AudioListener.pause = true; // Pause all audio

        // resume pulsing while paused (if there are any bricks placed)
        UpdatePlayPulse();

    }

    public void PlayFromPanel()
    {
        if (isPaused)
        {
            Play(); // Use the regular Play which handles resume
            return;
        }

        if (isPlaying) return;

        // remove any previous commands
        queue.Clear();
        pausedAtIndex = -1;

        if (PanelThatPlaysTheSequence == null) { RefreshLabel(); return; }

        // read slots left to right
        for (int i = 0; i < PanelThatPlaysTheSequence.childCount; i++)
        {
            var slot = PanelThatPlaysTheSequence.GetChild(i).GetComponent<Slot>();
            if (slot == null) continue;

            var brickGO = slot.brickPrefab;
            if (brickGO == null) continue;

            var piece = brickGO.GetComponent<BrickPiece>();
            if (piece == null) continue;

            Enqueue(piece.action);
        }

        Play();
        timer.StartTimer(); // ensure timer is running
        Debug.Log("Resuming from pause");
    }

    // Rebuild the queue starting from a specific index
    private void RebuildQueueFromIndex(int startIndex)
    {
        queue.Clear();

        if (PanelThatPlaysTheSequence == null) { RefreshLabel(); return; }

        // Start from the paused position and read the rest
        for (int i = startIndex; i < PanelThatPlaysTheSequence.childCount; i++)
        {
            var slot = PanelThatPlaysTheSequence.GetChild(i).GetComponent<Slot>();
            if (slot == null) continue;

            var brickGO = slot.brickPrefab;
            if (brickGO == null) continue;

            var piece = brickGO.GetComponent<BrickPiece>();
            if (piece == null) continue;

            queue.Enqueue(piece.action);
        }

        RefreshLabel();
    }

    // Finder panel-indekset for det N'te udfyldte slot (occupiedIndex).
    private int PanelIndexFromOccupiedIndex(int occupiedIndex)
    {
        if (occupiedIndex <= 0) return 0;
        int count = 0;
        for (int i = 0; i < PanelThatPlaysTheSequence.childCount; i++) // loop through all the slots empty/filled slot, from left to right
        {
            //Check if there is a slot valid and if it has a brick inside it
            // If yes -> Continue. If not (empty) -> skip one, and try next
            var s = PanelThatPlaysTheSequence.GetChild(i).GetComponent<Slot>();
            if (s != null && s.brickPrefab != null)
            {
                if (count == occupiedIndex) return i; // panel-index found and return the actual panel-position
                count++;
            }
        }
        return PanelThatPlaysTheSequence.childCount; // after last brick
    }

    private void Enqueue(ActionType a)
    {
        queue.Enqueue(a);
        RefreshLabel();
    }

    private IEnumerator Run()
    {
        isPlaying = true;

        // Snapshot and accumulate the queue for replay (only when not replaying)
        if (!isReplaying)
        {
            var snapshot = queue.ToArray();
            if (snapshot.Length > 0)
            {
                // Append to the end to preserve chronological order
                replayQueue.AddRange(snapshot);
            }
        }

        if (pausedAtIndex >= 0)
        {
            currentExecutingIndex = pausedAtIndex;
            pausedAtIndex = -1;
        }
        else
        {
            currentExecutingIndex = 0;
        }

        // Find all slots with bricks
        List<Slot> occupiedSlots = new List<Slot>();
        for (int i = 0; i < PanelThatPlaysTheSequence.childCount; i++)
        {
            var slot = PanelThatPlaysTheSequence.GetChild(i).GetComponent<Slot>();
            if (slot != null && slot.brickPrefab != null)
            {
                occupiedSlots.Add(slot);
            }
        }

        while (queue.Count > 0)
        {
            yield return new WaitUntil(() => !isPaused);

            // Highlight current slot
            if (currentExecutingIndex < occupiedSlots.Count)
            {
                HighlightSlot(occupiedSlots[currentExecutingIndex], true);
            }

            var a = queue.Dequeue();

            // Centralize action execution so we can apply temporary speed boost
            yield return ExecuteActionWithSpeedBoost(a);

            // Remove highlight from current slot
            if (currentExecutingIndex < occupiedSlots.Count)
            {
                HighlightSlot(occupiedSlots[currentExecutingIndex], false);
            }

            if (commandDelay > 0f)
                yield return new WaitForSeconds(commandDelay);

            currentExecutingIndex++;
            RefreshLabel(); // show remaining
        }

        isPlaying = false;
        isReplaying = false; // ensure we leave replay mode if it was set
        currentExecutingIndex = -1;
        pausedAtIndex = -1;

        // playback finished - resume pulse if any bricks remain
        UpdatePlayPulse();
    }
    
    private IEnumerator ExecuteActionWithSpeedBoost(ActionType a)
    {
        // If this is the speed brick, arm the boost for the next N moves (no move now)
        if (a == ActionType.SpeedBoost)
        {
            boostMovesLeft = speedBoostMoves;
            if (originalMoveDuration == null && player != null)
                originalMoveDuration = player.moveDuration;
            yield break;
        }

        // Only these actions are considered "moves" that consume the boost
        bool isMove =
            a == ActionType.MoveForward ||
            a == ActionType.TurnLeft    ||
            a == ActionType.TurnRight   ||
            a == ActionType.MoveBackward;

        // If boosted, use a shorter duration (faster move) for this action
        if (isMove && boostMovesLeft > 0 && originalMoveDuration.HasValue && player != null)
        {
            player.moveDuration = originalMoveDuration.Value / Mathf.Max(0.0001f, speedBoostMultiplier);
        }

        // Execute the action exactly like before
        switch (a)
        {
            case ActionType.None:
                // Do nothing
                break;

            case ActionType.MoveForward:
                player.MoveUp();
                break;

            case ActionType.TurnLeft:
                player.MoveLeft();
                break;

            case ActionType.TurnRight:
                player.MoveRight();
                break;
                        
            case ActionType.MoveBackward:
                player.MoveDown();
                break;
                    
            case ActionType.PlayHorn:
                player.PlayHorn();
                break;
                    
            case ActionType.DropOil:
                player.DropOil(oilPrefab, oilDropSound);
                break;
        }

        // Wait until the car finishes moving/rotating/acting
        yield return new WaitUntil(() => player.isIdle);

        // If this was a boosted move, decrement and restore when finished
        if (isMove && boostMovesLeft > 0 && originalMoveDuration.HasValue && player != null)
        {
            boostMovesLeft--;
            if (boostMovesLeft == 0)
            {
                // restore original movement speed after last boosted move
                player.moveDuration = originalMoveDuration.Value;
            }
        }
    }

    private void HighlightSlot(Slot slot, bool highlight)
    {
        if (slot == null || slot.brickPrefab == null) return;

        var brickTransform = slot.brickPrefab.transform;
        if (brickTransform != null)
            brickTransform.localScale = highlight ? Vector3.one * highlightScale : Vector3.one;

        slot.SetBackgroundActive(highlight);
    }

    private void RemoveAllHighlights()
    {
        if (PanelThatPlaysTheSequence == null) return;

        for (int i = 0; i < PanelThatPlaysTheSequence.childCount; i++)
        {
            var slot = PanelThatPlaysTheSequence.GetChild(i).GetComponent<Slot>();
            if (slot != null)
            {
                HighlightSlot(slot, false);
            }
        }
    }

    // If there is only one empty slot left in the play panel, create another slot
    private void EnsureSlots()
    {
        if (PanelThatPlaysTheSequence == null || slotPrefab == null) return;

        int emptyCount = 0;
        int filledCount = 0;
        
        for (int i = 0; i < PanelThatPlaysTheSequence.childCount; i++)
        {
            var slot = PanelThatPlaysTheSequence.GetChild(i).GetComponent<Slot>();
            if (slot == null) continue;
            
            if (slot.brickPrefab == null) 
                emptyCount++;
            else 
                filledCount++;
        }

        // Start pulsing if at least one brick is placed
        if (playButtonPulse != null)
        {
            if (filledCount > 0)
                playButtonPulse.StartPulsing();
            else
                playButtonPulse.StopPulsing();
        }

        // Update pulse state based on current filled count
        UpdatePlayPulse(filledCount);

        if (emptyCount <= 0)
        {
            var newSlot = Instantiate(slotPrefab, PanelThatPlaysTheSequence, false);
        }
    }

    public void ClearAll()
    {
        // stop any running playback
        if (isPlaying)
        {
            StopAllCoroutines();
            isPlaying = false;
        }
        isPaused = false;
        currentExecutingIndex = -1;
        pausedAtIndex = -1;

        // clear the logical queue & label
        queue.Clear();
        RefreshLabel();

        // Stop pulsing when cleared
        if (playButtonPulse != null)
            playButtonPulse.StopPulsing();

        // clear the placed bricks from the bottom panel
        ClearSlotsUI();

        // remove any dropped oil from the scene
        GameObject oilContainer = GameObject.Find("DroppedOils");
        if (oilContainer != null)
        {
            Destroy(oilContainer);
        }

        // >>> NEW: reset any pending speed boost and restore original speed
        boostMovesLeft = 0;
        if (originalMoveDuration.HasValue && player != null)
        {
            player.moveDuration = originalMoveDuration.Value;
        }
        originalMoveDuration = null;
    }

    public void ResetPlayerPosition()
    {
        // Don't allow reset during active playback
        if (isPlaying && !isPaused) return; 

        if (player != null)
        {
            // Stop any running playback
            if (isPlaying)
            {
                StopAllCoroutines();
                isPlaying = false;
            }

            // Reset pause and execution state
            isPaused = false;
            currentExecutingIndex = -1;
            pausedAtIndex = -1;
            pausedBrickGO = null;

            // Also reset the accumulated replay as requested
            replayQueue.Clear();

            // Unpause audio if it was paused
            AudioListener.pause = false;

            // Reset player position
            player.RespawnToCurrentStart();

            // Remove all visual highlights
            RemoveAllHighlights();

            // Rebuild the queue from the beginning
            RebuildQueueFromIndex(0);

            // remove any dropped oil from the scene
            GameObject oilContainer = GameObject.Find("DroppedOils");
            if (oilContainer != null)
            {
                Destroy(oilContainer);
            }

            // >>> NEW: reset any pending speed boost and restore original speed
            boostMovesLeft = 0;
            if (originalMoveDuration.HasValue)
            {
                player.moveDuration = originalMoveDuration.Value;
            }
            originalMoveDuration = null;
        }
    }

    // Remove any bricks from each slot in PanelThatPlaysTheSequence
    private void ClearSlotsUI()
    {
        if (PanelThatPlaysTheSequence == null) return;

        for (int i = 0; i < PanelThatPlaysTheSequence.childCount; i++)
        {
            var slot = PanelThatPlaysTheSequence.GetChild(i).GetComponent<Slot>();
            if (slot == null) continue;

            // if a brick is in this slot, destroy it and null the reference
            if (slot.brickPrefab != null)
            {
                Destroy(slot.brickPrefab);
                slot.brickPrefab = null;
            }

            // Turn off the background highlight
            slot.SetBackgroundActive(false);

            // Also hide the "Background (1)" child object if it exists
            Transform bgTransform = slot.transform.Find("Background (1)");
            if (bgTransform != null)
            {
                bgTransform.gameObject.SetActive(false);
            }

            // Remove any other GameObjects that have BrickPiece component (but keep slot UI elements)
            for (int c = slot.transform.childCount - 1; c >= 0; c--)
            {
                GameObject child = slot.transform.GetChild(c).gameObject;
                if (child.GetComponent<BrickPiece>() != null)
                {
                    Destroy(child);
                }
            }
        }
    }

    // Update the queue label text
    private void RefreshLabel()
    {
        if (queueLabel == null) return;

        var arr = queue.ToArray();
        if (arr.Length == 0)
        {
            queueLabel.text = "Queue: ";
            return;
        }

        var sb = new StringBuilder("Queue: ");
        for (int i = 0; i < arr.Length; i++)
        {
            sb.Append(arr[i] switch
            {
                ActionType.MoveForward => "F",
                ActionType.TurnLeft => "L",
                ActionType.TurnRight => "R",
                ActionType.MoveBackward => "B",
                _ => "?"
            });
            if (i < arr.Length - 1) sb.Append(" → ");
        }
        queueLabel.text = sb.ToString();
    }

    public void ShowWarning()
    {
        if (WarningUI != null)
        {
            WarningUI.SetActive(true);
        }
    }

    public void RemoveWarning()
    {
        if (WarningUI != null)
        {
            WarningUI.SetActive(false);
        }
    }
    
    private bool HasAnyFilledSlots()
    {
        if (PanelThatPlaysTheSequence == null) return false;
        for (int i = 0; i < PanelThatPlaysTheSequence.childCount; i++)
        {
            var slot = PanelThatPlaysTheSequence.GetChild(i).GetComponent<Slot>();
            if (slot != null && slot.brickPrefab != null) return true;
        }
        return false;
    }

    private void UpdatePlayPulse(int filledCount = -1)
    {
        if (playButtonPulse == null) return;

        bool anyFilled = filledCount >= 0 ? (filledCount > 0) : HasAnyFilledSlots();

        // Pulse only when there are bricks AND we are NOT currently playing.
        // This means: stop pulsing during execution, resume when finished or paused.
        if (anyFilled && !isPlaying)
            playButtonPulse.StartPulsing();
        else
            playButtonPulse.StopPulsing();
    }

    // --- Optional public helpers for replay control ---

    // Manually clear the accumulated replay (if needed elsewhere)
    public void ClearReplayQueue()
    {
        replayQueue.Clear();
    }

    // Play the accumulated replay from the beginning
    public void PlayReplay()
    {
        if (isPlaying) return;
        if (replayQueue.Count == 0) return;
        // Reset player position
        player.RespawnToCurrentStart();
        ResetTimer();

        // Load the accumulated commands into the execution queue
        queue.Clear();
        for (int i = 0; i < replayQueue.Count; i++)
            queue.Enqueue(replayQueue[i]);

        RemoveAllHighlights();
        pausedAtIndex = -1;
        currentExecutingIndex = 0;
        isReplaying = true;

        StartCoroutine(Run());
        if (timer != null) timer.StartTimer();
    }

    public void ResetTimer()
    {
        if (timer != null)
        {
            timer.ResetTimer();
        }
    }

}
