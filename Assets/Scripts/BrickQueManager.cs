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
    public PulseEffect recordingPulse;

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
    [SerializeField] private int speedBoostMoves = 2;   // affect next two moves
    [SerializeField] private Color speedBorderColor = new Color(1f, 0.84f, 0f, 1f); // Gold/yellow color

    private int boostMovesLeft = 0;
    private float? originalMoveDuration = null;

    public GameObject WarningUI;
    public GameObject WinningUI;
    public GameObject ReplayBorder;
    public GameObject TimerObject;
    public GameObject RecordCamera;
    private GameObject ReplayBlockers;
    public GameObject PlayerParticles;

    // --- Replay support ---
    private readonly List<ActionType> replayQueue = new List<ActionType>();
    private bool isReplaying = false;

    // Track if the goal was crossed while we are replaying
    private bool goalCrossedDuringReplay = false;
    public bool IsReplaying => isReplaying;

    [Header("Slots")]
    [SerializeField] private int defaultEmptySlotCount = 5;

    [Header("Juice: Squash & Stretch (applies while queue executes)")]
    [Tooltip("Enable simple squash & stretch on the player while the queue is executing.")]
    [SerializeField] private bool enableSquashStretch = true;
    [Tooltip("Peak stretch multiplier along the forward axis (local Y) when playing.")]
    [SerializeField] private float stretchAmount = 1.12f;
    [Tooltip("Squash multiplier along forward axis (local Y) when not playing.")]
    [SerializeField] private float squashAmount = 0.88f;
    [Tooltip("How fast the player's scale lerps toward the target.")]
    [SerializeField] private float scaleLerpSpeed = 8f;

    // runtime state for scale handling
    private Vector3 playerOriginalScale = Vector3.one;
    private bool originalScaleCaptured = false;
    private Coroutine scaleRoutine = null;

    private void Awake()
    {
        if (!timer) timer = FindFirstObjectByType<RaceTimer>();

        // capture player's original scale if already assigned
        if (player != null && player.transform != null)
        {
            playerOriginalScale = player.transform.localScale;
            originalScaleCaptured = true;
        }
    }

    private void Start()
    {
        //Find ReplayBloockers
        ReplayBlockers = GameObject.Find("ReplayBlockers");
        ReplayBlockers.SetActive(false);
        // Make sure all slot backgrounds are OFF at scene start
        if (PanelThatPlaysTheSequence == null) return;

        for (int i = 0; i < PanelThatPlaysTheSequence.childCount; i++)
        {
            var slot = PanelThatPlaysTheSequence.GetChild(i).GetComponent<Slot>();
            if (slot != null)
            {
                slot.SetBackgroundActive(false);
                slot.SetSpeedBorderActive(false);
            }
        }
    }

    private void Update()
    {
        // Ensure there's always more than one empty slot available
        EnsureSlots();

        // Update speed boost borders whenever slots change
        UpdateSpeedBoostBorders();

        // Update replay border visibility
        bool playerHasWon = false;
        if (player != null)
        {
            player.GetCurrentWinState(out playerHasWon);
        }
        if (IsReplaying && !playerHasWon)
        {
            ReplayBorder.SetActive(true);
        }
        else
        {
            ReplayBorder.SetActive(false);
        }

        // Keep recording pulse in sync with execution state
        UpdateRecordingPulse();

        if (isReplaying && TimerObject != null)
        {
            TimerObject.SetActive(true);
            RecordCamera.SetActive(false);
            ReplayBlockers.SetActive(true);
        }
        else if (TimerObject != null)
        {
            TimerObject.SetActive(false);
            RecordCamera.SetActive(true);
            ReplayBlockers.SetActive(false);
        }
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

        // When execution stops, switch to squash state
        StartScaleMode(false);

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

        // when queue execution starts, start stretch state
        StartScaleMode(true);

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

        // Capture if we were replaying on this run
        bool wasReplaying = isReplaying;

        isPlaying = false;
        isReplaying = false; // ensure we leave replay mode if it was set
        currentExecutingIndex = -1;
        pausedAtIndex = -1;

        // when execution stopped, switch to squash state
        StartScaleMode(false);

        // If we crossed goal while replaying AND the replayed queue has finished, show winning UI now
        if (wasReplaying && goalCrossedDuringReplay && WinningUI != null)
        {
            WinningUI.SetActive(true);
            commandDelay = 0.05f; // small gap between commands
        }
        // reset the flag after handling
        goalCrossedDuringReplay = false;

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
            a == ActionType.TurnLeft ||
            a == ActionType.TurnRight ||
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

    // Start the scale routine toward stretch (playing=true) or squash (playing=false)
    private void StartScaleMode(bool playing)
    {
        if (!enableSquashStretch || player == null || player.transform == null) return;

        // capture original scale if not yet done
        if (!originalScaleCaptured)
        {
            playerOriginalScale = player.transform.localScale;
            originalScaleCaptured = true;
        }

        // stop any previous scale coroutine
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        Vector3 target;
        if (playing)
        {
            // stretch along local Y (forward). Compensate X so volume feels consistent.
            float stretch = Mathf.Max(0.0001f, stretchAmount);
            float xComp = 1f - (stretch - 1f) * 0.55f;
            target = new Vector3(playerOriginalScale.x * xComp, playerOriginalScale.y * stretch, playerOriginalScale.z);
            //Enable player particles when moving
            PlayerParticles.SetActive(true);
        }
        else
        {
            // squash along local Y and compensate X
            float squash = Mathf.Clamp(squashAmount, 0.01f, 2f);
            float xComp = 1f + (1f - squash) * 0.6f;
            target = new Vector3(playerOriginalScale.x * xComp, playerOriginalScale.y * squash, playerOriginalScale.z);
            //Disable player particles when not moving
            PlayerParticles.SetActive(false);
        }

        scaleRoutine = StartCoroutine(LerpPlayerScaleTo(target));
    }

    private IEnumerator LerpPlayerScaleTo(Vector3 target)
    {
        if (player == null || player.transform == null) yield break;
        Transform t = player.transform;
        while (Vector3.Distance(t.localScale, target) > 0.001f)
        {
            t.localScale = Vector3.Lerp(t.localScale, target, Time.deltaTime * scaleLerpSpeed);
            yield return null;
        }
        t.localScale = target;
        scaleRoutine = null;
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

    // Update speed boost border indicators for slots after speed bricks
    private void UpdateSpeedBoostBorders()
    {
        if (PanelThatPlaysTheSequence == null) return;

        // First, clear all speed borders
        for (int i = 0; i < PanelThatPlaysTheSequence.childCount; i++)
        {
            var slot = PanelThatPlaysTheSequence.GetChild(i).GetComponent<Slot>();
            if (slot != null)
            {
                slot.SetSpeedBorderActive(false);
            }
        }

        // Go through all slots from left to right
        for (int i = 0; i < PanelThatPlaysTheSequence.childCount; i++)
        {
            var slot = PanelThatPlaysTheSequence.GetChild(i).GetComponent<Slot>();
            if (slot == null || slot.brickPrefab == null) continue;

            var piece = slot.brickPrefab.GetComponent<BrickPiece>();
            if (piece == null) continue;

            // If this is a speed boost brick, highlight the next N slots
            if (piece.action == ActionType.SpeedBoost)
            {
                int slotsHighlighted = 0;
                
                // Look at the next slots (both empty and filled)
                for (int nextIdx = i + 1; 
                     nextIdx < PanelThatPlaysTheSequence.childCount && slotsHighlighted < speedBoostMoves; 
                     nextIdx++)
                {
                    var nextSlot = PanelThatPlaysTheSequence.GetChild(nextIdx).GetComponent<Slot>();
                    if (nextSlot == null) continue;

                    // Highlight this slot (whether empty or filled)
                    nextSlot.SetSpeedBorderActive(true);
                    nextSlot.SetSpeedBorderColor(speedBorderColor);
                    slotsHighlighted++;
                }
            }
        }
    }

    // Public method that can be called from other scripts when bricks are added/removed
    public void RefreshSpeedBoostBorders()
    {
        UpdateSpeedBoostBorders();
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

        // Reset slots to a fixed amount of empty slots
        ResetPlayPanelToEmptySlots(defaultEmptySlotCount);

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

        // >>> NEW: reset goal flag
        goalCrossedDuringReplay = false;

        // ensure pulses reflect the now-empty state
        UpdatePlayPulse(0);

        // apply squash since execution ended / cleared
        StartScaleMode(false);
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

            // >>> NEW: reset goal flag
            goalCrossedDuringReplay = false;

            // ensure player shows squash (not stretched)
            StartScaleMode(false);
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
            slot.SetSpeedBorderActive(false);

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

    // Create exactly N empty slots in the play panel (deletes existing Slot children)
    private void ResetPlayPanelToEmptySlots(int desiredEmpty)
    {
        if (PanelThatPlaysTheSequence == null || slotPrefab == null) return;

        // Delete existing Slot children only
        for (int i = PanelThatPlaysTheSequence.childCount - 1; i >= 0; i--)
        {
            var child = PanelThatPlaysTheSequence.GetChild(i);
            if (child.GetComponent<Slot>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        // Recreate desired amount of empty slots
        int count = Mathf.Max(0, desiredEmpty);
        for (int i = 0; i < count; i++)
        {
            Instantiate(slotPrefab, PanelThatPlaysTheSequence, false);
        }

        // Ensure layout updates so the UI reflects the new slots immediately
        var rt = PanelThatPlaysTheSequence.GetComponent<RectTransform>();
        if (rt != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
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
            Debug.Log("Removing Warning UI");
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

    // Keep the recording indicator in sync: pulse while playing and there are pending commands
    private void UpdateRecordingPulse()
    {
        if (recordingPulse == null) return;

        bool shouldPulse = isPlaying && !isPaused && queue.Count > 0 && !isReplaying;
        if (shouldPulse)
            recordingPulse.StartPulsing();
        else
            recordingPulse.StopPulsing();
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

        //remove Winning UI if active
        if (WinningUI != null && WinningUI.activeSelf)
        {
            WinningUI.SetActive(false);
        }

        player.ResetWinState(); // timer can run again when replay starts

        commandDelay = 0f; // no delay during replay

        //Remove all oil stains before replay
        for (int i = 0; i < GameObject.FindGameObjectsWithTag("Oil").Length; i++)
        {
            Destroy(GameObject.FindGameObjectsWithTag("Oil")[i]);
        }
        // Reset player position
        player.RespawnToCurrentStart();
        ResetTimer();

        Debug.Log($"PlayReplay: replayQueue.Count={replayQueue.Count} items=[{string.Join(",", replayQueue)}]");

        // --- Build the visual slots from replayQueue ---
        // Clear existing bricks and ensure enough slots
        ClearSlotsUI();

        if (PanelThatPlaysTheSequence != null && slotPrefab != null)
        {
            // Ensure there are at least as many slots as replay items
            int neededSlots = replayQueue.Count;
            for (int i = PanelThatPlaysTheSequence.childCount; i < neededSlots; i++)
            {
                Instantiate(slotPrefab, PanelThatPlaysTheSequence, false);
            }

            // Try to get the inventory prefabs so we can instantiate matching brick visuals
            var inventory = FindFirstObjectByType<InventoryController>();
            GameObject[] brickPrefabs = inventory != null ? inventory.brickPrefabs : null;

            int replayIndex = 0;
            for (int i = 0; i < PanelThatPlaysTheSequence.childCount && replayIndex < replayQueue.Count; i++)
            {
                var slot = PanelThatPlaysTheSequence.GetChild(i).GetComponent<Slot>();
                if (slot == null) continue;

                // only fill empty slots (preserve existing if any)
                if (slot.brickPrefab != null) continue;

                ActionType action = replayQueue[replayIndex];

                GameObject prefabToUse = null;
                if (brickPrefabs != null)
                {
                    for (int p = 0; p < brickPrefabs.Length; p++)
                    {
                        var pf = brickPrefabs[p];
                        if (pf == null) continue;
                        var bp = pf.GetComponent<BrickPiece>();
                        if (bp != null && bp.action == action)
                        {
                            prefabToUse = pf;
                            break;
                        }
                    }
                }

                GameObject instance;
                if (prefabToUse != null)
                {
                    instance = Instantiate(prefabToUse, slot.transform, false);
                }
                else
                {
                    // Fallback: create a simple placeholder GameObject that has BrickPiece with the right action
                    instance = new GameObject("ReplayBrick_" + action.ToString());
                    instance.transform.SetParent(slot.transform, false);
                    var bp = instance.AddComponent<BrickPiece>();
                    bp.action = action;
                    // optional: add a minimal RectTransform so layout won't break
                    var rt = instance.AddComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.localScale = Vector3.one;
                }

                // Normalize RectTransform/visuals so the brick looks correct inside slot
                var instanceRT = instance.GetComponent<RectTransform>();
                if (instanceRT != null)
                {
                    instanceRT.anchorMin = instanceRT.anchorMax = new Vector2(0.5f, 0.5f);
                    instanceRT.pivot = new Vector2(0.5f, 0.5f);
                    instanceRT.anchoredPosition = Vector2.zero;
                    instanceRT.localRotation = Quaternion.identity;
                    instanceRT.localScale = Vector3.one;
                }

                slot.brickPrefab = instance;
                slot.SetBackgroundActive(false);

                replayIndex++;
            }

            // Force layout rebuild so UI reflects new slots/bricks immediately
            var rtPanel = PanelThatPlaysTheSequence.GetComponent<RectTransform>();
            if (rtPanel != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rtPanel);
        }

        // Load the accumulated commands into the execution queue (logical)
        queue.Clear();
        for (int i = 0; i < replayQueue.Count; i++)
        {
            Enqueue(replayQueue[i]); // uses RefreshLabel
            Debug.Log($"  Enqueued replay action: {replayQueue[i]}");
        }
        Debug.Log("Queue:" + string.Join(", ", queue));

        RemoveAllHighlights();
        pausedAtIndex = -1;
        currentExecutingIndex = 0;
        isReplaying = true;

        // reset goal flag at the start of replay
        goalCrossedDuringReplay = false;

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

    // Call this from the script that detects the goal line (e.g., PlayerController or Finish handler)
    public void NotifyGoalCrossed()
    {
        if (isReplaying)
        {
            goalCrossedDuringReplay = true;
        }
    }
}