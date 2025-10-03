using UnityEngine;
using UnityEngine.UI;

public enum RoadPieceType
{
    Goal,
    Start,
    Pit,
}

[CreateAssetMenu(fileName = "RoadPieceData", menuName = "Scriptable Objects/RoadPieceData")]
public class RoadPieceData : ScriptableObject
{
    public RoadPieceType type;
    public Sprite sprite;


    public Vector2 startDirection = Vector2.left; // car starts facing this way
    
    [Header("Pit Settings")]
    public AudioClip pitAudio;
    public float pitDelay = 3f; // seconds to wait in pit

    public void OnEnter(RoadPiece piece, Collider2D other)
    {
        switch (type)
        {

            case RoadPieceType.Goal:
                Debug.Log("GOAL!");
                if (UIManager.Instance != null && UIManager.Instance.WinningUI != null)
                    UIManager.Instance.WinningUI.SetActive(true);
                break;


            case RoadPieceType.Start:
                {
                    //var player = other.GetComponent<PlayerController>();
                    //if (player != null)
                        //player.SetSpawnWorld(piece.transform.position, Vector2Int.RoundToInt(startDirection)); // exact prefab position
                    break;
                }
            case RoadPieceType.Pit:
                {
                    var player = other.GetComponent<PlayerController>();
                    if (player == null) break;

                    if (pitAudio != null)
                        AudioSource.PlayClipAtPoint(pitAudio, piece.transform.position);

                    player.Hold(pitDelay);
                    break;
                }
            
        }
    }
}
