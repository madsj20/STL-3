using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class BrickQueManager : MonoBehaviour
{
    public enum ActionType { MoveForward, TurnLeft, TurnRight }

    public PlayerController player;
    public TMP_Text queueLabel;    // shows the queued actions
    public float commandDelay = 0.05f; // small gap between commands

    public Transform PanelThatPlaysTheSequence; //The panel that holds the inventory slots

    private readonly Queue<ActionType> queue = new Queue<ActionType>();
    private bool isPlaying = false;

    // --- Hook these from UI Buttons ---
    public void Move()  => Enqueue(ActionType.MoveForward);
    public void Left()  => Enqueue(ActionType.TurnLeft);
    public void Right() => Enqueue(ActionType.TurnRight);
    public void ClearQueue()
    {
        queue.Clear();
        RefreshLabel();
    }
    //play the queued commands
    public void Play()
    {
        if (!isPlaying)
            StartCoroutine(Run());
    }

    public void PlayFromPanel()
    {
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
            var a = queue.Dequeue();

            switch (a)
            {
                case ActionType.MoveForward: player.MoveForward(); break;
                case ActionType.TurnLeft:    player.TurnLeft();    break;
                case ActionType.TurnRight:   player.TurnRight();   break;
            }

            // Wait until the car finishes moving/rotating
            yield return new WaitUntil(() => player.isIdle);

            if (commandDelay > 0f)
                yield return new WaitForSeconds(commandDelay);

            RefreshLabel(); // show remaining
        }
        isPlaying = false;
    }

    public void ClearAll()
    {
        // stop any running playback
        if (isPlaying)
        {
            StopAllCoroutines();
            isPlaying = false;
        }

        // clear the logical queue & label
        queue.Clear();
        RefreshLabel();

        // clear the placed bricks from the bottom panel
        ClearSlotsUI();

        // reset the player position
        player.ResetPosition();
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
                _ => "?"
            });
            if (i < arr.Length - 1) sb.Append(" → ");
        }
        queueLabel.text = sb.ToString();
    }
}
