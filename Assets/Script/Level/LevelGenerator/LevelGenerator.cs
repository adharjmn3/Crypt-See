using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelGenerator : MonoBehaviour
{
    public GameObject roomPrefab; // Assign a RoomLayout prefab in the Unity Inspector
    public Tilemap wallTilemap; // Tilemap for the walls
    public TileBase wallTile; // Tile to use for the walls
    public int roomSize = 20; // Size of each room (20x20 tiles)
    public int gridSize = 3; // 3x3 grid
    public int roomSpacing = 1; // Space between rooms in tiles

    void Start()
    {
        GenerateLevel();
    }

    void GenerateLevel()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                // Calculate the position of the room with spacing
                Vector3 roomPosition = new Vector3(
                    x * (roomSize + roomSpacing), 
                    y * (roomSize + roomSpacing), 
                    0
                );

                // Instantiate the RoomLayout prefab
                GameObject room = Instantiate(roomPrefab, roomPosition, Quaternion.identity, transform);

                // Call the GenerateWalls method in RoomLayout
                RoomLayout roomLayout = room.GetComponent<RoomLayout>();
                if (roomLayout != null)
                {
                    roomLayout.GenerateWalls();
                }

                // Generate walls in the gap (roomSpacing) between rooms
                GenerateGapWalls(x, y, roomPosition);
            }
        }

        // Generate the outer boundary around all rooms
        GenerateOuterBoundary();
    }

    void GenerateGapWalls(int x, int y, Vector3 roomPosition)
    {
        // Generate vertical walls in the gap between rooms
        if (x < gridSize - 1) // If not the last column
        {
            Vector3Int verticalWallPosition = new Vector3Int(
                Mathf.RoundToInt(roomPosition.x + roomSize + roomSpacing / 2f),
                Mathf.RoundToInt(roomPosition.y + roomSize / 2f),
                0
            );
            wallTilemap.SetTile(verticalWallPosition, wallTile);
        }

        // Generate horizontal walls in the gap between rooms
        if (y < gridSize - 1) // If not the last row
        {
            Vector3Int horizontalWallPosition = new Vector3Int(
                Mathf.RoundToInt(roomPosition.x + roomSize / 2f),
                Mathf.RoundToInt(roomPosition.y + roomSize + roomSpacing / 2f),
                0
            );
            wallTilemap.SetTile(horizontalWallPosition, wallTile);
        }

        // Generate corner walls in the gap between rooms
        if (x < gridSize - 1 && y < gridSize - 1) // If not the last column or row
        {
            Vector3Int cornerWallPosition = new Vector3Int(
                Mathf.RoundToInt(roomPosition.x + roomSize + roomSpacing / 2f),
                Mathf.RoundToInt(roomPosition.y + roomSize + roomSpacing / 2f),
                0
            );
            wallTilemap.SetTile(cornerWallPosition, wallTile);
        }
    }

    void GenerateOuterBoundary()
    {
        // Calculate the total size of the grid including spacing
        int totalWidth = gridSize * (roomSize + roomSpacing);
        int totalHeight = gridSize * (roomSize + roomSpacing);

        // Generate top and bottom boundary walls
        for (int x = -1; x <= totalWidth; x++)
        {
            // Top boundary
            wallTilemap.SetTile(new Vector3Int(x, totalHeight, 0), wallTile);

            // Bottom boundary
            wallTilemap.SetTile(new Vector3Int(x, -1, 0), wallTile);
        }

        // Generate left and right boundary walls
        for (int y = -1; y <= totalHeight; y++)
        {
            // Left boundary
            wallTilemap.SetTile(new Vector3Int(-1, y, 0), wallTile);

            // Right boundary
            wallTilemap.SetTile(new Vector3Int(totalWidth, y, 0), wallTile);
        }
    }
}
