using System.Collections;
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
    public int cornerMarkerOffset = 2; // Offset from corners for placing corner markers

    public EnemyManager enemyManager; // Reference to the EnemyManager
    private List<Transform> enemySpawnPoints = new List<Transform>(); // Collect all enemy spawn points
    private List<Transform> objectiveSpawnPoints = new List<Transform>(); // Collect all objective spawn points
    
    [SerializeField] 
    private NavMeshBaker navMeshBaker; // Reference to the NavMeshBaker

    // Store references to the corner markers
    public GameObject topLeftCorner;
    public GameObject topRightCorner;
    public GameObject bottomLeftCorner;
    public GameObject bottomRightCorner;

    public List<Transform> GetObjectiveSpawnPoints()
    {
        return objectiveSpawnPoints;
    }

    void Start()
    {
        GenerateLevel();
        BakeNavMesh();
        TransferSpawnPointsToManagers();
        PlaceCornerMarkers();
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
                    roomLayout.ApplyRandomRotation();
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

    // Add this method to place corner markers on valid floor tiles
    void PlaceCornerMarkers()
    {
        // Calculate the total size of the grid including spacing
        int totalWidth = gridSize * (roomSize + roomSpacing);
        int totalHeight = gridSize * (roomSize + roomSpacing);

        // Calculate boundary positions
        int boundaryLeft = -outerBoundarySpacing;
        int boundaryRight = totalWidth + outerBoundarySpacing;
        int boundaryBottom = -outerBoundarySpacing;
        int boundaryTop = totalHeight + outerBoundarySpacing;

        // Create parent object to keep hierarchy clean
        GameObject cornersParent = new GameObject("Corner Markers");
        cornersParent.transform.SetParent(transform);

        // Find valid floor tile positions for the corners
        // We'll use cornerMarkerOffset to place them inside the level, away from walls
        
        // Top Left Corner - inside the level
        Vector3Int topLeftPos = new Vector3Int(
            boundaryLeft + cornerMarkerOffset, 
            boundaryTop - cornerMarkerOffset, 
            0
        );
        
        // Ensure we're placing on a floor tile - move inward if needed
        while (!IsValidFloorTile(topLeftPos) && topLeftPos.x < boundaryRight && topLeftPos.y > boundaryBottom)
        {
            topLeftPos.x += 1;
            topLeftPos.y -= 1;
        }
        
        // Top Right Corner - inside the level
        Vector3Int topRightPos = new Vector3Int(
            boundaryRight - cornerMarkerOffset,
            boundaryTop - cornerMarkerOffset,
            0
        );
        
        // Ensure we're placing on a floor tile - move inward if needed
        while (!IsValidFloorTile(topRightPos) && topRightPos.x > boundaryLeft && topRightPos.y > boundaryBottom)
        {
            topRightPos.x -= 1;
            topRightPos.y -= 1;
        }
        
        // Bottom Left Corner - inside the level
        Vector3Int bottomLeftPos = new Vector3Int(
            boundaryLeft + cornerMarkerOffset,
            boundaryBottom + cornerMarkerOffset,
            0
        );
        
        // Ensure we're placing on a floor tile - move inward if needed
        while (!IsValidFloorTile(bottomLeftPos) && bottomLeftPos.x < boundaryRight && bottomLeftPos.y < boundaryTop)
        {
            bottomLeftPos.x += 1;
            bottomLeftPos.y += 1;
        }
        
        // Bottom Right Corner - inside the level
        Vector3Int bottomRightPos = new Vector3Int(
            boundaryRight - cornerMarkerOffset,
            boundaryBottom + cornerMarkerOffset,
            0
        );
        
        // Ensure we're placing on a floor tile - move inward if needed
        while (!IsValidFloorTile(bottomRightPos) && bottomRightPos.x > boundaryLeft && bottomRightPos.y < boundaryTop)
        {
            bottomRightPos.x -= 1;
            bottomRightPos.y += 1;
        }
        
        // Create the corner markers at the valid positions
        topLeftCorner = CreateCornerMarker("Corner Top Left", 
            floorTilemap.GetCellCenterWorld(topLeftPos),
            cornersParent.transform);
        
        topRightCorner = CreateCornerMarker("Corner Top Right", 
            floorTilemap.GetCellCenterWorld(topRightPos),
            cornersParent.transform);
        
        bottomLeftCorner = CreateCornerMarker("Corner Bottom Left", 
            floorTilemap.GetCellCenterWorld(bottomLeftPos),
            cornersParent.transform);
        
        bottomRightCorner = CreateCornerMarker("Corner Bottom Right", 
            floorTilemap.GetCellCenterWorld(bottomRightPos),
            cornersParent.transform);

        Debug.Log("Corner markers placed at the four corners of the level on valid floor tiles.");
    }

    // Helper method to check if a position has a valid floor tile
    private bool IsValidFloorTile(Vector3Int position)
    {
        // Check if this position has a floor tile
        TileBase tile = floorTilemap.GetTile(position);
        
        // Also check that there's no wall tile at this position
        TileBase wallAtPosition = wallTilemap.GetTile(position);
        
        return tile == floorTile && wallAtPosition == null;
    }

    // Helper method to create a corner marker GameObject
    private GameObject CreateCornerMarker(string name, Vector3 position, Transform parent)
    {
        GameObject marker = new GameObject(name);
        marker.transform.position = position;
        marker.transform.SetParent(parent);
        
        // Add a tag for easier finding by other systems
        marker.tag = "SpawnPoint";
        
        // Add a visual indicator for debugging if needed
        if (Application.isEditor) 
        {
            // Add a sprite renderer for visibility in the editor
            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange semi-transparent
            renderer.sprite = Resources.FindObjectsOfTypeAll<Sprite>().Length > 0 ? 
                Resources.FindObjectsOfTypeAll<Sprite>()[0] : null;
            
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.sortingOrder = 100; // Make sure it renders on top
            
            // Scale down the sprite
            marker.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }
        
        Debug.Log($"Created corner marker '{name}' at position {position}");
        return marker;
    }

    // Add this method to bake the NavMesh after level generation
    void BakeNavMesh()
    {
        // Find the NavMeshBaker if not assigned in the inspector
        if (navMeshBaker == null)
        {
            navMeshBaker = FindObjectOfType<NavMeshBaker>();
        }
        
        if (navMeshBaker != null)
        {
            Debug.Log("Baking NavMesh after level generation...");
            // Use async version to avoid freezing the game
            navMeshBaker.BakeNavMeshAsync();
        }
        else
        {
            Debug.LogWarning("NavMeshBaker not found! Cannot bake NavMesh.");
        }
    }
    
    // Public getter methods for corner markers
    public Transform GetTopLeftCorner() => topLeftCorner?.transform;
    public Transform GetTopRightCorner() => topRightCorner?.transform;
    public Transform GetBottomLeftCorner() => bottomLeftCorner?.transform;
    public Transform GetBottomRightCorner() => bottomRightCorner?.transform;
    
    // Get all corners as a list
    public List<Transform> GetAllCorners()
    {
        List<Transform> corners = new List<Transform>();
        if (topLeftCorner) corners.Add(topLeftCorner.transform);
        if (topRightCorner) corners.Add(topRightCorner.transform);
        if (bottomLeftCorner) corners.Add(bottomLeftCorner.transform);
        if (bottomRightCorner) corners.Add(bottomRightCorner.transform);
        return corners;
    }
}