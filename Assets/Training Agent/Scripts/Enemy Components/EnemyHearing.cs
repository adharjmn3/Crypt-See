using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHearing : MonoBehaviour
{
    [SerializeField] private AudioSource playerAudioSource;
    [SerializeField] private float hearingRadius = 2f;

    void Awake()
    {
        if (playerAudioSource == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerAudioSource = player.GetComponent<AudioSource>();
            }
            Debug.Log($"Enemy hearing component get gameobject for audiosource from: {player}");
        }
    }

    public bool CanHearPlayer(Vector3 agentPosition, Vector3 playerPosition)
    {
        if (playerAudioSource == null)
            return false;

        float distance = Vector3.Distance(agentPosition, playerPosition);

        return playerAudioSource.isPlaying && distance <= hearingRadius && playerAudioSource.volume > 0.01f;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position;
        Gizmos.DrawWireSphere(origin, hearingRadius);
    }
}
