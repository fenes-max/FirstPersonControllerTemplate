using UnityEditor;
using UnityEngine;

public class CreatePrefab
{
    [MenuItem("Tools/FPC/Create FirstPersonController")]
    private static void CreateFirstPersonControllerPrefab()
    {
        // Prefab yolunu belirleyin (Assets klas�r� alt�nda bulunan prefab'�n yolu)
        string prefabPath = "Assets/Prefabs/FirstPersonController.prefab";

        // Prefab'� y�kleyin
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab != null)
        {
            // Prefab'� sahneye ekleyin
            PrefabUtility.InstantiatePrefab(prefab);
            Debug.Log("FirstPersonController prefab has been added to the scene.");
        }
        else
        {
            Debug.LogError("Failed to load the prefab. Please check the path. " + prefabPath);
        }
    }
}
