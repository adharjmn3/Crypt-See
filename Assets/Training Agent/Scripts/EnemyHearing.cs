using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHearing : MonoBehaviour
{
    [SerializeField] private AudioSource playerAudioSource;
    [SerializeField] private float hearingRadius = 8f;

    public bool CanHearPlayer(Vector3 agentPosition, Vector3 playerPosition)
    {
        if (playerAudioSource == null)
            return false;

        float distance = Vector3.Distance(agentPosition, playerPosition);

        // Jika player terdengar dalam radius dan volume lebih besar dari threshold
        return playerAudioSource.isPlaying && distance <= hearingRadius && playerAudioSource.volume > 0.01f;
    }
}
