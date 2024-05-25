using UnityEditor;
using UnityEngine;

public class CreatePrefab
{
    [MenuItem("Tools/FPC/Create FirstPersonController")]
    private static void CreateFirstPersonControllerPrefab()
    {
        // Prefab yolunu belirleyin (Assets klasörü altýnda bulunan prefab'ýn yolu)
        string prefabPath = "Assets/Prefabs/FirstPersonController.prefab";

        // Prefab'ý yükleyin
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab != null)
        {
            // Prefab'ý sahneye ekleyin
            PrefabUtility.InstantiatePrefab(prefab);
            Debug.Log("FirstPersonController prefab has been added to the scene.");
        }
        else
        {
            Debug.LogError("Failed to load the prefab. Please check the path. " + prefabPath);
        }
    }
}
