using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] public int width, height;
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private Transform cam;
    [SerializeField] private Transform grid;
    [SerializeField] private Transform map;


    private Dictionary<Vector2, Tile> tiles; // to store tiles for future use

    private void Start()
    {
        GenerateGrid();
    }
    void GenerateGrid()
    {
        tiles = new Dictionary<Vector2, Tile>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var spawnedTile = Instantiate(tilePrefab, new Vector3(x, y), Quaternion.identity);  
                spawnedTile.name = $"Tile {x} {y}";

                var isOffset = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0); // check for checkerboard pattern
                spawnedTile.Init(isOffset); // initialize tile color

                tiles[new Vector2(x, y)] = spawnedTile; // store tile in dictionary
            }
        }

        cam.transform.position = new Vector3((float)width / 2 - 0.5f, (float)height / 2 - 0.5f, -10);
        grid.transform.position = new Vector3((float)width / 2 - 0.5f, (float)height / 2, 0);
        map.transform.position = new Vector3((float)width / 2 - 0.5f, (float)height / 2 - 0.5f, 0);
    }

    public Tile GetTileAtPosition(Vector2 pos)
    {
        if (tiles.TryGetValue(pos, out Tile tile))
        {
            return tile;
        }
        return null;
    }

    public Vector3 GetGridOrigin()
    {
        float cameraDistance = Mathf.Abs(Camera.main.transform.position.z);
        return Camera.main.ViewportToWorldPoint(new Vector3(0, 0, cameraDistance));
    }
    public Vector2 GetTileSize()
    {
        return tilePrefab.GetComponent<SpriteRenderer>().bounds.size;
    }

}
