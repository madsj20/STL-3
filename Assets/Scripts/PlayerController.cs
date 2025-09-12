using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public Vector2Int gridPosition; //Current grid position
    public float moveDuration = 0.5f; //Car speed

    private bool isMoving = false;
    private GridManager gridManager;
    public Vector2Int faceDirection = Vector2Int.up; //The car facing direction
    public bool isIdle => !isMoving;

    void Start()
    {
        gridManager = FindAnyObjectByType<GridManager>();
        transform.position = new Vector3(gridPosition.x, gridPosition.y, 0);

    }

    private bool TryMove(Vector2Int delta)
    {
        if (isMoving) return false;

        Vector2Int newPos = gridPosition + delta;
        Tile targetTile = gridManager.GetTileAtPosition(newPos);
        if (targetTile == null) return false;

        StartCoroutine(MoveTo(newPos));
        return true;
    }

    public void MoveForward()
    {
        TryMove(faceDirection); // Move one step in the current direction
    }

    public void TurnLeft()
    {
        // 90° venstre: (x,y) -> (-y, x)
        Vector2Int left = new Vector2Int(-faceDirection.y, faceDirection.x);  // 90° ccw
        TryMove(left);

    }
    public void TurnRight()
    {
        // 90° højre: (x,y) -> (y, -x)
        Vector2Int right = new Vector2Int(faceDirection.y, -faceDirection.x); // 90° cw
        TryMove(right);
        
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

}
