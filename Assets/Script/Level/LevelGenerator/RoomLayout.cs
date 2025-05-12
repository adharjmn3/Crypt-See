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

    public List<Transform> EnemySpawnPosition; 

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

        // Randomly decide if each side will have a door (70% chance to generate a door)
        bool hasTopDoor = Random.value > 0.3f;
        bool hasBottomDoor = Random.value > 0.3f;
        bool hasLeftDoor = Random.value > 0.3f;
        bool hasRightDoor = Random.value > 0.3f;

        // Ensure at least one side has a wall
        if (!hasTopWall && !hasBottomWall && !hasLeftWall && !hasRightWall)
        {
            hasTopWall = true; // Default to having a top wall if no walls are selected
        }

        // Fetch or generate door positions for each wall
        List<int> topDoorPositions = hasTopWall && hasTopDoor && topNeighbor != null ? topNeighbor.GetDoorPositionsForNeighbor("bottom") : GenerateDoorPositions(roomWidth);
        List<int> bottomDoorPositions = hasBottomWall && hasBottomDoor && bottomNeighbor != null ? bottomNeighbor.GetDoorPositionsForNeighbor("top") : GenerateDoorPositions(roomWidth);
        List<int> leftDoorPositions = hasLeftWall && hasLeftDoor && leftNeighbor != null ? leftNeighbor.GetDoorPositionsForNeighbor("right") : GenerateDoorPositions(roomHeight);
        List<int> rightDoorPositions = hasRightWall && hasRightDoor && rightNeighbor != null ? rightNeighbor.GetDoorPositionsForNeighbor("left") : GenerateDoorPositions(roomHeight);

        // Generate top and bottom walls
        for (int x = -roomWidth / 2; x <= roomWidth / 2; x++)
        {
            // Top wall
            if (hasTopWall)
            {
                Vector3Int position = middleCell + new Vector3Int(x, roomHeight / 2, 0);
                if (hasTopDoor && IsWithinDoorRange(x + roomWidth / 2, topDoorPositions))
                {
                    wallTilemap.SetTile(position, emptyTile);
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
                if (hasBottomDoor && IsWithinDoorRange(x + roomWidth / 2, bottomDoorPositions))
                {
                    wallTilemap.SetTile(position, emptyTile);
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
                if (hasLeftDoor && IsWithinDoorRange(y + roomHeight / 2, leftDoorPositions))
                {
                    wallTilemap.SetTile(position, emptyTile);
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
                if (hasRightDoor && IsWithinDoorRange(y + roomHeight / 2, rightDoorPositions))
                {
                    wallTilemap.SetTile(position, emptyTile);
                }
                else
                {
                    wallTilemap.SetTile(position, wallTile);
                    minimapTilemap.SetTile(position, minimapWallTile); // Copy to minimap
                }
            }
        }
    }

    private List<int> GenerateDoorPositions(int length)
    {
        List<int> doorPositions = new List<int>();
        int doorCount = Random.Range(1, 3); // Randomly decide the number of doors (1 or 2)

        while (doorPositions.Count < doorCount)
        {
            int doorPosition = Random.Range(1, length - doorSize - 1); // Ensure doors are within bounds
            bool overlaps = false;

            // Check if the new door overlaps with existing doors
            foreach (int existingDoor in doorPositions)
            {
                if (Mathf.Abs(existingDoor - doorPosition) < doorSize)
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                doorPositions.Add(doorPosition);
            }
        }

        return doorPositions;
    }

    private bool IsWithinDoorRange(int position, List<int> doorPositions)
    {
        foreach (int doorStart in doorPositions)
        {
            if (position >= doorStart && position < doorStart + doorSize)
            {
                return true;
            }
        }
        return false;
    }

    public List<int> GetDoorPositionsForNeighbor(string side)
    {
        // Return door positions for the specified side
        switch (side)
        {
            case "top":
                return GenerateDoorPositions(roomWidth);
            case "bottom":
                return GenerateDoorPositions(roomWidth);
            case "left":
                return GenerateDoorPositions(roomHeight);
            case "right":
                return GenerateDoorPositions(roomHeight);
            default:
                return new List<int>();
        }
    }
}