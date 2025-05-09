using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomLayout : MonoBehaviour
{
    public Transform middle; // Reference to the "Middle" GameObject in the room layout
    public Tilemap wallTilemap; // Tilemap for the walls
    public TileBase wallTile; // Tile to use for the walls
    public TileBase emptyTile; // Tile to represent the door (or leave empty)
    public int roomWidth = 20; // Width of the room in tiles
    public int roomHeight = 20; // Height of the room in tiles
    public int doorSize = 3; // Size of the door in tiles

    public void GenerateWalls()
    {
        if (middle == null)
        {
            Debug.LogError("Middle Transform is not assigned!");
            return;
        }

        if (wallTilemap == null || wallTile == null)
        {
            Debug.LogError("Wall Tilemap or Wall Tile is not assigned!");
            return;
        }

        // Calculate the bounds of the room based on the middle position
        Vector3Int middleCell = wallTilemap.WorldToCell(middle.position);

        // Randomly determine door positions for each wall
        HashSet<int> topDoorPositions = GenerateDoorPositions(roomWidth);
        HashSet<int> bottomDoorPositions = GenerateDoorPositions(roomWidth);
        HashSet<int> leftDoorPositions = GenerateDoorPositions(roomHeight);
        HashSet<int> rightDoorPositions = GenerateDoorPositions(roomHeight);

        // Generate top and bottom walls
        for (int x = -roomWidth / 2; x <= roomWidth / 2; x++)
        {
            // Top wall
            if (IsWithinDoorRange(x + roomWidth / 2, topDoorPositions))
            {
                wallTilemap.SetTile(middleCell + new Vector3Int(x, roomHeight / 2, 0), emptyTile);
            }
            else
            {
                wallTilemap.SetTile(middleCell + new Vector3Int(x, roomHeight / 2, 0), wallTile);
            }

            // Bottom wall
            if (IsWithinDoorRange(x + roomWidth / 2, bottomDoorPositions))
            {
                wallTilemap.SetTile(middleCell + new Vector3Int(x, -roomHeight / 2, 0), emptyTile);
            }
            else
            {
                wallTilemap.SetTile(middleCell + new Vector3Int(x, -roomHeight / 2, 0), wallTile);
            }
        }

        // Generate left and right walls
        for (int y = -roomHeight / 2; y <= roomHeight / 2; y++)
        {
            // Left wall
            if (IsWithinDoorRange(y + roomHeight / 2, leftDoorPositions))
            {
                wallTilemap.SetTile(middleCell + new Vector3Int(-roomWidth / 2, y, 0), emptyTile);
            }
            else
            {
                wallTilemap.SetTile(middleCell + new Vector3Int(-roomWidth / 2, y, 0), wallTile);
            }

            // Right wall
            if (IsWithinDoorRange(y + roomHeight / 2, rightDoorPositions))
            {
                wallTilemap.SetTile(middleCell + new Vector3Int(roomWidth / 2, y, 0), emptyTile);
            }
            else
            {
                wallTilemap.SetTile(middleCell + new Vector3Int(roomWidth / 2, y, 0), wallTile);
            }
        }
    }

    private HashSet<int> GenerateDoorPositions(int length)
    {
        HashSet<int> doorPositions = new HashSet<int>();
        int doorCount = Random.Range(1, 4); // Randomly decide the number of doors (1 to 3)

        while (doorPositions.Count < doorCount)
        {
            int doorPosition = Random.Range(0, length - doorSize);
            doorPositions.Add(doorPosition);
        }

        return doorPositions;
    }

    private bool IsWithinDoorRange(int position, HashSet<int> doorPositions)
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