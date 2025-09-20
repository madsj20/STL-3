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


    public void OnEnter(RoadPiece piece, Collider2D other)
    {
        switch (type)
        {

            case RoadPieceType.Goal:
                Debug.Log("GOAL!");
                
                break;
            

            case RoadPieceType.Start:
            {
                var player = other.GetComponent<PlayerController>();
                if (player != null)
                    player.SetSpawnWorld(piece.transform.position, Vector2Int.RoundToInt(startDirection)); // exact prefab position
                break;
            }
        }   
    }
}
