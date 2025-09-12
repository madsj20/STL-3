using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class BrickQueManager : MonoBehaviour
{
    public enum ActionType { MoveForward, TurnLeft, TurnRight }

    [Header("References")]
    public PlayerController player;
    public TMP_Text queueLabel;    // shows the queued actions

    [Header("Timing")]
    public float commandDelay = 0.05f; // small gap between commands

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
    public void Play()
    {
        if (!isPlaying)
            StartCoroutine(Run());
    }
    // ----------------------------------

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

    private void RefreshLabel()
    {
        if (queueLabel == null) return;

        var arr = queue.ToArray();
        if (arr.Length == 0)
        {
            queueLabel.text = "Queue: —";
            return;
        }

        var sb = new StringBuilder("Queue: ");
        for (int i = 0; i < arr.Length; i++)
        {
            sb.Append(arr[i] switch
            {
                ActionType.MoveForward => "F",
                ActionType.TurnLeft    => "L",
                ActionType.TurnRight   => "R",
                _ => "?"
            });
            if (i < arr.Length - 1) sb.Append(" → ");
        }
        queueLabel.text = sb.ToString();
    }
}
