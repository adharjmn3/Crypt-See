using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerTraining : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] Transform agentTransform;
    [SerializeField] Transform targetTransform;
    [SerializeField] Transform[] spawnPointsTransform;
    List<Transform> spawnPointsList;

    public void ResetStartPosition()
    {
        ResetSpawnPositionList();

        int index = Random.Range(0, spawnPointsList.Count);
        agentTransform.position = spawnPointsList[index].localPosition;
        spawnPointsList.RemoveAt(index);

        index = Random.Range(0, spawnPointsList.Count);
        targetTransform.position = spawnPointsList[index].localPosition;
        spawnPointsList.Clear();
    }

    private void ResetSpawnPositionList()
    {
        spawnPointsList = new List<Transform>(spawnPointsTransform);
    }
}
