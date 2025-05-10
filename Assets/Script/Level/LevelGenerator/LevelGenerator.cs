using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelGenerator : MonoBehaviour
{
    public List<GameObject> roomPrefabs; // List of RoomLayout prefabs to choose from
    public Tilemap wallTilemap; // Tilemap for the walls
    public Tilemap minimapTilemap; // Tilemap for the minimap
    public TileBase wallTile; // Tile to use for the walls
    public TileBase floorTile; // Tile to use for the floor
    public TileBase minimapWallTile; // Tile to use for the minimap walls
    public int roomSize = 20; // Size of each room (20x20 tiles)
    public int gridSize = 3; // 3x3 grid
    public int roomSpacing = 1; // Space between rooms in tiles
    public int outerBoundarySpacing = 2; // Space between the outer boundary and the rooms

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

                // Randomly select a room prefab from the list
                GameObject selectedRoomPrefab = roomPrefabs[Random.Range(0, roomPrefabs.Count)];

                // Instantiate the selected RoomLayout prefab
                GameObject room = Instantiate(selectedRoomPrefab, roomPosition, Quaternion.identity, transform);

                // Apply random rotation or mirroring
                ApplyRandomModifiers(room);

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

    void ApplyRandomModifiers(GameObject room)
    {
        // Randomly rotate the room (0, 90, 180, or 270 degrees)
        int randomRotation = Random.Range(0, 4) * 90; // 0, 90, 180, or 270
        room.transform.Rotate(0, 0, randomRotation);

        // Randomly mirror the room layout (flip on X or Y axis)
        bool mirrorX = Random.value > 0.5f; // 50% chance to flip on X-axis
        bool mirrorY = Random.value > 0.5f; // 50% chance to flip on Y-axis
        Vector3 scale = room.transform.localScale;
        scale.x *= mirrorX ? -1 : 1; // Flip X-axis if mirrorX is true
        scale.y *= mirrorY ? -1 : 1; // Flip Y-axis if mirrorY is true
        room.transform.localScale = scale;
    }

    void GenerateGapWalls(int x, int y, Vector3 roomPosition)
    {
        // Removed vertical wall generation
        if (x < gridSize - 1) // If not the last column
        {
            // Code for vertical wall generation removed
        }

        // Removed horizontal wall generation
        if (y < gridSize - 1) // If not the last row
        {
            // Code for horizontal wall generation removed
        }

        // Removed corner wall generation
        if (x < gridSize - 1 && y < gridSize - 1) // If not the last column or row
        {
            // Code for corner wall generation removed
        }
    }

    void GenerateOuterBoundary()
    {
        // Set the order in layer of the floor to -50
        TilemapRenderer wallTilemapRenderer = wallTilemap.GetComponent<TilemapRenderer>();
        if (wallTilemapRenderer != null)
        {
            wallTilemapRenderer.sortingOrder = -50;
        }

        // Calculate the total size of the grid including spacing
        int totalWidth = gridSize * (roomSize + roomSpacing);
        int totalHeight = gridSize * (roomSize + roomSpacing);

        // Adjust the boundary size by adding the outerBoundarySpacing
        int boundaryLeft = -outerBoundarySpacing;
        int boundaryRight = totalWidth + outerBoundarySpacing;
        int boundaryBottom = -outerBoundarySpacing;
        int boundaryTop = totalHeight + outerBoundarySpacing;

        // Generate top and bottom boundary walls
        for (int x = boundaryLeft; x <= boundaryRight; x++)
        {
            // Top boundary
            wallTilemap.SetTile(new Vector3Int(x, boundaryTop, 0), wallTile);
            minimapTilemap.SetTile(new Vector3Int(x, boundaryTop, 0), minimapWallTile); // Copy to minimap

            // Bottom boundary
            wallTilemap.SetTile(new Vector3Int(x, boundaryBottom, 0), wallTile);
            minimapTilemap.SetTile(new Vector3Int(x, boundaryBottom, 0), minimapWallTile); // Copy to minimap
        }

        // Generate left and right boundary walls
        for (int y = boundaryBottom; y <= boundaryTop; y++)
        {
            // Left boundary
            wallTilemap.SetTile(new Vector3Int(boundaryLeft, y, 0), wallTile);
            minimapTilemap.SetTile(new Vector3Int(boundaryLeft, y, 0), minimapWallTile); // Copy to minimap

            // Right boundary
            wallTilemap.SetTile(new Vector3Int(boundaryRight, y, 0), wallTile);
            minimapTilemap.SetTile(new Vector3Int(boundaryRight, y, 0), minimapWallTile); // Copy to minimap
        }

        // Fill the inner area of the boundary with floor tiles
        for (int x = boundaryLeft + 1; x < boundaryRight; x++)
        {
            for (int y = boundaryBottom + 1; y < boundaryTop; y++)
            {
                wallTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
            }
        }
    }
}
