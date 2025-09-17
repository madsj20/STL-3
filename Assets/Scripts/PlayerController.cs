using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public Vector2Int gridPosition; //Current grid position
    private Vector2Int startGridPosition;
    public float moveDuration = 1f; //Car speed from one tile to another

    private bool isMoving = false;
    private GridManager gridManager;
    public Vector2Int faceDirection = Vector2Int.up; //The car facing direction
    public bool isIdle => !isMoving;
    private bool isRotating = false;
    public float rotateDuration = 0.25f; // how long a 90° turn takes

    void Start()
    {
        gridManager = FindAnyObjectByType<GridManager>();
        transform.position = new Vector3(gridPosition.x, gridPosition.y, 0);
        transform.up = new Vector3(faceDirection.x, faceDirection.y, 0); // Set initial facing direction
        startGridPosition = gridPosition;
    }

    private bool TryMove(Vector2Int delta)
    {
        if (isMoving || isRotating) return false;

        Vector2Int newPos = gridPosition + delta;
        Tile targetTile = gridManager.GetTileAtPosition(newPos);
        if (targetTile == null) return false;

        StartCoroutine(MoveTo(newPos)); // Start the movement coroutine
        return true;
    }
    
    public void MoveForward()
    {
        TryMove(faceDirection); // Move one step in the current direction
    }

    public void TurnLeft()
    {
        // 90° Left: (x,y) -> (-y, x)
        Vector2Int left = new Vector2Int(-faceDirection.y, faceDirection.x);  // 90° ccw
        TryMove(left); // Move when turning (Can be removed if we want to turn in place)
        StartCoroutine(RotateTo(left));
    }
    public void TurnRight()
    {
        // 90° Right: (x,y) -> (y, -x)
        Vector2Int right = new Vector2Int(faceDirection.y, -faceDirection.x); // 90° cw
        TryMove(right); // Move when turning (Can be removed if we want to turn in place)
        StartCoroutine(RotateTo(right));
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

    public void ResetPosition()
    {
        transform.position = new Vector3(startGridPosition.x, startGridPosition.y, 0);
        faceDirection = Vector2Int.up;
        transform.up = new Vector3(faceDirection.x, faceDirection.y, 0); // Reset facing direction
    }

}
