using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomLayout : MonoBehaviour
{
    public Transform middle; // Reference to the "Middle" GameObject in the room layout
    public Tilemap wallTilemap; // Tilemap for the walls
    public Tilemap minimapTilemap; // Tilemap for the minimap
    public TileBase wallTile; // Tile to use for the walls
    public TileBase emptyTile; // Tile to represent the door (or leave empty)
    public TileBase minimapWallTile; // Tile to represent the walls on the minimap
    public int roomWidth = 20; // Width of the room in tiles
    public int roomHeight = 20; // Height of the room in tiles
    public int doorSize = 3; // Size of the door in tiles

    // Neighboring rooms
    public RoomLayout topNeighbor;
    public RoomLayout bottomNeighbor;
    public RoomLayout leftNeighbor;
    public RoomLayout rightNeighbor;

    public List<Transform> EnemySpawnPosition; // List of spawn points for enemies
    public List<Transform> ObjectiveSpawnPosition; // List of spawn points for objectives

    public void GenerateWalls()
    {
        if (middle == null)
        {
            Debug.LogError("Middle Transform is not assigned!");
            return;
        }

        if (wallTilemap == null || wallTile == null || minimapTilemap == null || minimapWallTile == null)
        {
            Debug.LogError("Tilemaps or Tiles are not assigned!");
            return;
        }

        // Calculate the bounds of the room based on the middle position
        Vector3Int middleCell = wallTilemap.WorldToCell(middle.position);

        // Randomly decide which sides will have walls (80% chance to spawn a wall)
        bool hasTopWall = Random.value > 0.2f;
        bool hasBottomWall = Random.value > 0.2f;
        bool hasLeftWall = Random.value > 0.2f;
        bool hasRightWall = Random.value > 0.2f;

        // Ensure at least one side has a wall
        if (!hasTopWall && !hasBottomWall && !hasLeftWall && !hasRightWall)
        {
            hasTopWall = true; // Default to having a top wall if no walls are selected
        }

        // Generate top and bottom walls
        for (int x = -roomWidth / 2; x <= roomWidth / 2; x++)
        {
            // Top wall
            if (hasTopWall)
            {
                Vector3Int position = middleCell + new Vector3Int(x, roomHeight / 2, 0);
                wallTilemap.SetTile(position, wallTile);
                minimapTilemap.SetTile(position, minimapWallTile); // Copy to minimap
            }

            // Bottom wall
            if (hasBottomWall)
            {
                Vector3Int position = middleCell + new Vector3Int(x, -roomHeight / 2, 0);
                wallTilemap.SetTile(position, wallTile);
                minimapTilemap.SetTile(position, minimapWallTile); // Copy to minimap
            }
        }

        // Generate left and right walls
        for (int y = -roomHeight / 2; y <= roomHeight / 2; y++)
        {
            // Left wall
            if (hasLeftWall)
            {
                Vector3Int position = middleCell + new Vector3Int(-roomWidth / 2, y, 0);
                wallTilemap.SetTile(position, wallTile);
                minimapTilemap.SetTile(position, minimapWallTile); // Copy to minimap
            }

            // Right wall
            if (hasRightWall)
            {
                Vector3Int position = middleCell + new Vector3Int(roomWidth / 2, y, 0);
                wallTilemap.SetTile(position, wallTile);
                minimapTilemap.SetTile(position, minimapWallTile); // Copy to minimap
            }
        }
    }
}