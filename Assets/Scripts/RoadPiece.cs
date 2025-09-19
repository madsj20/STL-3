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
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        if (data != null)
            data.OnEnter(this, other);
    }
}
