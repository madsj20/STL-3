using UnityEngine;

public class RoadPiece : MonoBehaviour
{
    public RoadPieceData data;

    void Start()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    if (data != null)
        Debug.Log($"RoadPiece '{name}' is using data '{data.name}' of type {data.type}");

    if (data != null && data.type == RoadPieceType.Start)
        StartCoroutine(SetSpawnNextFrame());
    }

    private System.Collections.IEnumerator SetSpawnNextFrame()
    {
        yield return null;
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetSpawnWorld(transform.position, Vector2Int.RoundToInt(data.startDirection));
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (data != null)
            data.OnEnter(this, other);
    }
}
