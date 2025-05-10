using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecorGenerator : MonoBehaviour
{
    public List<GameObject> decorPrefabs; // List of decoration prefabs to choose from
    public int decorCount = 5; // Number of decorations to spawn

    void Start()
    {
        GenerateDecorations();
    }

    void GenerateDecorations()
    {
        if (decorPrefabs == null || decorPrefabs.Count == 0)
        {
            Debug.LogWarning("No decoration prefabs assigned!");
            return;
        }

        // Get the RoomLayout component to determine room size and middle position
        RoomLayout roomLayout = GetComponent<RoomLayout>();
        if (roomLayout == null)
        {
            Debug.LogError("RoomLayout component not found on this GameObject!");
            return;
        }

        // Ensure the middle transform is assigned
        if (roomLayout.middle == null)
        {
            Debug.LogError("Middle Transform is not assigned in RoomLayout!");
            return;
        }

        // Use roomWidth and roomHeight from RoomLayout
        float roomWidth = roomLayout.roomWidth;
        float roomHeight = roomLayout.roomHeight;

        // Get the middle position
        Vector3 middlePosition = roomLayout.middle.position;

        // Ensure decorations are placed only within the room bounds
        for (int i = 0; i < decorCount; i++)
        {
            // Randomly select a decoration prefab
            GameObject selectedDecorPrefab = decorPrefabs[Random.Range(0, decorPrefabs.Count)];

            // Randomly generate a position within the room bounds relative to the middle
            Vector3 randomPosition = middlePosition + new Vector3(
                Random.Range(-roomWidth / 2, roomWidth / 2),
                Random.Range(-roomHeight / 2, roomHeight / 2),
                0
            );

            // Calculate rotation based on position
            float rotationAngle = Mathf.Atan2(randomPosition.y - middlePosition.y, randomPosition.x - middlePosition.x) * Mathf.Rad2Deg;

            // Determine mirroring based on position
            bool mirrorX = randomPosition.x < middlePosition.x; // Mirror on X-axis if position is to the left of the middle
            bool mirrorY = randomPosition.y < middlePosition.y; // Mirror on Y-axis if position is below the middle

            // Instantiate the decoration prefab at the random position
            GameObject decor = Instantiate(selectedDecorPrefab, randomPosition, Quaternion.Euler(0, 0, rotationAngle), transform);

            // Apply mirroring by modifying the scale
            Vector3 scale = decor.transform.localScale;
            scale.x *= mirrorX ? -1 : 1; // Flip X-axis if mirrorX is true
            scale.y *= mirrorY ? -1 : 1; // Flip Y-axis if mirrorY is true
            decor.transform.localScale = scale;
        }
    }
}
