using UnityEditor;
using UnityEngine;

namespace Project.Editor
{
    public static class PlayerCarPrefabUtility
    {
        private const string BasePrefabPath = "Assets/_Project/Prefabs/PlayerCar.prefab";

        [MenuItem("Game/Player Car/Create Base PlayerCar Prefab")]
        public static void CreateBasePlayerCarPrefab()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogError("Select a car GameObject in the scene to create a PlayerCar prefab from.");
                return;
            }

            if (selected.GetComponent<CarControl>() == null)
            {
                Debug.LogError("Selected GameObject has no CarControl (Pack_Pickup). Add CarControl before creating the prefab.");
                return;
            }

            string path = BasePrefabPath;
            var prefab = PrefabUtility.SaveAsPrefabAsset(selected, path);
            if (prefab != null)
            {
                Debug.Log($"Base PlayerCar prefab created at {path}");
            }
        }

        [MenuItem("Game/Player Car/Create Variant From Base")]
        public static void CreateVariantFromBase()
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
            if (basePrefab == null)
            {
                Debug.LogError($"Base PlayerCar prefab not found at {BasePrefabPath}. Create it first.");
                return;
            }

            string directory = "Assets/_Project/Prefabs";
            string name = "PlayerCar_Variant";
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{name}.prefab");

            var instance = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
            if (instance == null)
            {
                Debug.LogError("Failed to instantiate base PlayerCar prefab.");
                return;
            }

            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var variant = PrefabUtility.SaveAsPrefabAsset(instance, uniquePath);
            Object.DestroyImmediate(instance);

            if (variant != null)
            {
                Debug.Log($"PlayerCar variant prefab created at {uniquePath}");
            }
        }
    }
}

