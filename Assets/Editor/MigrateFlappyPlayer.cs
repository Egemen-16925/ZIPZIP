using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class MigrateFlappyPlayer
{
    [MenuItem("Tools/Migrate 3D Avatars")]
    public static void Execute()
    {
        string sampleScenePath = "Assets/Scenes/SampleScene.unity";
        string flappyScenePath = "Assets/Scenes/Flappy Bird.unity";
        
        // 1. Open SampleScene to grab the configured objects
        var sampleScene = EditorSceneManager.OpenScene(sampleScenePath, OpenSceneMode.Single);
        string[] objectNames = new string[] { "Anka kusu", "Kelebek", "Ordek_Parcalanmis", "KediKutusu" };
        string[] prefabNames = new string[] { "AnkaKusu", "Kelebek", "Ordek", "KediKutusu" };
        List<GameObject> createdPrefabs = new List<GameObject>();

        if (!Directory.Exists("Assets/SplitMeshes"))
        {
            Directory.CreateDirectory("Assets/SplitMeshes");
        }

        for (int i = 0; i < objectNames.Length; i++)
        {
            GameObject objInScene = GameObject.Find(objectNames[i]);
            if (objInScene != null)
            {
                string prefabPath = $"Assets/SplitMeshes/{prefabNames[i]}_Migrated.prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(objInScene, prefabPath);
                createdPrefabs.Add(prefab);
                Debug.Log($"Created prefab from {objectNames[i]}");
            }
            else
            {
                Debug.LogError($"Could not find '{objectNames[i]}' in SampleScene!");
            }
        }

        if (createdPrefabs.Count == 0)
        {
            Debug.LogError("No objects found in SampleScene to migrate!");
            return;
        }

        // 2. Open Flappy Bird scene to insert them
        var flappyScene = EditorSceneManager.OpenScene(flappyScenePath, OpenSceneMode.Single);
        Legacy2D.Player player = Object.FindObjectOfType<Legacy2D.Player>();
        if (player == null)
        {
            Debug.LogError("Player component not found in Flappy Bird scene!");
            return;
        }

        Transform birdsParent = player.birds;
        if (birdsParent == null) birdsParent = player.transform;

        // Clear existing children (including the debug cube)
        for (int i = birdsParent.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(birdsParent.GetChild(i).gameObject);
        }

        player.Avatars = new List<GameObject>();

        // Add Directional Light just in case
        if (Object.FindObjectOfType<Light>() == null)
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        birdsParent.localScale = Vector3.one;

        // Instantiate the prefabs we just created
        for (int i = 0; i < createdPrefabs.Count; i++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(createdPrefabs[i], birdsParent);
            instance.name = prefabNames[i];
            
            // Konumu tam merkeze alıyoruz (Z=0). 
            // -5'te iken büyük modelin yarısı kameranın merceğine (Z=-10) girip kesiliyordu!
            instance.transform.localPosition = new Vector3(0, 0, 0f);
            
            // Kullanıcı ters açı demişti, (0, 90, 0) ile sağa doğru (Flappy Bird uçuş yönüne) bakmasını sağlıyoruz
            instance.transform.localRotation = Quaternion.Euler(0, 90, 0);

            // Scale'i 300 yapıyoruz (kullanıcının isteği)
            instance.transform.localScale = Vector3.one * 300f;

            // Fix Sorting Order
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                r.sortingOrder = 1000;
            }

            instance.SetActive(false);
            player.Avatars.Add(instance);
        }

        if (player.Avatars.Count > 0)
        {
            player.Avatars[0].SetActive(true);
        }

        EditorSceneManager.SaveScene(flappyScene);
        Debug.Log("Successfully migrated perfectly configured avatars from SampleScene to Flappy Bird scene!");
    }

    [MenuItem("Tools/Fix UI Menu")]
    public static void FixUI()
    {
        UnityEngine.EventSystems.EventSystem es = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (es != null)
        {
            if (es.GetComponent<UnityEngine.EventSystems.BaseInputModule>() == null)
            {
                es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("Added StandaloneInputModule to EventSystem.");
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }
        }
    }
}
