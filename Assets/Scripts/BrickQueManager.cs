using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BrickQueManager : MonoBehaviour
{
    // Define the possible actions the player can take
    public enum ActionType { MoveForward, TurnLeft, TurnRight, MoveBackward, MoveUp, MoveDown, MoveRight, MoveLeft, None }

    public PlayerController player;
    public TMP_Text queueLabel;    // shows the queued actions
    public float commandDelay = 0.05f; // small gap between commands

    public Transform PanelThatPlaysTheSequence; //The panel that holds the inventory slots
    public Slot slotPrefab; // prefab used to create additional slots when needed

    // Highlight settings
    public float highlightScale = 1.1f; // Slight scale increase

    private readonly Queue<ActionType> queue = new Queue<ActionType>();
    private bool isPlaying = false;
    private bool isPaused = false;
    private int currentExecutingIndex = -1; // Track which slot is currently executing
    private int pausedAtIndex = -1; // Remember where we paused

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
        if (isPaused)
        {
            isPaused = false;
            AudioListener.pause = false; // Resume all audio
            
            // Rebuild queue from where we left off
            RebuildQueueFromIndex(pausedAtIndex);
            
            if (!isPlaying)
                StartCoroutine(Run());
            return;
        }

        if (!isPlaying)
            StartCoroutine(Run());
    }

    public void Pause()
    {
        if (!isPlaying) return;
        
        isPaused = true;
        pausedAtIndex = currentExecutingIndex; // Remember current position
        
        StopAllCoroutines();
        isPlaying = false;

        AudioListener.pause = true; // Pause all audio
        
        // Keep the highlight visible during pause - don't remove it!
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

    private void Enqueue(ActionType a)
    {
        queue.Enqueue(a);
        RefreshLabel();
    }

    private IEnumerator Run()
    {
        isPlaying = true;
        
        // If resuming from pause, continue from where we left off
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
            }

            // Wait until the car finishes moving/rotating
            yield return new WaitUntil(() => player.isIdle);

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
        currentExecutingIndex = -1;
        pausedAtIndex = -1;
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

    // Helper class to remember original colors
    private class ColorMemory : MonoBehaviour
    {
        public Color originalColor;
    }

    // If there is only one empty slot left in the play panel, create another slot
    private void EnsureSlots()
    {
        if (PanelThatPlaysTheSequence == null || slotPrefab == null) return;

        int emptyCount = 0;
        for (int i = 0; i < PanelThatPlaysTheSequence.childCount; i++)
        {
            var slot = PanelThatPlaysTheSequence.GetChild(i).GetComponent<Slot>();
            if (slot == null) continue;
            if (slot.brickPrefab == null) emptyCount++;
        }

        if (emptyCount <= 0)
        {
            // Instantiate one new slot at the end
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

        // clear the placed bricks from the bottom panel
        ClearSlotsUI();
    }

    public void ResetPlayerPosition()
    {
        if (player != null)
        {
            player.RespawnToCurrentStart();
            RemoveAllHighlights();
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

            // safety: if anything else was parented under the slot, remove it too
            for (int c = slot.transform.childCount - 1; c >= 0; c--)
                Destroy(slot.transform.GetChild(c).gameObject);
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
}