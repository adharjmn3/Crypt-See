using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public GameObject roomPrefab; // Assign a basic room layout prefab in the Unity Inspector
    public GameObject wallPrefab; // Assign a wall prefab in the Unity Inspector
    public int roomSize = 20; // Size of each room (20x20 tiles)
    public int gridSize = 3; // 3x3 grid
    public int doorSize = 3; // Size of the door gap in tiles

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
                // Calculate the position of the room
                Vector3 roomPosition = new Vector3(x * roomSize, y * roomSize, 0);

                // Instantiate the room layout prefab
                Instantiate(roomPrefab, roomPosition, Quaternion.identity, transform);

                // Generate walls and doors for the room
                GenerateWallsAndDoors(x, y, roomPosition);
            }
        }
    }

    void GenerateWallsAndDoors(int x, int y, Vector3 roomPosition)
    {
        // Generate walls for the current room
        // Top wall
        if (y < gridSize - 1) // If not the last row
        {
            GenerateWallWithDoor(roomPosition + new Vector3(0, roomSize, 0), Vector3.right, roomSize, true);
        }
        else
        {
            GenerateWallWithDoor(roomPosition + new Vector3(0, roomSize, 0), Vector3.right, roomSize, false);
        }

        // Bottom wall
        if (y > 0) // If not the first row
        {
            GenerateWallWithDoor(roomPosition, Vector3.right, roomSize, true);
        }
        else
        {
            GenerateWallWithDoor(roomPosition, Vector3.right, roomSize, false);
        }

        // Left wall
        if (x > 0) // If not the first column
        {
            GenerateWallWithDoor(roomPosition, Vector3.up, roomSize, true);
        }
        else
        {
            GenerateWallWithDoor(roomPosition, Vector3.up, roomSize, false);
        }

        // Right wall
        if (x < gridSize - 1) // If not the last column
        {
            GenerateWallWithDoor(roomPosition + new Vector3(roomSize, 0, 0), Vector3.up, roomSize, true);
        }
        else
        {
            GenerateWallWithDoor(roomPosition + new Vector3(roomSize, 0, 0), Vector3.up, roomSize, false);
        }
    }

    void GenerateWallWithDoor(Vector3 startPosition, Vector3 direction, int length, bool allowDoor)
    {
        int doorCount = allowDoor ? Random.Range(1, 4) : 0; // Randomly decide the number of doors (1 to 3)
        HashSet<int> doorPositions = new HashSet<int>();

        // Randomly select door positions
        while (doorPositions.Count < doorCount)
        {
            int doorPosition = Random.Range(0, length - doorSize);
            doorPositions.Add(doorPosition);
        }

        // Generate the wall
        for (int i = 0; i < length; i++)
        {
            if (allowDoor && IsWithinDoorRange(i, doorPositions))
            {
                // Skip tiles for the door
                continue;
            }

            // Instantiate a wall tile
            Vector3 wallPosition = startPosition + direction * i;
            Instantiate(wallPrefab, wallPosition, Quaternion.identity, transform);
        }
    }

    bool IsWithinDoorRange(int position, HashSet<int> doorPositions)
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
}
