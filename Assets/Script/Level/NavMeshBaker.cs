using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using NavMeshPlus.Components;

public class NavMeshBaker : MonoBehaviour
{
    [SerializeField]
    private NavMeshSurface[] navMeshSurfaces;

    private void Awake()
    {
        // If no surfaces assigned in inspector, try to find them in the scene
        if (navMeshSurfaces == null || navMeshSurfaces.Length == 0)
        {
            navMeshSurfaces = FindObjectsOfType<NavMeshSurface>();
        }
    }

    // Call this method to bake the NavMesh
    public void BakeNavMesh()
    {
        Debug.Log("Starting NavMesh baking...");
        
        if (navMeshSurfaces == null || navMeshSurfaces.Length == 0)
        {
            Debug.LogWarning("No NavMeshSurfaces found to bake!");
            return;
        }

        foreach (NavMeshSurface surface in navMeshSurfaces)
        {
            if (surface != null)
            {
                surface.BuildNavMesh();
                Debug.Log($"Baked NavMesh on surface: {surface.name}");
            }
        }
        
        Debug.Log("NavMesh baking completed!");
    }

    // Async version of the bake method
    public void BakeNavMeshAsync()
    {
        Debug.Log("Starting NavMesh baking asynchronously...");
        
        if (navMeshSurfaces == null || navMeshSurfaces.Length == 0)
        {
            Debug.LogWarning("No NavMeshSurfaces found to bake!");
            return;
        }

        StartCoroutine(BakeNavMeshAsyncRoutine());
    }

    private IEnumerator BakeNavMeshAsyncRoutine()
    {
        foreach (NavMeshSurface surface in navMeshSurfaces)
        {
            if (surface != null)
            {
                AsyncOperation operation = surface.BuildNavMeshAsync();
                Debug.Log($"Building NavMesh on surface: {surface.name}");
                
                // Wait until the async operation is done
                yield return operation;
                
                Debug.Log($"Finished baking NavMesh on surface: {surface.name}");
            }
        }
        
        Debug.Log("Async NavMesh baking completed!");
    }

    // Example: You can call BakeNavMesh at runtime when needed
    public void BakeNavMeshDelayed(float delay)
    {
        StartCoroutine(BakeAfterDelay(delay));
    }

    private IEnumerator BakeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        BakeNavMesh();
    }
}
