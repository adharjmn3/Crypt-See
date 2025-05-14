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

    public List<Transform> EnemySpawnPosition = new List<Transform>(); // List of spawn points for enemies
    public List<Transform> ObjectiveSpawnPosition = new List<Transform>(); // List of spawn points for objectives

    public Transform objectiveSpawnArea; // Parent object containing objective spawn points

    private void Awake()
    {
        // Populate ObjectiveSpawnPosition with child transforms of objectiveSpawnArea
        if (objectiveSpawnArea != null)
        {
            foreach (Transform child in objectiveSpawnArea)
            {
                ObjectiveSpawnPosition.Add(child);
            }
        }
        else
        {
            Debug.LogWarning("ObjectiveSpawnArea is not assigned in RoomLayout!");
        }
    }

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

        // Randomly decide if each wall will be generated
        bool hasTopWall = Random.value > 0.1f; // 90% chance to generate the top wall
        bool hasBottomWall = Random.value > 0.1f; // 90% chance to generate the bottom wall
        bool hasLeftWall = Random.value > 0.1f; // 90% chance to generate the left wall
        bool hasRightWall = Random.value > 0.1f; // 90% chance to generate the right wall

        // Randomly decide if each wall will have a door
        bool hasTopDoor = Random.value > 0.3f;
        bool hasBottomDoor = Random.value > 0.3f;
        bool hasLeftDoor = Random.value > 0.3f;
        bool hasRightDoor = Random.value > 0.3f;

        // Ensure at least one wall has a door
        if (!hasTopDoor && !hasBottomDoor && !hasLeftDoor && !hasRightDoor)
        {
            hasTopDoor = true; // Default to having a top door
        }

        // Generate top and bottom walls
        for (int x = -roomWidth / 2; x <= roomWidth / 2; x++)
        {
            // Top wall
            if (hasTopWall)
            {
                Vector3Int position = middleCell + new Vector3Int(x, roomHeight / 2, 0);
                if (hasTopDoor && IsWithinDoorRange(x + roomWidth / 2, roomWidth))
                {
                    wallTilemap.SetTile(position, emptyTile); // Leave space for the door
                }
                else
                {
                    wallTilemap.SetTile(position, wallTile);
                    minimapTilemap.SetTile(position, minimapWallTile); // Copy to minimap
                }
            }

            // Bottom wall
            if (hasBottomWall)
            {
                Vector3Int position = middleCell + new Vector3Int(x, -roomHeight / 2, 0);
                if (hasBottomDoor && IsWithinDoorRange(x + roomWidth / 2, roomWidth))
                {
                    wallTilemap.SetTile(position, emptyTile); // Leave space for the door
                }
                else
                {
                    wallTilemap.SetTile(position, wallTile);
                    minimapTilemap.SetTile(position, minimapWallTile); // Copy to minimap
                }
            }
        }

        // Generate left and right walls
        for (int y = -roomHeight / 2; y <= roomHeight / 2; y++)
        {
            // Left wall
            if (hasLeftWall)
            {
                Vector3Int position = middleCell + new Vector3Int(-roomWidth / 2, y, 0);
                if (hasLeftDoor && IsWithinDoorRange(y + roomHeight / 2, roomHeight))
                {
                    wallTilemap.SetTile(position, emptyTile); // Leave space for the door
                }
                else
                {
                    wallTilemap.SetTile(position, wallTile);
                    minimapTilemap.SetTile(position, minimapWallTile); // Copy to minimap
                }
            }

            // Right wall
            if (hasRightWall)
            {
                Vector3Int position = middleCell + new Vector3Int(roomWidth / 2, y, 0);
                if (hasRightDoor && IsWithinDoorRange(y + roomHeight / 2, roomHeight))
                {
                    wallTilemap.SetTile(position, emptyTile); // Leave space for the door
                }
                else
                {
                    wallTilemap.SetTile(position, wallTile);
                    minimapTilemap.SetTile(position, minimapWallTile); // Copy to minimap
                }
            }
        }
    }

    private bool IsWithinDoorRange(int position, int length)
    {
        // Ensure doors are within bounds and do not overlap
        int doorStart = length / 2 - doorSize / 2;
        return position >= doorStart && position < doorStart + doorSize;
    }
}