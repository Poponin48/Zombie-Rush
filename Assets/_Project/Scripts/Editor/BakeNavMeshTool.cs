using UnityEditor;
using UnityEngine;
using Unity.AI.Navigation;

public static class BakeNavMeshTool
{
    [MenuItem("Zombie Rush/Tools/Bake NavMesh (Active Scene)")]
    public static void BakeActiveSceneNavMesh()
    {
        NavMeshSurface[] surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        if (surfaces == null || surfaces.Length == 0)
        {
            Debug.LogWarning("No NavMeshSurface found in active scene.");
            return;
        }

        foreach (var surface in surfaces)
        {
            if (surface == null) continue;
            surface.BuildNavMesh();
            Debug.Log($"NavMesh baked for surface: {surface.gameObject.name}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"NavMesh bake completed. Surfaces: {surfaces.Length}");
    }
}
