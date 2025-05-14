using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelGenerator : MonoBehaviour
{
    public List<GameObject> roomPrefabs; // List of RoomLayout prefabs to choose from
    public Tilemap wallTilemap; // Tilemap for the walls
    public Tilemap floorTilemap; // Tilemap for the floors
    public Tilemap minimapTilemap; // Tilemap for the minimap
    public TileBase wallTile; // Tile to use for the walls
    public TileBase floorTile; // Tile to use for the floor
    public TileBase minimapWallTile; // Tile to use for the minimap walls
    public int roomSize = 20; // Size of each room (20x20 tiles)
    public int gridSize = 3; // 3x3 grid
    public int roomSpacing = 1; // Space between rooms in tiles
    public int outerBoundarySpacing = 2; // Space between the outer boundary and the rooms

    public EnemyManager enemyManager; // Reference to the EnemyManager
    private List<Transform> enemySpawnPoints = new List<Transform>(); // Collect all enemy spawn points
    private List<Transform> objectiveSpawnPoints = new List<Transform>(); // Collect all objective spawn points

    public List<Transform> GetObjectiveSpawnPoints()
    {
        return objectiveSpawnPoints;
    }

    void Start()
    {
        GenerateLevel();
        TransferSpawnPointsToManagers();
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

                // Collect spawn points from the room
                RoomLayout roomLayout = room.GetComponent<RoomLayout>();
                if (roomLayout != null)
                {
                    enemySpawnPoints.AddRange(roomLayout.EnemySpawnPosition);
                    objectiveSpawnPoints.AddRange(roomLayout.ObjectiveSpawnPosition);
                    Debug.Log($"Room at {roomPosition} added {roomLayout.ObjectiveSpawnPosition.Count} objective spawn points.");
                    roomLayout.GenerateWalls();
                }
                else
                {
                    Debug.LogWarning($"Room at {roomPosition} does not have a RoomLayout component!");
                }
            }
        }

        Debug.Log($"Total objective spawn points collected: {objectiveSpawnPoints.Count}");

        // Generate the outer boundary around all rooms
        GenerateOuterBoundary();
    }

    void TransferSpawnPointsToManagers()
    {
        if (enemyManager != null)
        {
            enemyManager.InitializeSpawnPoints(enemySpawnPoints);
            Debug.Log($"Transferred {enemySpawnPoints.Count} enemy spawn points to EnemyManager.");
        }
        else
        {
            Debug.LogError("EnemyManager is not assigned in the LevelGenerator!");
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
            wallTilemap.SetTile(new Vector3Int(x, boundaryTop, 0), wallTile);
            minimapTilemap.SetTile(new Vector3Int(x, boundaryTop, 0), minimapWallTile);

            wallTilemap.SetTile(new Vector3Int(x, boundaryBottom, 0), wallTile);
            minimapTilemap.SetTile(new Vector3Int(x, boundaryBottom, 0), minimapWallTile);
        }

        // Generate left and right boundary walls
        for (int y = boundaryBottom; y <= boundaryTop; y++)
        {
            wallTilemap.SetTile(new Vector3Int(boundaryLeft, y, 0), wallTile);
            minimapTilemap.SetTile(new Vector3Int(boundaryLeft, y, 0), minimapWallTile);

            wallTilemap.SetTile(new Vector3Int(boundaryRight, y, 0), wallTile);
            minimapTilemap.SetTile(new Vector3Int(boundaryRight, y, 0), minimapWallTile);
        }

        // Fill the inner area of the boundary with floor tiles
        for (int x = boundaryLeft + 1; x < boundaryRight; x++)
        {
            for (int y = boundaryBottom + 1; y < boundaryTop; y++)
            {
                floorTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
            }
        }
    }
}