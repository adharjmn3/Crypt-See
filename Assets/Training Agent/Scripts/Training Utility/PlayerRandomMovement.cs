using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlayerRandomMovement : MonoBehaviour
{
    public float radius = 5f; // Radius di sekitar karakter untuk gerakan acak
    public NavMeshAgent navMeshAgent; // Referensi ke NavMesh Agent
    private Vector3 _destination;

    public bool chaseAgent = false;

    public GameObject agentObj;

    void Start()
    {
        // Pastikan NavMesh Agent ada di GameObject ini
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            if (navMeshAgent == null)
            {
                Debug.LogError("NavMesh Agent tidak ditemukan. Harap pastikan sudah terpasang di GameObject ini.");
                enabled = false; // Nonaktifkan skrip jika tidak ada NavMesh Agent
                return;
            }
        }

        // Setel kecepatan agen agar pergerakan tidak terlalu cepat atau terlalu lambat
        // navMeshAgent.speed = 3.5f; // Sesuaikan sesuai kebutuhan
        // navMeshAgent.stoppingDistance = 0.5f; // Jarak henti yang wajar
        
        // Atur tujuan awal
        SetNewRandomDestination();
    }

    void Update()
    {
        if (chaseAgent)
        {
            navMeshAgent.SetDestination(agentObj.transform.position);
        }
        else
        {
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.1f)
            {
                // Hasilkan tujuan acak baru
                SetNewRandomDestination();
            }
        }
    }

    private void SetNewRandomDestination()
    {
        _destination = GetRandomNavMeshPosition(radius);
        // Setel tujuan baru
        navMeshAgent.SetDestination(_destination);
        // Debug.Log("Tujuan baru: " + _destination);
    }

    private Vector3 GetRandomNavMeshPosition(float radius)
    {
        // 1. Hasilkan titik acak di bidang horizontal (X, Z) dalam radius
        //    'insideUnitCircle' menghasilkan Vector2, jadi kita konversi ke Vector3 (X, 0, Z)
        Vector2 randomCirclePoint = Random.insideUnitCircle * radius;
        Vector3 randomDirection = transform.position + new Vector3(randomCirclePoint.x, 0f, randomCirclePoint.y);

        // 2. Cari titik terdekat di NavMesh dari 'randomDirection'
        NavMeshHit hit;
        // Parameter ketiga 'radius * 2' adalah jarak maksimum untuk mencari NavMesh
        // Parameter keempat 'NavMesh.AllAreas' memastikan kita mencari di semua area NavMesh yang di-bake
        if (NavMesh.SamplePosition(randomDirection, out hit, radius * 2, NavMesh.AllAreas))
        {
            return hit.position; // Mengembalikan posisi valid di NavMesh
        }
        else
        {
            // Fallback: Jika tidak ditemukan posisi NavMesh yang valid setelah beberapa percobaan,
            // kembalikan posisi saat ini atau coba lagi.
            // Mengembalikan posisi saat ini bisa menyebabkan agen diam jika sering gagal.
            // Untuk lebih robust, kita bisa coba beberapa kali atau kembali ke posisi saat ini.
            Debug.LogWarning("Tidak dapat menemukan posisi NavMesh yang valid di dekat " + randomDirection + ". Mencoba lagi...");
            return transform.position; // Kembali ke posisi saat ini sebagai fallback
        }
    }
}
