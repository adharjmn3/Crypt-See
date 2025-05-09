using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public GameObject roomPrefab; // Assign a RoomLayout prefab in the Unity Inspector
    public int roomSize = 20; // Size of each room (20x20 tiles)
    public int gridSize = 3; // 3x3 grid

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

                // Instantiate the RoomLayout prefab
                GameObject room = Instantiate(roomPrefab, roomPosition, Quaternion.identity, transform);

                // Call the GenerateWalls method in RoomLayout
                RoomLayout roomLayout = room.GetComponent<RoomLayout>();
                if (roomLayout != null)
                {
                    roomLayout.GenerateWalls();
                }
            }
        }
    }
}
