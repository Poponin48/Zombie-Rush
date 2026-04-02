using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Editor
{
    /// <summary>
    /// One-click scene population (same outcome as driving the editor via MCP tools).
    /// Run: Zombie Rush → Demo: Add Building + 10 Zombies (CAR_TEST)
    /// </summary>
    public static class DemoScenePopulateTool
    {
        private const string ScenePath = "Assets/Scenes/CAR_TEST.unity";
        private const string BuildingPrefabPath =
            "Assets/Models/SimplePoly City - Low Poly Assets/Prefab/Buildings/Building Sky_big_color01.prefab";
        private const string BuildingMaterialPath =
            "Assets/Models/SimplePoly City - Low Poly Assets/Materials/Building Sky_big_color03.mat";
        private const string ZombiePrefabPath = "Assets/_Project/Prefabs/Zombie/Zombie_01 1.prefab";

        [MenuItem("Zombie Rush/Demo: Add Building + 10 Zombies (CAR_TEST)")]
        public static void AddBuildingAndZombies()
        {
            var buildingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BuildingPrefabPath);
            var zombiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(BuildingMaterialPath);

            if (buildingPrefab == null)
            {
                Debug.LogError($"[DemoScenePopulate] Missing prefab: {BuildingPrefabPath}");
                return;
            }

            if (zombiePrefab == null)
            {
                Debug.LogError($"[DemoScenePopulate] Missing prefab: {ZombiePrefabPath}");
                return;
            }

            if (mat == null)
            {
                Debug.LogError($"[DemoScenePopulate] Missing material: {BuildingMaterialPath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var root = new GameObject("Demo_Building_And_Zombies");
            SceneManager.MoveGameObjectToScene(root, scene);

            var building = PrefabUtility.InstantiatePrefab(buildingPrefab) as GameObject;
            if (building == null)
            {
                Object.DestroyImmediate(root);
                Debug.LogError("[DemoScenePopulate] Failed to instantiate building.");
                return;
            }

            SceneManager.MoveGameObjectToScene(building, scene);
            building.transform.SetParent(root.transform, false);
            building.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            building.transform.SetPositionAndRotation(new Vector3(22f, 0f, 48f), Quaternion.Euler(0f, 25f, 0f));

            foreach (var r in building.GetComponentsInChildren<MeshRenderer>(true))
            {
                Undo.RecordObject(r, "Apply demo building material");
                r.sharedMaterial = mat;
            }

            for (var i = 0; i < 10; i++)
            {
                var z = PrefabUtility.InstantiatePrefab(zombiePrefab) as GameObject;
                if (z == null)
                    continue;
                SceneManager.MoveGameObjectToScene(z, scene);
                z.transform.SetParent(root.transform, false);
                z.transform.position = new Vector3(8f + i * 3.5f, 0.11f, 42f);
                z.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
            Debug.Log(
                "[DemoScenePopulate] Added Building Sky_big (material → Building Sky_big_color03) + 10 zombies under " +
                root.name + ". Re-bake NavMesh if agents behave oddly.");
        }
    }
}
