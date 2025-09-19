using UnityEngine;

public enum RoadPieceType
{
    Goal,
    Start,
}

[CreateAssetMenu(fileName = "RoadPieceData", menuName = "Scriptable Objects/RoadPieceData")]
public class RoadPieceData : ScriptableObject
{
    public RoadPieceType type;
    public Sprite sprite;

    public Vector2 startDirection = Vector2.left; // car starts facing this way


    // Called when something (e.g. the car) enters this piece
    public void OnEnter(RoadPiece piece, Collider2D other)
    {
        switch (type)
        {

            case RoadPieceType.Goal:
                Debug.Log("GOAL reached on " + piece.name);
                // put your win code here (UI, stop timer, etc.)
                break;
            case RoadPieceType.Start:
                var player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.gridPosition = Vector2Int.RoundToInt(piece.transform.position);
                    player.faceDirection = Vector2Int.RoundToInt(startDirection);
                    player.transform.up = new Vector3(player.faceDirection.x, player.faceDirection.y, 0);
                    Debug.Log("Car reset to start position " + player.gridPosition + " facing " + player.faceDirection);
                }
                break;

        }
    }
}
