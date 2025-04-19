using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHearing : MonoBehaviour
{
    [SerializeField] private AudioSource playerAudioSource;
    [SerializeField] private float hearingRadius = 2f;

    private Vector3 agentPostionDebug;

    public bool CanHearPlayer(Vector3 agentPosition, Vector3 playerPosition)
    {
        if (playerAudioSource == null)
            return false;

        agentPostionDebug = agentPosition;

        float distance = Vector3.Distance(agentPosition, playerPosition);

        return playerAudioSource.isPlaying && distance <= hearingRadius && playerAudioSource.volume > 0.01f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(agentPostionDebug, hearingRadius);
    }
}
