using UnityEngine;

public class RoadPiece : MonoBehaviour
{
    public RoadPieceData data;

    void Start()
    {
        // ensure trigger collider for “enter”
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (data != null)
            data.OnEnter(this, other);
    }
}
