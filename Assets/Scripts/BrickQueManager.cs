using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class BrickQueManager : MonoBehaviour
{
    // Define the possible actions the player can take
    public enum ActionType { MoveForward, TurnLeft, TurnRight, MoveBackward, MoveUp, MoveDown, MoveRight, MoveLeft, None }

    public PlayerController player;
    public TMP_Text queueLabel;    // shows the queued actions
    public float commandDelay = 0.05f; // small gap between commands

    public Transform PanelThatPlaysTheSequence; //The panel that holds the inventory slots
    public Slot slotPrefab; // prefab used to create additional slots when needed

    private readonly Queue<ActionType> queue = new Queue<ActionType>();
    private bool isPlaying = false;
    private bool isPaused = false;

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
            PlayFromPanel(); // Plays from the rebuilt queue
            return; // Run() will continue from where it left off
        }

        if (!isPlaying)
            StartCoroutine(Run());
    }

    public void Pause()
    {
        isPaused = true;
        if (isPlaying)
        {
            StopAllCoroutines();
            isPlaying = false;
        }

        AudioListener.pause = true; // Pause all audio
    }

    public void PlayFromPanel()
    {

        if (isPaused)
        {
            isPaused = false;
            AudioListener.pause = false; // mute all audio
            if (!isPlaying)
                StartCoroutine(Run());
                
        }

        if (isPlaying) return;

        // remove any previous commands
        queue.Clear();

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

    private void Enqueue(ActionType a)
    {
        queue.Enqueue(a);
        RefreshLabel();
    }

    private IEnumerator Run()
    {
        isPlaying = true;
        while (queue.Count > 0)
        {

            yield return new WaitUntil(() => !isPaused);

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

            if (commandDelay > 0f)
                yield return new WaitForSeconds(commandDelay);

            RefreshLabel(); // show remaining
        }
        isPlaying = false;
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
        // clear the logical queue & label
        queue.Clear();
        RefreshLabel();

        // clear the placed bricks from the bottom panel
        ClearSlotsUI();
    }

    public void ResetPlayerPosition()
    {
        if (player != null)
            player.RespawnToCurrentStart();
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
